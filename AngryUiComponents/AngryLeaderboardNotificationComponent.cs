using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryLeaderboardNotificationComponent : MonoBehaviour
	{
		public Text header;

		public Dropdown category;
		public Dropdown difficulty;
		public Dropdown group;

		public GameObject localUserRecordContainer;
		public RawImage localUserPfp;
		public Text localUserRank;
		public Text localUserName;
		public Text localUserTime;

		public GameObject recordEnabler;
		public Transform recordContainer;
		public AngryLeaderboardRecordEntryComponent recordTemplate;

		public GameObject pageText;
		public Button nextPage;
		public Button prevPage;
		public InputField pageInput;

		public Button closeButton;

		public GameObject refreshCircle;
		public Text failMessage;
		public Button refreshButton;

		public GameObject reportToggle;
		public GameObject reportFormToggle;
		public GameObject reportResultToggle;

		public Text reportBody;
		public GameObject reportLoadCircle;

		public Toggle inappropriateName;
		public Toggle inappropriatePicture;
		public Toggle cheatedScore;
		public Toggle reportValidation;

		public Button reportCancel;
		public Button reportSend;
		public Button reportReturn;
	}
}
