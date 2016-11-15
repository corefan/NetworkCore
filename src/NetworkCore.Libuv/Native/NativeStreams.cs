// ReSharper disable InconsistentNaming

namespace NetworkCore.Libuv.Native
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Handles;
    using NetworkCore.Libuv.Requests;

    [StructLayout(LayoutKind.Sequential)]
    struct uv_buf_t
    {
        /*
           Windows 
           public int length;
           public IntPtr data;

           Unix
           public IntPtr data;
           public IntPtr length;
        */

        IntPtr first;
        IntPtr second;

        internal uv_buf_t(IntPtr memory, int length)
        {
            Contract.Requires(length >= 0);

            if (Platform.IsWindows)
            {
                this.first = (IntPtr)length;
                this.second = memory;
            }
            else
            {
                this.first = memory;
                this.second = (IntPtr)length;
            }
        }

        internal IntPtr Memory
        {
            get
            {
                return Platform.IsWindows 
                    ? this.second 
                    : this.first;
            }
            set
            {
                if (Platform.IsWindows)
                {
                    this.second = value;
                }
                else
                {
                    this.first = value;
                }
            }
        }

        internal int Length
        {
            get
            {
                return Platform.IsWindows 
                    ? this.first.ToInt32() 
                    : this.second.ToInt32();
            }
            set
            {
                Contract.Requires(value >= 0);

                if (Platform.IsWindows)
                {
                    this.first = (IntPtr)value;
                }
                else
                {
                    this.second = (IntPtr)value;
                }
            }
        } 
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_stream_t
    {
        /* handle fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_handle_type type;
        public IntPtr close_cb;

        /* stream fields */
        public IntPtr write_queue_size; /* number of bytes queued for writing */
        public IntPtr alloc_cb;
        public IntPtr read_cb;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_alloc_cb(IntPtr handle, IntPtr suggested_size, out uv_buf_t buf);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void uv_read_cb(IntPtr handle, IntPtr nread, ref uv_buf_t buf);

    static partial class NativeMethods
    {
        internal static void StreamReadStart(IntPtr handle) => 
            Invoke(uv_read_start, handle, StreamHandle.AllocateCallback, StreamHandle.ReadCallback);

        internal static void StreamReadStop(IntPtr handle) => 
            Invoke(uv_read_stop, handle);

        internal static bool IsStreamReadable(IntPtr handle) => 
            handle != IntPtr.Zero 
            && InvokeFunction(uv_is_readable, handle) == 1;

        internal static bool IsStreamWritable(IntPtr handle) => 
            handle != IntPtr.Zero 
            && InvokeFunction(uv_is_writable, handle) == 1;

        internal static void TryWriteStream(IntPtr handle, uv_buf_t buf)
        {
            var bufs = new [] { buf };
            Invoke(uv_try_write, handle , bufs, bufs.Length);
        }

        internal static void WriteStream(IntPtr requestHandle, IntPtr streamHandle, uv_buf_t buf)
        {
            if (streamHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid empty handle value.", nameof(streamHandle));
            }

            var bufs = new[] { buf };
            Invoke(uv_write, requestHandle, streamHandle, bufs, bufs.Length, WatcherRequest.WatcherCallback);
        }

        internal static void StreamListen(IntPtr handle, int backlog)
        {
            if (backlog < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(backlog));
            }

            Invoke(uv_listen, handle, backlog, StreamHandle.ConnectionCallback);
        }

        internal static void StreamAccept(IntPtr serverHandle, IntPtr clientHandle)
        {
            if (clientHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid client handle value", nameof(clientHandle));
            }

            Invoke(uv_accept, serverHandle, clientHandle);
        }

        #region Stream Status

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_listen(IntPtr handle, int backlog, uv_watcher_cb connection_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_accept(IntPtr server, IntPtr client);

        #endregion Stream Status

        #region Read/Write

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_is_readable(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_is_writable(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_read_start(IntPtr handle, uv_alloc_cb alloc_cb, uv_read_cb read_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_read_stop(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_try_write(IntPtr handle, uv_buf_t[] bufs, int bufcnt);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_write(IntPtr req, IntPtr handle, uv_buf_t[] bufs, int nbufs, uv_watcher_cb cb);

        #endregion
    }
}
