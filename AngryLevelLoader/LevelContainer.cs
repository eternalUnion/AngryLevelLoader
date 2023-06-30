using PluginConfig.API.Fields;
using PluginConfig.API;
using System.Linq;
using UnityEngine;

namespace AngryLevelLoader
{
	public class LevelContainer
	{
		public LevelField field;

		public delegate void onLevelButtonPressDelegate();
		public event onLevelButtonPressDelegate onLevelButtonPress;
		public bool TriggerLevelButtonPress()
		{
			if (onLevelButtonPress == null)
				return false;

			onLevelButtonPress.Invoke();
			return true;
		}

		
		public bool hidden
		{
			get => field.hidden;
			set => field.hidden = value;
		}

		public FloatField time;
		public StringField timeRank;
		public IntField kills;
		public StringField killsRank;
		public IntField style;
		public StringField styleRank;
		public StringField finalRank;
		public StringField secrets;
		public BoolField challenge;
		public BoolField discovered;

		public void UpdateUI()
		{
			field.time = time.value;
			field.timeRank = timeRank.value[0];
			field.kills = kills.value;
			field.killsRank = killsRank.value[0];
			field.style = style.value;
			field.styleRank = styleRank.value[0];

			field.finalRank = finalRank.value[0];
			field.secrets = secrets.value.ToCharArray().Count(c => c == 'T');
			field.challenge = challenge.value;
			field.discovered = discovered.value;

			field.UpdateUI();
		}

		public void AssureSecretsSize()
		{
			int currentSecretCount = secrets.value.Length;
			if (currentSecretCount != field.data.secretCount)
			{
				Debug.LogWarning("Inconsistent secrets data detected");
				string secretsStr = secrets.value;

				if (currentSecretCount < field.data.secretCount)
				{
					while (secretsStr.Length != field.data.secretCount)
						secretsStr += 'F';
					secrets.value = secretsStr;
					secrets.defaultValue = secretsStr.Replace('T', 'F');
				}
				else
				{
					secrets.value = secrets.value.Substring(0, field.data.secretCount);
					secrets.defaultValue = secrets.value.Replace('T', 'F');
				}
			}
		}

		public void UpdateData(RudeLevelScript.RudeLevelData data)
		{
			field.data = data;

			AssureSecretsSize();
			field.UpdateData();
		}

		public LevelContainer(ConfigPanel panel, RudeLevelScript.RudeLevelData data)
		{
			field = new LevelField(panel, data);
			field.onLevelButtonPress += () =>
			{
				if (onLevelButtonPress != null)
					onLevelButtonPress.Invoke();
			};

			time = new FloatField(panel, "", $"l_{data.uniqueIdentifier}_time", 0) { hidden = true, presetLoadPriority = -1 };
			timeRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_timeRank", "-") { hidden = true };
			kills = new IntField(panel, "", $"l_{data.uniqueIdentifier}_kills", 0) { hidden = true };
			killsRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_killsRank", "-") { hidden = true };
			style = new IntField(panel, "", $"l_{data.uniqueIdentifier}_style", 0) { hidden = true };
			styleRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_styleRank", "-") { hidden = true };

			finalRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_finalRank", "-") { hidden = true };

			string defSecretText = "";
			for (int i = 0; i < data.secretCount; i++)
				defSecretText += 'F';
			secrets = new StringField(panel, "", $"l_{data.uniqueIdentifier}_secrets", defSecretText, true) { hidden = true };
			if (secrets.value.Length != data.secretCount)
			{
				Debug.LogWarning($"Secret orb count does not match for {data.scenePath}, resetting");
				secrets.value = defSecretText;
			}

			challenge = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_challenge", false) { hidden = true };
			discovered = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_discovered", !data.hideIfNotPlayed) { hidden = true };

			UpdateUI();

			time.onValueChange += (e) =>
			{
				AssureSecretsSize();
				UpdateUI();
			};
		}
	}
}
