using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryReportViewComponent : MonoBehaviour
	{
		public RawImage userIcon;
		public Text userInfo;

		public Button viewUserHistoryButton;
		public Button removeAllReportsButton;
		public TextMeshProUGUI removeAllReportsText;

		public Button backButton;
		public Button nextPageButton;
		public Button prevPageButton;
		public InputField pageNumber;
		public Button nextReportButton;
		public Button previousReportButton;

		public AngryReportEntryComponent template;
		public CanvasGroup UIBlockGroup;
		public TextMeshProUGUI errorText;

		public AngryManageUserComponent manageUserPanel;
		public AngryUserHistoryPanelComponent historyPanel;
	}
}
