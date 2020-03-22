using emotitron.Utilities;
using System.Collections;
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

	public interface IOnVitalsTrigger
	{
		void OnVitalsTrigger(int frameInt, ContactEvent triggerEvent);
	}


	/// <summary>
	/// The generic base class for any VitalTrigger derived class.
	/// </summary>
	/// <typeparam name="TFrame"></typeparam>
	public abstract class SyncVitalTrigger<TFrame> : SyncObject<TFrame>
		, IOnSnapshot
		, IOnNetSerialize
		, IOnNetDeserialize
		, IOnAuthorityChanged
		, IOnCaptureState

		where TFrame : SyncVitalTrigger<TFrame>.Frame, new()
	{
		public List<IOnVitalsTrigger> onVitalsTrigger = new List<IOnVitalsTrigger>();

		#region Inspector

		[Header("Trigger:")]
		public VitalNameType _vitalNameType = new VitalNameType(VitalType.Health);
		public VitalNameType VitalNameType { get { return _vitalNameType; } }

		[Utilities.GUIUtilities.EnumMask]
		public ContactType triggerOn = ContactType.Enter;

		#endregion Inspector

		protected TFrame currentState = new TFrame();

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

		public override void OnAwake()
		{
			base.OnAwake();

			rb = GetComponentInParent<Rigidbody>();
			rb2d = GetComponentInParent<Rigidbody2D>();
			_hasRigidbody = rb || rb2d;

			transform.GetNestedComponentsInChildren(onVitalsTrigger);
			//GetComponents(onTriggerSerialize);
			//GetComponentsInChildren(onTrigger);
			//onTriggerCount = onTrigger.Count;

		}

		public override void OnStart()
		{
			base.OnStart();
		}

		#endregion Startup

		#region Triggers

		protected Queue<ContactEvent> queueContactEvent = new Queue<ContactEvent>();

		/// <summary>
		/// Primary triggering event
		/// </summary>
		public virtual void OnContactEvent(ContactEvent contactEvent)
		{
			if (!IsMine)
				return;

			var ivc = contactEvent.itc as IVitalsComponent;
			if (ReferenceEquals(ivc, null))
				return;

			/// Only log this if we are responding ot OnEnter
			if ((triggerOn & ContactType.Enter) == 0)
				return;

			queueContactEvent.Enqueue(contactEvent);
		}
	
		/// <summary>
		/// Primary triggering event
		/// </summary>
		public virtual void OnTriggeringStay(ContactEvent contactEvent)
		{
			if (!IsMine)
				return;

			/// Only log this if we are responding ot OnEnter
			if ((triggerOn & ContactType.Stay) == 0)
				return;

			queueContactEvent.Enqueue(contactEvent);
		}
		
		/// <summary>
		/// Primary triggering event
		/// </summary>
		public virtual void OnTriggeringExit(ContactEvent contactEvent)
		{
			if (!IsMine)
				return;
			
			/// Only log this if we are responding ot OnEnter
			if ((triggerOn & ContactType.Exit) == 0)
				return;

			queueContactEvent.Enqueue(contactEvent);
		}

		private void OnTrigger(Component otherCollider, ContactType triggerOn)
		{
			if (!IsMine)
				return;

			if ((this.triggerOn & triggerOn) == 0)
				return;

			IVitalsComponent ivc = otherCollider.GetComponentInParent<IVitalsComponent>();
			if (ReferenceEquals(ivc, null))
				return;

			queueContactEvent.Enqueue(new ContactEvent(null, ivc, null, otherCollider, triggerOn));
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="frameId"></param>
		/// <param name="amActingAuthority"></param>
		/// <param name="realm"></param>
		public virtual void OnCaptureCurrentState(int frameId, Realm realm)
		{
			TFrame frame = frames[frameId];
			
			while (queueContactEvent.Count > 0)
				OnTrigger(frame, queueContactEvent.Dequeue());
			
		}

		protected virtual void OnTrigger(TFrame frame, ContactEvent trigger)
		{

			frame.triggeredById = trigger.itc == null ? null : (int?)trigger.itc.NetObjId;

			int cnt = onVitalsTrigger.Count;
			for (int i = 0; i < cnt; ++i)
				onVitalsTrigger[i].OnVitalsTrigger(frame.frameId, trigger);
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

			TFrame frame = frames[frameId];

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

			//for (int i = 0; i < onTriggerSerialize.Count; ++i)
			//	flags |= onTriggerSerialize[i].OnSerialize(frameId, buffer, ref bitposition);

			return flags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			TFrame frame = frames[localFrameId];
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

			//for (int i = 0; i < onTriggerSerialize.Count; ++i)
			//	flags |= onTriggerSerialize[i].OnDeserialize(originFrameId, localFrameId, buffer, ref bitposition);

			return flags;
		}

		#endregion Serialization

		public override bool OnSnapshot(int frameId)
		{
			bool ready = base.OnSnapshot(frameId);

			if (!ready)
				return false;


			int? snapTriggeredById = snapFrame.triggeredById;

			/// If there was a trigger this tick
			//if (!ReferenceEquals(snapTriggeredById, null))
			{
				IVitalsComponent attachedTo;
				attachedTo = (snapTriggeredById == null) ? null : UnifiedNetTools.FindComponentByNetId<IVitalsComponent>(snapTriggeredById.Value);
				SnapshotTrigger(new ContactEvent(null, attachedTo, null, null, ContactType.Enter));
			}

			return true;
		}

		protected virtual void SnapshotTrigger(ContactEvent triggerEvent)
		{
			//var attachedTo = triggerEvent.ivc; // UnifiedNetTools.FindComponentByNetId<IVitalsComponent>((uint)triggerEvent.ivc.NetObjId);
			//if (!ReferenceEquals(attachedTo, null))
			//{
			//	ApplyAffect(attachedTo);
			//}
		}

		protected override FrameContents InterpolateFrame(TFrame targ, TFrame start, TFrame end, float t)
		{
			targ.CopyFrom(end);
			return FrameContents.Complete;
		}

		protected override FrameContents ExtrapolateFrame()
		{
			targFrame.CopyFrom(snapFrame);
			return FrameContents.Complete;
		}
	}

}
