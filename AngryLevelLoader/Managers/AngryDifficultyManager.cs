using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Utilities;

namespace AngryLevelLoader.Managers
{
	public class AngryDifficulty
	{
		public readonly string name;
		public readonly int difficulty;

		public virtual bool IsSupported => true;

		public virtual bool IsCurrentlySet => PrefsManager.Instance.GetInt("difficulty") == difficulty;

		public AngryDifficulty(string name, int difficulty)
		{
			this.name = name;
			this.difficulty = difficulty;
		}

		internal virtual void SetDifficulty()
		{
			PrefsManager.Instance.SetInt("difficulty", difficulty);
		}

		internal virtual void UnsetDifficulty()
		{
		}

		// Modded difficulties. We need auxilary methods to access other assemblies to avoid
		// exceptions in case the mod is not loaded. We also need to access auxilary methods
		// in try-catch blocks in case the mod is updated.

		public class UltrapainDifficulty : AngryDifficulty
		{
			public static readonly UltrapainDifficulty Instance = new UltrapainDifficulty();

			public override bool IsSupported => Chainloader.PluginInfos.ContainsKey(Ultrapain.Plugin.PLUGIN_GUID);

			private bool _IsCurrentlySet()
			{
				return base.IsCurrentlySet && Ultrapain.Plugin.ultrapainDifficulty;
			}

			public override bool IsCurrentlySet
			{
				get
				{
					if (!IsSupported)
						return false;

					try
					{
						return _IsCurrentlySet();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
						return false;
					}
				}
			}

			private UltrapainDifficulty() : base("ULTRAPAIN", 6)
			{ }

			private void _SetDifficulty()
			{
				PrefsManager.Instance.SetInt("difficulty", 6);
				Ultrapain.Plugin.ultrapainDifficulty = true;
				Ultrapain.Plugin.realUltrapainDifficulty = true;
			}

			internal override void SetDifficulty()
			{
				if (IsSupported)
				{
					try
					{
						_SetDifficulty();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
					}
				}
			}

			private void _UnsetDifficulty()
			{
				Ultrapain.Plugin.realUltrapainDifficulty = false;
			}

			internal override void UnsetDifficulty()
			{
				if (IsSupported)
				{
					try
					{
						_UnsetDifficulty();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
					}
				}
			}
		}

		public class BananaDifficulty : AngryDifficulty
		{
			public static BananaDifficulty Instance = new BananaDifficulty();

			public override bool IsSupported => Chainloader.PluginInfos.ContainsKey("com.banana.BananaDifficulty");

			private BananaDifficulty() : base("BANANA", 5)
			{ }
		}

		public class BillionDifficulty : AngryDifficulty
		{
			public static BillionDifficulty Instance = new BillionDifficulty();

			public override bool IsSupported => Chainloader.PluginInfos.ContainsKey("billy.billiondifficulty");

			private bool _IsCurrentlySet()
			{
				return base.IsCurrentlySet && !global::BillionDifficulty.Plugin.IsBrilliantBillion.Value;
			}

			public override bool IsCurrentlySet
			{
				get
				{
					if (!IsSupported)
						return false;

					try
					{
						return _IsCurrentlySet();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
						return false;
					}
				}
			}

			private BillionDifficulty() : base("BILLION", 19)
			{ }

			private void _SetDifficulty()
			{
				global::BillionDifficulty.Plugin.IsBrilliantBillion.SetValue(false);
				PrefsManager.Instance.SetInt("difficulty", 19);
			}

			internal override void SetDifficulty()
			{
				if (IsSupported)
				{
					try
					{
						_SetDifficulty();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
					}
				}
			}
		}

		public class BrilliantBillionDifficulty : AngryDifficulty
		{
			public static BrilliantBillionDifficulty Instance = new BrilliantBillionDifficulty();

			public override bool IsSupported => Chainloader.PluginInfos.ContainsKey("billy.billiondifficulty");

			private bool _IsCurrentlySet()
			{
				return base.IsCurrentlySet && global::BillionDifficulty.Plugin.IsBrilliantBillion.Value;
			}

			public override bool IsCurrentlySet
			{
				get
				{
					if (!IsSupported)
						return false;

					try
					{
						return _IsCurrentlySet();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
						return false;
					}
				}
			}

			private BrilliantBillionDifficulty() : base("BILLION (HARD)", 19)
			{ }

			private void _SetDifficulty()
			{
				global::BillionDifficulty.Plugin.IsBrilliantBillion.SetValue(true);
				PrefsManager.Instance.SetInt("difficulty", 19);
			}

			internal override void SetDifficulty()
			{
				if (IsSupported)
				{
					try
					{
						_SetDifficulty();
					}
					catch (Exception ex)
					{
						Plugin.logger.LogError(ex);
					}
				}
			}
		}
	}

	/// <summary>
	/// Read or modify difficulty used for angry levels.
	/// </summary>
	public static class AngryDifficultyManager
	{
		public static readonly AngryDifficulty HARMLESS = new AngryDifficulty("HARMLESS", 0);
		public static readonly AngryDifficulty LENIENT = new AngryDifficulty("LENIENT", 1);
		public static readonly AngryDifficulty STANDARD = new AngryDifficulty("STANDARD", 2);
		public static readonly AngryDifficulty VIOLENT = new AngryDifficulty("VIOLENT", 3);
		public static readonly AngryDifficulty BRUTAL = new AngryDifficulty("BRUTAL", 4);

		/// <summary>
		/// Difficulty selected from Angry panel. May be overwritten by gamemode.
		/// </summary>
		public static AngryDifficulty SelectedDifficulty { get; internal set; } = VIOLENT;

