namespace NetworkCore.Libuv.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;

    public static class RangeExtensions
    {
        public static string GetString(this IReadOnlyList<byte> bytes, Encoding encoding)
        {
            Contract.Requires(bytes != null);
            Contract.Requires(encoding != null);

            if (bytes is BlockRange)
            {
                return GetString((BlockRange)bytes, encoding);
            }

            return bytes.Count > 0 
                ? encoding.GetString(bytes.ToArray()) 
                : null;
        }

        internal static unsafe string GetString(BlockRange range, Encoding encoding)
        {
            Contract.Requires(encoding != null);

            if (range.Count == 0)
            {
                return null;
            }

            var handle = (IntPtr)range;
            return encoding.GetString((byte*)handle, range.Count);
        }
    }
}
