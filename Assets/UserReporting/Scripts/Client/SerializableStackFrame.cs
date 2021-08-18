using System.Diagnostics;

namespace Unity.Cloud
{
    /// <summary>
    ///     Represents a serializable stack frame.
    /// </summary>
    public class SerializableStackFrame
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="SerializableStackFrame" /> class.
        /// </summary>
        public SerializableStackFrame()
        {
            // Empty
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="SerializableStackFrame" /> class.
        /// </summary>
        /// <param name="stackFrame">The stack frame.</param>
        public SerializableStackFrame(StackFrame stackFrame)
        {
            var method = stackFrame.GetMethod();
            var declaringType = method.DeclaringType;
            DeclaringType = declaringType != null ? declaringType.FullName : null;
            Method = method.ToString();
            MethodName = method.Name;
            FileName = stackFrame.GetFileName();
            FileLine = stackFrame.GetFileLineNumber();
            FileColumn = stackFrame.GetFileColumnNumber();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the declaring type.
        /// </summary>
        public string DeclaringType { get; set; }

        /// <summary>
        ///     Gets or sets the file column.
        /// </summary>
        public int FileColumn { get; set; }

        /// <summary>
        ///     Gets or sets the file line.
        /// </summary>
        public int FileLine { get; set; }

        /// <summary>
        ///     Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///     Gets or sets the method.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///     Gets or sets the method name.
        /// </summary>
        public string MethodName { get; set; }

        #endregion
    }
}