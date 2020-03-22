using emotitron.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	public class OnTriggerValueChange : OnTriggerValueBase
		, IOnContactEvent
	{

		[HideInInspector][SerializeField]
		protected VitalNameType vitalNameType = new VitalNameType(VitalType.Health);
		public VitalNameType VitalNameType { get { return vitalNameType; } }

		public bool allowOverload = false;

		protected override bool ProcessContactEvent(ref ContactEvent contactEvent)
		{
			Vital vital = contactEvent.GetTriggeringVital(vitalNameType);

			if (ReferenceEquals(vital, null))
				return false;

			float value = GetValueForTriggerType(contactEvent.contactType);

			vital.ApplyChange(value, allowOverload);
			return true;
		}


	}

#if UNITY_EDITOR

	[CustomEditor(typeof(OnTriggerValueChange))]
	[CanEditMultipleObjects]
	public class OnTriggerValueChangeEditor : ReactorHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Responds to "+ typeof(IOnContactEvent).Name + " callbacks, and applies value change to indicated Vital.";
			}
		}

		SerializedProperty vitalNameType;

		public override void OnEnable()
		{
			base.OnEnable();
			vitalNameType = serializedObject.FindProperty("vitalNameType");
		}
		protected override void OnInspectorGUIInjectMiddle()
		{
			base.OnInspectorGUIInjectMiddle();
			EditorGUILayout.PropertyField(vitalNameType);
		}

		//public override void OnInspectorGUI()
		//{
		//	base.OnInspectorGUI();
		//	EditorGUILayout.LabelField("<b>OnTriggerEvent()</b>\n{ " + typeof(IVitalsComponent).Name + ".ChangeValue() }", richBox);
		//}
	}
#endif
}
