//Copyright 2019, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Utilities;
using System.Collections.Generic;
using emotitron.Utilities.HitGroups;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	public class ContactTrigger : MonoBehaviour
		, IContactTrigger
		, IContactable
		, IOnPreSimulate
		//, IOnStateChange
	{
		#region Inspector

		//[Utilities.GUIUtilities.VersaMask(typeof(ContactType))]
		//public ContactType triggerOn = ContactType.Enter | ContactType.Hitscan;

		[Tooltip("If ITriggeringComponent has multiple colliders, they will all be capable of triggering Enter/Stay/Exit events. Enabling this prevents that, and will suppress multiple calls on the same object.")]
		[SerializeField] public bool preventRepeats = true;
		public bool PreventRepeats { get { return preventRepeats; } set { preventRepeats = value; } }

		[Tooltip("Will trigger callbacks, even if the hit object is not an ITriggeringComponent.")]
		[SerializeField] protected bool triggerOnNull = false;
		public bool TriggerOnNull { get { return TriggerOnNull; } set { triggerOnNull = value; } }

		#endregion

		//[SerializeField] public HitGroupMaskSelector validHitGroups = new HitGroupMaskSelector(0);

		public List<IOnContactEvent> onContactEventCallbacks = new List<IOnContactEvent>();
		[System.NonSerialized]
		public List<IContacting> reusableITriggering = new List<IContacting>();

		//[Tooltip("This ContactTrigger can act as a proxy of another. " +
		//	"For example non-networked projectiles set the proxy as the shooters ContactTrigger, so projectile hits can be treated as hits by the players weapon. " +
		//	"Default setting is 'this', indicating this isn't a proxy.")]
		[SerializeField]
		protected IContactTrigger proxy;
		public IContactTrigger Proxy {
			get { return proxy; }
			set { proxy = value; }
		}

		protected bool hasRB;
		protected Rigidbody rb;
		protected Rigidbody2D rb2d;
		public bool HasRigidbody { get { return hasRB; } }

		/// Hashsets used to make sure things only trigger once per tick, so multiple colliders
		/// can't cause multiple retriggers
		protected HashSet<IContacting> triggeringHitscans, triggeringEnters,triggeringStays;

#if UNITY_EDITOR

		protected virtual void Reset()
		{
			proxy = this;
		}

#endif
		protected virtual void Awake()
		{
			transform.GetNestedComponentsInChildren(onContactEventCallbacks);

			rb = GetComponentInParent<Rigidbody>();
			rb2d = GetComponentInParent<Rigidbody2D>();
			hasRB = rb || rb2d;

			if (preventRepeats)
			{
				triggeringHitscans = new HashSet<IContacting>();
				triggeringEnters = new HashSet<IContacting>();
				triggeringStays = new HashSet<IContacting>();
			}

			/// Default to no proxy
			if (!(proxy as Component))
				proxy = this;
		}

		protected virtual void OnEnable()
		{
			if (preventRepeats)
			{
				NetMaster.RegisterCallbackInterfaces(this);

				triggeringHitscans.Clear();
				triggeringEnters.Clear();
				triggeringStays.Clear();
			}
		}

		protected virtual void OnDisable()
		{
			if (preventRepeats)
				NetMaster.RegisterCallbackInterfaces(this, false, true);
		}


		//public void OnStateChange(ObjState state, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
		//{
		//	if (preventRepeats)
		//		if (state == ObjState.Despawned)
		//		{
		//			triggeredHitscans.Clear();
		//			triggeredEnters.Clear();
		//			triggeredStays.Clear();
		//		}
		//}

		/// <summary>
		/// Converts Unity Collision/Trigger events into OnContact calls on both ContactTriggers involved.
		/// </summary>
		protected virtual void Contact(Component otherCollider, ContactType contactType)
		{
			var otherCT = otherCollider.transform.GetNestedComponentInParents<IContactTrigger>();

			Debug.Log("contact " + name + " : " + (otherCT as Component).name + " : " + (proxy as Component).name);

			if (!ReferenceEquals(otherCT, null))
			{
				/// Ignore self collisions
				if (ReferenceEquals(proxy, otherCT.Proxy))
				{
					//Debug.Log("Early self-collide detect " + contactTriggerProxy + " " + otherCT.contactTriggerProxy);
					return;
				}

				otherCT.OnContact(this, this, contactType);
				this.OnContact(otherCT, otherCollider, contactType);
			}
		}

		
		public virtual void OnContact(IContactTrigger otherCT, Component otherCollider, ContactType contactType)
		{
			Debug.Log("contact " + name);

			///// Ignore collisions if the HitGroups don't check out
			//var otherHGS = otherCollider.GetComponent<HitGroupAssign>();
			//int otherMask = otherHGS ? otherHGS.LayerMask : 0;
			//if (validHitGroups != 0 && (otherMask & validHitGroups) == 0)
			//	return;

			/// Find an ITriggering component on other
			if (ReferenceEquals(otherCT.Proxy, otherCT))
			{
				otherCollider.transform.GetNestedComponentsInParents(reusableITriggering);
			}
			else
			{
				(otherCT.Proxy as Component).transform.GetNestedComponentsInParents(reusableITriggering);
			}

			//if (contactType == ContactType.Enter)
			//	Debug.Log(name + " <b>" + contactType + "</b> " + otherCollider.name + ":" + otherCollider.GetType().Name + 
			//		" ticCnt: " + reusableTriggeringComponents.Count);

			/// Check to see if we have already reacted to this collision (multiple colliders/etc)
			int cnt = reusableITriggering.Count;
			for (int i = 0; i < cnt; i++)
			{
				var itc = reusableITriggering[i];
				if (preventRepeats)
				{
					switch (contactType)
					{
						case ContactType.Enter:
							{
								if (triggeringEnters.Contains(itc))
									continue;

								triggeringEnters.Add(itc);
								break;
							}
						case ContactType.Stay:
							{
								if (triggeringStays.Contains(itc))
									continue;

								triggeringStays.Add(itc);
								break;
							}
						case ContactType.Exit:
							{
								if (!triggeringEnters.Contains(itc))
									continue;

								triggeringEnters.Remove(itc);
								break;
							}
						case ContactType.Hitscan:
							{
								if (triggeringHitscans.Contains(itc))
									continue;

								triggeringHitscans.Add(itc);
								break;
							}
					}
				}

				//if (contactType == ContactType.Enter)
				//	Debug.Log(name + " " + (reusableTriggeringComponents[i] as Component).name + " : " + reusableTriggeringComponents[i].GetType());

				Trigger(new ContactEvent(null, itc, this, otherCollider, contactType));
			}
		}

		#region Triggers

		#region Enter

		private void OnTriggerEnter2D(Collider2D other)
		{
			Contact(other, ContactType.Enter);
		}
		private void OnTriggerEnter(Collider other)
		{
			Contact(other, ContactType.Enter);
		}
		private void OnCollisionEnter2D(Collision2D collision)
		{
			Contact(collision.collider, ContactType.Enter);
		}
		private void OnCollisionEnter(Collision collision)
		{
			Contact(collision.collider, ContactType.Enter);
		}

		#endregion Enter

		#region Stay

		private void OnTriggerStay2D(Collider2D other)
		{
			Contact(other, ContactType.Stay);
		}
		private void OnTriggerStay(Collider other)
		{
			Contact(other, ContactType.Stay);
		}
		private void OnCollisionStay2D(Collision2D collision)
		{
			Contact(collision.collider, ContactType.Stay);
		}
		private void OnCollisionStay(Collision collision)
		{
			Contact(collision.collider, ContactType.Stay);
		}

		#endregion Stay

		#region Exit

		private void OnTriggerExit2D(Collider2D other)
		{
			Contact(other, ContactType.Exit);
		}
		private void OnTriggerExit(Collider other)
		{
			Contact(other, ContactType.Exit);
		}
		private void OnCollisionExit2D(Collision2D collision)
		{
			Contact(collision.collider, ContactType.Exit);
		}
		private void OnCollisionExit(Collision collision)
		{
			Contact(collision.collider, ContactType.Exit);
		}

		#endregion Exit

		public virtual void Trigger(ContactEvent contactEvent)
		{
			Debug.Log("Trigger " + name);
			int cnt = onContactEventCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onContactEventCallbacks[i].OnContactEvent(ref contactEvent);
		}

		#endregion Triggers

		/// <summary>
		/// This callback only is registered if preventRepeats is true.
		/// </summary>
		public void OnPreSimulate(int frameId, int subFrameId)
		{
			if (preventRepeats)
			{
				triggeringHitscans.Clear();
				triggeringStays.Clear();
			}
		}

		
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(ContactTrigger))]
	[CanEditMultipleObjects]
	public class ContactTriggerEditor : TriggerHeaderEditor
	{
		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.wlli7lh83npf";
			}
		}
		protected override string Instructions
		{
			get
			{
				return "Responds to Trigger/Collision events between this and " + typeof(IContacting).Name + 
					" objects, and produces an OnTriggerEvent() callback to components with " + typeof(IOnContactEvent).Name + ".";
			}
		}

	}
#endif
}

