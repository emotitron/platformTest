using emotitron.Utilities;
using System.Collections.Generic;
using emotitron.Utilities.Networking;
using UnityEngine;
using emotitron.Utilities.GhostWorlds;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{


	/// TODO: this class can become non-abstract if I finish it out and see reason for it.
	/// <summary>
	/// The generic base class for any VitalTrigger derived class.
	/// </summary>
	/// <typeparam name="TFrame"></typeparam>
	public class SyncTrigger : SyncObject<SyncTrigger.Frame>
		, IOnContactEvent
		, IOnSnapshot
		, IOnNetSerialize
		, IOnNetDeserialize
		, IOnAuthorityChanged
		, IOnCaptureState

	{
		[System.NonSerialized] public List<IOnTrigger> onTriggerCallbacks = new List<IOnTrigger>();

		protected Frame currentState = new Frame();

		protected IContactTrigger trigger;

		protected Rigidbody rb;
		protected Rigidbody2D rb2d;
		protected bool _hasRigidbody;
		public bool HasRigidbody { get { return _hasRigidbody; } }

		public GameObject VisiblePickupObj
		{
			get
			{
				return gameObject;
			}
		}

		#region Frame

		public class Frame : FrameBase
		{
			public int? triggeredById;

			public Frame() : base() { }

			public Frame(int frameId) : base(frameId)
			{
				triggeredById = null;
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);
				triggeredById = null;
			}

			public static Frame Construct(int frameId)
			{
				return new Frame(frameId);
			}

		}

		#endregion Frame

		#region Startup

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();

			trigger = GetComponent<IContactTrigger>();
			if (ReferenceEquals(trigger, null))
				trigger = gameObject.AddComponent<ContactTrigger>();
		}
