using System.Collections;
using System.Collections.Generic;
using emotitron.Utilities;
using emotitron.Utilities.GenericHitscan;
using emotitron.Utilities.Networking;
using UnityEngine;

namespace emotitron.Networking
{
	
	/// <summary>
	/// A non-networked version of SyncHitscan. Will ony trigger Contact events locally.
	/// </summary>
	public class ContactingHitscan : HitscanComponent
		, IOnPreUpdate
	{

		[Tooltip("Built in convenience KeyCode for trigger this hitscan. If set to null, the Update callback will be removed at startup, so setting this after startup to a value will not work.")]
		public KeyCode triggerKey = KeyCode.None;

		protected List<IContacting> contactingComponents = new List<IContacting>();
		protected static List<IContactTrigger> reusableOnTriggerEvents = new List<IContactTrigger>();

		public override void OnAwake()
		{
			base.OnAwake();

			// Remmove the callback at startup if we aren't actually checking for the trigger key
			if (triggerKey == KeyCode.None)
			{
				bool OnPreUpdateHasBeenDerived = this.GetType().GetMethod("OnPreUpdate").DeclaringType != this.GetType();
				if (!OnPreUpdateHasBeenDerived)
					netObj.onPreUpdateCallbacks.Remove(this);
			}

			transform.GetNestedComponentsInParents(contactingComponents);
		}

		public virtual void OnPreUpdate()
		{
			if (Input.GetKeyDown(triggerKey))
				triggerQueued = true;
		}

		public override bool ProcessHit(Collider hit)
		{
			/// TODO: May at some point want to restrict any GetComponent calls to Nested NetObj
			//hits[i].GetNestedComponentInParent();
			hit.GetComponentsInParent(false, reusableOnTriggerEvents);

			int cnt = reusableOnTriggerEvents.Count;
			//Debug.Log("Hitcount: " + hitcount + " cnt:" + cnt);
			for (int h = 0; h < cnt; h++)
			{
				var contacted = reusableOnTriggerEvents[h];
				for (int c = 0; c < contactingComponents.Count; c++)
				{
					var itc = contactingComponents[c];
					contacted.Trigger(new ContactEvent(null, itc, contacted as Component, this, ContactType.Hitscan));
					Debug.Log(name + " Hit itc: " + (itc as Component).name + " found: " + contacted.GetType().Name + " on " + (contacted as Component).name);
				}

			}
			return false;
		}
	}
}
