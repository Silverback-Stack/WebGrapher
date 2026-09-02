using System;

namespace Normalisation.Core.Processors
{
    /// <summary>
    /// Represents an error that occurs while processing HTML.
    /// </summary>
    public class HtmlProcessorException : Exception
    {
        /// <summary>
        /// Creates a new HtmlProcessorException with a friendly client message and optional inner exception.
        /// </summary>
        /// <param name="message">Message safe to show to client.</param>
        /// <param name="innerException">The original exception causing this error.</param>
        public HtmlProcessorException(
            string message, 
            Exception? innerException = null) : base(message, innerException)
        {
            // Values are passed via the base during construction
        }
    }
}
