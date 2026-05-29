using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryReportEntryComponent : MonoBehaviour
	{
		public RawImage bundleIcon;
		public RawImage levelIcon;
		public TextMeshProUGUI senderInfo;
		public TextMeshProUGUI bundleInfo;
		public TextMeshProUGUI recordInfo;
		public TextMeshProUGUI time;

		public Button manageReceiver;
		public Button manageSender;
		public Button markAsDone;
	}
}
