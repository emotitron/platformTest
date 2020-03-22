//using emotitron.Networking;
//using emotitron.Utilities;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//public class VitalsTriggerReactor : ContactReactorBase<IVitalsComponent>
//	, IOnTrigger
//{

//	#region Inspector
//	[SerializeField]
//	[Tooltip("Will respond to collision with ITriggeringComponent if it is an IVitalsComponent that uses this named vital. All other collisions will be ignored.")]
//	protected VitalNameType vitalNameType = new VitalNameType(VitalType.Health);
//	public VitalNameType VitalNameType { get { return vitalNameType; } }

//	#endregion Inspector

//	//public override bool OnTrigger(ref ContactEvent contactEvent)
//	//{
//	//	var ivc = contactEvent.itc as IVitalsComponent;
//	//	if (ivc == null)
//	//		return false;

//	//	return ivc.TryTrigger(this, ref contactEvent, compatibleMountsMask);

//	//	//Vital vital = contactEvent.GetTriggeringVital(vitalNameType);

//	//	///// Update the triggerEvent with the specific triggering object
//	//	//contactEvent.triggeringObj = vital;

//	//	//return ReferenceEquals(vital, null) ? false : true;
//	//}

//#if UNITY_EDITOR

//	[CustomEditor(typeof(VitalsTriggerReactor))]
//	[CanEditMultipleObjects]
//	public class VitalsTriggerReactorEditor : ReactorHeaderEditor
//	{

//		protected override string Instructions
//		{
//			get
//			{
//				return "Define the target vital for an IVitalsComponent trigger.";
//			}
//		}
//	}
//#endif
//}


