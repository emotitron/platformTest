//Copyright 2019, Davin Carten, All rights reserved

using emotitron.Utilities;
using emotitron.Utilities.Networking;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace emotitron.Networking
{

	public class OnTriggerApplyDamage : OnTriggerValueBase
		, IOnContactEvent
	{
		
		protected override bool ProcessContactEvent(ref ContactEvent contactEvent)
		{
			IDamageable vitals = (contactEvent.itc as IDamageable);

			if (ReferenceEquals(vitals, null))
				return false;

			float value = GetValueForTriggerType(contactEvent.contactType);

			if (!ReferenceEquals(vitals, null))
				vitals.ApplyDamage(value);

			return true;
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(OnTriggerApplyDamage))]
	[CanEditMultipleObjects]
	public class OnTriggerApplyDamageEditor : ReactorHeaderEditor
	{

		protected override string Instructions
		{
			get
			{
				return "Designates an Object/Collider as a damage source and will trigger ApplyDamage() calls to any Vitals component that is overlapping.";
			}
		}

		//protected override void OnInspectorGUIInjectMiddle()
		//{
		//	base.OnInspectorGUIInjectMiddle();
		//	EditorGUILayout.LabelField("<b>OnTriggerEvent()</b>\n{ " + typeof(IDamageable).Name + ".ApplyDamage() }", richLabel);
		//}
	}
#endif
}