		/// <summary>
		/// Set the config value for the difficulty used by angry. This method will have no effect if a custom level
		/// is being played (can be checked by <see cref="AngrySceneManager.isInCustomLevel"/>). This method will have no
		/// effect if difficulty parameter is not in <see cref="Difficulties"/>.
		/// </summary>
		/// <param name="difficulty">One of the difficulties inside <see cref="Difficulties"/></param>
		/// <returns>True if difficulty was changed. False if a custom level is being played or parameter is invalid.</returns>
		public static bool SetDifficulty(AngryDifficulty difficulty)
		{
			if (AngrySceneManager.isInCustomLevel)
				return false;

			return ForceSetDifficulty(difficulty);
		}

		internal static bool ForceSetDifficulty(AngryDifficulty difficulty)
		{
			int difficultyIndex = Difficulties.IndexOf(difficulty);
			if (difficultyIndex == -1)
				return false;

			ConfigManager.difficultyField.difficultyListValueIndex = difficultyIndex;
			ConfigManager.difficultyField.postDifficultyChange.Invoke(difficulty.name, difficultyIndex);
			return true;
		}

		/// <summary>
		/// List of supported difficulties. Can include modded difficulties.
		/// </summary>
		private static readonly List<AngryDifficulty> difficulties = new List<AngryDifficulty>()
		{
			HARMLESS,
			LENIENT,
			STANDARD,
			VIOLENT,
			BRUTAL,
		};

		/// <summary>
		/// List of supported difficulties. Can include modded difficulties.
		/// </summary>
		public static IReadOnlyList<AngryDifficulty> Difficulties => difficulties;

		/// <summary>
		/// List of difficulty names in upper case letters.
		/// </summary>
		public static IEnumerable<string> DifficultyNames => Difficulties.Select(d => d.name);

		private static bool _inited = false;
		public static void Init()
		{
			if (_inited)
				return;
			_inited = true;

			difficulties.Clear();
			difficulties.Add(HARMLESS);
			difficulties.Add(LENIENT);
			difficulties.Add(STANDARD);
			difficulties.Add(VIOLENT);
			difficulties.Add(BRUTAL);

			// Add modded difficulties, if the mod is loaded

			if (AngryDifficulty.UltrapainDifficulty.Instance.IsSupported)
				difficulties.Add(AngryDifficulty.UltrapainDifficulty.Instance);

			if (AngryDifficulty.BananaDifficulty.Instance.IsSupported)
				difficulties.Add(AngryDifficulty.BananaDifficulty.Instance);

			if (AngryDifficulty.BillionDifficulty.Instance.IsSupported)
				difficulties.Add(AngryDifficulty.BillionDifficulty.Instance);

			if (AngryDifficulty.BrilliantBillionDifficulty.Instance.IsSupported)
				difficulties.Add(AngryDifficulty.BrilliantBillionDifficulty.Instance);
		}

		// After clicking a difficulty button in the act menu, change the difficulty accordingly
		internal static void SetDifficultyFromPrefs()
		{
			int difficulty = PrefsManager.Instance.GetInt("difficulty", 3);
			switch (difficulty)
			{
				// Stock difficulties
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
					AngryDifficulty selectedDifficulty = Difficulties.Where(d => d.difficulty == difficulty).FirstOrDefault();
					Plugin.logger.LogInfo($"Angry setting difficulty to {(selectedDifficulty == null ? "<unknown game difficulty>" : selectedDifficulty.name)}");

					if (selectedDifficulty == null)
						selectedDifficulty = VIOLENT;
					ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(selectedDifficulty);
					break;

				// Possibly bananas
				case 5:
					if (AngryDifficulty.BananaDifficulty.Instance.IsSupported)
					{
						ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(AngryDifficulty.BananaDifficulty.Instance);
					}
					else
					{
						Plugin.logger.LogWarning("Difficulty was set to BANANAS, but angry does not support it. Setting to violent");
						ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(VIOLENT);
					}
					break;

				// Possibly ultrapain
				case 6:
					if (AngryDifficulty.UltrapainDifficulty.Instance.IsSupported)
					{
						if (AngryDifficulty.UltrapainDifficulty.Instance.IsCurrentlySet)
						{
							ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(AngryDifficulty.UltrapainDifficulty.Instance);
						}
						else
						{
							Plugin.logger.LogWarning("Difficulty was set to UKMD, but angry does not support it. Setting to violent");
							ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(VIOLENT);
						}
					}
					else
					{
						Plugin.logger.LogWarning("Difficulty was set to UKMD, but angry does not support it. Setting to violent");
						ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(VIOLENT);
					}
					break;

				// Possibly billion
				case 19:
					if (AngryDifficulty.BillionDifficulty.Instance.IsSupported)
					{
						if (AngryDifficulty.BillionDifficulty.Instance.IsCurrentlySet)
						{
							ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(AngryDifficulty.BillionDifficulty.Instance);
						}
						else if (AngryDifficulty.BrilliantBillionDifficulty.Instance.IsCurrentlySet)
						{
							ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(AngryDifficulty.BrilliantBillionDifficulty.Instance);
						}
						else
						{
							Plugin.logger.LogWarning("Difficulty was set to 19, but angry does not support it. Setting to violent");
							ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(VIOLENT);
						}
					}
					break;

				// Invalid difficulty
				default:
					Plugin.logger.LogWarning("Unknown difficulty, defaulting to violent");
					ConfigManager.difficultyField.difficultyListValueIndex = Difficulties.IndexOf(VIOLENT);
					break;
			}

			ConfigManager.difficultyField.TriggerPostDifficultyChangeEvent();
		}
	}
}
