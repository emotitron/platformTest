//Copyright 2020 Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.Networking;
using emotitron.Utilities.HitGroups;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// Base class for Synced weapons.
	/// </summary>
	public abstract class SyncNetHitBase : SyncObject<SyncNetHitBase.WeaponFrame>
		, IOnSnapshot
		, IOnNetSerialize
		, IOnNetDeserialize
		, IOnAuthorityChanged
		, IOnPreUpdate
		, IOnIncrementFrame
		, IOnPostSimulate
	{

		#region Inspector

		[Tooltip("Specify the transform hitscans/projectiles will originate from. If null this gameObject will be used as the origin.")]
		[SerializeField] protected Transform origin;
		[SerializeField] public KeyCode triggerKey = KeyCode.None;

		#endregion

		/// <summary>
		/// Callbacks for when a NetworkHit is being applied
		/// </summary>
		protected List<IOnNetworkHit> onNetworkHit = new List<IOnNetworkHit>();

		// internal States
		protected bool triggerQueued;

		#region Frame

		public class WeaponFrame : FrameBase
		{
			public uint triggerMask;

			public uint hitmask;
			public NetworkHits[] netHits;

			public WeaponFrame() : base()
			{

			}

			public WeaponFrame(int frameId) : base(frameId)
			{

			}

			public WeaponFrame(SyncNetHitBase weapon, bool nearestOnly, int frameId) : base(frameId)
			{
				netHits = new NetworkHits[sendEveryXTick];
				for (int i = 0; i < sendEveryXTick; ++i)
					netHits[i] = new NetworkHits(nearestOnly, HitGroupSettings.bitsForMask);
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				/// We do not want to copy triggers (would produce new trigger events)
				triggerMask = 0;
				hitmask = 0;

				for (int i = 0; i < sendEveryXTick; ++i)
					netHits[i].Clear();
			}

			public override void Clear()
			{
				base.Clear();
				hitmask = 0;
				triggerMask = 0;

				for (int i = 0; i < sendEveryXTick; ++i)
					netHits[i].hits.Clear();
			}

			public override string ToString()
			{
				string str = "Trigmask: " + triggerMask + " hitmask: " + hitmask + "\n";
				for (int i = 0; i < sendEveryXTick; ++i)
					str += netHits[i] + "\n";

				return str;
			}
		}

		#endregion

		#region Initialization

		protected override void Reset()
		{
			base.Reset();
			_applyOrder = ApplyOrderConstants.HITSCAN;
#if UNITY_EDITOR
			//gameObject.EnsureComponentExists<OnNetHitApplyDamage>();
#endif
		}

		public override void OnAwake()
		{
			base.OnAwake();

			if (origin == null)
				origin = transform;

			transform.GetNestedComponentsInChildren(onNetworkHit);
			
		}

		public virtual void OnPreUpdate()
		{
			if (IsMine && Input.GetKeyDown(triggerKey))
				QueueTrigger();
		}

		#endregion

		#region Serialization


		public virtual SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			SerializationFlags flags = SerializationFlags.None;

			/// TODO: Should be able to remove this dev check
			if (!IsMine)
			{
				Debug.LogError(name + " Write but not ActingAuthority" + name + " " + frameId + " ismine:" + IsMine);
				buffer.WriteBool(false, ref bitposition);
				return flags;
			}

			WeaponFrame frame = frames[frameId];

			/// Serialize TriggerMask (each 
			if (frame.triggerMask != 0)
			{
				buffer.WriteBool(true, ref bitposition);
				buffer.Write(frame.triggerMask, ref bitposition, sendEveryXTick);
				flags = SerializationFlags.HasChanged /*| SerializationFlags.ForceReliable*/;

			}
			else
			{
				buffer.WriteBool(false, ref bitposition);
				flags = SerializationFlags.None;
			}

			/// Serialize projectile hits TODO: give projectiles ids
			buffer.Write(frame.hitmask, ref bitposition, sendEveryXTick);
			for (int i = 0; i < sendEveryXTick; ++i)
			{
				if ((frame.hitmask & (1 << i)) != 0)
					flags |= frame.netHits[i].Serialize(buffer, ref bitposition, NetObj.bitsForColliderIndex);
			}

			return flags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			WeaponFrame frame = frames[localFrameId];
			SerializationFlags flags = SerializationFlags.None;

			if (buffer.ReadBool(ref bitposition))
			{
				frame.triggerMask = buffer.ReadUInt32(ref bitposition, sendEveryXTick);
				flags |= SerializationFlags.HasChanged /*| SerializationFlags.ForceReliable*/;
			}
			else
			{
				frame.triggerMask = 0;
			}

			frame.hitmask = (uint)buffer.Read(ref bitposition, sendEveryXTick);

			for (int i = 0; i < sendEveryXTick; ++i)
			{
				if ((frame.hitmask & (1 << i)) != 0)
					flags |= frame.netHits[i].Deserialize(buffer, ref bitposition, NetObj.bitsForColliderIndex);
			}

			frame.content = flags == SerializationFlags.None ? FrameContents.Empty : FrameContents.Complete;
			return flags;
		}

		#endregion Serialization

		#region Timings

		public virtual void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
		{
			if (!IsMine)
				return;

			WeaponFrame frame = frames[frameId];

			/// Clear the trigger mask for new frames
			if (subFrameId == 0)
			{
				frame.Clear();
			}

			/// Process Fire 
			if (triggerQueued) //subFrameId < SimpleSyncSettings.sendEveryXTick)
			{
				frame.triggerMask |= (uint)1 << subFrameId;

				Trigger(frame, subFrameId);
				triggerQueued = false;
				///TODO: is this needed?
				frame.content = FrameContents.Complete;
			}

		}


		/// <summary>
		/// Since shots can be spread over the duration of a frame, we apply them onIncrement.
		/// </summary>
		public virtual void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId)
		{
			if (IsMine)
				return;

			if (hadInitialSnapshot)
			{
				//if (targFrame.hitmask != 0)

				int offset = (newSubFrameId == 0) ? (sendEveryXTick - 1) : newSubFrameId - 1;

				if ((targFrame.triggerMask & (1 << offset)) != 0)
				{
					Trigger(targFrame, newSubFrameId);
				}

				if ((targFrame.hitmask & (1 << offset)) != 0)
				{
					HitsCallbacks(targFrame.netHits[offset]);
				}
			}
		}

		#endregion Timings

		#region Trigger

		/// <summary>
		/// Call this on the authority to initiate a hitscan. Actual firing may be defered based on settings.
		/// </summary>
		public virtual void QueueTrigger()
		{
			triggerQueued = true;
		}

		/// <summary>
		/// Instantiate the weapon graphic and hit tests code if applicable. Results should be stored to the frame.
		/// </summary>
		/// <param name="frame"></param>
		protected abstract void Trigger(WeaponFrame frame, int subFrameId);
		

		/// <summary>
		/// Broadcast to interested interfaces that a hit has been processed.
		/// </summary>
		/// <param name="hits"></param>
		protected virtual void HitsCallbacks(NetworkHits hits)
		{
			if (hits.hits.Count == 0)
				return;

			int cnt = onNetworkHit.Count;
			for (int i = 0; i < cnt; ++i)
				onNetworkHit[i].OnNetworkHit(hits);
		}

		#endregion

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncNetHitBase), true)]
	[CanEditMultipleObjects]
	public class SyncNetHitBaseEditor : SyncObjectEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Trigger by calling this" + typeof(SyncNetHitBase).Name + ".QueueTrigger()";
			}
		}
		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncNetHitText";
			}
		}
	}
#endif
}
