namespace NetworkCore.Libuv.Handles
{
    using System;
    using NetworkCore.Libuv.Buffers;

    public interface IReadCompletion
    {
        IRange Data { get; }

        Exception Error { get; }
    }
}
