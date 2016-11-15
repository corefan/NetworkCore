// ReSharper disable InconsistentNaming

namespace NetworkCore.Libuv.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Requests;

    enum uv_handle_type
    {
        UV_UNKNOWN_HANDLE = 0,
        UV_ASYNC,
        UV_CHECK,
        UV_FS_EVENT,
        UV_FS_POLL,
        UV_HANDLE,
        UV_IDLE,
        UV_NAMED_PIPE,
        UV_POLL,
        UV_PREPARE,
        UV_PROCESS,
        UV_STREAM,
        UV_TCP,
        UV_TIMER,
        UV_TTY,
        UV_UDP,
        UV_SIGNAL,
        UV_FILE,
        UV_HANDLE_TYPE_MAX
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_handle_t
    {
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_prepare_t
    {
        /* uv_handle_t fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        /* prepare fields */
        public IntPtr prepare_prev;
        public IntPtr prepare_next;
        public IntPtr prepare_cb;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_check_t
    {
        /* uv_handle_t fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        /* prepare fields */
        public IntPtr check_prev;
        public IntPtr check_next;
        public IntPtr uv_check_cb;
    }

    /// <summary>
    /// https://github.com/aspnet/KestrelHttpServer/blob/dev/src/Microsoft.AspNetCore.Server.Kestrel/Internal/Networking/SockAddr.cs
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct sockAddr
    {
        // this type represents native memory occupied by sockaddr struct
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms740496(v=vs.85).aspx
        // although the c/c++ header defines it as a 2-byte short followed by a 14-byte array,
        // the simplest way to reserve the same size in c# is with four nameless long values
        public long field0;
        public long field1;
        public long field2;
        public long field3;

        // ReSharper disable once UnusedParameter.Local
        internal sockAddr(long ignored)
        {
            this.field0 = 0;
            this.field1 = 0;
            this.field2 = 0;
            this.field3 = 0;
        }

        internal unsafe IPEndPoint GetIPEndPoint()
        {
            // The bytes are represented in network byte order.
            //
            // Example 1: [2001:4898:e0:391:b9ef:1124:9d3e:a354]:39179
            //
            // 0000 0000 0b99 0017  => The third and fourth bytes 990B is the actual port
            // 9103 e000 9848 0120  => IPv6 address is represented in the 128bit field1 and field2.
            // 54a3 3e9d 2411 efb9     Read these two 64-bit long from right to left byte by byte.
            // 0000 0000 0000 0000
            //
            // Example 2: 10.135.34.141:39178 when adopt dual-stack sockets, IPv4 is mapped to IPv6
            //
            // 0000 0000 0a99 0017  => The port representation are the same
            // 0000 0000 0000 0000
            // 8d22 870a ffff 0000  => IPv4 occupies the last 32 bit: 0A.87.22.8d is the actual address.
            // 0000 0000 0000 0000
            //
            // Example 3: 10.135.34.141:12804, not dual-stack sockets
            //
            // 8d22 870a fd31 0002  => sa_family == AF_INET (02)
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            // 0000 0000 0000 0000
            //
            // Example 4: 127.0.0.1:52798, on a Mac OS
            //
            // 0100 007F 3ECE 0210  => sa_family == AF_INET (02) Note that struct sockaddr on mac use
            // 0000 0000 0000 0000     the second unint8 field for sa family type
            // 0000 0000 0000 0000     http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h
            // 0000 0000 0000 0000
            //
            // Reference:
            //  - Windows: https://msdn.microsoft.com/en-us/library/windows/desktop/ms740506(v=vs.85).aspx
            //  - Linux: https://github.com/torvalds/linux/blob/6a13feb9c82803e2b815eca72fa7a9f5561d7861/include/linux/socket.h
            //  - Apple: http://www.opensource.apple.com/source/xnu/xnu-1456.1.26/bsd/sys/socket.h

            // Quick calculate the port by mask the field and locate the byte 3 and byte 4
            // and then shift them to correct place to form a int.
            int port = ((int)(this.field0 & 0x00FF0000) >> 8) | (int)((this.field0 & 0xFF000000) >> 24);

            int family = (int)this.field0;
            if (Platform.IsMac)
            {
                // see explaination in example 4
                family = family >> 8;
            }
            family = family & 0xFF;

            if (family == 2)
            {
                // AF_INET => IPv4
                return new IPEndPoint(new IPAddress((this.field0 >> 32) & 0xFFFFFFFF), port);
            }
            else if (this.IsIPv4MappedToIPv6())
            {
                long ipv4bits = (this.field2 >> 32) & 0x00000000FFFFFFFF;
                return new IPEndPoint(new IPAddress(ipv4bits), port);
            }
            else
            {
                // otherwise IPv6
                var bytes = new byte[16];
                fixed (byte* b = bytes)
                {
                    *((long*)b) = this.field1;
                    *((long*)(b + 8)) = this.field2;
                }

                return new IPEndPoint(new IPAddress(bytes), port);
            }
        }

        bool IsIPv4MappedToIPv6()
        {
            // If the IPAddress is an IPv4 mapped to IPv6, return the IPv4 representation instead.
            // For example [::FFFF:127.0.0.1] will be transform to IPAddress of 127.0.0.1
            if (this.field1 != 0)
            {
                return false;
            }

            return (this.field2 & 0xFFFFFFFF) == 0xFFFF0000;
        }
    }

    static partial class NativeMethods
    {
        internal static HandleContext Initialize(IntPtr loopHandle, uv_handle_type handleType, ScheduleHandle target)
        {
            Action<IntPtr> action;
            switch (handleType)
            {
                case uv_handle_type.UV_TIMER:
                    action = handle => Invoke(uv_timer_init, loopHandle, handle);
                    break;
                case uv_handle_type.UV_PREPARE:
                    action = handle => Invoke(uv_prepare_init, loopHandle, handle);
                    break;
                case uv_handle_type.UV_CHECK:
                    action = handle => Invoke(uv_check_init, loopHandle, handle);
                    break;
                case uv_handle_type.UV_IDLE:
                    action = handle => Invoke(uv_idle_init, loopHandle, handle);
                    break;
                case uv_handle_type.UV_ASYNC:
                    action = handle => Invoke(uv_async_init, loopHandle, handle,
                        WorkHandle.WorkCallback);
                    break;
                case uv_handle_type.UV_TCP:
                    action = handle => Invoke(uv_tcp_init, loopHandle, handle);
                    break;
                default:
                    throw new NotSupportedException($"Handle type to initialize {handleType} not supported");
            }

            return new HandleContext(handleType, action, target);
        }

        internal static void Start(uv_handle_type handleType, IntPtr handle, uv_work_cb callback)
        {
            switch (handleType)
            {
                case uv_handle_type.UV_PREPARE:
                    Invoke(uv_prepare_start, handle, callback);
                    break;
                case uv_handle_type.UV_CHECK:
                    Invoke(uv_check_start, handle, callback);
                    break;
                case uv_handle_type.UV_IDLE:
                    Invoke(uv_idle_start, handle, callback);
                    break;
                default:
                    throw new NotSupportedException($"Handle type to start {handleType} not supported");
            }

            Log.Debug($"{handleType} {handle} started.");
        }

        internal static void Stop(uv_handle_type handleType, IntPtr handle)
        {
            switch (handleType)
            {
                case uv_handle_type.UV_TIMER:
                    Invoke(uv_timer_stop, handle);
                    break;
                case uv_handle_type.UV_PREPARE:
                    InvokeAction(uv_prepare_stop, handle);
                    break;
                case uv_handle_type.UV_CHECK:
                    InvokeFunction(uv_check_stop, handle);
                    break;
                case uv_handle_type.UV_IDLE:
                    InvokeFunction(uv_idle_stop, handle);
                    break;
                default:
                    throw new NotSupportedException($"Handle type to stop {handleType} not supported");
            }

            Log.Debug($"{handleType} {handle} stopped.");
        }

        internal static int GetSize(uv_handle_type handleType)
        {
            IntPtr value = uv_handle_size(handleType);
            int size = value.ToInt32();
            if (size <= 0)
            {
                throw new InvalidOperationException($"Handle {handleType} size must be greater than zero.");
            }

            return size;
        }

        #region TCP

        internal static void TcpSetNoDelay(IntPtr handle, bool value) => 
            Invoke(uv_tcp_nodelay, handle, value ? 1 : 0);

        internal static void TcpSetKeepAlive(IntPtr handle, bool value, int delay) => 
            Invoke(uv_tcp_keepalive, handle, value ? 1: 0, delay);

        internal static void TcpSimultaneousAccepts(IntPtr handle, bool value) => 
            Invoke(uv_tcp_simultaneous_accepts, handle, value ? 1 : 0);

        internal static void TcpBind(IntPtr handle, IPEndPoint endPoint, bool dualStack /* Both IPv4 & IPv6 */)
        {
            Contract.Requires(endPoint != null);

            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid stream handle value", nameof(handle));
            }

            string ip = endPoint.Address.ToString();

            // IPv4
            if (endPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                sockAddr addr;
                int result = uv_ip4_addr(ip, endPoint.Port, out addr);
                ThrowIfError(result);

                result = uv_tcp_bind(handle, ref addr, (uint)(dualStack ? 1 : 0));
                ThrowIfError(result);

                return;
            }

            // IPv6
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                sockAddr addr;
                int result = uv_ip6_addr(ip, endPoint.Port, out addr);
                ThrowIfError(result);

                result = uv_tcp_bind(handle, ref addr, (uint)(dualStack ? 1 : 0));
                ThrowIfError(result);

                return;
            }

            throw new NotSupportedException(
                $"End point {endPoint} is not supported, expecting InterNetwork/InterNetworkV6.");
        }

        internal static void TcpConnect(IntPtr requestHandle, IntPtr handle, IPEndPoint endPoint)
        {
            Contract.Requires(endPoint != null);

            if (requestHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid request handle value", nameof(requestHandle));
            }
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid stream handle value", nameof(handle));
            }
            string ip = endPoint.Address.ToString();

            // IPv4
            if (endPoint.AddressFamily == AddressFamily.InterNetwork)
            {
                sockAddr addr;
                int result = uv_ip4_addr(ip, endPoint.Port, out addr);
                ThrowIfError(result);

                result = uv_tcp_connect(requestHandle, handle, ref addr, WatcherRequest.WatcherCallback);
                ThrowIfError(result);

                return;
            }

            // IPv6
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                sockAddr address;
                int result = uv_ip6_addr(ip, endPoint.Port, out address);
                ThrowIfError(result);

                result = uv_tcp_connect(requestHandle, handle, ref address, WatcherRequest.WatcherCallback);
                ThrowIfError(result);

                return;
            }

            throw new NotSupportedException(
                $"End point {endPoint} is not supported, expecting InterNetwork/InterNetworkV6.");
        }

        internal static IPEndPoint TcpGetSocketName(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid stream handle value", nameof(handle));
            }

            sockAddr sockAddr;
            int namelen = Marshal.SizeOf<sockAddr>();
            uv_tcp_getsockname(handle, out sockAddr, ref namelen);

            return sockAddr.GetIPEndPoint();
        }

        internal static IPEndPoint TcpGetPeerName(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid stream handle value", nameof(handle));
            }

            sockAddr sockAddr;
            int namelen = Marshal.SizeOf<sockAddr>();
            uv_tcp_getpeername(handle, out sockAddr, ref namelen);

            return sockAddr.GetIPEndPoint();
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_connect(IntPtr req, IntPtr handle, ref sockAddr sockaddr, uv_watcher_cb connect_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_bind(IntPtr handle, ref sockAddr sockaddr, uint flags);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_getsockname(IntPtr handle, out sockAddr sockaddr, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int uv_tcp_getpeername(IntPtr handle, out sockAddr name, ref int namelen);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_nodelay(IntPtr handle, int enable);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_keepalive(IntPtr handle, int enable, int delay);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_tcp_simultaneous_accepts(IntPtr handle, int enable);

        #endregion TCP

        #region Timer

        internal static void Start(IntPtr handle, uv_work_cb callback, long timeout, long repeat)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid empty handle value.");
            }

            Log.Debug($"UV_TIMER {handle} Timeout = {timeout}, Repeat = {repeat} starting");
            Invoke(uv_timer_start, handle, callback, timeout, repeat);
        }

        internal static void Again(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid empty handle value.");
            }

            Invoke(uv_timer_again, handle);
        }

        internal static void SetTimerRepeat(IntPtr handle, long repeat)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid empty handle value.");
            }

            InvokeAction(uv_timer_set_repeat, handle, repeat);
        }

        internal static long GetTimerRepeat(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid empty handle value.");
            }

            return InvokeFunction(uv_timer_get_repeat, handle);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_start(IntPtr handle, uv_work_cb work_cb, long timeout, long repeat);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_timer_again(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_timer_set_repeat(IntPtr handle, long repeat);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern long uv_timer_get_repeat(IntPtr handle);

        #endregion Timer

        #region Prepare

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_prepare_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_prepare_start(IntPtr handle, uv_work_cb prepare_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern void uv_prepare_stop(IntPtr handle);

        #endregion Prepare

        #region Check

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_check_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_check_start(IntPtr handle, uv_work_cb check_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_check_stop(IntPtr handle);

        #endregion Check

        #region Idle

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_idle_init(IntPtr loopHandle, IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_idle_start(IntPtr handle, uv_work_cb check_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_idle_stop(IntPtr handle);

        #endregion Idle

        #region Async

        internal static void Send(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Invalid empty handle value.");
            }

            Invoke(uv_async_send, handle);
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_async_init(IntPtr loopHandle, IntPtr handle, uv_work_cb async_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_async_send(IntPtr handle);

        #endregion Async

        #region Common

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_ip4_addr(string ip, int port, out sockAddr address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_ip6_addr(string ip, int port, out sockAddr address);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr uv_handle_size(uv_handle_type handleType);

        #endregion Common
    }
}
