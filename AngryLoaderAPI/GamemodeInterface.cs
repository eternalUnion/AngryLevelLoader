using AngryLevelLoader;

namespace AngryLoaderAPI
{
	public static class GamemodeInterface
	{
		public enum Gamemode
		{
			None,
			NoMonsters,
			NoMonstersAndWeapons,
		}

		public static Gamemode GetCurrentGamemode()
		{
			switch (RudeGamemodeInterface.GetCurrentGamemode())
			{
				default:
				case AngryLevelLoader.Managers.AngryGamemodeManager.Gamemode.None:
					return Gamemode.None;

				case AngryLevelLoader.Managers.AngryGamemodeManager.Gamemode.NoMonsters:
					return Gamemode.NoMonsters;

				case AngryLevelLoader.Managers.AngryGamemodeManager.Gamemode.NoMonstersAndWeapons:
					return Gamemode.NoMonstersAndWeapons;
			}
		}
	}
}
