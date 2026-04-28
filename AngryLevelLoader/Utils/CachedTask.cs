using System;
using System.Threading;
using System.Threading.Tasks;

namespace AngryLevelLoader.Utils
{
	// Helper classes for creation of singleton tasks. If a task is running, the currently
	// running task is returned instead of creating a new task. This is useful for tasks
	// concerned with IO operations.

	internal class CachedTask
	{
		private Func<Task> task;

		public CachedTask(Func<Task> task)
		{
			this.task = task;
		}

		private Task currentTask = null;
		public bool Running => currentTask != null && !currentTask.IsCompleted;

		public async Task GetTask(CancellationToken cancellationToken = default)
		{
			if (!Running)
			{
				currentTask = task();
			}

			await Task.Run(async () =>
			{
				await currentTask;
			}, cancellationToken);
		}
	}

	internal class CachedTask<T>
	{
		private Func<Task<T>> task;

		public CachedTask(Func<Task<T>> task)
		{
			this.task = task;
		}

		private Task<T> currentTask = null;
		public bool Running => currentTask != null && !currentTask.IsCompleted;

		public async Task<T> GetTask(CancellationToken cancellationToken = default)
		{
			if (!Running)
			{
				currentTask = task();
			}

			return await Task.Run(async () =>
			{
				return await currentTask;
			}, cancellationToken);
		}
	}
}
