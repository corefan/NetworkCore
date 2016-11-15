namespace NetworkCore.Libuv.Tests
{
    using System;
    using System.Threading.Tasks;
    using NetworkCore.Libuv.Handles;
    using Xunit;

    public sealed class AsyncTests : IDisposable
    {
        Loop loop;
        Prepare prepare;
        Async async;
        Task task;

        int prepareCalled;
        int asyncCalled;

        void AsyncSend()
        {
            while (this.asyncCalled < 3)
            {
                this.async.Send();
            }
        }

        void PrepareCallback(Prepare handle)
        {
            if (handle != null
                && this.prepareCalled == 0)
            {
                this.prepareCalled++;

                this.task = Task.Run(() => this.AsyncSend());
            }
        }

        void AsyncCallback(Async handle)
        {
            if (handle != null)
            {
                int n = ++this.asyncCalled;

                if (n == 3)
                {
                    this.prepare.Dispose();
                    this.async.Dispose();
                }
            }
        }

        [Fact]
        public void Async()
        {
            this.loop = new Loop();
            this.prepareCalled = 0;
            this.asyncCalled = 0;

            this.prepare = this.loop.CreatePrepare();
            this.prepare.Start(this.PrepareCallback);

            this.async = this.loop.CreateAsync(this.AsyncCallback);

            this.loop.RunDefault();

            Assert.True(this.task.IsCompleted);
            Assert.True(this.prepareCalled > 0, "Prepare callback should be called at least once.");
            Assert.Equal(3, this.asyncCalled);
        }

        public void Dispose()
        {
            this.loop?.Dispose();
            this.loop = null;
        }
    }
}
