using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
    public class AngryDeleteOldBundlesNotificationComponent : MonoBehaviour
    {
        public Text body;
        public Button cancelButton;
        public Text cancelButtonText;
        public Button deleteButton;
        public Text deleteButtonText;

        public GameObject loadingCircle;
        public Text deletionProgress;
        public Text deletionLog;
    }
}
