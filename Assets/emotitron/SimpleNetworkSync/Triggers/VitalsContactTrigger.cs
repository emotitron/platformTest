////Copyright 2019, Davin Carten, All rights reserved

//using UnityEngine;
//using emotitron.Utilities;
//using emotitron.Utilities.Networking;
//using System.Collections.Generic;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Networking
//{

//	public abstract class VitalsContactTrigger : ContactTrigger
//		, IOnTriggeringStay
//		, IOnTriggeringEnter
//		, IOnTriggeringExit
//	{
//		[SerializeField] 
//		[HideInInspector]
//		protected float valueOnEnter = 20;

//		[SerializeField]
//		[HideInInspector]
//		protected float valuePerSec = 20;

//		[SerializeField]
//		[HideInInspector]
//		protected float valueOnExit = 0;

//		//cached
//		protected float valuePerFixed;

//		protected override void Awake()
//		{
//			base.Awake();

//			valuePerFixed = valuePerSec * Time.fixedDeltaTime;

//		}

//		public override void OnTriggeringEnter(TriggerEvent triggerEvent)
//		{
//			if ((triggerOn & triggerEvent.collideType) == 0)
//				return;

//			var itc = triggerEvent.itc;

//			if (preventRepeats && triggeredEnters.Contains(triggerEvent.itc))
//				return;

//			triggeredEnters.Add(itc);
//			Trigger(triggerEvent);
//			//Apply(trigger, valueOnEnter);
//		}

//		public override void OnTriggeringStay(TriggerEvent triggerEvent)
//		{
//			if ((triggerOn & triggerEvent.collideType) == 0)
//				return;

//			var itc = triggerEvent.itc;

//			if (preventRepeats && triggeredStays.Contains(triggerEvent.itc))
//				return;

//			triggeredStays.Add(itc);
//			Trigger(triggerEvent);

//			//Apply(trigger, valuePerFixed);
//		}

//		public override void OnTriggeringExit(TriggerEvent triggerEvent)
//		{
//			if ((triggerOn & triggerEvent.collideType) == 0)
//				return;

//			var itc = triggerEvent.itc;

//			if (preventRepeats && !triggeredEnters.Contains(itc))
//				return;

//			triggeredEnters.Remove(itc);
//			Trigger(triggerEvent);

//			//Apply(trigger, valueOnExit);
//		}

//		protected float GetValueForTriggerType(ContactType collideType)
//		{
			
//			float value;
//			switch (collideType)
//			{
//				case ContactType.Enter:
//					{
//						value = valueOnEnter;
//						break;
//					}
//				case ContactType.Stay:
//					{
//						value = valuePerFixed;
//						break;
//					}
//				case ContactType.Exit:
//					{
//						value = valueOnExit;
//						break;
//					}
//				default:
//					value = 0;
//					break;
//			}
//			return value;
//		}

//		//protected abstract void Apply(TriggerEvent trigger, float value);
//	}
	

//#if UNITY_EDITOR

//	[CustomEditor(typeof(VitalsContactTrigger))]
//	[CanEditMultipleObjects]
//	public class VitalsContactTriggerEditor : ContactTriggerEditor
//	{
//		//protected override string TextTexturePath
//		//{
//		//	get { return "Header/VitalsSystemText"; }
//		//}

//		protected override string TPotTexturePath
//		{
//			get { return "Header/TeapotVitalsBW"; }
//		}

//		//protected override string BackTexturePath
//		//{
//		//	get { return "Header/RedBack"; }
//		//}

//		//protected SerializedProperty valueOnEnter, valuePerSec, valueOnExit;

//		//public override void OnEnable()
//		//{
//		//	base.OnEnable();
//		//	valueOnEnter = serializedObject.FindProperty("valueOnEnter");
//		//	valuePerSec = serializedObject.FindProperty("valuePerSec");
//		//	valueOnExit = serializedObject.FindProperty("valueOnExit");
//		//}

//		protected override string Instructions
//		{
//			get
//			{
//				return "Designates an Object/Collider as a vital affector source and will trigger calls to any Vitals component that is overlapping.";
//			}
//		}

//		//public override void OnInspectorGUI()
//		//{
//		//	base.OnInspectorGUI();

//		//	EditorGUI.BeginChangeCheck();

//		//	EditorGUILayout.PropertyField(valueOnEnter);
//		//	EditorGUILayout.PropertyField(valuePerSec);
//		//	EditorGUILayout.PropertyField(valueOnExit);

//		//	if (EditorGUI.EndChangeCheck())
//		//		serializedObject.ApplyModifiedProperties();
//		//}

//	}
//#endif
//}

