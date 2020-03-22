//using emotitron.Networking;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using emotitron.Utilities;

//namespace emotitron.Networking
//{

//	public interface ICollideable
//	{
//		bool HasRigidbody { get; }
//		void RemoteCollide(ICollideable otherCollideable, ContactType type);
//	}

//	public interface ICollideFilter
//	{
//		void FilterCollisions(Queue<Collider> originalList, Queue<Collider> returnList);
//	}

//	public interface IOnCollide
//	{
//		bool OnCollide(ICollideable icollidee, ICollideable icollider);
//	}

//	//public struct CollideEvent
//	//{
//	//	public ICollideable collidable;
//	//	public Component collider;

//	//	public CollideEvent(ICollideable collidable, Component collider)
//	//	{
//	//		this.collidable = collidable;
//	//		this.collider = collider;
//	//	}
//	//}

	

//	/// <summary>
//	/// Turns all collisions and triggers into OnCollide that register on both sides of the event only once.
//	/// </summary>
//	public class Collideable : MonoBehaviour // : SyncObject // SyncObject<SyncCollideable.Frame>
//		, ICollideable
//		, IOnPreSimulate
//		, IApplyOrder
//		, IOnAwake
//	{
//		#region Inspector

//		[Utilities.GUIUtilities.EnumMask]
//		public ContactType collideOn = ContactType.Enter;
//		[Utilities.GUIUtilities.EnumMask]
//		public ContactType passThrough = ContactType.Enter;

//		public bool suppressMultiples = true;

//		protected List<ICollideFilter> _iCollisionFilter;
//		public Component collisionFilter;

//		#endregion Inspector

//		// cached locals
//		//protected NetObject netObj;
//		protected Rigidbody rb;
//		protected Rigidbody2D rb2d;
//		protected bool useEntr, useStay, useExit;

//		// Interface Property
//		protected bool _hasRigidbody;
//		public bool HasRigidbody { get { return _hasRigidbody; } }

//		public int ApplyOrder {  get { return ApplyOrderConstants.COLLISIONS; } }

//		protected Queue<ICollideable> entrQueue;
//		protected Queue<ICollideable> stayQueue;
//		protected Queue<ICollideable> exitQueue;

//		private HashSet<ICollideable> entrHashSet;
//		private HashSet<ICollideable> exitHashSet;
//		private HashSet<ICollideable> stayHashSet;


//		protected Queue<ICollideable> tempQueue = new Queue<ICollideable>();

//		public List<IOnCollide> onCollides = new List<IOnCollide>();

//		#region Startup

//		public void OnAwake()
//		{
//			rb = GetComponentInParent<Rigidbody>();
//			rb2d = GetComponentInParent<Rigidbody2D>();
//			_hasRigidbody = rb || rb2d;


//			useEntr = (collideOn & ContactType.Enter) != 0;
//			useStay = (collideOn & ContactType.Stay) != 0;
//			useExit = (collideOn & ContactType.Exit) != 0;

//			if (useEntr)
//			{
//				entrQueue = new Queue<ICollideable>();
//				entrHashSet = new HashSet<ICollideable>();
//			}

//			if (useStay)
//			{
//				stayQueue = new Queue<ICollideable>();
//				stayHashSet = new HashSet<ICollideable>();
//			}

//			if (useExit)
//			{
//				exitQueue = new Queue<ICollideable>();
//				exitHashSet = new HashSet<ICollideable>();
//			}

//			GetComponents(onCollides);
//		}

//		#endregion Startup

//		#region Triggers/Collisions

//		/// <summary>
//		/// All trigger events lead to this enqueue.
//		/// </summary>
//		private void QueueCollide(ICollideable otherCollideable, ContactType collideType)
//		{
//			//if (!amActingAuthority)
//			//	return;

//			///// We are not monitoring this type of triggerOn
//			//if ((collideOn & collideType) == 0)
//			//	return;

//			switch (collideType)
//			{
//				case ContactType.Enter:
//					Enqueue(entrQueue, entrHashSet, otherCollideable);
//					break;

//				case ContactType.Stay:
//					Enqueue(stayQueue, stayHashSet, otherCollideable);
//					break;

//				case ContactType.Exit:
//					Enqueue(exitQueue, exitHashSet, otherCollideable);
//					break;

//				default:
//					break;
//			}
//		}

//		private void Enqueue(Queue<ICollideable> queue, HashSet<ICollideable> hashes, ICollideable otherCollideable)
//		{
//			if (suppressMultiples)
//			{
//				if (hashes.Contains(otherCollideable))
//					return;
//				hashes.Add(otherCollideable);
//			}
//			queue.Enqueue(otherCollideable);
//		}

