namespace RESTar.Resources.Operations
{
    /// <summary>
    /// Contains the results of a call to IsAuthenticated()
    /// </summary>
    public struct AuthResults
    {
        internal readonly bool Success;
        internal readonly string Reason;

        /// <summary>
        /// Creates a new AuthenticationResults with the given success status and reason.
        /// Reason is only used when success is false.
        /// </summary>
        public AuthResults(bool success, string reason = null)
        {
            Success = success;
            Reason = reason;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        public static implicit operator AuthResults((bool success, string reason) input)
        {
            return new AuthResults(input.success,input.reason);
        }
    }
}