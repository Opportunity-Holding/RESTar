﻿using System;
using System.Diagnostics;
using System.Linq;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Success;
using RESTar.WebSockets;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Methods;

namespace RESTar
{
    [RESTar(Description = description, GETAvailableToAll = true)]
    internal class Shell : ITerminal
    {
        private const string description = "The RESTar WebSocket shell lets the client navigate around the resources of the " +
                                           "RESTar application, perform CRUD operations and enter terminal resources.";

        private string query = "";
        private string previousQuery = "";

        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

        public string Query
        {
            get => query;
            set
            {
                switch (value)
                {
                    case "": break;
                    case null:
                    case var _ when value[0] != '/' && value[0] != '-':
                        throw new InvalidSyntax(InvalidUriSyntax, "Shell queries must begin with '/' or '-'");
                }
                previousQuery = query;
                queryChangedPreEval = true;
                query = value;
            }
        }

        public bool Silent
        {
            get => !WriteStatusBeforeContent && !WriteTimeElapsed && !WriteQueryAfterContent && !WriteInfoTexts;
            set
            {
                WriteStatusBeforeContent = !value;
                WriteTimeElapsed = !value;
                WriteQueryAfterContent = !value;
                WriteInfoTexts = !value;
            }
        }

        public bool Unsafe { get; set; } = false;
        public bool WriteStatusBeforeContent { get; set; } = true;
        public bool WriteTimeElapsed { get; set; } = true;
        public bool WriteQueryAfterContent { get; set; } = true;
        public bool WriteInfoTexts { get; set; } = true;

        private Func<int, IUriComponents> GetNextPageLink;
        private Action OnConfirm;
        private IEntitiesMetadata PreviousResultMetadata;

        public IWebSocket WebSocket { private get; set; }
        private IWebSocketInternal WebSocketInternal => (IWebSocketInternal) WebSocket;
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;
        internal static ITerminalResourceInternal<Shell> TerminalResource { get; set; }

        public void Open()
        {
            if (Query != "")
                SafeOperation(GET);
            else SendShellInit();
        }

