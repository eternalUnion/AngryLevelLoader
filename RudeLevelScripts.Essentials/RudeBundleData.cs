using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RudeLevelScripts.Essentials
{
	public class RudeBundleData : ScriptableObject
	{
		[Tooltip("Will be shown on the angry bundle list")]
		public string bundleName;

		[Tooltip("Will be shown below the bundle name")]
		public string author;

		[Tooltip("Icon shown right next to the level name. Must be in png format. If not square, gets cropped")]
		public Sprite levelIcon;
	}
}
