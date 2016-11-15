namespace NetworkCore.Libuv.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using NetworkCore.Libuv.Logging;

    sealed class TaskQueue : IDisposable
    {
        static readonly ILog Log = LogFactory.ForContext<TaskQueue>();

        readonly TaskScheduler scheduler;
        readonly Queue<Task> queue;
        readonly Gate gate;

        volatile bool disposed;

        internal TaskQueue()
            : this(TaskScheduler.Default)
        { }

        internal TaskQueue(TaskScheduler scheduler)
        {
            Contract.Requires(scheduler != null);

            this.scheduler = scheduler;
            this.gate = new Gate();
            this.disposed = false;
            this.queue = new Queue<Task>();
        }

        internal Task Enqueue(Action action, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(action != null);

            Task task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }

                task = new Task(action, cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task Enqueue(Action<object> action, object state, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(action != null);

            Task task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }

                task = new Task(action, state, cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task Enqueue<T>(Action<T> action, T arg, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(action != null);

            Task task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }

                task = new Task(() => action.Invoke(arg), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task Enqueue<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(action != null);

            Task task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task(() => action.Invoke(arg1, arg2), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task Enqueue<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(action != null);

            Task task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task(() => action.Invoke(arg1, arg2, arg3), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task Enqueue<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(action != null);

            Task task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task(() => action.Invoke(arg1, arg2, arg3, arg4), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task<T> Enqueue<T>(Func<T> function,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(function != null);

            Task<T> task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task<T>(function, cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task<T> Enqueue<T1, T>(Func<T1, T> function, T1 arg,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(function != null);

            Task<T> task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task<T>(() => function.Invoke(arg), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task<T> Enqueue<T1, T2, T>(Func<T1, T2, T> function, T1 arg1, T2 arg2,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(function != null);

            Task<T> task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task<T>(() => function.Invoke(arg1, arg2), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        internal Task<T> Enqueue<T1, T2, T3, T>(Func<T1, T2, T3, T> function, T1 arg1, T2 arg2, T3 arg3,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.Requires(function != null);

            Task<T> task;
            using (this.gate.Aquire())
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(nameof(TaskQueue));
                }
                task = new Task<T>(() => function.Invoke(arg1, arg2, arg3), cancellationToken);
                this.Enqueue(task);
            }

            return task;
        }

        void Enqueue(Task task)
        {
            Contract.Assert(task != null);

            this.queue.Enqueue(task);
            int size = this.queue.Count;
            Log.Debug($"{nameof(TaskQueue)} Enqueue task, size = {size}.");
            if (this.queue.Count > 1)
            {
                return;
            }

            // The queue is empty, needs to kick off manually
            var next = new Task(() => this.Next(null));
            next.Start(this.scheduler);
        }

        void Next(Task previousTask)
        {
            Task task;
            int size;
            using (this.gate.Aquire())
            {
                if (previousTask != null)
                {
                    task = this.queue.Dequeue();
                    Contract.Assert(task == previousTask);
                }
                size = this.queue.Count;
                task = size > 0 && !this.disposed 
                    ? this.queue.Peek() 
                    : null;
            }

            Log.Debug($"{nameof(TaskQueue)} Next, size = {size}.");
            if (task == null)
            {
                return;
            }
            task.ContinueWith(this.Next);
            task.Start(this.scheduler);
        }

        public void Dispose()
        {
            using (this.gate.Aquire())
            {
                this.queue.Clear();
                this.disposed = true;
            }
            Log.Debug($"{nameof(TaskQueue)} disposed.");
        }
    }
}