        public void HandleTextInput(string input)
        {
            if (OnConfirm != null)
            {
                switch (input.FirstOrDefault())
                {
                    case var _ when input.Length > 1:
                    default:
                        SendConfirmationRequest();
                        break;
                    case 'Y':
                    case 'y':
                        OnConfirm();
                        OnConfirm = null;
                        break;
                    case 'N':
                    case 'n':
                        OnConfirm = null;
                        SendCancel();
                        break;
                }
                return;
            }
            if (input == " ")
            {
                SafeOperation(GET);
                return;
            }
            input = input.Trim();
            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case '-':
                case '/':
                    Query = input;
                    SafeOperation(GET);
                    break;
                case '[':
                case '{':
                    SafeOperation(POST, input.ToBytes());
                    break;
                case var _ when input.Length > 2000:
                    SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            byte[] body = null;
                            if (!string.IsNullOrWhiteSpace(tail))
                            {
                                var (q, b) = tail.TSplit(' ');
                                Query = q;
                                body = b?.ToBytes();
                            }
                            SafeOperation(GET, body);
                            break;
                        case "POST":
                            SafeOperation(POST, tail.ToBytes());
                            break;
                        case "PUT":
                            SendBadRequest("PUT is not available in the WebSocket interface");
                            break;
                        case "PATCH":
                            UnsafeOperation(PATCH, tail.ToBytes());
                            break;
                        case "DELETE":
                            UnsafeOperation(DELETE);
                            break;
                        case "REPORT":
                            if (!string.IsNullOrWhiteSpace(tail))
                                Query = tail;
                            SafeOperation(REPORT);
                            break;
                        case "@":
                        case "NAVIGATE":
                        case "GO":
                        case "HEAD":
                            if (!string.IsNullOrWhiteSpace(tail))
                                Query = tail;
                            var result = WsEvaluate(HEAD, null);
                            if (!(result is Head))
                                SendResult(result, null);
                            else if (WriteQueryAfterContent)
                                WebSocket.SendText("? " + Query);
                            break;
                        case "HELP":
                            SendHelp();
                            break;
                        case "EXIT":
                        case "QUIT":
                        case "DISCONNECT":
                        case "CLOSE":
                            Close();
                            break;
                        case "?":
                            WebSocket.SendText($"{(Query.Any() ? Query : "<empty>")}");
                            break;
                        case "RELOAD":
                            SafeOperation(GET);
                            break;
                        case "NEXT":
                            if (tail == null || !int.TryParse(tail, out var count))
                                count = -1;
                            var link = GetNextPageLink?.Invoke(count)?.ToString();
                            if (link == null)
                                SendResult(new NoContent(WebSocket), null);
                            else
                            {
                                Query = link;
                                SafeOperation(GET);
                            }
                            break;
                        case "HI":
                        case "HELLO":

                            string getGreeting()
                            {
                                switch (new Random().Next(0, 10))
                                {
                                    case 0: return "Well, hello there :D";
                                    case 1: return "Greetings, friend";
                                    case 2: return "Hello, dear client";
                                    case 3: return "Hello to you";
                                    case 4: return "Hi!";
                                    case 5: return "Nice to see you!";
                                    case 6: return "What's up?";
                                    case 7: return "✌️";
                                    case 8: return "'sup";
                                    default: return "Oh no, it's you again...";
                                }
                            }

                            WebSocket.SendText(getGreeting());
                            break;
                        case "NICE":
                        case "THANKS":
                        case "THANK":

                            string getYoureWelcome()
                            {
                                switch (new Random().Next(0, 7))
                                {
                                    case 0: return "😎";
                                    case 1: return "👍";
                                    case 2: return "🙌";
                                    case 3: return "🎉";
                                    case 4: return "🤘";
                                    case 5: return "You're welcome!";
                                    default: return "✌️";
                                }
                            }

                            WebSocket.SendText(getYoureWelcome());
                            break;
                        case "CREDITS":
                            SendCredits();
                            break;
                        case var unknown:
                            SendUnknownCommand(unknown);
                            break;
                    }
                    break;
            }
        }

        public void Dispose()
        {
            OnConfirm = null;
            PreviousResultMetadata = null;
            GetNextPageLink = null;
            query = "";
            previousQuery = "";
            WriteInfoTexts = true;
            WriteStatusBeforeContent = true;
            WriteTimeElapsed = true;
            WriteQueryAfterContent = true;
        }

        private IFinalizedResult WsEvaluate(Methods method, byte[] body)
        {
            if (Query.Length == 0) return new NoQuery(WebSocket);
            var localQuery = Query;
            var result = Request
                .Create(WebSocket, method, ref localQuery, body, WebSocket.Headers)
                .GetResult()
                .FinalizeResult();
            if (result is RESTarError _ && queryChangedPreEval)
                query = previousQuery;
            else query = localQuery;
            queryChangedPreEval = false;
            if (result is IEntitiesMetadata entitiesMetaData)
            {
                PreviousResultMetadata = entitiesMetaData;
                GetNextPageLink = entitiesMetaData.GetNextPageLink;
            }
            return result;
        }

        private void SafeOperation(Methods method, byte[] body = null)
        {
            var sw = Stopwatch.StartNew();
            switch (WsEvaluate(method, body))
            {
                case Entities entities:
                    SendResult(entities, sw.Elapsed);
                    break;
                case Report report:
                    SendResult(report, sw.Elapsed);
                    break;
                case var other:
                    SendResult(other, null);
                    break;
            }
            sw.Stop();
        }

        private void UnsafeOperation(Methods method, byte[] body = null)
        {
            void operate()
            {
                WebSocket.Headers.UnsafeOverride = true;
                SafeOperation(method, body);
            }

            switch (PreviousResultMetadata?.EntityCount)
            {
                case null:
                case 0:
                    SendBadRequest($". No entities for {method} operation. Make a selecting request before running {method}");
                    break;
                case 1:
                    operate();
                    break;
                case var many:
                    if (Unsafe)
                    {
                        operate();
                        break;
                    }
                    OnConfirm = operate;
                    SendConfirmationRequest($"This will run {method} on {many} entities in resource '{PreviousResultMetadata.ResourceFullName}'. ");
                    break;
            }
        }

        private void SendResult(IFinalizedResult result, TimeSpan? elapsed)
        {
            WebSocket.SendResult(result, WriteStatusBeforeContent, elapsed);
            if (!WriteQueryAfterContent) return;
            switch (result)
            {
                case WebSocketResult _: return;
                case NoQuery _:
                    WebSocket.SendText("? <empty>");
                    break;
                default:
                    WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private void SendShellInit()
        {
            if (!WriteInfoTexts) return;
            WebSocket.SendText("### Entering the RESTar WebSocket shell... ###");
            WebSocket.SendText("### Type a command to continue (e.g. HELP) ###");
        }

        private void SendConfirmationRequest(string initialInfo = null) => WebSocket.SendText($"{initialInfo}Type 'Y' to continue, 'N' to cancel");
        private void SendCancel() => WebSocket.SendText("Operation cancelled");
        private void SendBadRequest(string message = null) => WebSocket.SendText($"400: Bad request{message}");
        private void SendInvalidCommandArgument(string command, string arg) => WebSocket.SendText($"Invalid argument '{arg}' for command '{command}'");
        private void SendUnknownCommand(string command) => WebSocket.SendText($"Unknown command '{command}'");

        private void Close()
        {
            if (!WriteInfoTexts) return;
            WebSocket.SendText("### Closing the RESTar WebSocket shell... ###");
            WebSocketInternal.Disconnect();
        }

        private void SendHelp() => WebSocket.SendText(
            "\n  The RESTar WebSocket shell makes it possible to send\n" +
            "  multiple requests to a RESTar API, over a single TCP\n" +
            "  connection. Using commands, the client can navigate\n" +
            "  around the resources of the API, read, insert, update\n" +
            "  and/or delete entities, or enter terminals. To navigate\n" +
            "  and select entities, simply send a request URI over this\n" +
            "  WebSocket, e.g. '/availableresource//limit=3'. To insert\n" +
            "  an entity into a resource, send the JSON representation\n" +
            "  over the WebSocket. To update entities, send 'PATCH <json>',\n" +
            "  where <json> is the JSON data to update entities from. To\n" +
            "  delete selected entities, send 'DELETE'. For potentially\n" +
            "  unsafe operations, you will be asked to confirm before\n" +
            "  changes are applied.\n\n" +
            "  Some other simple commands:\n" +
            "  ?           Prints the current location\n" +
            "  REPORT      Counts the entities at the current location\n" +
            "  RELOAD      Relods the current location\n" +
            "  HELP        Prints this help page\n" +
            "  CLOSE       Closes the WebSocket\n");

        private void SendCredits()
        {
            WebSocket.SendText($"RESTar is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");
        }
    }
}