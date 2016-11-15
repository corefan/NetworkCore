namespace NetworkCore.Libuv.Common
{
    using System.Runtime.CompilerServices;

    static class BitOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RightUShift(this int value, int bits) => unchecked((int)((uint)value >> bits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RightUShift(this long value, int bits) => unchecked((long)((ulong)value >> bits));
    }
}
