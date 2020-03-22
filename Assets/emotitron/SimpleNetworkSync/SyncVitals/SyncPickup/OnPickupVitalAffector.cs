//using emotitron.Utilities;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace emotitron.Networking
//{
//	public class OnPickupVitalAffector : MonoBehaviour
//		, IOnPickup
//	{

//		#region Inspector

//		[Header("Affector:")]

//		[SerializeField] public float _value = 50;
//		public float Value { get { return _value; } }

//		[Tooltip("Will only trigger a state change if the item is completely or partially consumed. ie. If this is a health pickup, it will only pickup if player's health is not full.")]
//		[SerializeField]
//		protected bool _onlyPickupIfUsed = true;
//		public bool OnlyPickupIfUsed { get { return _onlyPickupIfUsed; } }


//		[SerializeField] public bool _allowOverload = false;
//		public bool AllowOverload { get { return _allowOverload; } }

//		#endregion Inspector

//		public bool OnPickup(TriggerEvent triggerEvent/*, Vital vital, IVitalsComponent pickedUpBy*/)
//		{
//			var ivc = triggerEvent.itc as IVitalsComponent;

//			if (ReferenceEquals(ivc, null))
//				return false;

//			Debug.Log("OnVitalPickup " + (ivc != null));
//			var trigger = triggerEvent.trigger as IVitalsTrigger;
//			var vitalName = trigger.VitalNameType;
//			var vital = ivc.Vitals.GetVital(vitalName);

//			if (ReferenceEquals(vital, null))
//				return false;

//			Debug.LogError("OnVPU " + ivc.IsMine);
//			/// Apply to vital if vital has authority.
//			if (ivc.IsMine)
//			{
//				float remainder = vital.ApplyChange(_value, _allowOverload);
//				return !_onlyPickupIfUsed || _value != remainder;
//			}
//			/// Vital does not belong to us, but we want to know IF it would have been consumed for prediction purposes.
//			else
//			{
//				if (_onlyPickupIfUsed)
//				{
//					float remainder = vital.TestApplyChange(_value, _allowOverload);
//					return _value != remainder;
//				}
//				return true;
//			}
//		}
//	}
//}
