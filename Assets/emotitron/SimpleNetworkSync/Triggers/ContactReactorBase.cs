using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	public abstract class ContactReactorsBase<T> : NetComponent 
		, IContactReactor<T>
		, IOnTrigger
		, IOnPickup
		where T : class, IContacting
	{
		protected int compatibleMountsMask;

		public override void OnAwake()
		{
			base.OnAwake();

			var syncState = GetComponent<SyncState>();

			if (syncState)
				compatibleMountsMask = syncState.mountableTo;
		}
		
		public virtual bool OnTrigger(ref ContactEvent contactEvent)
		{
			var itc = contactEvent.itc as T;
			if (itc == null)
				return false;

			///TODO: this should eventually not be needed
			contactEvent.triggeringObj = itc;

			return itc.TryTrigger(this, ref contactEvent, compatibleMountsMask);
		}

		public virtual Mount OnPickup(ContactEvent contactEvent)
		{
			var itc = contactEvent.itc as T;

			if (ReferenceEquals(itc, null))
				return null;

			return itc.TryPickup(this, contactEvent);
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(ContactReactorsBase<>), true)]
	[CanEditMultipleObjects]
	public class ContactReactorsBaseEditor : ReactorHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Reacts to <i>" + typeof(IOnTrigger).Name + "</i> and <i>" + typeof(IOnPickup).Name + "</i> events, " +
					"by testing for a valid interaction with the <i>" + typeof(IContacting).Name + "</i> involved in the contact.";
			}
		}
	}
#endif
}
