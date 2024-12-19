using System.Collections.Concurrent;

namespace AccessControl.Application.Services.BackgroundTaskService
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<IServiceProvider, Task>> _workItems = new();
        private readonly SemaphoreSlim _signal = new(0);

        public void QueueBackgroundWorkItem(Func<IServiceProvider, Task> workItem)
        {
            if (workItem != null)
            {
                _workItems.Enqueue(workItem);
                _signal.Release();
            }
            else
            {
                throw new ArgumentNullException(nameof(workItem));
            }
        }

        public async Task<Func<IServiceProvider, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem!;
        }
    }
}
