using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using RESTar.ContentTypeProviders;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using RESTar.WebSockets;
using static RESTar.Method;

#pragma warning disable 1591
// ReSharper disable All

namespace RESTar.Palindrom
{
    public enum SessionStatus
    {
        Waiting,
        Open,
        Closed
    }

    [RESTar]
    public class SessionReconnect : ISelector<SessionReconnect>, IInserter<SessionReconnect>, IUpdater<SessionReconnect>, IDeleter<SessionReconnect>
    {
        public string ID { get; }

        public IEnumerable<SessionReconnect> Select(IRequest<SessionReconnect> request)
        {
            return null;
        }

        public int Insert(IRequest<SessionReconnect> request)
        {
            return 0;
        }

        public int Update(IRequest<SessionReconnect> request)
        {
            return 0;
        }

        public int Delete(IRequest<SessionReconnect> request)
        {
            return 0;
        }
    }

    [RESTar(GET)]
    public class SessionRoot : IBinary<SessionRoot>
    {
        public string ID { get; }

        public (Stream stream, ContentType contentType) Select(IRequest<SessionRoot> request)
        {
            if (!(request.Conditions.Get(nameof(ID), Operators.EQUALS).Value is string id))
                throw new UnknownResource("No session ID!");
            if (!Session.ActiveSessions.TryGetValue(id, out var session))
                throw new UnknownResource(id);
            return (
                stream: Providers.Json.SerializeStream(session.Root.State),
                contentType: ContentType.JSON
            );
        }
    }

    public interface ISessionRoot
    {
        object State { get; }
        IResult ApplyPatch(string patchInput);
    }

    internal class SessionRootReference<T> : ISessionRoot where T : class
    {
        [RESTarMember(hide: true)] object ISessionRoot.State => State;
        public T State { get; }
        [RESTarMember(hide: true)] public IRequest<T> PatchRequest { get; }
        [RESTarMember(hide: true)] public IRequest<T> GetRequest { get; }
        [RESTarMember(hide: true)] public Session Session { get; }

        public IResult ApplyPatch(string patchInput)
        {
            PatchRequest.SetBody(patchInput);
            using (var request = PatchRequest)
                return request.Evaluate();
        }

        public SessionRootReference(Session session, T state, Context context)
        {
            State = state;
            PatchRequest = context.CreateRequest<T>(PATCH, protocolId: "palindrom");
            PatchRequest.Selector = () => new[] {State};
            PatchRequest.Headers.ContentType = JsonPatchProvider.JsonPatchMimeType;
            Session = session;
        }
    }

    [RESTar]
    public class Session : ITerminal, ITerminalInstanceResolver<Session>
    {
        public Session GetInstance(IDictionary<string, object> assignments, Headers headers, ReadonlyCookies cookies)
        {
            if (!(assignments.TryGetValue(nameof(ID), out var value) && value is string sessionId))
            {
                if (cookies.TryGetValue(SessionCookieName, out var sessionCookie))
                    sessionId = sessionCookie.Value;
                else throw new Exception("Found no session ID as parameter or in Session cookie.");
            }
            if (!ActiveSessions.TryGetValue(sessionId, out var session))
                throw new Exception($"The session ID '{sessionId}' is no longer active");
            return session;
        }

        internal static IDictionary<string, Session> ActiveSessions { get; }
        static Session() => ActiveSessions = new ConcurrentDictionary<string, Session>();

        internal const string SessionCookieName = "PalindromSession";
        internal const int TimeoutMilliseconds = 120 * 1000;

        internal static Session Create<T>(IEnumerable<T> entities, Context context) where T : class
        {
            var newSessionId = Guid.NewGuid().ToString("N");
            var session = new Session(newSessionId);
            session.Root = session.CreateSessionRoot(entities, context);
            return ActiveSessions[newSessionId] = session;
        }

        private Session(string id)
        {
            ID = id;
            Timer = new Timer
            (
                callback: args => Dispose(),
                state: this,
                dueTime: TimeoutMilliseconds,
                period: -1
            );
            Status = SessionStatus.Waiting;
        }

        public Session() { }

        // Instance:

        private Timer Timer { get; set; }

        public IWebSocket WebSocket { private get; set; }

        public string ID { get; private set; }
        public ISessionRoot Root { get; private set; }

        public DateTime LastUsed { get; private set; }
        public SessionStatus Status { get; private set; }

        public TimeSpan TimeUntilTimeout
        {
            get
            {
                var value = TimeSpan
                    .FromMilliseconds(TimeoutMilliseconds)
                    .Subtract(DateTime.UtcNow - LastUsed);
                return value > TimeSpan.Zero ? value : TimeSpan.Zero;
            }
        }

        private void ResetTimer()
        {
            Timer.Change(TimeoutMilliseconds, -1);
            LastUsed = DateTime.UtcNow;
        }

        public void Open()
        {
            Status = SessionStatus.Open;
            ResetTimer();
        }

        public void HandleTextInput(string input)
        {
            if (Status == SessionStatus.Waiting) return;
            if (Status == SessionStatus.Closed)
                throw new Exception("This session is now closed!");
            ResetTimer();
            var result = Root.ApplyPatch(input);
            if (result is Error error)
                throw error;
        }

        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        public ISessionRoot CreateSessionRootDynamic(IEntities entities, Context context)
        {
            return CreateSessionRoot((dynamic) entities, context);
        }

        public ISessionRoot CreateSessionRoot<T>(IEnumerable<T> entities, Context context) where T : class
        {
            var results = entities.ToList();
            switch (results.Count)
            {
                case 0:
                    throw new Exception("Found no objects to assign to root. Aborting");
                case 1 when results.First() is T result:
                    return new SessionRootReference<T>(this, result, context);
                default: throw new Exception("Found more than one object to assign to root. Aborting");
            }
        }


        public void Dispose()
        {
            ActiveSessions.Remove(ID);
            Status = SessionStatus.Closed;
        }
    }
}