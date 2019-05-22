using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;
using RESTar.WebSockets;

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

        private static IDictionary<string, Session> ActiveSessions { get; }
        static Session() => ActiveSessions = new ConcurrentDictionary<string, Session>();

        internal const string SessionCookieName = "PalindromSession";
        internal const int TimeoutMilliseconds = 60 * 1000;

        internal static Session Create<T>(IEntities<T> result) where T : class
        {
            var newSessionId = Guid.NewGuid().ToString("N");
            var session = new Session(newSessionId);
            session.BaseOnto(result);
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
        private IRequest PatchRequest { get; set; }

        public IWebSocket WebSocket { private get; set; }

        public string ID { get; private set; }
        public object Root { get; private set; }
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
            WebSocket.SendText($"Now open! ID: {ID}, Root:");
            WebSocket.SendJson(Root, prettyPrint: true);
            ResetTimer();
        }

        public void HandleTextInput(string input)
        {
            if (Status == SessionStatus.Waiting) return;
            if (Status == SessionStatus.Closed)
                throw new Exception("This session is now closed!");

            switch (input)
            {
                case "GET":
                    WebSocket.SendJson(Root);
                    break;
                case "PING":
                    ResetTimer();
                    WebSocket.SendText("ECHO");
                    break;
                case var go when go.StartsWith("GO "):
                    var (_, uri) = go.TSplit(" ", trim: true);
                    using (var request = WebSocket.Context.CreateRequest(uri))
                    {
                        switch (request.Evaluate())
                        {
                            case IEntities entities:
                                try
                                {
                                    BaseOnto((dynamic) entities);
                                    WebSocket.SendText("Navigation successful. Current root:");
                                    WebSocket.SendJson(Root);
                                }
                                catch (Exception e)
                                {
                                    WebSocket.SendException(e);
                                }
                                break;
                            case Error error:
                                WebSocket.SendResult(error);
                                break;
                            default:
                                WebSocket.SendText("Navigation failed");
                                break;
                        }
                    }
                    break;
                default:
                    ResetTimer();
                    var body = $"{{{input.Replace('=', ':')}}}";
                    using (PatchRequest)
                    {
                        PatchRequest.SetBody(body);
                        var result = PatchRequest.Evaluate();
                        WebSocket.SendResult(result);
                    }
                    WebSocket.SendJson(Root);
                    break;
            }
        }

        private void BaseOnto<T>(IEntities<T> entities) where T : class
        {
            var results = entities.ToList();
            switch (results.Count)
            {
                case 0:
                    throw new Exception("Found no objects to assign to root. Aborting");
                case 1 when results.First() is T result:
                    var patchRequest = entities.Context.CreateRequest<T>(Method.PATCH);
                    patchRequest.Selector = () => new[] {result};
                    Root = result;
                    PatchRequest = patchRequest;
                    break;
                case var more when more > 1:
                    throw new Exception("Found more than one object to assign to root. Aborting");
            }
        }

        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        public void Dispose()
        {
            ActiveSessions.Remove(ID);
            Status = SessionStatus.Closed;
        }
    }
}