#endif

		public override void OnAwake()
		{
			base.OnAwake();

			trigger = GetComponent<IContactTrigger>();
			if (ReferenceEquals(trigger, null))
				trigger = gameObject.AddComponent<ContactTrigger>();

			rb = GetComponentInParent<Rigidbody>();
			rb2d = GetComponentInParent<Rigidbody2D>();
			_hasRigidbody = rb || rb2d;

			transform.GetNestedComponentsInChildren(onTriggerCallbacks);
		}

		public override void OnStart()
		{
			base.OnStart();
		}

		#endregion Startup

		#region Triggers

		protected Queue<ContactEvent> queuedContactEvents = new Queue<ContactEvent>();

		#region OnEnter

		// Step #1
		
		public bool OnContactEvent(ref ContactEvent contactEvent)
		{
			if (!IsMine)
				return false;

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

				case ContactType.Hitscan:
					{
						return OnHitscan(ref contactEvent);
					}

				default:
					return false;
			}
		}

		protected virtual bool OnEnter(ref ContactEvent contactEvent)
		{
			return EnqueueEvent(ref contactEvent);
		}

		protected virtual bool OnStay(ref ContactEvent contactEvent)
		{
			return false;
		}

		protected virtual bool OnExit(ref ContactEvent contactEvent)
		{
			return false;
		}

		protected virtual bool OnHitscan(ref ContactEvent contactEvent)
		{
			
			return EnqueueEvent(ref contactEvent);
		}

		protected virtual bool EnqueueEvent(ref ContactEvent contactEvent)
		{
			/// TODO: need to put a consumption validity test here
			IContacting ivc = contactEvent.itc;
			if (ReferenceEquals(ivc, null))
				return false;

			queuedContactEvents.Enqueue(contactEvent);

			return true;
		}

		#endregion

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="frameId"></param>
		/// <param name="amActingAuthority"></param>
		/// <param name="realm"></param>
		public virtual void OnCaptureCurrentState(int frameId, Realm realm)
		{
			Frame frame = frames[frameId];

			ContactEvent contactEvent;
			bool found = GetFirstTriggerEvent(frame, out contactEvent);

			//Debug.Log(name + " Triggered Mount " + (mount ? mount.name : "null") + " triggermount: ");
			frame.triggeredById = (found) ? contactEvent.itc.NetObjId : (int?)null;
			
		}

		/// <summary>
		/// Find the first valid trigger in whatever trigger events have been registered.
		/// </summary>
		protected bool GetFirstTriggerEvent(Frame frame, out ContactEvent contactEvent)
		{
			while (queuedContactEvents.Count > 0)
			{
				contactEvent = queuedContactEvents.Dequeue();
				if (OnTrigger(frame, ref contactEvent))
					return true;
			}

			contactEvent = new ContactEvent();
			return false;
		}

		/// <summary>
		/// Attempt a trigger. Returns true if a triggerEvent results in a valid collision.
		/// </summary>
		protected virtual bool OnTrigger(Frame frame, ref ContactEvent contactEvent)
		{
			frame.triggeredById = contactEvent.itc == null ? null : (int?)contactEvent.itc.NetObjId;

			int cnt = onTriggerCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
			{
				//Debug.Log("onTriggerCallbacks? " + (onTriggerCallbacks[i] as Object).name + " : " + (onTriggerCallbacks[i] as Object).GetType().Name);

				if (onTriggerCallbacks[i].OnTrigger(ref contactEvent))
					return true;
			}

			//Debug.Log(name + " OnTrigger try to find mount :" + found.name);
			return false;
		}

		#region Serialization

		public virtual SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{

			if (!IsMine)
			{
				/// attached bool
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}

			Frame frame = frames[frameId];

			SerializationFlags flags;

			/// pickup event
			int? pickedUpById = frame.triggeredById;

			if (pickedUpById.HasValue)
			{
				/// attached bool
				buffer.WriteBool(true, ref bitposition);
				buffer.WritePackedBytes((uint)pickedUpById, ref bitposition, 32);
				flags = SerializationFlags.HasChanged /*| SerializationFlags.ForceReliable*/;
			}
			else
			{
				/// attached bool
				buffer.WriteBool(false, ref bitposition);
				flags = SerializationFlags.HasChanged;
			}

			return flags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			Frame frame = frames[localFrameId];
			SerializationFlags flags = SerializationFlags.HasChanged;

			/// if pickup event bool
			if (buffer.ReadBool(ref bitposition))
			{
				int netObjId = (int)buffer.ReadPackedBytes(ref bitposition, MasterNetAdapter.BITS_FOR_NETID);
				frame.triggeredById = netObjId;
				flags = SerializationFlags.HasChanged /*| SerializationFlags.ForceReliable*/;
			}
			else
				frame.triggeredById = null;

			frame.content = FrameContents.Complete;

			return flags;
		}

		#endregion Serialization

		public override bool OnSnapshot(int frameId)
		{
			bool ready = base.OnSnapshot(frameId);

			if (!ready)
				return false;



			/// TODO: There is no actual default implementation here yet

			//int? snapTriggeredById = snapFrame.triggeredById;

			///// If there was a trigger this tick
			//ITriggeringComponent attachedTo;
			//attachedTo = (snapTriggeredById == null) ? null : UnifiedNetTools.FindComponentByNetId<ITriggeringComponent>(snapTriggeredById.Value);
			//SnapshotTrigger(new TriggerEvent(this, attachedTo, null, CollideType.Enter));

			return true;
		}

		protected override FrameContents InterpolateFrame(Frame targ, Frame start, Frame end, float t)
		{
			targ.CopyFrom(end);
			return FrameContents.Complete;
		}

		protected override FrameContents ExtrapolateFrame()
		{
			/// Don't extrapolate if we have an invalid snapFrame
			if (snapFrame.content == FrameContents.Empty)
				return FrameContents.Empty;

			targFrame.CopyFrom(snapFrame);
			return FrameContents.Complete;
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncTriggerTFrameEditor))]
	[CanEditMultipleObjects]
	public class SyncTriggerTFrameEditor : SyncObjectTFrameEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Responds to " + typeof(IOnContactEvent).Name + " events.\n";
			}
		}
		//public override void OnInspectorGUI()
		//{
		//	base.OnInspectorGUI();

		//	var _target = (target as SyncPickup);

		//	//ListFoundInterfaces(_target.gameObject, _target.onTriggerCallbacks);
		//}

		//protected override void OnInspectorGUIInjectMiddle()
		//{
		//	base.OnInspectorGUIInjectMiddle();
		//	EditorGUILayout.LabelField("Generates OnTrigger()", richLabel);
		//}
	}

#endif

}
