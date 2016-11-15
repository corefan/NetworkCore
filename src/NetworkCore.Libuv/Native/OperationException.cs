namespace NetworkCore.Libuv.Native
{
    using System;
    using System.Diagnostics;

    public sealed class OperationException : Exception
    {
        public OperationException(
            int errorCode, 
            string errorName, 
            string description)
            : base(description)
        {
            this.ErrorCode = errorCode;
            this.ErrorName = errorName;
            var stackTrace = new StackTrace(this, true);
            this.StackTrace = stackTrace.ToString();
        }

        public int ErrorCode { get; }

        public string ErrorName { get; }

        public override string StackTrace { get; }
    }
}
