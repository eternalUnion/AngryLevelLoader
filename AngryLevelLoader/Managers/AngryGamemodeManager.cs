using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Utilities;

namespace AngryLevelLoader.Managers
{
	/// <summary>
	/// Read or modify gamemode configuration of angry.
	/// </summary>
	public static class AngryGamemodeManager
	{
		/// <summary>
		/// None: No modifications<br></br>
		/// NoMonsters: Enemies do not spawn, difficulty locked to HARMLESS<br></br>
		/// NoMonstersAndWeapons: Enemies do not spawn, weapons cannot be used, difficulty locked to HARMLESS
		/// </summary>
		public enum Gamemode
		{
			None,
			NoMonsters,
			NoMonstersAndWeapons,
		}

		internal static IReadOnlyList<Gamemode> gamemodeEnumList = new List<Gamemode>() { Gamemode.None, Gamemode.NoMonsters, Gamemode.NoMonstersAndWeapons };
		internal static IReadOnlyList<string> gamemodeList = new List<string> { "None", "No Monsters", "No Monsters/Weapons" };

		/// <summary>
		/// Gamemode selected from Angry panel.
		/// </summary>
		public static Gamemode SelectedGamemode
		{
			get => gamemodeEnumList[ConfigManager.difficultyField.gamemodeListValueIndex];
		}
		
		/// <summary>
		/// Set to true if the current gamemode does not alow spawning enemies.
		/// </summary>
		public static bool NoMonsters => SelectedGamemode == Gamemode.NoMonsters || SelectedGamemode == Gamemode.NoMonstersAndWeapons;
		
		/// <summary>
		/// Set to true if the current gamemode does not allow using weapons.
		/// </summary>
		public static bool NoWeapons => SelectedGamemode == Gamemode.NoMonstersAndWeapons;

		/// <summary>
		/// Set the config value for the gamemode used by angry. This method will have no effect if a custom level
		/// is being played (can be checked by <see cref="AngrySceneManager.isInCustomLevel"/>).
		/// </summary>
		/// <returns>True if gamemode was changed. False if a custom level is being played.</returns>
		public static bool SetGamemode(Gamemode gamemode)
		{
			if (AngrySceneManager.isInCustomLevel)
				return false;

			return ForceSetGamemode(gamemode);
		}

		internal static bool ForceSetGamemode(Gamemode gamemode)
		{
			int gamemodeIndex = gamemodeEnumList.IndexOf(gamemode);
			if (gamemodeIndex == -1)
				return false;

			ConfigManager.difficultyField.gamemodeListValueIndex = gamemodeIndex;
			ConfigManager.difficultyField.postGamemodeChange.Invoke(gamemodeList[gamemodeIndex], gamemodeIndex);
			return true;
		}
	}
}
