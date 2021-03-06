﻿using System;
using RESTar.Requests;
using Starcounter;
using static Starcounter.Response.ConnectionFlags;
using WebSocket = RESTar.WebSockets.WebSocket;

namespace RESTar.Internal.Sc
{
    internal class ScWebSocket : WebSocket
    {
        private readonly Request UpgradeRequest;
        private readonly string GroupName;
        private Starcounter.WebSocket WebSocket;
        private static string GetRESTarWsId(Request upgradeRequest) => MakeRESTarWsId(upgradeRequest.GetWebSocketId());
        internal static string GetRESTarWsId(Starcounter.WebSocket webSocket) => MakeRESTarWsId(webSocket.ToUInt64());

        private static string MakeRESTarWsId(ulong id) => DbHelper.Base64EncodeObjectNo(id);

        protected override void Send(string text)
        {
            if (text.Length == 0) return;
            Scheduling.RunTask(() => WebSocket.Send(text)).Wait();
        }

        protected override void Send(byte[] data, bool isText, int offset, int length)
        {
            if (length == 0) return;
            if (offset == 0)
                Scheduling.RunTask(() => WebSocket.Send(data, length, isText)).Wait();
            else
            {
                Scheduling.RunTask(() =>
                {
                    var buffer = new byte[length];
                    Array.Copy(data, offset, buffer, 0, length);
                    WebSocket.Send(buffer, length, isText);
                }).Wait();
            }
        }

        protected override bool IsConnected => WebSocket?.IsDead() == false;

        protected override async void DisconnectWebSocket(string message = null) => await Scheduling.RunTask(() =>
        {
            if (message == null)
                WebSocket.Disconnect();
            else WebSocket.Send(message, connFlags: DisconnectAfterSend);
        });

        protected override void SendUpgrade() => WebSocket = UpgradeRequest.SendUpgrade(GroupName);

        internal ScWebSocket(string groupName, Request upgradeRequest, Client client) : base(GetRESTarWsId(upgradeRequest), client)
        {
            GroupName = groupName;
            UpgradeRequest = upgradeRequest;
        }
    }
}