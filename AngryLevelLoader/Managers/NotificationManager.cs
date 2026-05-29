using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

namespace AngryLevelLoader.Managers
{
	internal static class NotificationManager
	{
		internal static bool NotiffyAvailable { get; private set; }

		static NotificationManager()
		{
			NotiffyAvailable = Chainloader.PluginInfos.ContainsKey(Notiffy.NotiffyPlugin.PluginGUID);
		}

		internal static void SendNotification(string header, string body)
		{
			if (!NotiffyAvailable)
				return;

			_SendNotification(header, body);
		}

		private static void _SendNotification(string header, string body)
		{
			Notiffy.API.NotificationSystem.NotifySend(header, body, iconFilePath: Path.Combine(Plugin.workingDir, "plugin-icon.png"));
		}

		internal static void SendNotification(string header, string body, List<(string, Action)> actions)
		{
			if (!NotiffyAvailable)
				return;

			_SendNotification(header, body, actions);
		}

		private static void _SendNotification(string header, string body, List<(string, Action)> actions)
		{
			Dictionary<string, string> actionsDict = new();

			foreach ((string actionName, Action _) in actions)
				actionsDict[actionName] = actionName;

			uint notification_id = Notiffy.API.NotificationSystem.NotifySend(header, body, actions: actionsDict, iconFilePath: Path.Combine(Plugin.workingDir, "plugin-icon.png"));
			Notiffy.API.NotificationSystem.ActionInvoked += OnAction;
			Notiffy.API.NotificationSystem.NotificationDeleted += OnDeleted;

			void OnAction(uint id, string actionIdentifier)
			{
				if (id != notification_id)
					return;

				foreach ((string actionName, Action callback) in actions)
				{
					if (actionName != actionIdentifier)
						continue;

					callback();
					break;
				}

				Notiffy.API.NotificationSystem.ActionInvoked -= OnAction;
				Notiffy.API.NotificationSystem.NotificationDeleted -= OnDeleted;
			}

			void OnDeleted(uint id)
			{
				if (id != notification_id)
					return;

				Notiffy.API.NotificationSystem.ActionInvoked -= OnAction;
				Notiffy.API.NotificationSystem.NotificationDeleted -= OnDeleted;
			}
		}

		internal static void SendNotificationInMainMenu(string header, string body)
		{
			if (!NotiffyAvailable)
				return;

			_SendNotificationInMainMenu(header, body);
		}

		private static void _SendNotificationInMainMenu(string header, string body)
		{
			if (SceneHelper.CurrentScene == "Main Menu")
			{
				_SendNotification(header, body);
			}
			else
			{
				void ShowNewLevelsOnMainMenu(Scene scene, LoadSceneMode mode)
				{
					if (SceneHelper.CurrentScene != "Main Menu")
						return;

					_SendNotification(header, body);
					SceneManager.sceneLoaded -= ShowNewLevelsOnMainMenu;
				}

				SceneManager.sceneLoaded += ShowNewLevelsOnMainMenu;
			}
		}

		internal static void SendNotificationInMainMenu(string header, string body, List<(string, Action)> actions)
		{
			if (!NotiffyAvailable)
				return;

			_SendNotificationInMainMenu(header, body, actions);
		}

		private static void _SendNotificationInMainMenu(string header, string body, List<(string, Action)> actions)
		{
			if (SceneHelper.CurrentScene == "Main Menu")
			{
				_SendNotification(header, body, actions);
			}
			else
			{
				void ShowNewLevelsOnMainMenu(Scene scene, LoadSceneMode mode)
				{
					if (SceneHelper.CurrentScene != "Main Menu")
						return;

					_SendNotification(header, body, actions);
					SceneManager.sceneLoaded -= ShowNewLevelsOnMainMenu;
				}

				SceneManager.sceneLoaded += ShowNewLevelsOnMainMenu;
			}
		}
	}
}
