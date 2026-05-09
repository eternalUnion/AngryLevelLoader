using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Utils
{
	internal static class AngryAsyncUtils
	{
		public static async Task WaitUntilSceneLoaded(string sceneName)
		{
			if (SceneHelper.CurrentScene == sceneName)
				return;

			TaskCompletionSource<bool> source = new TaskCompletionSource<bool>();
			void ListenSceneEvent(Scene scene, LoadSceneMode mode)
			{
				if (SceneHelper.CurrentScene != sceneName)
					return;

				source.SetResult(true);
				SceneManager.sceneLoaded -= ListenSceneEvent;
			}

			SceneManager.sceneLoaded += ListenSceneEvent;
			await source.Task;
		}

		public static async Task LoadSceneAsync(string sceneName, bool noBlocker = false)
		{
			TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
			SceneHelper.LoadSceneAsync(sceneName, noBlocker).ContinueWith(SceneHelper.Instance, () => completionSource.SetResult(true));
			await completionSource.Task;
		}
	}
}
