using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace RudeLevelScripts.Essentials
{
	public class ExecuteOnSceneLoad : MonoBehaviour
	{
		public int relativeExecutionOrder = 0;
		public UnityEvent onSceneLoad;

		public void Execute()
		{
			if (onSceneLoad == null)
				return;

			onSceneLoad.Invoke();
		}
	}
}
