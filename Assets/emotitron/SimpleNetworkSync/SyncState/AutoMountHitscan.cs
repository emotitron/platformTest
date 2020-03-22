using emotitron.Utilities.HitGroups;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// This component will generate a hitscan based on the transform it is attached to. For each mount hit, 
	/// SyncState.SoftMount will be called to attempt to reparent to the transform of the Mount.
	/// </summary>
	public class AutoMountHitscan : HitscanComponent
	{
		//public HitGroupMaskSelector validHitGroups;

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			hitscanDefinition.distance = 1;
			visualize = true;

			var rootSyncTransform = netObj.transform.GetNestedComponentInParents<SyncTransform>();
			if (rootSyncTransform)
			{
				if (!rootSyncTransform.transformCrusher.PosCrusher.local)
				{
					Debug.LogWarning(typeof(SyncTransform).Name + " on root of NetObject " + netObj.name + 
						" does not have its position sync set to Local, which is the preferred setting when netObj is going to be changing parents. Setting to true for you.");
					rootSyncTransform.transformCrusher.PosCrusher.local = true;
				}
			}
		}
#endif

		public override void OnAuthorityChanged(bool isMine, bool asServer)
		{
			base.OnAuthorityChanged(isMine, asServer);

			/// Rather than an IsMine test every tick, we are completely removing this object from the OnPreSiulate callback list when !IsMine
			var callbacklist = netObj.onPreSimulateCallbacks;
			bool containsThis = callbacklist.Contains(this);

			base.OnAuthorityChanged(isMine, asServer);
			if (isMine)
			{
				if (!containsThis)
					callbacklist.Add(this);
			}
			else
			{
				if (containsThis)
					callbacklist.Remove(this);
			}
		}

		public override void OnPreSimulate(int frameId, int subFrameId)
		{

			if (subFrameId == SimpleSyncSettings.SendEveryXTick - 1)
			{
				triggerQueued = true;
				//foundMounts.Clear();

				base.OnPreSimulate(frameId, subFrameId);

				if (foundMounts.Count != 0)
				{
					do
					{
						var mount = foundMounts.Dequeue();
						syncState.SoftMount(mount);

					} while (foundMounts.Count != 0);
				}
				else
				{
					syncState.SoftMount(null);
				}
			}
		}

		Queue<Mount> foundMounts = new Queue<Mount>();

		public override bool ProcessHit(Collider hit)
		{
			var mount = hit.transform.GetNestedComponentInParents<Mount>();

			//if (validHitGroups != 0)
			//{
			//	var hga = hit.GetComponent<IHitGroupMask>();

			//	//Debug.Log(hit.name + " " + validHitGroups.Mask + " : " + hga + " : " + hgaa + " : " + (hga as Component ? hga.Mask.ToString() : "???"));

			//	if (!ReferenceEquals(hga, null) && (hga.Mask & validHitGroups) == 0)
			//		return false;

			//}

			if (mount)
			{
				//Debug.Log(Time.time + " " + name + " Mount to " + mount);
				foundMounts.Enqueue(mount);
			}

			return false;
		}
	}



}