//		/// <summary>
//		/// Collide event passed from other
//		/// </summary>
//		public virtual void RemoteCollide(ICollideable otherCollideable, ContactType collideType)
//		{
//			bool local = ((collideType & collideOn) != 0);
//			if (local)
//				QueueCollide(otherCollideable, collideType);
//		}

//		#region Collide originating from this object

//		/// TryGetComponent doesn't exist for all versions, so using GetComponents and using first return.
//		private static readonly List<ICollideable> reusableFindCollidable = new List<ICollideable>();

//		private void LocalCollide(Component other, ContactType collideType)
//		{
//			bool local = ((collideType & collideOn) != 0);
//			bool pass = ((collideType & passThrough) != 0);

//			/// Only proceed if we are set to even monitor this CollideType
//			if (local || pass)
//			{
//				/// A bit of an expensive check to constantly have to make.
//				other.GetComponentsInParent(false, reusableFindCollidable);

//				/// Did we collide with an ICollideable?
//				if (reusableFindCollidable.Count == 0)
//					return;

//				/// We are only collecting multiple to avoid garbage collection. We really are only expecting one result.
//				var otherCollideable = reusableFindCollidable[0];

//				if (local)
//					QueueCollide(otherCollideable, collideType);

//				//QueueTrigger(otherCollideable, other, type);

//				/// Other object will not detect this collide, so remote call it.
//				if (pass && !otherCollideable.HasRigidbody)
//					otherCollideable.RemoteCollide(this, collideType);
//			}
			
//		}

//		#region OnEnter

//		private void OnTriggerEnter2D(Collider2D other)
//		{
//			LocalCollide(other, ContactType.Enter);
//		}
//		private void OnTriggerEnter(Collider other)
//		{
//			LocalCollide(other, ContactType.Enter);
//		}
//		private void OnCollisionEnter2D(Collision2D collision)
//		{
//			LocalCollide(collision.otherCollider, ContactType.Enter);
//		}
//		private void OnCollisionEnter(Collision collision)
//		{
//			//Debug.Log(name + " Collide with  " + collision.transform.name);
//			LocalCollide(collision.transform, ContactType.Enter);
//		}

//		#endregion

//		#region OnStay

//		private void OnTriggerStay2D(Collider2D other)
//		{
//			LocalCollide(other, ContactType.Stay);
//		}
//		private void OnTriggerStay(Collider other)
//		{
//			LocalCollide(other, ContactType.Stay);
//		}
//		private void OnCollisionStay2D(Collision2D collision)
//		{
//			LocalCollide(collision.otherCollider, ContactType.Stay);
//		}
//		private void OnCollisionStay(Collision collision)
//		{
//			LocalCollide(collision.transform, ContactType.Stay);
//		}

//		#endregion

//		#region OnExit

//		private void OnTriggerExit2D(Collider2D other)
//		{
//			LocalCollide(other, ContactType.Exit);
//		}
//		private void OnTriggerExit(Collider other)
//		{
//			LocalCollide(other, ContactType.Exit);
//		}
//		private void OnCollisionExit2D(Collision2D collision)
//		{
//			LocalCollide(collision.otherCollider, ContactType.Exit);
//		}
//		private void OnCollisionExit(Collision collision)
//		{
//			LocalCollide(collision.transform, ContactType.Exit);
//		}

//		#endregion

//		#endregion

//		#endregion Triggers/Collisions

//		#region NetObj Timings

//		public void OnPreSimulate(int frameId, int subFrameId)
//		{
//			if (useEntr)
//				ProcessQueue(entrQueue, entrHashSet, ContactType.Enter);

//			if (useStay)
//				ProcessQueue(stayQueue, stayHashSet, ContactType.Stay);

//			if (useExit)
//				ProcessQueue(exitQueue, exitHashSet, ContactType.Exit);
//		}

//		private void ProcessQueue(Queue<ICollideable> queue, HashSet<ICollideable> hashes, ContactType collideType)
//		{
//			int cnt = queue.Count;
//			for (int i = 0; i < cnt; ++i)
//				OnCollideCallbacks(queue.Dequeue(), collideType);

//			queue.Clear();
//			hashes.Clear();
//		}

//		protected void OnCollideCallbacks(ICollideable otherCollideable, ContactType collideType)
//		{
//			Debug.Log(Time.time + "  " + name + "<b>Collision!</b> with " + (otherCollideable as Component).name + " " + collideType);
//		}

//		#endregion


//	}

//}
