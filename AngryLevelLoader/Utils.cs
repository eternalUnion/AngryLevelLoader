using PluginConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AngryLevelLoader
{
	public static class RankUtils
	{
		public static int GetRankScore(char rank)
		{
			if (rank == 'D')
				return 1;
			if (rank == 'C')
				return 2;
			if (rank == 'B')
				return 3;
			if (rank == 'A')
				return 4;
			if (rank == 'S')
				return 5;
			if (rank == 'P')
				return 6;
			if (rank == '-')
				return -1;
			if (rank == ' ')
				return 0;

			return -1;
		}

		private static Dictionary<char, Color> rankColors = new Dictionary<char, Color>()
		{
			{ 'D', new Color(0, 0x94 / 255f, 0xFF / 255f) },
			{ 'C', new Color(0x4C / 255f, 0xFF / 255f, 0) },
			{ 'B', new Color(0xFF / 255f, 0xD8 / 255f, 0) },
			{ 'A', new Color(0xFF / 255f, 0x6A / 255f, 0) },
			{ 'S', Color.red },
			{ 'P', Color.white },
			{ '-', Color.gray }
		};
		public static string GetFormattedRankText(char rank)
		{
			Color textColor;
			if (!rankColors.TryGetValue(rank, out textColor))
				textColor = Color.gray;

			return $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{rank}</color>";
		}
	}

	public static class IOUtils
	{
		public static string GetUniqueFileName(string folder, string name)
		{
			string nameExtensionless = Path.GetFileNameWithoutExtension(name);
			string newName = nameExtensionless;
			string ext = Path.GetExtension(name);

			int i = 0;
			while (File.Exists(Path.Combine(folder, newName)))
				newName = $"{nameExtensionless}_{i++}";

			return $"{newName}{ext}";
		}
	
		public static string AppData
		{
			get => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

		public static bool TryCreateDirectory(string path)
		{
			if (Directory.Exists(path))
				return false;
			Directory.CreateDirectory(path);
			return true;
		}
	}

	public static class CryptographyUtils
	{
		public static bool VerifyFileCertificate(string filePath, string certificatePath)
		{
			const string angryPublicKey = "<RSAKeyValue><Modulus>+/ueeOpso05dA+5GjKbjQ0VpM+JAHmRRgYRw36G4dXqmpCGfVDNVdjjBBkVWO+6lJoSNaaG4Yprn4uQVslUQ7OYWAw6Y+9E0Ezvr1quWE7i0KGxG6weplRTsu9aO0/9gJgP/gWQxC0Cf83NwyvMPsThtCruAQFT+cW0LGghtFgrBr++aknI06SJI5ydrbZgEtU5i4FfjrV1ms4CRRojhydJglfGQfG8W3pTDge4jVdND+RGB6F01QGi0+Bnq5DfKdjvb3/Zh1ko7WocWgavDaIgLYj88AgbGdC0lidLMIgzdnGxkLyxbTzsgi/mvUpB2foy4uHoV22EaWMj+6H+oXQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
			RSA publicKey = RSA.Create();
			publicKey.FromXmlString(angryPublicKey);
			byte[] cert = File.ReadAllBytes(certificatePath);
			return publicKey.VerifyData(File.ReadAllBytes(filePath), cert, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		// Why don't you exist Convert.ToHexString(byte[])? Why nothing exists in this framework?
		private static string ByteArrayToString(byte[] ba)
		{
			return BitConverter.ToString(ba).Replace("-", "");
		}

		public static string GetSHA256Hash(string filePath)
		{
			SHA256 hash = SHA256.Create();
			hash.Initialize();
			return ByteArrayToString(hash.ComputeHash(File.ReadAllBytes(filePath))).ToLower();
		}

		public static string GetMD5String(string text)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text));
			return ByteArrayToString(hash).ToLower();
		}

		public static string GetMD5String(byte[] data)
		{
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(data);
			return ByteArrayToString(hash).ToLower();
		}
	}

	public static class UIUtils
	{
		public static RectTransform MakeSimpleRect(Transform parent)
		{
			RectTransform rect = new GameObject().AddComponent<RectTransform>();
			rect.SetParent(parent);
			rect.localScale = Vector3.one;

			return rect;
		}

		private static GameObject sampleButton;
		public static RectTransform MakeButton(Transform parent, string text)
		{
			if (sampleButton == null)
			{
				Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(o => o.name == "Canvas").First().transform;
				sampleButton = canvas.Find("OptionsMenu/Controls Options/Scroll Rect/Contents/Default").gameObject;
			}

			GameObject button = GameObject.Instantiate(sampleButton, parent);
			RectTransform buttonRect = button.GetComponent<RectTransform>();
			Button buttonButtonComp = button.GetComponent<Button>();
			buttonButtonComp.onClick = new Button.ButtonClickedEvent();
			Text buttonButtonText = button.GetComponentInChildren<Text>();
			buttonButtonText.text = text;
			
			return buttonRect;
		}
	
		public static RectTransform MakeText(Transform parent, string text, int fontSize, TextAnchor alignment)
		{
			RectTransform rect = MakeSimpleRect(parent);

			Text rectText = rect.gameObject.AddComponent<Text>();
			rectText.text = text;
			rectText.fontSize = fontSize;
			rectText.alignment = alignment;
			rectText.font = Plugin.gameFont;

			return rect;
		}
	
		private static GameObject sampleMenu;
		public static RectTransform MakePanel(Transform parent, int spacing)
		{
			if (sampleMenu == null)
			{
				Transform canvas = SceneManager.GetActiveScene().GetRootGameObjects().Where(o => o.name == "Canvas").First().transform;
				sampleMenu = canvas.Find("OptionsMenu/Gameplay Options").gameObject;
			}

			GameObject panel = GameObject.Instantiate(sampleMenu, parent);
			panel.SetActive(true);
			RectTransform panelRect = panel.GetComponent<RectTransform>();
			panelRect.anchoredPosition = new Vector2(0, 40);

			UnityUtils.GetComponentInChildrenRecursively<ScrollRect>(panel.transform).normalizedPosition = new Vector2(0, 1);
			VerticalLayoutGroup contentLayout = UnityUtils.GetComponentInChildrenRecursively<VerticalLayoutGroup>(panel.transform);
			contentLayout.spacing = spacing;
			ContentSizeFitter fitter = contentLayout.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			foreach (Transform child in contentLayout.transform)
			{
				GameObject.Destroy(child.gameObject);
			}

			Transform header = panel.transform.Find("Text");
			header.SetParent(null);
			GameObject.Destroy(header.gameObject);

			return contentLayout.GetComponent<RectTransform>();
		}
	}
}
