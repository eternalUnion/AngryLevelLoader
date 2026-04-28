using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryUserHistoryPanelComponent : MonoBehaviour
	{
		public RawImage userIcon;
		public Text userInfo;

		public Button backButton;
		public Button nextPageButton;
		public Button prevPageButton;
		public InputField pageNumber;

		public Dropdown categoryFilter;
		public Dropdown difficultyFilter;
		public Dropdown sortOrder;
		public Toggle reverseOrder;

		public InputField bundleName;
		public InputField levelName;

		public CanvasGroup inputGroup;
		public AngryUserRecordEntryComponent template;
		public GameObject loadingCircle;
	}
}
