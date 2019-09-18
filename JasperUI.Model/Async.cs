using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace BingLibrary.hjb.tools
{
    public static class Async
    {
        public static async void RunFuncAsync(Action function, Action callback)
        {
            await ((Func<Task>)(() =>
            {
                return Task.Run(() =>
                {
                    function();
                });
            }))();
            callback?.Invoke();
        }

        public static async void RunFuncAsync<TResult>(Func<TResult> function, Action<TResult> callback)
        {
            TResult rlt = await ((Func<Task<TResult>>)(() =>
            {
                return Task.Run(() =>
                {
                    return function();
                });
            }))();
            callback?.Invoke(rlt);
        }

        public static T RunFuncAsync<T>(T v, object p)
        {
            throw new NotImplementedException();
        }

        public static async Task RunFuncAsyncTimeout(this Task task, TimeSpan timeout, string msg = "")
        {
            var delay = Task.Delay(timeout);
            if (await Task.WhenAny(task, delay) == delay)
            {
                throw new TimeoutException(msg);
            }
        }

        public static async Task<T> RunFuncAsyncTimeout<T>(this Task<T> task, TimeSpan timeout, string msg = "")
        {
            await ((Task)task).RunFuncAsyncTimeout(timeout, msg);
            return await task;
        }

    }

    class AsyncSemaphore
    {
        private readonly static Task s_completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> m_waiters = new Queue<TaskCompletionSource<bool>>();
        private int m_currentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0) throw new ArgumentOutOfRangeException("initialCount");
            m_currentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (m_waiters)
            {
                if (m_currentCount > 0)
                {
                    --m_currentCount;
                    return s_completed;
                }
                else
                {
                    var waiter = new TaskCompletionSource<bool>();
                    m_waiters.Enqueue(waiter);
                    return waiter.Task;
                }
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_waiters)
            {
                if (m_waiters.Count > 0)
                    toRelease = m_waiters.Dequeue();
                else
                    ++m_currentCount;
            }
            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }

    public class AsyncLock
    {
        private readonly AsyncSemaphore m_semaphore;
        private readonly Task<Releaser> m_releaser;

        public AsyncLock()
        {
            m_semaphore = new AsyncSemaphore(1);
            m_releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            var wait = m_semaphore.WaitAsync();
            return wait.IsCompleted ?
                m_releaser :
                wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                    this, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        public struct Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;

            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }

            public void Dispose()
            {
                if (m_toRelease != null)
                    m_toRelease.m_semaphore.Release();
            }
        }
    }

    //readonly AsyncLock m_lock = new AsyncLock(); 
    //using (var releaser = await m_lock.LockAsync())
    //{
    //    await FileIO.WriteTextAsync(configureFile, jsonString);
    //}
}