using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryBundleSortFieldComponent : MonoBehaviour
	{
		class GraphicCopier : MonoBehaviour
		{
			public Graphic thisGraphic;
			public CanvasRenderer thisRenderer;
			public Graphic targetGraphic;
			public CanvasRenderer targetRenderer;

			void Start()
			{
				if (thisGraphic == null)
					thisGraphic = GetComponent<Graphic>();

				if (thisRenderer == null)
					thisRenderer = GetComponent<CanvasRenderer>();
			}

			void Update()
			{
				if (targetGraphic != null && thisGraphic != null && thisRenderer != null && targetRenderer != null)
				{
					targetGraphic.color = thisGraphic.color;
					targetRenderer.SetColor(thisRenderer.GetColor());
				}
			}
		}

		public Button favSortButton;
		public Image favSortIcon;

		[Space(5)]
		public Button nameButton;
		public Image nameBg;
		public Image nameFrame;
		public TextMeshProUGUI nameText;
		private GraphicCopier nameCopier;

		[Space(5)]
		public Button authorButton;
		public Image authorBg;
		public Image authorFrame;
		public TextMeshProUGUI authorText;
		private GraphicCopier authorCopier;

		[Space(5)]
		public Button lastUpdateButton;
		public Image lastUpdateBg;
		public Image lastUpdateFrame;
		public TextMeshProUGUI lastUpdateText;
		private GraphicCopier lastUpdateCopier;

		[Space(5)]
		public Button lastPlayedButton;
		public Image lastPlayedBg;
		public Image lastPlayedFrame;
		public TextMeshProUGUI lastPlayedText;
		private GraphicCopier lastPlayedCopier;

		struct ButtonInfo
		{
			public Button button;
			public Image bg;
			public TextMeshProUGUI text;
			public GraphicCopier copier;

			public ButtonInfo(Button button, Image bg, TextMeshProUGUI frame, GraphicCopier copier)
			{
				this.button = button;
				this.bg = bg;
				this.text = frame;
				this.copier = copier;
			}
		}

		bool _inited = false;
		void Init()
		{
			if (_inited)
				return;
			_inited = true;

			nameCopier = nameFrame.gameObject.AddComponent<GraphicCopier>();
			authorCopier = authorFrame.gameObject.AddComponent<GraphicCopier>();
			lastUpdateCopier = lastUpdateFrame.gameObject.AddComponent<GraphicCopier>();
			lastPlayedCopier = lastPlayedFrame.gameObject.AddComponent<GraphicCopier>();

			nameCopier.targetGraphic = nameText;
			authorCopier.targetGraphic = authorText;
			lastUpdateCopier.targetGraphic = lastUpdateText;
			lastPlayedCopier.targetGraphic = lastPlayedText;
		}

		void Awake()
		{
			Init();
		}

		public void SetButtonSelected(Button button)
		{
			Init();

			foreach (ButtonInfo btn in new ButtonInfo[] { new ButtonInfo(nameButton, nameBg, nameText, nameCopier), new ButtonInfo(authorButton, authorBg, authorText, authorCopier), new ButtonInfo(lastUpdateButton, lastUpdateBg, lastUpdateText, lastUpdateCopier), new ButtonInfo(lastPlayedButton, lastPlayedBg, lastPlayedText, lastPlayedCopier) })
			{
				if (btn.button == button)
				{
					btn.copier.targetGraphic = btn.bg;
					btn.copier.targetRenderer = btn.bg.GetComponent<CanvasRenderer>();
					btn.bg.color = Color.white;
					btn.text.color = Color.black;
				}
				else
				{
					btn.copier.targetGraphic = btn.text;
					btn.copier.targetRenderer = btn.text.GetComponent<CanvasRenderer>();
					btn.bg.color = Color.black;
					btn.text.color = Color.white;
				}
			}
		}
	}
}
