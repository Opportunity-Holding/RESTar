﻿using System;
using System.Diagnostics;
using System.Linq;
using RESTar.Queries;
using RESTar.Resources;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Success;
using RESTar.WebSockets;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Method;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// The WebSocket shell, used to navigate and execute commands against RESTar resources
    /// from a connected WebSocket. 
    /// </summary>
    [RESTar(Description = description, GETAvailableToAll = true)]
    public class Shell : ITerminal
    {
        private const string description = "The RESTar WebSocket shell lets the client navigate around the resources of the " +
                                           "RESTar application, perform CRUD operations and enter terminal resources.";

        private string query = "";
        private string previousQuery = "";

        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

        /// <summary>
        /// The query to perform in the shell
        /// </summary>
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

        /// <summary>
        /// Should the shell be silent in output? Sets WriteStatusBeforeContent, WriteTimeElapsed, WriteQueryAfterContent
        /// and WriteInfoTexts to false.
        /// </summary>
        public bool Silent
        {
            get => !WriteStatusBeforeContent && !WriteTimeElapsed && !WriteQueryAfterContent && !WriteInfoTexts;
            set => WriteStatusBeforeContent = WriteTimeElapsed = WriteQueryAfterContent = WriteInfoTexts = !value;
        }

        /// <summary>
        /// Should unsafe commands be allowed?
        /// </summary>
        public bool Unsafe { get; set; } = false;

        /// <summary>
        /// Should the shell output the result status before included content?
        /// </summary>
        public bool WriteStatusBeforeContent { get; set; } = true;

        /// <summary>
        /// Should the shell output the time elapsed in evaluating the command?
        /// </summary>
        public bool WriteTimeElapsed { get; set; } = true;

        /// <summary>
        /// Should the shell output the current query after writing content?
        /// </summary>
        public bool WriteQueryAfterContent { get; set; } = true;

        /// <summary>
        /// Should the shell output info texts?
        /// </summary>
        public bool WriteInfoTexts { get; set; } = true;

        private Func<int, IUriComponents> GetNextPageLink;
        private Action OnConfirm;
        private IEntitiesMetadata PreviousResultMetadata;

        /// <inheritdoc />
        public IWebSocket WebSocket { private get; set; }

        /// <inheritdoc />
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();

        /// <inheritdoc />
        public bool SupportsTextInput { get; } = true;

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; } = false;

        internal static ITerminalResource<Shell> TerminalResource { get; set; }

        /// <inheritdoc />
        public void Open()
        {
            if (Query != "")
                SafeOperation(GET);
            else SendShellInit();
        }

        /// <inheritdoc />
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
            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case '-':
                case '/':
                    if (input.Length == 1)
                        input = "/restar.availableresource";
                    if (input.StartsWith("//"))
                        input = $"/restar.availableresource/{input.Substring(2)}";
                    Query = input;
                    ValidateQuery();
                    break;
                case '[':
                case '{':
                    SafeOperation(POST, input.ToBytes());
                    break;
                case var _ when input.Length > 16_000_000:
                    SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            byte[] body = null;
                            if (!string.IsNullOrWhiteSpace(tail))
                                body = tail.ToBytes();
                            SafeOperation(GET, body);
                            break;
                        case "POST":
                            SafeOperation(POST, tail.ToBytes());
                            break;
                        case "PATCH":
                            UnsafeOperation(PATCH, tail.ToBytes());
                            break;
                        case "PUT":
                            SafeOperation(PUT, tail.ToBytes());
                            break;
                        case "DELETE":
                            UnsafeOperation(DELETE);
                            break;
                        case "REPORT":
                            SafeOperation(REPORT);
                            break;
                        case "HEAD":
                            SafeOperation(HEAD);
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
                        case "NEXT":
                            var stopwatch = Stopwatch.StartNew();
                            if (tail == null || !int.TryParse(tail, out var count))
                                count = -1;
                            var link = GetNextPageLink?.Invoke(count)?.ToString();
                            if (link == null)
                                SendResult(new NoContent(WebSocket, stopwatch.Elapsed));
                            else
                            {
                                Query = link;
                                SafeOperation(GET);
                            }
                            break;

                        #region Nonsense

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

                        #endregion
                    }
                    break;
            }
        }

        /// <inheritdoc />
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

        private ISerializedResult WsEvaluate(Method method, byte[] body)
        {
            if (Query.Length == 0) return new NoQuery(WebSocket, default);
            var local = Query;
            var result = RESTar.Query.Create(WebSocket, method, ref local, body, WebSocket.Headers).Result.Serialize();
            switch (result)
            {
                case RESTarError _ when queryChangedPreEval:
                    query = previousQuery;
                    break;
                case IEntitiesMetadata entitiesMetaData:
                    query = local;
                    PreviousResultMetadata = entitiesMetaData;
                    GetNextPageLink = entitiesMetaData.GetNextPageLink;
                    break;
                default:
                    query = local;
                    break;
            }
            queryChangedPreEval = false;
            return result;
        }

        private void ValidateQuery()
        {
            var localQuery = Query;
            if (!RESTar.Query.IsValid(WebSocket, ref localQuery, out var error))
            {
                query = previousQuery;
                SendResult(error);
            }
            else
            {
                query = localQuery;
                WebSocket.SendText("? " + Query);
            }
            queryChangedPreEval = false;
        }

        private void SafeOperation(Method method, byte[] body = null)
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
                    SendResult(other);
                    break;
            }
            sw.Stop();
        }

        private void UnsafeOperation(Method method, byte[] body = null)
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

        private void SendResult(ISerializedResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            WebSocket.SendResult(result, WriteStatusBeforeContent, elapsed);
            if (!WriteQueryAfterContent) return;
            switch (result)
            {
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
            var connection = (WebSocketConnection) WebSocket;
            connection.WebSocket.Disconnect();
        }

        private void SendHelp() => WebSocket.SendText(
            "\n  The RESTar WebSocket shell makes it possible to send\n" +
            "  multiple requests to a RESTar API, over a single TCP\n" +
            "  connection. Using commands, the client can navigate\n" +
            "  around the resources of the API, read, insert, update\n" +
            "  and/or delete entities, or enter terminals. To navigate\n" +
            "  to a resource, simply send a request URI over this WebSocket,\n" +
            "  e.g. '/availableresource//limit=3'. To list the entities,\n" +
            "  send 'GET'. To insert a new entity into a resource, send the\n" +
            "  representation over the WebSocket, for example in JSON. To \n" +
            "  update entities, send 'PATCH <json>',  where <json> is the \n" +
            "  JSON data to update entities from. To delete selected entities,\n" +
            "  send 'DELETE'. For potentially unsafe operations, you will be\n" +
            "  asked to confirm before changes are applied.\n\n" +
            "  Some other simple commands:\n" +
            "  ?           Prints the current location\n" +
            "  REPORT      Counts the entities at the current location\n" +
            "  HELP        Prints this help page\n" +
            "  CLOSE       Closes the WebSocket\n");

        private void SendCredits()
        {
            WebSocket.SendText($"RESTar is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");
        }
    }
}