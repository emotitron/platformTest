
using emotitron.Utilities;
using emotitron.Utilities.HitGroups;
using emotitron.Utilities.Networking;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// Commmon Reactor code for Vitals. These allow multiple components to do similar things without a lot of repeated code.
	/// </summary>
	public static class VitalsReactors
	{

		/// <summary>
		/// Check if the ITriggeringComponent is an IVitalsComponent, and if so that it has the vital that matches the trigger.
		/// </summary>
		/// <param name="frameInt"></param>
		/// <param name="contactEvent"></param>
		/// <returns>Returns true if this triggerer is a IVitalsComponent, and contains the indicated vital type.</returns>
		public static Vital GetTriggeringVital(this ContactEvent contactEvent, VitalNameType vitalNameType)
		{
			var ivc = (contactEvent.itc as IVitalsComponent);

			if (ReferenceEquals(ivc, null))
				return null;

			Vitals vitals = ivc.Vitals;
			Vital vital = vitals.GetVital(vitalNameType);

			return vital;
		}

		///// <summary>
		///// 
		///// </summary>
		///// <returns>Returns true if consumed.</returns>
		//public static Mount TryVitalPickup(this ContactEvent contactEvent, float value, bool allowOverload, bool onlyPickupIfUsed)
		//{

		//	var ivc = contactEvent.itc as IVitalsComponent;

		//	if (ReferenceEquals(ivc, null))
		//		return null;

		//	var vital = contactEvent.triggeringObj as Vital;

		//	//var vital = ivc.Vitals.GetVital(vitalNameType);

		//	if (ReferenceEquals(vital, null))
		//		return null;

		//	/// Apply to vital if vital has authority.
		//	if (ivc.IsMine)
		//	{
		//		float remainder = vital.ApplyChange(value, allowOverload);
		//		return (!onlyPickupIfUsed || value != remainder) ? ivc.DefaultMount : null;
		//	}
		//	/// Vital does not belong to us, but we want to know IF it would have been consumed for prediction purposes.
		//	else
		//	{
		//		if (onlyPickupIfUsed)
		//		{
		//			float remainder = vital.TestApplyChange(value, allowOverload);
		//			return value != remainder ? ivc.DefaultMount : null;
		//		}
		//		return ivc.DefaultMount;
		//	}
		//}

		public static void ProcessHit(this NetworkHit hit, float damage, HitGroupValues multipliers, int bitsForHitGroupMask)
		{
			//var go = UnifiedNetTools.FindGameObjectByNetId(hit.netObjId);

#if PUN_2_OR_NEWER
			var netObj = PhotonView.Find(hit.netObjId).GetComponent<NetObject>();

			var collider = netObj.indexedColliders[hit.colliderId];

			/// Look for IDamageable relative to the collider
			var iDamageable = collider.GetComponentInParent<IDamageable>();

			if (ReferenceEquals(iDamageable, null))
				return;

			if (iDamageable.IsMine)
			{
				int mask = hit.hitMask;

				/// Some totally arbitrary logic for how to deal with the HitGroups. TODO: Something real here please
				if (mask != 0)
				{
					float multiplier = 1f;
					float mitigation = 1f;

					for (int hg = 0; hg < bitsForHitGroupMask; ++hg)
					{
						if ((mask & 1 << hg) != 0)
						{
							int index = hg + 1;
							multiplier = System.Math.Max(multiplier, multipliers.values[index]);
							mitigation = System.Math.Min(mitigation, multipliers.values[index]);
						}
					}
					//Debug.Log("Hit " + mask + " dmg:" + damage + " / " + mitigation + " / " + multiplier);
					iDamageable.ApplyDamage(damage * mitigation * multiplier);
				}
				else
					iDamageable.ApplyDamage(damage);
			}
#endif
		}

	}

}

