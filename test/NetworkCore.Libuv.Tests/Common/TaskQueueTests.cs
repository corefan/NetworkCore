namespace NetworkCore.Libuv.Tests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NetworkCore.Libuv.Common;
    using Xunit;

    public sealed class TaskQueueTests
    {
        [Fact]
        public void Order()
        {
            const int TaskCount = 10;

            var taskQueue = new TaskQueue();
            var counter = new List<int>();
            int n = 0;
            Action action = () =>
            {
                counter.Add(n++);
            };

            Task task = null;
            for (int i = 0; i < TaskCount; i++)
            {
                task = taskQueue.Enqueue(action);
            }

            Assert.NotNull(task);
            bool result = task.Wait(TimeSpan.FromMilliseconds(100));
            Assert.True(result);

            Assert.Equal(n, TaskCount);
            Assert.Equal(counter.Count, TaskCount);
            for (int i = 0; i < counter.Count; i++)
            {
                Assert.Equal(i, counter[i]);
            }
        }

        [Fact]
        public void DisposedShouldThrow()
        {
            var taskQueue = new TaskQueue();
            taskQueue.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(() => taskQueue.Enqueue(() => DateTime.UtcNow)).Wait();
        }
    }
}
