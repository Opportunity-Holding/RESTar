using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;
using RESTar.WebSockets;
using static RESTar.ErrorCodes;
using static RESTar.Method;
using Error = RESTar.Results.Error;
using IResource = RESTar.Meta.IResource;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// The WebSocket shell, used to navigate and execute commands against RESTar resources
    /// from a connected WebSocket. 
    /// </summary>
    [RESTar(Description = description, GETAvailableToAll = true)]
    public sealed class Shell : ITerminal
    {
        #region Private

        private const string description =
            "The RESTar WebSocket shell lets the client navigate around the resources of the " +
            "RESTar API, perform CRUD operations and enter terminal resources.";

        private const int MaxStreamBufferSize = 16_000_000;
        private const int MinStreamBufferSize = 512;
        private const int ResultStreamThreshold = 16_000_000;
        private const int MaxInputSize = 16_000_000;

        private string query;
        private string previousQuery;
        private Action OnConfirm;
        private bool _autoGet;
        private int streamBufferSize;
        private IEntities _previousEntities;
        private bool _autoOptions;
        private string _protocol;

        private IEntities PreviousEntities
        {
            get => _previousEntities;
            set
            {
                if (value?.Equals(_previousEntities) != true)
                    _previousEntities?.Dispose();
                _previousEntities = value;
            }
        }

        /// <summary>
        /// Signals that there are changes to the query that have been made pre evaluation
        /// </summary>
        private bool queryChangedPreEval;

        internal static ITerminalResource<Shell> TerminalResource { get; set; }

        private void Reset()
        {
            streamBufferSize = MaxStreamBufferSize;
            Unsafe = false;
            OnConfirm = null;
            PreviousEntities = null;
            query = "";
            previousQuery = "";
            WriteHeaders = false;
            AutoOptions = false;
            AutoGet = false;
            ReformatQueries = true;
            _protocol = "";
        }

        #endregion

        #region Terminal properties

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
                if (_protocol != "" && value.FirstOrDefault() != '-')
                    query = $"-{_protocol}{value}";
                else query = value;
            }
        }

        /// <summary>
        /// Should unsafe commands be allowed?
        /// </summary>
        public bool Unsafe { get; set; }

        /// <summary>
        /// Should the shell write result headers?
        /// </summary>
        public bool WriteHeaders { get; set; }

        /// <summary>
        /// Should the shell write options after a succesful navigation?
        /// </summary>
        public bool AutoOptions
        {
            get => _autoOptions;
            set
            {
                if (value) _autoGet = false;
                _autoOptions = value;
            }
        }

        /// <summary>
        /// Should the shell automatically run a GET operation after a successful navigation?
        /// </summary>
        public bool AutoGet
        {
            get => _autoGet;
            set
            {
                if (value) _autoOptions = false;
                _autoGet = value;
            }
        }

        /// <summary>
        /// The size of stream messages in bytes
        /// </summary>
        public int StreamBufferSize
        {
            get => streamBufferSize;
            set
            {
                if (value < MinStreamBufferSize)
                    streamBufferSize = MinStreamBufferSize;
                else if (MaxStreamBufferSize < value)
                    streamBufferSize = MaxStreamBufferSize;
                else streamBufferSize = value;
            }
        }

        /// <summary>
        /// Should queries be reformatted after input?
        /// </summary>
        public bool ReformatQueries { get; set; }

        /// <summary>
        /// The protocol to use in queries
        /// </summary>
        public string Protocol
        {
            get => _protocol == "" ? "restar" : _protocol;
            set
            {
                var p = ProtocolController.ResolveProtocolProvider(value).ProtocolProvider;
                _protocol = p.ProtocolIdentifier;
            }
        }

        #endregion

        /// <inheritdoc />
        public Shell()
        {
            SupportsTextInput = true;
            SupportsBinaryInput = true;
            Reset();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            WebSocket.Context.Client.ShellConfig = Providers.Json.Serialize(this);
            Reset();
        }

        /// <inheritdoc />
        public IWebSocket WebSocket { private get; set; }

        /// <inheritdoc />
        public void HandleBinaryInput(byte[] input)
        {
            if (!(input?.Length > 0)) return;
            if (Query.Length == 0 || OnConfirm != null)
                WebSocket.SendResult(new InvalidShellStateForBinaryInput());
            else SafeOperation(POST, input);
        }

        /// <inheritdoc />
        public bool SupportsTextInput { get; }

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; }

        /// <inheritdoc />
        public void Open()
        {
            if (WebSocket.Context.Client.ShellConfig is string config)
            {
                Providers.Json.Populate(config, this);
                SendShellInit();
                SendQuery();
            }
            else if (Query != "")
                Navigate();
            else SendShellInit();
        }

        private void Navigate(string input = null)
        {
            if (input != null)
                Query = input;
            if (!QueryIsValid(out var resource)) return;
            PreviousEntities = null;
            if (AutoOptions) SendOptions(resource);
            else if (AutoGet) SafeOperation(GET);
            else SendQuery();
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
                input = "GET";

            switch (input.FirstOrDefault())
            {
                case '\0':
                case '\n': break;
                case '-':
                case '/':
                    Navigate(input);
                    break;
                case '[':
                case '{':
                    SafeOperation(POST, input.ToBytes());
                    break;
                case var _ when input.Length > MaxInputSize:
                    SendBadRequest();
                    break;
                default:
                    var (command, tail) = input.TSplit(' ');
                    switch (command.ToUpperInvariant())
                    {
                        case "GET":
                            SafeOperation(GET, tail?.ToBytes());
                            break;
                        case "POST":
                            SafeOperation(POST, tail?.ToBytes());
                            break;
                        case "PATCH":
                            UnsafeOperation(PATCH, tail?.ToBytes());
                            break;
                        case "PUT":
                            SafeOperation(PUT, tail?.ToBytes());
                            break;
                        case "DELETE":
                            UnsafeOperation(DELETE, tail?.ToBytes());
                            break;
                        case "REPORT":
                            SafeOperation(REPORT, tail?.ToBytes());
                            break;
                        case "HEAD":
                            SafeOperation(HEAD, tail?.ToBytes());
                            break;
                        case "STREAM":
                            var result = WsEvaluate(GET, null);
                            if (result is Content)
                                StreamResult(result, result.TimeElapsed);
                            else SendResult(result);
                            break;
                        case "OPTIONS":
                            if (!QueryIsValid(out var _resource)) break;
                            SendOptions(_resource);
                            break;

                        case "HEADERS":
                        case "HEADER":
                            tail = tail?.Trim();
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                SendHeaders();
                                break;
                            }
                            var (key, value) = tail.TSplit('=', true);
                            if (value == null)
                            {
                                SendHeaders();
                                break;
                            }
                            if (key.IsCustomHeaderName())
                            {
                                if (value == "null")
                                {
                                    WebSocket.Headers.Remove(key);
                                    SendHeaders();
                                    break;
                                }
                                WebSocket.Headers[key] = value;
                                SendHeaders();
                            }
                            else WebSocket.SendText($"400: Bad request. Cannot read or write reserved header '{key}'.");
                            return;

                        case "VAR":
                            if (string.IsNullOrWhiteSpace(tail))
                            {
                                WebSocket.SendJson(this);
                                break;
                            }
                            var (property, valueString) = tail.TSplit('=', true);
                            if (property == null || valueString == null)
                            {
                                WebSocket.SendText("Invalid property assignment syntax. Should be: VAR <property> = <value>");
                                break;
                            }
                            if (valueString.EqualsNoCase("null"))
                                valueString = null;
                            if (!TerminalResource.Members.TryGetValue(property, out var declaredProperty))
                            {
                                WebSocket.SendText($"Unknown shell property '{property}'. To list properties, type VAR");
                                break;
                            }
                            try
                            {
                                declaredProperty.SetValue(this, valueString.ParseConditionValue(declaredProperty));
                                WebSocket.SendJson(this);
                            }
                            catch (Exception e)
                            {
                                WebSocket.SendException(e);
                            }
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

                        case "GO":
                        case "NAVIGATE":
                        case "?":
                            if (!string.IsNullOrWhiteSpace(tail))
                            {
                                Navigate(tail);
                                break;
                            }
                            WebSocket.SendText($"{(Query.Any() ? Query : "< empty >")}");
                            break;
                        case "FIRST":
                            Permute(p => p.GetFirstLink(tail.ToNumber() ?? 1));
                            break;
                        case "LAST":
                            Permute(p => p.GetLastLink(tail.ToNumber() ?? 1));
                            break;
                        case "ALL":
                            Permute(p => p.GetAllLink());
                            break;
                        case "NEXT":
                            Permute(p => p.GetNextPageLink(tail.ToNumber() ?? -1));
                            break;
                        case "PREV":
                        case "PREVIOUS":
                            Permute(p => p.GetPreviousPageLink(tail.ToNumber() ?? -1));
                            break;

                        #region Nonsense

                        case "HELLO" when tail.EqualsNoCase("world"):

                            string getHelloWorld()
                            {
                                switch (new Random().Next(0, 7))
                                {
                                    case 0: return "The world says: 'hi!'";
                                    case 1: return "The world says: 'what's up?'";
                                    case 2: return "The world says: 'greetings!'";
                                    case 3: return "The world is currently busy";
                                    case 4: return "The world cannot answer right now";
                                    case 5: return "The world is currently out on lunch";
                                    default: return "The world says: 'why do people keep saying that?'";
                                }
                            }

                            WebSocket.SendText(getHelloWorld());
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

                        #endregion
                    }
                    break;
            }
        }

        private void SendHeaders() => WebSocket.SendJson(new {WebSocket.Headers});

        private void Permute(Func<IEntities, IUriComponents> permuter)
        {
            var stopwatch = Stopwatch.StartNew();
            if (PreviousEntities == null)
            {
                WsGetPreliminary().Dispose();
                if (PreviousEntities == null)
                {
                    SendResult(new ShellNoContent(WebSocket, stopwatch.Elapsed));
                    return;
                }
            }
            var link = permuter(PreviousEntities);
            Navigate(link.ToString());
        }

        private IResult WsGetPreliminary()
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            using (var request = WebSocket.Context.CreateRequest(local, GET, null, WebSocket.Headers))
            {
                var result = request.Evaluate();
                switch (result)
                {
                    case Error _ when queryChangedPreEval:
                        query = previousQuery;
                        break;
                    case IEntities entities:
                        query = local;
                        PreviousEntities = entities;
                        break;
                    case Change _:
                        query = local;
                        PreviousEntities = null;
                        break;
                    default:
                        query = local;
                        break;
                }
                queryChangedPreEval = false;
                return result;
            }
        }

        private ISerializedResult WsEvaluate(Method method, byte[] body)
        {
            if (Query.Length == 0) return new ShellNoQuery(WebSocket);
            var local = Query;
            using (var request = WebSocket.Context.CreateRequest(local, method, body, WebSocket.Headers))
            {
                var result = request.Evaluate().Serialize();
                switch (result)
                {
                    case Error _ when queryChangedPreEval:
                        query = previousQuery;
                        break;
                    case IEntities entities:
                        query = local;
                        PreviousEntities = entities;
                        break;
                    case Change _:
                        query = local;
                        PreviousEntities = null;
                        break;
                    default:
                        query = local;
                        break;
                }
                queryChangedPreEval = false;
                return result;
            }
        }

        private bool QueryIsValid(out IResource resource)
        {
            var localQuery = Query;
            if (!WebSocket.Context.UriIsValid(localQuery, out var error, out resource, out var components))
            {
                query = previousQuery;
                SendResult(error);
                return false;
            }
            if (ReformatQueries)
                localQuery = components.ToString();
            query = localQuery;
            queryChangedPreEval = false;
            return true;
        }

        private void SendQuery() => WebSocket.SendText("? " + Query);

        private void SendOptions(IResource resource)
        {
            var availableResource = AvailableResource.Make(resource, WebSocket);
            var options = new OptionsBody(availableResource.Name, availableResource.Kind, availableResource.Methods);
            WebSocket.SendJson(options, true);
            SendQuery();
        }

        private void SafeOperation(Method method, byte[] body = null)
        {
            var sw = Stopwatch.StartNew();
            switch (WsEvaluate(method, body))
            {
                case Content content when content.Body is Stream _:
                    if (!content.Body.CanRead)
                    {
                        SendResult(new UnreadableContentStream(content));
                        break;
                    }
                    if (!content.Body.CanSeek)
                    {
                        content.Body = new RESTarStream
                        (
                            contentType: content.Headers.ContentType.GetValueOrDefault(),
                            stream: content.Body
                        );
                        break;
                    }
                    if (content.Body.Length > ResultStreamThreshold)
                    {
                        OnConfirm = () => StreamResult(content, sw.Elapsed);
                        SendConfirmationRequest("426: The result body is too large to be sent in a single WebSocket message. " +
                                                "Do you want to stream the result instead? ");
                        break;
                    }
                    SendResult(content, sw.Elapsed);
                    break;
                case OK ok:
                    SendResult(ok, sw.Elapsed);
                    break;
                case var other:
                    SendResult(other);
                    break;
            }
            sw.Stop();
        }

        private void UnsafeOperation(Method method, byte[] body = null)
        {
            void runOperation()
            {
                WebSocket.Headers.UnsafeOverride = true;
                SafeOperation(method, body);
            }

            if (PreviousEntities == null)
            {
                var result = WsEvaluate(GET, null);
                if (result is IEntities entities)
                    PreviousEntities = entities;
                else
                {
                    SendResult(result);
                    return;
                }
            }

            switch (PreviousEntities.EntityCount)
            {
                case 0:
                    SendBadRequest($". No entities for to run {method} on");
                    break;
                case 1:
                    runOperation();
                    break;
                case var multiple:
                    if (Unsafe)
                    {
                        runOperation();
                        break;
                    }
                    OnConfirm = runOperation;
                    SendConfirmationRequest($"This will run {method} on {multiple} entities in resource " +
                                            $"'{PreviousEntities.Request.Resource}'. ");
                    break;
            }
        }

        private void SendResult(ISerializedResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            WebSocket.SendResult(result, elapsed, WriteHeaders);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    WebSocket.SendText("? <no query>");
                    break;
                default:
                    WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private void StreamResult(ISerializedResult result, TimeSpan? elapsed = null)
        {
            if (result is SwitchedTerminal) return;
            WebSocket.StreamResult(result, StreamBufferSize, elapsed, WriteHeaders);
            switch (result)
            {
                case var _ when Query == "":
                case ShellNoQuery _:
                    WebSocket.SendText("? <no query>");
                    break;
                default:
                    WebSocket.SendText("? " + Query);
                    break;
            }
        }

        private void SendShellInit()
        {
            WebSocket.SendText("### Entering the RESTar WebSocket shell... ###");
            WebSocket.SendText("### Type a command to continue (e.g. HELP) ###");
        }

        private void SendConfirmationRequest(string initialInfo = null) =>
            WebSocket.SendText($"{initialInfo}Type 'Y' to continue, 'N' to cancel");

        private void SendCancel() => WebSocket.SendText("Operation cancelled");
        private void SendBadRequest(string message = null) => WebSocket.SendText($"400: Bad request{message}");

        private void SendInvalidCommandArgument(string command, string arg) =>
            WebSocket.SendText($"Invalid argument '{arg}' for command '{command}'");

        private void SendUnknownCommand(string command) => WebSocket.SendText($"Unknown command '{command}'");

        private void Close()
        {
            WebSocket.SendText("### Closing the RESTar WebSocket shell... ###");
            var connection = (WebSocketConnection) WebSocket;
            connection.WebSocket.Disconnect();
        }

        private void SendHelp() => WebSocket.SendText(
            "Shell documentation is available at https://github.com/Mopedo/Home/tree/master/RESTar/Built-in%20resources/RESTar/Shell");

        private void SendCredits()
        {
            WebSocket.SendText($"RESTar is designed and developed by Erik von Krusenstierna, © Mopedo AB {DateTime.Now.Year}");
        }
    }

    internal static class ShellExtensions
    {
        internal static int? ToNumber(this string tail)
        {
            if (tail == null || !int.TryParse(tail, out var nr))
                return null;
            return nr;
        }
    }
}