using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AngryLevelLoader.Extensions
{
	public static class AsyncExtensions
	{
		public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
		{
			/*if (asyncOp.isDone)
			{
				var instantReturn = new TaskCompletionSource<object>();
				instantReturn.SetResult(null);
				return ((Task)instantReturn.Task).GetAwaiter();
			}*/

			var tcs = new TaskCompletionSource<object>();
			asyncOp.completed += obj => { tcs.SetResult(null); };
			return ((Task)tcs.Task).GetAwaiter();
		}

		public static TaskAwaiter GetAwaiter<T>(this AsyncOperationHandle<T> asyncOp)
		{
			/*if (asyncOp.IsDone)
			{
				var instantReturn = new TaskCompletionSource<object>();
				instantReturn.SetResult(null);
				return ((Task)instantReturn.Task).GetAwaiter();
			}*/

			var tcs = new TaskCompletionSource<object>();
			asyncOp.Completed += obj => { tcs.SetResult(null); };
			return ((Task)tcs.Task).GetAwaiter();
		}

		public static TaskAwaiter GetAwaiter(this AsyncOperationHandle asyncOp)
		{
			/*if (asyncOp.IsDone)
			{
				var instantReturn = new TaskCompletionSource<object>();
				instantReturn.SetResult(null);
				return ((Task)instantReturn.Task).GetAwaiter();
			}*/

			var tcs = new TaskCompletionSource<object>();
			asyncOp.Completed += obj => { tcs.SetResult(null); };
			return ((Task)tcs.Task).GetAwaiter();
		}
	}
}
