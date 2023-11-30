using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AngryLevelLoader.Extensions
{
	public static class GameObjectExtensions
	{
		public static T GetComponentInParent<T>(this GameObject go, bool includeInactive) where T : Component
		{
			if (!includeInactive)
				return go.GetComponentInParent<T>();

			Transform current = go.transform;

			while (current != null)
			{
				T comp = current.gameObject.GetComponent<T>();

				if (comp != null)
					return comp;
				
				current = current.parent;
			}

			return null;
		}
	}
}
