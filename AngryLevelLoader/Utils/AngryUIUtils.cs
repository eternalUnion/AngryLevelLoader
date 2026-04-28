using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AngryLevelLoader.Utils
{
	internal static class AngryUIUtils
	{
        public static void AddMouseEvents(GameObject field, Button btn, Action<BaseEventData> mouseOnEvent, Action<BaseEventData> mouseOffEvent)
        {
            EventTrigger trigger = field.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = field.AddComponent<EventTrigger>();
                PluginConfig.API.Utils.AddScrollEvents(trigger, PluginConfig.API.Utils.GetComponentInParent<ScrollRect>(btn.transform));
            }

            EventTrigger.Entry mouseOn = new EventTrigger.Entry() { eventID = EventTriggerType.PointerEnter };
            mouseOn.callback.AddListener(e => mouseOnEvent(e));
            EventTrigger.Entry mouseOff = new EventTrigger.Entry() { eventID = EventTriggerType.PointerExit };
            mouseOff.callback.AddListener(e => mouseOffEvent(e));
            trigger.triggers.Add(mouseOn);
            trigger.triggers.Add(mouseOff);
        }
    }
}
