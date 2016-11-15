namespace NetworkCore.Libuv.Native
{
    using System;
    using NetworkCore.Libuv.Logging;

    abstract class NativeHandle : IDisposable
    {
        protected static readonly ILog Log = LogFactory.ForContext<NativeHandle>();

        protected NativeHandle()
        {
            this.Handle = IntPtr.Zero;
        }

        protected internal IntPtr Handle
        {
            get;
            protected set;
        }

        internal bool IsValid => this.Handle != IntPtr.Zero;

        protected internal void Validate()
        {
            if (this.IsValid)
            {
                return;
            }

            throw new ObjectDisposedException($"{nameof(NativeHandle)} has already been disposed");
        }

        internal void SetHandleAsInvalid() => this.Handle = IntPtr.Zero;

        protected internal abstract void CloseHandle();

        void Dispose(bool diposing)
        {
            try
            {
                if (this.IsValid)
                {
                    Log.Debug($"Disposing {this.Handle} (Finalizer {!diposing})");
                    this.CloseHandle();
                }
            }
            catch (Exception exception) 
            {
                Log.Error($"{nameof(NativeHandle)} {this.Handle} error whilst closing handle.", exception);

                // For finalizer, we cannot allow this to escape.
                if (diposing) throw;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NativeHandle()
        {
            this.Dispose(false);
        }
    }
}
