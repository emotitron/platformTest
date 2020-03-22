using emotitron.Utilities;
using emotitron.Utilities.GUIUtilities;
using emotitron.Utilities.HitGroups;
using emotitron.Utilities.Networking;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class Projectile : MonoBehaviour
		, IProjectile
		, IOnPreSimulate
		//, IOnInterpolate
		, IOnContactEvent
		, IOnPreUpdate
	{
		protected IProjectileLauncher owner;
		public IProjectileLauncher Owner { get { return owner; } set { owner = value; } }

		[System.NonSerialized] public Vector3 velocity;
		[System.NonSerialized] public int frameId;
		[System.NonSerialized] public int subFrameId;

		#region Inspector

		[SerializeField] [EnumMask] protected RespondTo terminateOn =  RespondTo.HitNetObj | RespondTo.HitNonNetObj;
		[SerializeField] [EnumMask] protected RespondTo damageOn = RespondTo.HitNetObj | RespondTo.HitNonNetObj;
		[SerializeField] protected bool ignoreOwner;

		#endregion

		// Cache
		protected Rigidbody rb;
		protected Rigidbody2D rb2d;
		protected bool _hasRigidBody;
		public bool HasRigidbody { get { return _hasRigidBody; } }
		protected bool needsSnapshot;
		protected IContactTrigger ownerContactTrigger;
		protected IContactTrigger localContactTrigger;
		

		public VitalNameType VitalNameType { get { return new VitalNameType(VitalType.None); } }

		/// Hit callbacks
		public List<IOnNetworkHit> onHit = new List<IOnNetworkHit>();
		public List<IOnTerminate> onTerminate = new List<IOnTerminate>();

		private void Reset()
		{
			/// Projectiles need to detect collisions with everything
			localContactTrigger = GetComponent<IContactTrigger>();
			if (ReferenceEquals(localContactTrigger, null))
				localContactTrigger = gameObject.AddComponent<ContactTrigger>();

			localContactTrigger.TriggerOnNull = true;
		}

		private void Awake()
		{
			rb = GetComponentInParent<Rigidbody>();
			rb2d = GetComponentInParent<Rigidbody2D>();
			_hasRigidBody = rb || rb2d;

			needsSnapshot = !_hasRigidBody || (rb && rb.isKinematic) || (rb2d && rb2d.isKinematic);
			localContactTrigger = GetComponent<IContactTrigger>();
			/// Register timing callbacks with Master. 
			/// TODO: We likely should slave timings off of the owner
			if (needsSnapshot)
				NetMaster.RegisterCallbackInterfaces(this);

			/// No need for the interpolation callback if we are using forces.
			if (_hasRigidBody)
				NetMaster.onPreUpdates.Remove(this);

			/// Find interfaces for termination callbacks
			GetComponents(onHit);
			GetComponents(onTerminate);
		}

		private void OnDestroy()
		{
			NetMaster.RegisterCallbackInterfaces(this, false, true);
		}

		public void Initialize(IProjectileLauncher owner, int frameId, int subFrameId, Vector3 velocity, RespondTo terminateOn, RespondTo damageOn)
		{
			this.owner = owner;
			this.velocity = velocity;
			this.terminateOn = terminateOn;
			this.damageOn = damageOn;
			this.frameId = frameId;
			this.subFrameId = subFrameId;

			if (rb)
				rb.velocity = rb.rotation * velocity;
			/// TODO: NOT TESTED
			else if (rb2d)
				rb2d.velocity = rb2d.transform.TransformVector(velocity);

			snap = transform.position;
			targ = transform.position;

			localContactTrigger.Proxy = owner.ContactTrigger;
		}

		public bool OnContactEvent(ref ContactEvent contactEvent)
		{
			switch (contactEvent.contactType)
			{
				case ContactType.Enter:
					{
						return OnEnter(ref contactEvent);
					}

				case ContactType.Stay:
					{
						return OnStay(ref contactEvent);
					}

				case ContactType.Exit:
					{
						return OnExit(ref contactEvent);
					}

				default:
					return false;
			}
		}

		protected virtual bool OnEnter(ref ContactEvent contactEvent)
		{
			/// Ignore collision with self/owner
			if (ignoreOwner && !ReferenceEquals(contactEvent.itc, null) && contactEvent.itc.NetObjId == owner.NetObjId)
				return false;

			//Debug.Log("contacted: " + contactEvent.contacted.name + " thisCol" + contactEvent.contacted.transform.root.name + " contactor: " + contactEvent.contacter.name);
			OnHit(contactEvent.contacter);
			return true;
		}

		protected virtual bool OnStay(ref ContactEvent contactEvent)
		{
			return false;

		}

		protected virtual bool OnExit(ref ContactEvent contactEvent)
		{
			return false;
		}


		Vector3 snap, targ;

		/// Pre Fixed
		public void OnPreSimulate(int frameId, int subFrameId)
		{
			if (rb)
			{
				rb.MovePosition(rb.position + rb.rotation * velocity * Time.fixedDeltaTime);
				if (rb.useGravity)
					velocity += Physics.gravity * Time.fixedDeltaTime;
			}
			/// UNTESTED
			else if (rb2d)
			{
				rb2d.MovePosition(rb2d.position + (Vector2)transform.TransformVector(velocity));
				if (rb2d.gravityScale != 0)
					velocity += Physics.gravity * rb2d.gravityScale * Time.fixedDeltaTime;
			}
			else
			{
				snap = targ;
				targ = targ + transform.rotation * velocity * Time.fixedDeltaTime;

				velocity += Physics.gravity * Time.fixedDeltaTime;
				Interpolate(0);
			}
		}

		/// Interpolation
		public virtual void OnPreUpdate()
		{
			Interpolate(NetMaster.NormTimeSinceFixed);
		}

		protected void Interpolate(float t)
		{
			transform.position = Vector3.Lerp(snap, targ, t);
		}

		#region Hit Triggers

		protected virtual void OnHit(Component other)
		{
			//Debug.Log(name + " Proj hit on " + other.name + " - " + other.GetType().Name);
			NetObject netObj = other.GetComponentInParent<NetObject>();

			/// Hit was not NetOBj
			if (ReferenceEquals(netObj, null))
			{
				bool terminateOnNonNetObj = (terminateOn & RespondTo.HitNonNetObj) != 0;

				if (terminateOnNonNetObj)
					Terminate();
			}
			/// Hit was NetObj
			else
			{
				int netObjId = netObj.NetObjId;

				bool hitWasOwner = netObjId == owner.NetObjId;

				/// Hit was self
				if (hitWasOwner)
				{
					if ((terminateOn & RespondTo.HitSelf) != 0)
					{
						Debug.LogError("Terminate On Self");
						Terminate();
					}

					if ((damageOn & RespondTo.HitSelf) == 0)
						return;
				}

				/// TODO: this collider ID code is pretty hacked together. Need to decide if syncing the collider ID
				/// should even happen, and if so how colliders are determined for some events like triggers.
				bool isCollider = ((other is Collider) || (other is Collider2D));
				int colliderId = (isCollider) ? netObj.colliderLookup[other] : 0;

				/// Hit was NetObj
				var hitgroup = other.GetComponentInParent<IHitGroupAssign>();

				int mask = ReferenceEquals(hitgroup, null) ? 0 : hitgroup.Mask;


				/// If this connection owns this launcher/projectile, log this hit
				owner.QueueHit(new NetworkHit(netObjId, mask, colliderId));

				if ((terminateOn & RespondTo.HitNetObj) != 0)
					Terminate();
			}
		}

		/// <summary>
		/// Override this and extend the base with your own projectile termination code, or place a component on this GameObject with IOnTerminate to define a response.
		/// </summary>
		protected virtual void Terminate()
		{
			int cnt = onTerminate.Count;
			for (int i = 0; i < cnt; ++i)
				onTerminate[i].OnTerminate();

			gameObject.SetActive(false);
		}

		#endregion
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(Projectile), true)]
	[CanEditMultipleObjects]
	public class ProjectileEditor : ReactorHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Defines a projectile that can be used by " + typeof(SyncLauncher).Name + ".";
			}
		}

		//protected override void OnInspectorGUIInjectMiddle()
		//{
		//	base.OnInspectorGUIInjectMiddle();
		//	EditorGUILayout.LabelField("<b>OnTriggerEvent()</b>\n{\n  Terminate()\n  owner.QueueHit() \n}", richBox);
		//}
	}
#endif
}
