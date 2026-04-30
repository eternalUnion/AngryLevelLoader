using AngryLevelLoader;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RudeLevelScripts
{
	[Flags]
	public enum Gamemode
	{
		None,
		NoMonsters,
		NoMonstersAndWeapons,
	}

	public class RudeGamemodeChecker : MonoBehaviour
	{
		public Gamemode gamemode = Gamemode.NoMonsters | Gamemode.NoMonstersAndWeapons;

		public UltrakillEvent OnSuccess = null;
		public UltrakillEvent OnFail = null;

		public bool activateOnEnable = true;
		public void OnEnable()
		{
			if (activateOnEnable)
				Activate();
		}

		public void Activate()
		{
			bool success = false;

			switch (RudeGamemodeInterface.GetCurrentGamemode())
			{
				case AngryLevelLoader.Managers.AngryGamemodeManager.Gamemode.None:
					success = gamemode.HasFlag(Gamemode.None);
					break;

				case AngryLevelLoader.Managers.AngryGamemodeManager.Gamemode.NoMonsters:
					success = gamemode.HasFlag(Gamemode.NoMonsters);
					break;

				case AngryLevelLoader.Managers.AngryGamemodeManager.Gamemode.NoMonstersAndWeapons:
					success = gamemode.HasFlag(Gamemode.NoMonstersAndWeapons);
					break;
			}

			if (success)
			{
				if (OnSuccess != null)
					OnSuccess.Invoke();
			}
			else
			{
				if (OnFail != null)
					OnFail.Invoke();
			}
		}
	}
}
