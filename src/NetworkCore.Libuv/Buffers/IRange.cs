namespace NetworkCore.Libuv.Buffers
{
    using System.Collections.Generic;

    public interface IRange : IReadOnlyList<byte>
    {
        byte[] Copy();
    }
}
