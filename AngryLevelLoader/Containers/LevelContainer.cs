using AngryLevelLoader.DataTypes;
using AngryLevelLoader.Fields;
using AngryLevelLoader.Managers;
using PluginConfig.API;
using PluginConfig.API.Fields;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AngryLevelLoader.Containers
{
    /// <summary>
    /// Container which stores save data as well as UI and metadata for an angry level.
    /// </summary>
    public class LevelContainer
    {
        public readonly BundleContainer bundleContainer;
        public readonly string levelId;
        
        private readonly LevelField field;
        private AngryLevelData data;
        internal AngryLevelData LevelData { get => data; }

        private FloatField time;
        private StringField timeRank;
        private IntField kills;
        private StringField killsRank;
        private IntField style;
        private StringField styleRank;
        private StringField finalRank;
        private StringField secrets;
        private BoolField challenge;
        private BoolField discovered;

        /// <summary>
        /// Full name of the level.
        /// </summary>
        public string LevelName
        {
            get => (data == null) ? string.Empty : data.levelName ?? string.Empty;
        }

        /// <summary>
        /// Custom scripts required by the level to be functional.
        /// </summary>
        public string[] RequiredScripts
        {
            get => (data == null) ? new string[0] : data.requiredDllNames ?? new string[0];
        }

        /// <summary>
        /// 4:3 preview image of the level.
        /// </summary>
        public Sprite PreviewImage
        {
            get => field.PreviewImage;
            internal set => field.PreviewImage = value;
        }

        private Texture2D _previewImageTexture = null;
        private Sprite _previewImageSprite = null;
		/// <summary>
		/// 4:3 preview image of the level.
		/// </summary>
		public Texture2D PreviewImageTexture
        {
            get => (field.PreviewImage == null) ? null : field.PreviewImage.texture;
            internal set
            {
                if (_previewImageTexture != null)
					UnityEngine.Object.Destroy(_previewImageTexture);

                if (_previewImageSprite != null)
                    UnityEngine.Object.Destroy(_previewImageSprite);

                _previewImageTexture = value;
                if (_previewImageTexture != null)
                    _previewImageTexture.filterMode = FilterMode.Point;
                _previewImageSprite = (_previewImageTexture == null) ? null : Sprite.Create(_previewImageTexture, new Rect(0, 0, _previewImageTexture.width, _previewImageTexture.height), new Vector2(0.5f, 0.5f));
                PreviewImage = _previewImageSprite;
			}
        }

        internal void LoadPreviewImageFromUrl(string url)
        {
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);

            var handler = req.SendWebRequest();
            handler.completed += (op) =>
            {
				if (req.result == UnityWebRequest.Result.Success)
					PreviewImageTexture = DownloadHandlerTexture.GetContent(req);

                req.Dispose();
			};
        }

		/// <summary>
		/// Completion time in seconds of the playthrough with best final rank.
		/// </summary>
		public float Time
        {
            get => time.value;
            internal set
            {
                time.value = value;
                time.TriggerPostValueChangeEvent();
			}
        }

		/// <summary>
		/// Can be one of the following:<br></br>
		/// '-': Not played before<br></br>
		/// 'A|B|C|D|S|P': Completed with or without cheats<br></br>
		/// </summary>
		public char TimeRank
        {
            get => timeRank.value.Length == 0 ? '-' : timeRank.value[0];
            internal set
            {
                timeRank.value = $"{value}";
                timeRank.TriggerPostValueChangeEvent();
			}
        }
        
        /// <summary>
        /// Total kill count of the playthrough with best final rank
        /// </summary>
        public int Kills
        {
            get => kills.value;
            internal set
            {
                kills.value = value;
                kills.TriggerPostValueChangeEvent();
            }
        }

		/// <summary>
		/// Can be one of the following:<br></br>
		/// '-': Not played before<br></br>
		/// 'A|B|C|D|S|P': Completed with or without cheats<br></br>
		/// </summary>
		public char KillsRank
		{
			get => killsRank.value.Length == 0 ? '-' : killsRank.value[0];
			internal set
			{
				killsRank.value = $"{value}";
                killsRank.TriggerPostValueChangeEvent();
			}
		}

		/// <summary>
		/// Style of the playthrough with best final rank
		/// </summary>
		public int Style
		{
			get => style.value;
			internal set
			{
				style.value = value;
                style.TriggerPostValueChangeEvent();
			}
		}

		/// <summary>
		/// Can be one of the following:<br></br>
		/// '-': Not played before<br></br>
		/// 'A|B|C|D|S|P': Completed with or without cheats<br></br>
		/// </summary>
		public char StyleRank
		{
			get => styleRank.value.Length == 0 ? '-' : styleRank.value[0];
			internal set
			{
				styleRank.value = $"{value}";
                styleRank.TriggerPostValueChangeEvent();
			}
		}

		/// <summary>
		/// Can be one of the following:<br></br>
		/// '-': Not played before<br></br>
		/// ' ': Completed with cheats<br></br>
		/// 'A|B|C|D|S|P': Completed without cheats<br></br>
		/// </summary>
		public char FinalRank
		{
			get => finalRank.value.Length == 0 ? '-' : finalRank.value[0];
            set
            {
                finalRank.value = $"{value}";
                finalRank.TriggerPostValueChangeEvent();
				bundleContainer.RecalculateFinalRank();
			}
		}

		private void AssureSecretsSize()
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

        /// <summary>
        /// Total number of secrets in the level.
        /// </summary>
		public int SecretCount
        {
            get => (data == null) ? 0 : data.secretCount;
        }

        /// <param name="secretNum">Zero indexed number of the secret.</param>
        /// <returns>True if the secret was discovered by the player previously.</returns>
        public bool SecretDiscovered(int secretNum)
        {
            if (secretNum < 0 || secretNum >= SecretCount)
                return false;

            AssureSecretsSize();
            return secrets.value[secretNum] == 'T';
        }

        internal bool SetSecretDiscovered(int secretNum, bool discovered)
        {
			if (secretNum < 0 || secretNum >= SecretCount)
				return false;

			AssureSecretsSize();
            StringBuilder currentSecrets = new StringBuilder(secrets.value);
            currentSecrets[secretNum] = (discovered) ? 'T' : 'F';
            secrets.value = currentSecrets.ToString();
            secrets.TriggerPostValueChangeEvent();
			return true;
		}

        /// <summary>
        /// Not all levels support challenges.
        /// </summary>
        public bool ChallengeSupported
        {
            get => (data == null) ? false : data.levelChallengeEnabled;
        }

        /// <summary>
        /// Description of the challenge provided by the level maker.
        /// </summary>
        public string ChallengeText
        {
            get => (data == null) ? string.Empty : data.levelChallengeText ?? string.Empty;
        }

        /// <summary>
        /// Returns true if the player previously completed the level challenge.
        /// </summary>
        public bool ChallengeDone
        {
            get => challenge.value;
            internal set
            {
                challenge.value = value;
                challenge.TriggerPostValueChangeEvent();
			}
        }

        /// <summary>
        /// Is set once the level is entered at least once. This field is used to hide secret levels.
        /// </summary>
        public bool LevelDiscovered
        {
            get => discovered.value;
            internal set
            {
                discovered.value = value;
                discovered.TriggerPostValueChangeEvent();
			}
        }

        private bool _locked = false;
        /// <summary>
        /// Some levels may not be played until previous levels are completed.
        /// </summary>
        public bool Locked
        {
            get => _locked;
            internal set
            {
                _locked = value;
                field.Locked = value;
            }
        }

        internal int SiblingIndex
        {
            get => field.siblingIndex;
            set => field.siblingIndex = value;
        }

        internal bool ForceHidden
        {
            get => field.forceHidden;
            set => field.forceHidden = value;
        }

        internal void UpdateData(AngryLevelData data)
        {
            if (data.uniqueIdentifier != levelId)
                throw new System.Exception("Attempted to update data of a level container with another RudeLevelData having wrong unique id");

            this.data = data;

			string defaultSecretText = "";
			for (int i = 0; i < data.secretCount; i++)
				defaultSecretText += 'F';
            secrets.defaultValue = defaultSecretText;

			AssureSecretsSize();

            field.LevelName = data.levelName;
			field.IsSecretLevel = data.isSecretLevel;
			field.HideIfNotPlayed = data.hideIfNotPlayed;
			field.DoNotHideLevelPreviewWhenNotCompleted = data.doNotHideLevelPreviewWhenNotCompleted;
			field.ChallengeEnabled = data.levelChallengeEnabled;
			field.ChallengeText = data.levelChallengeText;
		}

        internal LevelContainer(ConfigPanel panel, BundleContainer container, AngryLevelData data)
        {
            levelId = data.uniqueIdentifier;
            bundleContainer = container;

			// Create UI

			field = new LevelField(panel, container, data.uniqueIdentifier);

			field.onLevelButtonPress += () =>
			{
				AngrySceneManager.LevelButtonPressed(this);
			};

			field.onResetStats += () =>
			{
				Time = 0;
				TimeRank = '-';
				Kills = 0;
				KillsRank = '-';
				Style = 0;
				StyleRank = '-';
				FinalRank = '-';
                
                // Might lock some levels
				container.UpdateAllUI();
			};

			field.onResetSecrets += () =>
			{
				secrets.value = "".PadRight(data.secretCount, 'F');
                secrets.TriggerPostValueChangeEvent();
			};

			field.onResetChallenge += () =>
			{
				ChallengeDone = false;
			};

			// Create config fields

			time = new FloatField(panel, "", $"l_{data.uniqueIdentifier}_time", 0, true, false) { hidden = true, presetLoadPriority = -1 };
            time.postValueChangeEvent += (e) =>
            {
                field.Time = e;
			};
            time.TriggerPostValueChangeEvent();

            timeRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_timeRank", "-", true, true, false) { hidden = true };
            timeRank.postValueChangeEvent += (e) =>
            {
                field.TimeRank = (e.Length == 0) ? '-' : e[0];
            };
            timeRank.TriggerPostValueChangeEvent();

			kills = new IntField(panel, "", $"l_{data.uniqueIdentifier}_kills", 0, true, false) { hidden = true };
            kills.postValueChangeEvent += (e) =>
			{
                field.Kills = e;
			};
            kills.TriggerPostValueChangeEvent();

			killsRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_killsRank", "-", true, true, false) { hidden = true };
            killsRank.postValueChangeEvent += (e) =>
			{
                field.KillsRank = (e.Length == 0) ? '-' : e[0];
			};
            killsRank.TriggerPostValueChangeEvent();

			style = new IntField(panel, "", $"l_{data.uniqueIdentifier}_style", 0, true, false) { hidden = true };
            style.postValueChangeEvent += (e) =>
			{
                field.Style = e;
			};
            style.TriggerPostValueChangeEvent();

			styleRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_styleRank", "-", true, true, false) { hidden = true };
            styleRank.postValueChangeEvent += (e) =>
			{
                field.StyleRank = (e.Length == 0) ? '-' : e[0];
			};
            styleRank.TriggerPostValueChangeEvent();

			finalRank = new StringField(panel, "", $"l_{data.uniqueIdentifier}_finalRank", "-", true, true, false) { hidden = true };
            finalRank.postValueChangeEvent += (e) =>
			{
                field.FinalRank = (e.Length == 0) ? '-' : e[0];
			};
            finalRank.TriggerPostValueChangeEvent();

			string defaultSecretText = "";
            for (int i = 0; i < data.secretCount; i++)
                defaultSecretText += 'F';
            secrets = new StringField(panel, "", $"l_{data.uniqueIdentifier}_secrets", defaultSecretText, true, true, false) { hidden = true };
            secrets.postValueChangeEvent += (e) =>
			{
                field.SecretsString = e;
			};
            secrets.TriggerPostValueChangeEvent();

			challenge = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_challenge", false, true, false) { hidden = true };
            challenge.postValueChangeEvent += (e) =>
			{
                field.ChallengeDone = e;
			};
            challenge.TriggerPostValueChangeEvent();

			discovered = new BoolField(panel, "", $"l_{data.uniqueIdentifier}_discovered", false, true, false) { hidden = true };
            discovered.postValueChangeEvent += (e) =>
			{
                field.Discovered = e;
			};
            discovered.TriggerPostValueChangeEvent();

			UpdateData(data);
		}
    }
}
