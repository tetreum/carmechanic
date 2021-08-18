using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Unity.Cloud
{
    /// <summary>
    ///     Represents a serializable exception.
    /// </summary>
    public class SerializableException
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="SerializableException" /> class.
        /// </summary>
        public SerializableException()
        {
            // Empty
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="SerializableException" /> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public SerializableException(Exception exception)
        {
            // Message
            Message = exception.Message;

            // Full Text
            FullText = exception.ToString();

            // Type
            var exceptionType = exception.GetType();
            Type = exceptionType.FullName;

            // Stack Trace
            StackTrace = new List<SerializableStackFrame>();
            var stackTrace = new StackTrace(exception, true);
            foreach (var stackFrame in stackTrace.GetFrames()) StackTrace.Add(new SerializableStackFrame(stackFrame));

            // Problem Identifier
            if (StackTrace.Count > 0)
            {
                var stackFrame = StackTrace[0];
                ProblemIdentifier =
                    string.Format("{0} at {1}.{2}", Type, stackFrame.DeclaringType, stackFrame.MethodName);
            }
            else
            {
                ProblemIdentifier = Type;
            }

            // Detailed Problem Identifier
            if (StackTrace.Count > 1)
            {
                var stackFrame1 = StackTrace[0];
                var stackFrame2 = StackTrace[1];
                DetailedProblemIdentifier = string.Format("{0} at {1}.{2} from {3}.{4}", Type,
                    stackFrame1.DeclaringType, stackFrame1.MethodName, stackFrame2.DeclaringType,
                    stackFrame2.MethodName);
            }

            // Inner Exception
            if (exception.InnerException != null) InnerException = new SerializableException(exception.InnerException);
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the detailed problem identifier.
        /// </summary>
        public string DetailedProblemIdentifier { get; set; }

        /// <summary>
        ///     Gets or sets the full text.
        /// </summary>
        public string FullText { get; set; }

        /// <summary>
        ///     Gets or sets the inner exception.
        /// </summary>
        public SerializableException InnerException { get; set; }

        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     Gets or sets the problem identifier.
        /// </summary>
        public string ProblemIdentifier { get; set; }

        /// <summary>
        ///     Gets or sets the stack trace.
        /// </summary>
        public List<SerializableStackFrame> StackTrace { get; set; }

        /// <summary>
        ///     Gets or sets the type.
        /// </summary>
        public string Type { get; set; }

        #endregion
    }
}