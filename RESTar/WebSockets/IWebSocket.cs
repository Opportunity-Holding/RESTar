using System;
using System.Collections.Generic;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.WebSockets
{
    /// <inheritdoc />
    /// <summary>
    /// WebSockets support sending continuous data over a single TCP connection
    /// </summary>
    public interface IWebSocket : ITraceable
    {
        /// <summary>
        /// Sends the string data as text over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendText(string data);

        /// <summary>
        /// Sends the byte array data as text over the WebSocket.
        /// </summary>
        void SendText(byte[] data, int offset, int length);

        /// <summary>
        /// Sends the byte array data as binary over the WebSocket.
        /// </summary>
        void SendBinary(byte[] data, int offset, int length);

        /// <summary>
        /// Sends an object over the WebSocket, serialized as JSON text. The output pretty print setting is controlled by
        /// the prettyPrint parameter. If null, the global pretty print setting is used.
        /// </summary>
        void SendJson(object item, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false);

        /// <summary>
        /// Sends a result synchronously over a WebSocket, with the body contained in a single binary message. If the result body is larger than
        /// 16 megabytes, a <see cref="WebSocketMessageTooLargeException"/> will be thrown. In these cases, use <see cref="StreamResult"/> instead.
        /// </summary>
        /// <param name="result">The result to send. The body of the result (if any) will be sent as binary over the websocket.
        /// Additional inforation can be included in separate text messages (see other parameters).</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included. If not null, timeElapsed 
        /// will be included in the status text message (see writeStatus)</param>
        /// <param name="writeHeaders">Should headers be included as a text message? If true, headers are printed after the status
        /// (if any) and before the content is sent.</param>
        /// <param name="disposeResult">Should the result be disposed after it is sent to the WebSocket?</param>
        void SendResult(IResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, bool disposeResult = true);

        /// <summary>
        /// Sends an arbitrarily large result over a WebSocket, with the body split over multiple binary messages. Before sending
        /// the contents of the result, RESTar will send a stream manifest as a text message, which enables a set of commands for
        /// initiating and controlling the actual data transfer. The WebSocket is directed back to the previous terminal once streaming
        /// is either completed or cancelled.
        /// </summary>
        /// <param name="result">The result to send. The body of the result will be sent as binary over the websocket.
        /// Additional inforation can be included in separate text messages (see other parameters).</param>
        /// <param name="messageSize">The size of each message, in bytes. Cannot be less than 512 or greater than 16000000</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included. If not null, timeElapsed 
        /// will be included in the status text message (see writeStatus)</param>
        /// <param name="writeHeaders">Should headers be included as a text message? If true, headers are printed after the status
        /// (if any) and before the content is sent.</param>
        /// <param name="disposeResult">Should the result be disposed after it is sent to the WebSocket?</param>
        void StreamResult(ISerializedResult result, int messageSize, TimeSpan? timeElapsed = null, bool writeHeaders = false, bool disposeResult = true);

        /// <summary>
        /// Sends an exception over the WebSocket.
        /// </summary>
        void SendException(Exception exception);

        /// <summary>
        /// The headers included in the initial HTTP request
        /// </summary>
        Headers Headers { get; }

        /// <summary>
        /// The cookies of the initial HTTP request
        /// </summary>
        ReadonlyCookies Cookies { get; }

        /// <summary>
        /// Closes the current terminal (if any) and directs the WebSocket to the Shell terminal. Use this to quit from a 
        /// terminal resource and launch the shell.
        /// </summary>
        void DirectToShell(IEnumerable<Condition<Shell>> assignments = null);

        /// <summary>
        /// Closes the current terminal (if any) and directs the WebSocket to the provided terminal. Use this to quit from a 
        /// terminal resource and open another terminal instead.
        /// </summary>
        void DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>> assignments = null) where T : class, ITerminal;

        /// <summary>
        /// The current status of this WebSocket
        /// </summary>
        WebSocketStatus Status { get; }
    }
}