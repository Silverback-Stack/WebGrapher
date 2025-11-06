using System;

namespace Normalisation.Core
{
    /// <summary>
    /// Represents an error that occurs during a normalisation pipeline step.
    /// </summary>
    public class NormalisationException : Exception
    {
        /// <summary>
        /// Creates a new NormalisationStepException with a client-friendly message and optional inner exception.
        /// </summary>
        /// <param name="message">Message safe to show to client.</param>
        /// <param name="innerException">The original exception causing this error.</param>
        public NormalisationException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
            //values are passed via the base during construction
        }
    }
}
