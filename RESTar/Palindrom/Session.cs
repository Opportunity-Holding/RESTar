using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.WebSockets;

#pragma warning disable 1591
// ReSharper disable All

namespace RESTar.Palindrom
{
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
        internal const int TimeoutMilliseconds = 60;

        internal static Session Create(IRequest initialRequest, object root, IRequest patchRequest)
        {
            var newSessionId = Guid.NewGuid().ToString("N");
            return ActiveSessions[newSessionId] = new Session(newSessionId)
            {
                Resource = initialRequest.Resource,
                Root = root,
                PatchRequest = patchRequest
            };
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
        }

        public Session() { }

        // Instance:

        private DateTime LastUsed { get; set; }
        private Timer Timer { get; set; }
        private IRequest PatchRequest { get; set; }

        public IWebSocket WebSocket { private get; set; }

        public string ID { get; private set; }
        public Meta.IResource Resource { get; private set; }
        public object Root { get; private set; }

        private void ResetTimer() => Timer.Change(TimeoutMilliseconds, -1);

        public void Open()
        {
            WebSocket.SendText($"Now open! ID: {ID}, Root:");
            WebSocket.SendJson(Root, prettyPrint: true);
            ResetTimer();
        }

        public void HandleTextInput(string input)
        {
            LastUsed = DateTime.Now;
            ResetTimer();
            var (command, arg) = input.TSplit(" ", trim: true);
            switch (command)
            {
                case "GET":
                    WebSocket.SendJson(Root);
                    break;
                case "PATCH" when !string.IsNullOrWhiteSpace(arg):


                    WebSocket.SendJson("Patch applied!");
                    break;
                default:
                    WebSocket.SendException(new InvalidOperationException("Invalid command syntax"));
                    break;
            }
        }

        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        public void Dispose()
        {
            ActiveSessions.Remove(ID);
        }
    }
}