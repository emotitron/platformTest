using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace emotitron.Networking
{
	

	public class VitalsContactReactors : ContactReactorsBase<IVitalsComponent>
		, IVitalsAffector
	{

		#region Inspector

		// OnTrigger Items
		[SerializeField]
		[Tooltip("Will respond to collision with ITriggeringComponent if it is an IVitalsComponent that uses this named vital. All other collisions will be ignored.")]
		protected VitalNameType vitalNameType = new VitalNameType(VitalType.Health);
		public VitalNameType VitalNameType { get { return vitalNameType; } }

		// OnPickup Items
		[SerializeField] public float _value = 50;
		public float Value { get { return _value; } }

		[Tooltip("Will only trigger a state change if the item is completely or partially consumed. ie. If this is a health pickup, it will only pickup if player's health is not full.")]
		[SerializeField]
		protected bool _onlyPickupIfUsed = true;

		public bool OnlyPickupIfUsed { get { return _onlyPickupIfUsed; } }

		[SerializeField] public bool _allowOverload = true;
		public bool AllowOverload { get { return _allowOverload; } }

		#endregion Inspector

	}

}
