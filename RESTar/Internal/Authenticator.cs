﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RESTar.Auth;
using RESTar.Requests;
using RESTar.Results.Error.Forbidden;
using Starcounter;
using static RESTar.RESTarConfig;

namespace RESTar.Internal
{
    internal static class Authenticator
    {
        internal const string CurrentUser = "SELECT t.Token.User FROM Simplified.Ring5.SystemUserSession t " +
                                            "WHERE t.SessionIdString =? AND t.Token.User IS NOT NULL";

        internal static IDictionary<string, AccessRights> ApiKeys { get; private set; }
        internal static ConcurrentDictionary<string, AccessRights> AuthTokens { get; private set; }
        internal static readonly string AppToken = Guid.NewGuid().ToString();

        internal static void NewState()
        {
            ApiKeys = new Dictionary<string, AccessRights>();
            AuthTokens = new ConcurrentDictionary<string, AccessRights>();
            AuthTokens.TryAdd(AppToken, AccessRights.Root);
        }

        private static object GetCurrentSystemUser()
        {
            if (Session.Current == null) return null;
            return Db.SQL(CurrentUser, Session.Current.SessionId).FirstOrDefault();
        }

        internal static void CheckUser()
        {
            if (GetCurrentSystemUser() == null)
                throw new UserNotSignedIn();
        }

        internal static void RunResourceAuthentication<T>(this IRequest<T> request) where T : class
        {
            if (!request.Resource.RequiresAuthentication) return;
            var authResults = request.Resource.Authenticate(request);
            if (!authResults.Success)
                throw new FailedResourceAuthentication(authResults.Reason);
        }

        internal static void Authenticate(this Context context)
        {
            context.TcpConnection.AuthToken = GetAuthToken(context);
            if (context.TcpConnection.AuthToken == null)
                context.Error = new NotAuthorized();
        }

        private static string GetAuthToken(Context context)
        {
            if (!RequireApiKey)
                return AssignAuthtoken(AccessRights.Root);
            if (context.TcpConnection.AuthToken is string existing)
                return AuthTokens.TryGetValue(existing, out _) ? existing : null;

            var authorizationHeader = context.Headers.SafeGet("Authorization");
            if (string.IsNullOrWhiteSpace(authorizationHeader)) return null;
            var (method, key) = authorizationHeader.TSplit(' ');
            if (key == null) return null;
            switch (method.ToLower())
            {
                case "apikey": break;
                case "basic":
                    key = Encoding.UTF8.GetString(Convert.FromBase64String(key)).Split(":").ElementAtOrDefault(1);
                    if (key == null) return null;
                    break;
                default: return null;
            }
            if (!ApiKeys.TryGetValue(key.SHA256(), out var accessRights))
                return null;
            context.Headers["Authorization"] = "*******";
            return AssignAuthtoken(accessRights);
        }

        internal static void Authenticate<T>(this Request<T> request) where T : class
        {
            request.AuthToken = AppToken;
        }

        private static string AssignAuthtoken(AccessRights rights)
        {
            var token = Guid.NewGuid().ToString();
            AuthTokens[token] = rights;
            return token;
        }

        internal static bool MethodCheck(Methods requestedMethod, IEntityResource resource, string authToken, out bool failedAuth)
        {
            failedAuth = false;
            if (!resource.AvailableMethods.Contains(requestedMethod)) return false;
            var accessRights = AuthTokens[authToken];
            var rights = accessRights?[resource];
            if (rights?.Contains(requestedMethod) == true)
                return true;
            failedAuth = true;
            return false;
        }
    }
}