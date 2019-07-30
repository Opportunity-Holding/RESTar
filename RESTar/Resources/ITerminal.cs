using System;
using RESTar.WebSockets;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// ITerminal defines the functionality of a RESTar terminal. The terminal will be
    /// made available as a WebSocket endpoint in the API, and each ITerminal type will be
    /// instantiated, using the Create method, when targeted in a request or from the 
    /// RESTar WebSocket shell.
    /// </summary>
    public interface ITerminal : IDisposable
    {
        /// <summary>
        /// The WebSocket connected to this terminal. This property is set just before the 
        /// WebSocket is opened.
        /// </summary>
        IWebSocket WebSocket { set; }

        /// <summary>
        /// This method is called when the WebSocket is opened, and when data can be sent
        /// and received by this terminal.
        /// </summary>
        void Open();

        /// <summary>
        /// Performs an action on string input
        /// </summary>
        void HandleTextInput(string input);

        /// <summary>
        /// Performs and action on binary input
        /// </summary>
        void HandleBinaryInput(byte[] input);

        /// <summary>
        /// Does this terminal support text input?
        /// </summary>
        /// <returns></returns>
        bool SupportsTextInput { get; }

        /// <summary>
        /// Does this terminal support binary input?
        /// </summary>
        /// <returns></returns>
        bool SupportsBinaryInput { get; }

    }
}