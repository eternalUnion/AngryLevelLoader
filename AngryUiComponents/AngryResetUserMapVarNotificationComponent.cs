using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AngryUiComponents
{
	public class AngryResetUserMapVarNotificationComponent : MonoBehaviour
	{
		public GameObject notFoundText;
		public AngryResetUserMapVarNotificationElementComponent template;
		public Button exitButton;

		public AngryResetUserMapVarNotificationElementComponent CreateTemplate()
		{
			GameObject newTemplate = Instantiate(template.gameObject, template.transform.parent);
			newTemplate.SetActive(true);
			return newTemplate.GetComponent<AngryResetUserMapVarNotificationElementComponent>();
		}
	}
}
