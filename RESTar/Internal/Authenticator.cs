﻿using System;
using System.Linq;
using RESTar.Auth;
using Simplified.Ring3;
using static RESTar.Internal.ErrorCodes;
using static RESTar.RESTarConfig;

namespace RESTar.Internal
{
    internal static class Authenticator
    {
        internal static bool SignedIn => SystemUser.GetCurrentSystemUser() != null;

        internal static void UserCheck()
        {
            if (!SignedIn)
                throw new ForbiddenException(NotSignedIn, "User is not signed in");
        }

        internal static ForbiddenException NotAuthorizedException => new ForbiddenException(NotAuthorized, "Not authorized");

        internal static string Authenticate(Starcounter.Request ScRequest)
        {
            AccessRights accessRights;
            string authToken;
            if (!ScRequest.IsExternal)
            {
                authToken = ScRequest.Headers["RESTar-AuthToken"];
                if (string.IsNullOrWhiteSpace(authToken))
                    throw NotAuthorizedException;
                if (!AuthTokens.TryGetValue(authToken, out accessRights))
                    throw NotAuthorizedException;
                return authToken;
            }
            var authorizationHeader = ScRequest.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(authorizationHeader))
                throw NotAuthorizedException;
            var apikey_key = authorizationHeader.Split(' ');
            if (apikey_key[0].ToLower() != "apikey" || apikey_key.Length != 2)
                throw NotAuthorizedException;
            var apiKey = apikey_key[1].SHA256();
            if (!ApiKeys.TryGetValue(apiKey, out accessRights))
                throw NotAuthorizedException;
            authToken = Guid.NewGuid().ToString();
            AuthTokens[authToken] = accessRights;
            return authToken;
        }

        internal static bool MethodCheck(RESTarMethods requestedMethod, IResource resource, string authToken)
        {
            if (!resource.AvailableMethods.Contains(requestedMethod)) return false;
            if (SignedIn) return true;
            if (!RequireApiKey) return true;
            var accessRights = AuthTokens[authToken];
            var rights = accessRights?[resource];
            return rights?.Contains(requestedMethod) == true;
        }
    }
}