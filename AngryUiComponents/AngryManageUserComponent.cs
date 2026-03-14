using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryManageUserComponent : MonoBehaviour
	{
		public GameObject managePanel;
		public RawImage userIcon;
		public Text userInfo;

		public Toggle cencorProfilePicture;
		public Toggle cencorProfileName;
		public Toggle banUser;
		public Toggle removeRecord;
		public Toggle removeAll;

		public Button cancelManage;
		public Button applyManage;

		public GameObject warningPanel;
		public RawImage warningUserIcon;
		public Text warningUserInfo;
		public Button warningCancelButton;
		public Button warningSubmitButton;
		public Text warningSubmitButtonText;

		public GameObject resultPanel;
		public GameObject loadingCircle;
		public Text resultText;
		public Button returnButton;

		public Button historyButton;
	}
}
