// ReSharper disable InconsistentNaming

namespace NetworkCore.Libuv.Native
{
    using System;
    using System.Runtime.InteropServices;
    using NetworkCore.Libuv.Requests;

    enum uv_req_type
    {
        UV_UNKNOWN_REQ = 0,
        UV_REQ,
        UV_CONNECT,
        UV_WRITE,
        UV_SHUTDOWN,
        UV_UDP_SEND,
        UV_FS,
        UV_WORK,
        UV_GETADDRINFO,
        UV_GETNAMEINFO,
        UV_REQ_TYPE_PRIVATE,
        UV_REQ_TYPE_MAX
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_req_t
    {
        public IntPtr data;
        public uv_req_type type;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_write_t
    {
        /* uv_req_t fields */
        public IntPtr data;
        public uv_req_type type;

        /* uv_write_t fields */

        // Write callback
        public IntPtr cb; // uv_write_cb cb;

        // Pointer to the stream being sent using this write request.
        public IntPtr send_handle;  // uv_stream_t* send_handle;

        // Pointer to the stream where this write request is running.
        public IntPtr handle; // uv_stream_t* handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_shutdown_t
    {
        /* uv_req_t fields */
        public IntPtr data;
        public uv_req_type type;

        public IntPtr handle;  // uv_stream_t*
        public IntPtr cb;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_work_t
    {
        /* uv_handle_t fields */
        public IntPtr data;
        public IntPtr loop;
        public uv_req_type type;
        public IntPtr close_cb;

        /* work fields */
        public IntPtr work_cb;
        public IntPtr after_work_cb;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct uv_connect_t
    {
        /* uv_req_t fields */
        public IntPtr data;
        public uv_req_type type;

        /* connect fields */
        public IntPtr cb; // uv_connect_cb
        public IntPtr handle; // uv_stream_t*
    }

    static partial class NativeMethods
    {
        internal static void Shutdown(IntPtr requestHandle, IntPtr streamHandle)
        {
            if (streamHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid stream handle value.", nameof(streamHandle));
            }

            Invoke(uv_shutdown, requestHandle, streamHandle, WatcherRequest.WatcherCallback);
        }

        internal static void Queue(IntPtr loopHandle, IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid work handle value.", nameof(handle));
            }

            Invoke(uv_queue_work, loopHandle, handle, Work.WorkCallback, Work.AfterWorkCallback);
        }

        internal static bool Cancel(IntPtr handle) => 
            InvokeFunction(uv_cancel, handle) == 0;

        internal static int GetSize(uv_req_type requestType)
        {
            IntPtr value = uv_req_size(requestType);
            int size = value.ToInt32();
            if (size <= 0)
            {
                throw new InvalidOperationException($"Request {requestType} size must be greater than zero.");
            }

            return size;
        }

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_queue_work(IntPtr loopHandle, IntPtr handle, uv_work_cb work_cb, uv_watcher_cb after_work_cb);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_cancel(IntPtr handle);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern int uv_shutdown(IntPtr requestHandle, IntPtr streamHandle, uv_watcher_cb callback);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr uv_req_size(uv_req_type reqType);
    }
}
