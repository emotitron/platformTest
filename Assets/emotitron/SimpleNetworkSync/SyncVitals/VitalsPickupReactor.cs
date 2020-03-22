
//using emotitron.Utilities;
//using UnityEngine;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Networking
//{
	
//	public class VitalsPickupReactor : MonoBehaviour
//		//, IMountMaskSelector
//		, IOnPickup
//	{
//		#region Inspector
		
//		//public MountMaskSelector mountMask = new MountMaskSelector(1);
//		//public int MountMask { get { return mountMask.mask; } }

//		[SerializeField] public float _value = 50;
//		public float Value { get { return _value; } }

//		[Tooltip("Will only trigger a state change if the item is completely or partially consumed. ie. If this is a health pickup, it will only pickup if player's health is not full.")]
//		[SerializeField]
//		protected bool _onlyPickupIfUsed = true;

//		public bool OnlyPickupIfUsed { get { return _onlyPickupIfUsed; } }

//		[SerializeField] public bool _allowOverload = true;
//		public bool AllowOverload { get { return _allowOverload; } }
		
//		#endregion Inspector
		
//		public Mount OnPickup(ContactEvent contactEvent)
//		{
//			var ivc = contactEvent.itc as IVitalsComponent;

//			if (ReferenceEquals(ivc, null))
//				return null;

//			return contactEvent.itc.TryPickup(this, contactEvent);
//			//return contactEvent.TryVitalPickup(_value, _allowOverload, _onlyPickupIfUsed);
//		}
//	}

//#if UNITY_EDITOR

//	[CustomEditor(typeof(VitalsPickupReactor))]
//	[CanEditMultipleObjects]
//	public class VitalsPickupReactorsEditor : ReactorHeaderEditor
//	{
		
//		protected override string Instructions
//		{
//			get
//			{
//				return "Define the target vital and effect for an IPickup / IVitalsComponent trigger.";
//			}
//		}
//	}
//#endif
//}

