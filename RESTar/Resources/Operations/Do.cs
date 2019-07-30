using System;
using System.Threading.Tasks;

namespace RESTar.Resources.Operations
{
    /// <summary>
    /// The Do class provides static methods for task-related operations and 
    /// try/catch operations. It is provided as a utility for external assemblies.
    /// </summary>
    public static class Do
    {
        /// <summary>
        /// Returns the result of the function, or the onFail parameter if the 
        /// function invokation encounters any unhandled exceptions.
        /// </summary>
        public static T Try<T>(Func<T> function, T onfail)
        {
            try
            {
                return function();
            }
            catch
            {
                return onfail;
            }
        }

        /// <summary>
        /// Returns the result of the function, or the default for T if the action 
        /// encounters any unhandled exceptions.
        /// </summary>
        public static T SafeGet<T>(Func<T> function)
        {
            try
            {
                return function();
            }
            catch
            {   
                return default;
            }
        }

        /// <summary>
        /// Returns the result of the function, or the result of the onFail function 
        /// if the function encounters any unhandled exceptions.
        /// </summary>
        public static T Try<T>(Func<T> function, Func<T> onFail)
        {
            try
            {
                return function();
            }
            catch
            {
                return onFail();
            }
        }

        /// <summary>
        /// A functional programming approach to try/catch. Tries to return the result 
        /// of the try function, else runs the given action on the encountered exception.
        /// </summary>
        public static T TryCatch<T, TException>(Func<T> @try, Action<TException> @catch) where TException : Exception
        {
            try
            {
                return @try();
            }
            catch (TException e)
            {
                @catch(e);
                return default;
            }
        }

        /// <summary>
        /// A functional programming approach to try/catch. Tries to run the try action, 
        /// else runs the given action on the encountered exception.
        /// </summary>
        public static void TryCatch<TException>(Action @try, Action<TException> @catch) where TException : Exception
        {
            try
            {
                @try();
            }
            catch (TException e)
            {
                @catch(e);
            }
        }

        /// <summary>
        /// Tries to run the function, else throws a new exception with the provided message
        /// and the original exception as InnerException
        /// </summary>
        public static T TryAndThrow<T>(Func<T> function, string message)
        {
            try
            {
                return function();
            }
            catch (Exception e)
            {
                throw new Exception(message, e);
            }
        }

        /// <summary>
        /// Tries to run the action, and simply returns when encountering an unhandled exception
        /// </summary>
        /// <param name="action"></param>
        public static void Try(Action action)
        {
            try
            {
                action();
            }
            catch { }
        }

        /// <summary>
        /// Runs an action after a given delay. Uses Scheduling.ScheduleTask to ensure proper 
        /// Starcounter thread handling.
        /// </summary>
        public static async Task Schedule(Action action, TimeSpan delay)
        {
            await Task.Delay(delay);
            await Task.Run(action);
        }
    }
}