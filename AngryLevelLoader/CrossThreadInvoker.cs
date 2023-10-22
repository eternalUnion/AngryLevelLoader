using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;
using System;
using UnityEngine;

/*
	Events which are triggered from another thread (etc. FileSystemWatcher) cannot use Unity API
	It has to be run on the main thread
	This script just does that
 */

// https://gist.github.com/JakubNei/90bf21be3fdc4829e631
public class CrossThreadInvoker : ISynchronizeInvoke
{
	private static CrossThreadInvoker instance;
	public static CrossThreadInvoker Instance => instance;

	private static GameObject backgroundUpdater;
	private class BackgroundUpdater : MonoBehaviour
	{
		public void Update()
		{
			ProcessQueue();
		}
	}

	public static void Init()
	{
		if (instance != null)
			return;

		mainThread = Thread.CurrentThread;
		instance = new CrossThreadInvoker();

		backgroundUpdater = new GameObject();
		GameObject.DontDestroyOnLoad(backgroundUpdater);
		backgroundUpdater.AddComponent<BackgroundUpdater>();
	}

	private static Thread mainThread;
	private static readonly Queue<AsyncResult> ToExecute = new Queue<AsyncResult>();

	private static void ProcessQueue()
	{
		if (Thread.CurrentThread != mainThread)
			throw new Exception(
				"must be called from the same thread it was created on " +
				"(created on thread id: " + mainThread.ManagedThreadId + ", called from thread id: " +
				Thread.CurrentThread.ManagedThreadId
			);

		AsyncResult data = null;
		while (true)
		{
			lock (ToExecute)
			{
				if (ToExecute.Count == 0)
				{
					break;
				}

				data = ToExecute.Dequeue();
			}

			data.Invoke();
		}
	}

	public bool InvokeRequired => mainThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId;

	public IAsyncResult BeginInvoke(Delegate method, object[] args)
	{
		var asyncResult = new AsyncResult()
		{
			method = method,
			args = args,
			IsCompleted = false,
			manualResetEvent = new ManualResetEvent(false),
			invokingThread = Thread.CurrentThread
		};

		if (mainThread.ManagedThreadId != asyncResult.invokingThread.ManagedThreadId)
		{
			lock (ToExecute)
			{
				ToExecute.Enqueue(asyncResult);
			}
		}
		else
		{
			asyncResult.Invoke();
			asyncResult.CompletedSynchronously = true;
		}

		return asyncResult;
	}

	public object EndInvoke(IAsyncResult result)
	{
		if (!result.IsCompleted)
			result.AsyncWaitHandle.WaitOne();

		return result.AsyncState;
	}

	public object Invoke(Delegate method, object[] args)
	{
		if (InvokeRequired)
		{
			var asyncResult = BeginInvoke(method, args);
			return EndInvoke(asyncResult);
		}
		else
		{
			return method.DynamicInvoke(args);
		}
	}

	private class AsyncResult : IAsyncResult
	{
		public Delegate method;
		public object[] args;
		public bool IsCompleted { get; set; }

		public WaitHandle AsyncWaitHandle => manualResetEvent;

		public ManualResetEvent manualResetEvent;
		public Thread invokingThread;

		public object AsyncState { get; set; }
		public bool CompletedSynchronously { get; set; }

		public void Invoke()
		{
			AsyncState = method.DynamicInvoke(args);
			IsCompleted = true;
			manualResetEvent.Set();
		}
	}
}
