using AngryLoaderAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RudeLevelScripts
{
	[Flags]
	public enum Gamemode
	{
		NoGamemode = 1,
		NoMonsters = 2,
		NoMonstersAndWeapons = 4,
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

			switch (GamemodeInterface.GetCurrentGamemode())
			{
				case GamemodeInterface.Gamemode.None:
					success = gamemode.HasFlag(Gamemode.NoGamemode);
					break;

				case GamemodeInterface.Gamemode.NoMonsters:
					success = gamemode.HasFlag(Gamemode.NoMonsters);
					break;

				case GamemodeInterface.Gamemode.NoMonstersAndWeapons:
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
