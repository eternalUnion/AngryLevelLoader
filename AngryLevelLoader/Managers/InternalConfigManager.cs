using AngryLevelLoader.Notifications;
using AngryLevelLoader.Utils;
using PluginConfig;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System.IO;
using System.Text.RegularExpressions;

namespace AngryLevelLoader.Managers
{
	internal static class InternalConfigManager
	{
		public static PluginConfigurator internalConfig;
		public static BoolField devMode;
		public static StringField lastVersion;
		public static StringField ignoreUpdateVersion;
		public static StringField configDataPath;
		public static BoolField leaderboardToggle;
		public static BoolField askedPermissionForLeaderboards;
		public static StringField pendingRecordsField;
		public static BoolField ignoreEpilepsyWarning;
		public static BoolField instantLoadLevel;
		public static StringField instantLoadLevelGuid;
		public static StringField instantLoadLevelId;
		public static StringMultilineField ignoreNoMoWarning;
		public static StringField reportState;

		public static void InitializeInternalConfig()
		{
			if (internalConfig != null)
				return;

			// Initialize internal config
			internalConfig = PluginConfigurator.Create("Angry Level Loader (INTERNAL)", Plugin.PLUGIN_GUID + "_internal");
			internalConfig.hidden = true;
			internalConfig.interactable = false;
			internalConfig.presetButtonHidden = true;
			internalConfig.presetButtonInteractable = false;
			devMode = new BoolField(internalConfig.rootPanel, "devMode", "devMode", false);
			lastVersion = new StringField(internalConfig.rootPanel, "lastPluginVersion", "lastPluginVersion", Plugin.PLUGIN_VERSION, true, true, false);
			ignoreUpdateVersion = new StringField(internalConfig.rootPanel, "ignoreUpdateVersion", "ignoreUpdateVersion", Plugin.PLUGIN_VERSION, true, true, false);
			configDataPath = new StringField(internalConfig.rootPanel, "dataPath", "dataPath", Path.Combine(AngryIOUtils.AppData, "AngryLevelLoader"), false, true, false);
			reportState = new StringField(internalConfig.rootPanel, "reportState", "reportState", "", true);

			// Might be corrupted
			Regex badDataPath = new Regex(@"^[^:]+:\\Users\\User\\AppData\\Roaming");
			if (badDataPath.IsMatch(configDataPath.value) && !Directory.Exists(configDataPath.value))
			{
				Plugin.logger.LogWarning("Bad data path detected, resetting the value!");
				configDataPath.value = configDataPath.defaultValue;
			}

			pendingRecordsField = new StringField(internalConfig.rootPanel, "pendingRecordsField", "pendingRecordsField", "", true, true, false);
			ignoreEpilepsyWarning = new BoolField(internalConfig.rootPanel, "ignoreEpilepsyWarning", "ignoreEpilepsyWarning", false);
			instantLoadLevel = new BoolField(internalConfig.rootPanel, "instantLoadLevel", "instantLoadLevel", false);
			instantLoadLevelGuid = new StringField(internalConfig.rootPanel, "instantLoadLevelGuid", "instantLoadLevelGuid", "", true);
			instantLoadLevelId = new StringField(internalConfig.rootPanel, "instantLoadLevelId", "instantLoadLevelId", "", true);

			askedPermissionForLeaderboards = new BoolField(internalConfig.rootPanel, "askedPermissionForLeaderboards", "askedPermissionForLeaderboards", false);
			leaderboardToggle = new BoolField(internalConfig.rootPanel, "Post records to leaderboards", "leaderboardToggle", false);
			leaderboardToggle.onValueChange += e =>
			{
				if (e.value == true)
				{
					e.canceled = true;
					NotificationPanel.Open(new LeaderboardPermissionNotification());
				}
			};

			leaderboardToggle.postValueChangeEvent += newVal =>
			{
				ConfigManager.leaderboardsDivision.hidden = newVal;
			};

			ignoreNoMoWarning = new StringMultilineField(internalConfig.rootPanel, "ignoreNoMoWarning", "ignoreNoMoWarning", "", true);
		}
	}
}