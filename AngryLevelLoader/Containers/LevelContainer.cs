using PluginConfig.API.Fields;
using PluginConfig.API;
using System.Linq;
using UnityEngine;
using RudeLevelScript;
using AngryLevelLoader.Fields;

namespace AngryLevelLoader.Containers
{
    public class LevelContainer
    {
        public LevelField field;
        public AngryBundleContainer container;
        public RudeLevelData data;

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
            if (currentSecretCount != data.secretCount)
            {
                Plugin.logger.LogWarning("Inconsistent secrets data detected");
                string secretsStr = secrets.value;

                if (currentSecretCount < data.secretCount)
                {
                    while (secretsStr.Length != data.secretCount)
                        secretsStr += 'F';
                    secrets.value = secretsStr;
                    secrets.defaultValue = secretsStr.Replace('T', 'F');
                }
                else
                {
                    secrets.value = secrets.value.Substring(0, data.secretCount);
                    secrets.defaultValue = secrets.value.Replace('T', 'F');
                }
            }
        }

        public void UpdateData(RudeLevelData data)
        {
            this.data = data;
            field.data = data;
            AssureSecretsSize();

            UpdateUI();
        }

        public LevelContainer(ConfigPanel panel, AngryBundleContainer container, RudeLevelData data)
        {
            this.container = container;
            this.data = data;
            field = new LevelField(panel, container, data);
            field.onLevelButtonPress += () =>
            {
                if (onLevelButtonPress != null)
                    onLevelButtonPress.Invoke();
            };

            time = new FloatField(panel, "", $"l_{data.uniqueIdentifier}_time", 0, true, false) { hidden = true, presetLoadPriority = -1 };
            timeRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_timeRank", "-", true, true, false) { hidden = true };
            kills = new IntField(panel, "", $"l_{data.uniqueIdentifier}_kills", 0, true, false) { hidden = true };
            killsRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_killsRank", "-", true, true, false) { hidden = true };
            style = new IntField(panel, "", $"l_{data.uniqueIdentifier}_style", 0, true, false) { hidden = true };
            styleRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_styleRank", "-", true, true, false) { hidden = true };

            finalRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_finalRank", "-", true, true, false) { hidden = true };

            string defaultSecretText = "";
            for (int i = 0; i < data.secretCount; i++)
                defaultSecretText += 'F';
            secrets = new StringField(panel, "", $"l_{data.uniqueIdentifier}_secrets", defaultSecretText, true, true, false) { hidden = true };
            if (secrets.value.Length != data.secretCount)
            {
                Plugin.logger.LogWarning($"Secret orb count does not match for {data.scenePath}, resetting");
                secrets.value = defaultSecretText;
            }

            challenge = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_challenge", false, true, false) { hidden = true };
            discovered = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_discovered", false, true, false) { hidden = true };

            UpdateUI();

            time.onValueChange += (e) =>
            {
                AssureSecretsSize();
                UpdateUI();
            };
        }
    }
}
