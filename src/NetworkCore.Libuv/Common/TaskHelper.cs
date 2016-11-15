namespace NetworkCore.Libuv.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading.Tasks;

    static class TaskHelper
    {
        internal static Task RunInline(Action action)
        {
            Contract.Requires(action != null);

            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        internal static Task RunInline<T>(Action<T> action, T arg)
        {
            Contract.Requires(action != null);

            try
            {
                action(arg);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        internal static void CompleteFrom(this TaskCompletionSource<object> completion, Task task)
        {
            Contract.Requires(completion != null);

            if (task == null
                || task.IsCompleted)
            {
                completion.TrySetResult(null);
                return;
            }

            if (task.IsFaulted)
            {
                completion.TryUnwrapException(task.Exception);
                return;
            }

            if (task.IsCanceled)
            {
                completion.TrySetCanceled();
                return;
            }
            
            throw new InvalidOperationException(
                $"Invalid task status {task.Status}, the task must be finished to set the completion source.");
        }

        internal static Task Run<T>(Action<T> action, T arg)
        {
            Contract.Requires(action != null);

            var completionSource = new TaskCompletionSource<object>();

            Task.Run(() => action.Invoke(arg))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        completionSource.UnwrapException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        completionSource.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            completionSource.SetResult(null);
                        }
                        catch (Exception exception)
                        {
                            completionSource.UnwrapException(exception);
                        }
                    }
                });

            return completionSource.Task;
        }

        internal static Task Run<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            Contract.Requires(action != null);

            var completionSource = new TaskCompletionSource<object>();

            Task.Run(() => action.Invoke(arg1, arg2))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        completionSource.UnwrapException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        completionSource.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            completionSource.SetResult(null);
                        }
                        catch (Exception exception)
                        {
                            completionSource.UnwrapException(exception);
                        }
                    }
                });

            return completionSource.Task;
        }

        internal static Task Run<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            Contract.Requires(action != null);

            var completionSource = new TaskCompletionSource<object>();

            Task.Run(() => action.Invoke(arg1, arg2, arg3))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        completionSource.UnwrapException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        completionSource.SetCanceled();
                    }
                    else
                    {
                        try
                        {
                            completionSource.SetResult(null);
                        }
                        catch (Exception exception)
                        {
                            completionSource.UnwrapException(exception);
                        }
                    }
                });

            return completionSource.Task;
        }

        internal static Task Then(this Task task, Action action)
        {
            Contract.Requires(task != null);
            Contract.Requires(action != null);

            var completionSource = new TaskCompletionSource<object>();
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    completionSource.UnwrapException(t.Exception);
                }
                else if (t.IsCanceled)
                {
                    completionSource.SetCanceled();
                }
                else
                {
                    try
                    {
                        action();
                        completionSource.SetResult(null);
                    }
                    catch (Exception exception)
                    {
                        completionSource.UnwrapException(exception);
                    }
                }
            });

            return completionSource.Task;
        }

        internal static void UnwrapException<T>(this TaskCompletionSource<T> completionSource, Exception exception)
        {
            Contract.Requires(completionSource != null);
            Contract.Requires(exception != null);

            completionSource.SetException(exception.UnwrapException());
        }

        internal static void TryUnwrapException<T>(this TaskCompletionSource<T> completionSource, Exception exception)
        {
            Contract.Requires(completionSource != null);
            Contract.Requires(exception != null);

            completionSource.TrySetException(exception.UnwrapException());
        }

        internal static IList<Exception> UnwrapException(this Exception exception)
        {
            Contract.Requires(exception != null);
            Contract.Ensures(Contract.Result<IList<Exception>>() != null);

            var exceptions = new List<Exception>();
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
            {
                exceptions.AddRange(aggregateException.InnerExceptions);
            }
            else
            {
                exceptions.Add(exception);
            }

            return exceptions;
        }

        internal static Task Finally(this Task task, Action<object> action, object state)
        {
            try
            {
                switch (task.Status)
                {
                    case TaskStatus.Faulted:
                    case TaskStatus.Canceled:
                        action(state);
                        return task;
                    case TaskStatus.RanToCompletion:
                        return RunInline(action, state);

                    default:
                        return RunTaskSynchronously(task, action, state, onlyOnSuccess: false);
                }
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        static Task RunTaskSynchronously(Task task, Action<object> action, object state, bool onlyOnSuccess = true)
        {
            var completionSource = new TaskCompletionSource<object>();
            task.ContinueWith(t =>
            {
                try
                {
                    if (t.IsFaulted)
                    {
                        if (!onlyOnSuccess)
                        {
                            action(state);
                        }

                        completionSource.UnwrapException(t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        if (!onlyOnSuccess)
                        {
                            action(state);
                        }

                        completionSource.SetCanceled();
                    }
                    else
                    {
                        action(state);
                        completionSource.SetResult(null);
                    }
                }
                catch (Exception exception)
                {
                    completionSource.UnwrapException(exception);
                }
            },
            TaskContinuationOptions.ExecuteSynchronously);

            return completionSource.Task;
        }
    }
}
