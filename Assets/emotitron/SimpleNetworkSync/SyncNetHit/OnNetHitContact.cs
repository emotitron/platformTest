
using emotitron.Utilities.Networking;
using emotitron.Utilities;
using UnityEngine;
using emotitron.Utilities.HitGroups;
using System.Collections.Generic;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// Responds to OnHitworkHit callbacks from Hitscans and Projectiles, and creates OnContactEvents for ITriggering components.
	/// </summary>
	public class OnNetHitContact : NetComponent
		, IOnNetworkHit
	{

		[SerializeField] public HitGroupMaskSelector validHitGroups = new HitGroupMaskSelector(0);

		protected List<IContacting> triggeringComponents = new List<IContacting>();

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			validHitGroups = ~(0);
		}
#endif

		public override void OnAwake()
		{
			base.OnAwake();
			transform.GetNestedComponentsInParents(triggeringComponents);
		}


		public void OnNetworkHit(NetworkHits results)
		{
			var hits = results.hits;
			var hitcount = hits.Count;
			var bitsformask = results.bitsForHitGroupMask;

			/// if results is nearest only, we won't be looping
			if (results.nearestOnly)
			{
				ProcessHit(hits[0], bitsformask);
			}
			else
			{
				int cnt = hits.Count;
				for (int i = 0; i < cnt; ++i)
				{
					ProcessHit(hits[i], bitsformask);
				}
			}
		}

		protected static List<IContactTrigger> reusableContactTriggers = new List<IContactTrigger>();

		public void ProcessHit(NetworkHit hit, int bitsForHitGroupMask)
		{
#if PUN_2_OR_NEWER
			var netObj = PhotonView.Find(hit.netObjId).GetComponent<NetObject>();

			var collider = netObj.indexedColliders[hit.colliderId];

			/// Look for IOnTriggeringHitscan relative to the collider
			collider.transform.GetNestedComponentsInParents(reusableContactTriggers);

			int cnt = reusableContactTriggers.Count;
			if (cnt == 0)
				return;

			/// Iterate all of the trigger candidates found
			for (int h = 0; h < cnt; h++)
			{
				var found = reusableContactTriggers[h];

				/// We are only interested in the found triggers if they are networked, since this is NetHit based.
				var foundNetObj = (found as Component).GetComponent<NetObject>();
				if (foundNetObj == null)
					continue;

				int itcCount = triggeringComponents.Count;
				//Debug.Log("Collider " + collider.name + " " + (reusableContactTriggers[0] as Component).GetType().Name + " found: " + (found != null) + " cnt: " + itcCount);

				/// Check the found triggers against ITriggeringComponents to find valid matches
				for (int c = 0; c < itcCount; c++)
				{
					var itc = triggeringComponents[c];

					if (foundNetObj.pv.IsMine)
					{
						int mask = hit.hitMask;
						int hitgroupMask = mask; // 1 << (mask - 1); // (mask == 0) ? 0 : ((int)1 << (mask - 1)); 

						/// Some totally arbitrary logic for how to deal with the HitGroups. TODO: Something real here please
						//Debug.Log((int)validHitGroups + " mask: " + hitgroupMask);

						/// First test for equals allows masks of Default (0) to count as a match. The second test looks for any matching bit.
						if (validHitGroups == hitgroupMask || ((validHitGroups & hitgroupMask) != 0))
						{
							//Debug.Log("<b>HIT !!!</b>");
							var contactEvent = new ContactEvent(null, itc, found as Component, this, ContactType.Hitscan);
							found.Trigger(contactEvent);

						}
					}
				}
			}
#endif
		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(OnNetHitContact))]
	[CanEditMultipleObjects]
	public class OnNetHitContactEditor : ReactorHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Detects <i>" + typeof(IOnNetworkHit).Name + "</i> event, and tests all <i>"
				+ typeof(IOnContactEvent).Name + "</i> found on the hit object against <i>"
				+ typeof(IContacting).Name + "</i> on this/parents of this component. "
				+ "Hits against objects with <i>" + typeof(NetObject).Name + "</i> will be networked and replicated on other clients.";
			}
		}
		
	}

#endif
}

