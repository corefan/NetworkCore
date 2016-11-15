namespace NetworkCore.Libuv.Common
{
    using System;
    using System.Threading;

    sealed class Gate
    {
        const int Busy = 1;
        const int Free = 0;

        readonly Guard guard;
        long state;

        internal Gate()
        {
            this.state = Free;
            this.guard = new Guard(this);
        }

        internal IDisposable Aquire()
        {
            while (Interlocked.CompareExchange(ref this.state, Busy, Free) != Free) { /* Aquire */ }
            return this.guard;
        }

        void Release() =>
            Interlocked.Exchange(ref this.state, Free);

        struct Guard : IDisposable
        {
            readonly Gate gate;

            internal Guard(Gate gate)
            {
                this.gate = gate;
            }

            public void Dispose() => this.gate.Release();
        }
    }
}
