using emotitron.Utilities;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GhostWorlds;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// Derived version of SyncVitalTrigger, adding networking of despawn/respawn states and ID of object that picked it up.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NetObject))]
	public class SyncPickup : SyncTrigger
		, IUseKeyframes
		, IOnSnapshot
		, IOnCaptureState
		, IOnStateChange
	{
		[System.NonSerialized] public List<IOnPickup> onPickupCallbacks = new List<IOnPickup>();

		// Cached objects
		protected int mountMask;

		#region Startup

		public override void OnAwake()
		{
			base.OnAwake();

			transform.GetNestedComponentsInChildren(onPickupCallbacks);
		}

		#endregion Startup

		public override void OnCaptureCurrentState(int frameId, Realm realm)
		{
			Frame frame = frames[frameId];

			/// Check for valid triggering collision
			ContactEvent contactEvent;
			bool found = GetFirstTriggerEvent(frame, out contactEvent);

			if (found)
			{
				OnPickup(frame, contactEvent);
			}
			else
			{
				frame.triggeredById = null;
			}
			
			frame.CopyFrom(currentState);
		}

		/// <summary>
		/// Pickup trigger code on Owner. This fires if OnTrigger 
		/// </summary>
		protected virtual void OnPickup(Frame frame, ContactEvent contactEvent)
		{
			Mount mount = null;
			/// Callbacks for Pickup - Trigger effect
			for (int i = 0; i < onPickupCallbacks.Count; ++i)
			{
				var usedmount = onPickupCallbacks[i].OnPickup(contactEvent);

				if (usedmount)
					mount = usedmount;
			}

			if (mount)
			{
				syncState.HardMount(mount);
			}
		}

		/// <summary>
		/// Responds to State change from SyncState
		/// </summary>
		public void OnStateChange(ObjState state, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
		{

			/// If IsMine, we already Fired this in order to test for consumption.
			if (!IsMine)
			{
				var currentAttachedIVC = (attachTo) ? attachTo.GetComponentInParent<IContacting>() : null;
				var triggerEvent = new ContactEvent(null, currentAttachedIVC, null, null, ContactType.Enter);

				for (int i = 0; i < onPickupCallbacks.Count; ++i)
				{
					onPickupCallbacks[i].OnPickup(triggerEvent);
				}
			}
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncPickup))]
	[CanEditMultipleObjects]
	public class SyncPickupEditor : SyncTriggerTFrameEditor
	{
		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncPickupText";
			}
		}

		protected override string Instructions
		{
			get
			{
				return base.Instructions + "Handles the syncing of NetObjects that can be picked up or attached by other NetObjects." +
					"\nComponents with " + typeof(IOnPickup).Name + " will receive callbacks.";
			}
		}
	}
#endif
}
