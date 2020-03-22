// Copyright 2019, Davin Carten, All rights reserved
// This code may only be used in game development, but may not be used in any tools or assets that are sold or made publicly available to other developers.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;
using emotitron.Networking.Internal;
using emotitron.Utilities;
using emotitron.Utilities.Networking;
using emotitron.Utilities.GhostWorlds;

#if PUN_2_OR_NEWER
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking

{
	public enum RigidbodyType { None, RB, RB2D }

	[DisallowMultipleComponent]
#if !PUN_2_OR_NEWER
	public class NetObject : MonoBehaviour
#else
	[RequireComponent(typeof(PhotonView))]
	public class NetObject : MonoBehaviour
		, IMatchmakingCallbacks
		, IPunOwnershipCallbacks
#endif

		//, IOnSerializeInitialization
		, IOnPreUpdate
		, IOnPreSimulate
		, IOnPostSimulate
		, IOnNetSerialize
		//, IOnNetDeserialize
		//, IOnCaptureCurrentValues
		, IOnQuantize
		, IOnIncrementFrame
		, IOnSnapshot
		, IOnInterpolate
		, IOnPreQuit
	{

		#region Inspector Fields

		[System.NonSerialized] public Authority authority = Authority.Auto;

		[Tooltip("Enabling this will tell the serializer to completely exclude this net object from serialization if none of its content has changed. " +
			"While this will remove heartbeat data, It may also produce undeseriable extrapolation and buffer resizing behavior, as recieving clients will see this as a network failure.")]
		[SerializeField] private bool skipWhenEmpty = false;
		public bool SkipWhenEmpty { get { return skipWhenEmpty; } }

		protected Rigidbody _rigidbody;
		public Rigidbody Rb { get { return _rigidbody; } }

		protected Rigidbody2D _rigidbody2D;
		public Rigidbody2D Rb2D { get { return _rigidbody2D; } }

		#endregion

		// cache
		//[System.NonSerialized] public bool isMine;
		[System.NonSerialized] public int netObjId;


#if PUN_2_OR_NEWER
		public int NetObjId { get { return netObjId; } }
#else
		public int NetObjId { get { return 0; } }
#endif
		#region Static NetObject Lookups and Pools

		public static Dictionary<uint, NetObject> netObjLookup = new Dictionary<uint, NetObject>();
		public static List<NetObject> activeNetObjs = new List<NetObject>();
		
		#endregion


		#region Collider Lookup

		/// TODO: Pool these?
		[System.NonSerialized] public Dictionary<Component, int> colliderLookup = new Dictionary<Component, int>();
		[System.NonSerialized] public List<Component> indexedColliders = new List<Component>();
		[System.NonSerialized] public int bitsForColliderIndex;

		#endregion

		#region ObjReady

		[System.NonSerialized] private FastBitMask128 packObjValidMask;

		[System.NonSerialized] public FastBitMask128 syncObjReadyMask;
		[System.NonSerialized] public FastBitMask128 packObjReadyMask;
		[System.NonSerialized] private readonly Dictionary<Component, int> packObjIndexLookup = new Dictionary<Component, int>();

		public void OnSyncObjReadyChange(ISyncObject sobj, ReadyStateEnum readyState)
		{
			int syncObjIndex = sobj.SyncObjIndex;

			if (readyState != ReadyStateEnum.Unready)
			{
				syncObjReadyMask[syncObjIndex] = true;
			}
			else
			{
				syncObjReadyMask[syncObjIndex] = false;
			}

			AllObjsAreReady = syncObjReadyMask.AllAreTrue && packObjReadyMask.AllAreTrue;
		}

		public void OnPackObjReadyChange(Component pobj, ReadyStateEnum readyState)
		{
			int packObjIndex = packObjIndexLookup[pobj];

			if (readyState != ReadyStateEnum.Unready)
			{
				packObjReadyMask[packObjIndex] = true;
			}
			else
			{
				packObjReadyMask[packObjIndex] = false;
			}

			AllObjsAreReady = syncObjReadyMask.AllAreTrue && packObjReadyMask.AllAreTrue;
		}
		
		private bool _allObjsAreReady;
		public bool AllObjsAreReady
		{
			get
			{
#if PUN_2_OR_NEWER
				return pv.IsMine ? true : _allObjsAreReady;
#else
				return false;
#endif
			}
			private set
			{
				//Debug.Log(name + " <b>ALL READY Try</b> " + _allSyncObjsAreReady + " : " + value);

				if (_allObjsAreReady == value)
					return;

				_allObjsAreReady = value;

				for (int i = 0; i < onNetObjReadyCallbacks.Count; ++i)
					onNetObjReadyCallbacks[i].OnNetObjReadyChange(value);

				packObjReadyMask.SetAllTrue();
				syncObjReadyMask.SetAllTrue();
			}
		}

		#endregion ObjReady


		#region Cached Refs

		[System.NonSerialized] private Haunted haunted;
#if PUN_2_OR_NEWER
		[System.NonSerialized] public PhotonView pv;

#endif

		#endregion

		#region Startup / Shutdown

#if UNITY_EDITOR

		private void Reset()
		{
			UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(this, false);

			if (!_rigidbody)
				_rigidbody = transform.GetNestedComponentInChildren<Rigidbody>();

			if (!_rigidbody)
				_rigidbody2D = transform.GetNestedComponentInChildren<Rigidbody2D>();
		}
#endif

		protected void Awake()
		{

			//validFrames = new BitArray(SimpleSyncSettings.frameCount + 1);
			packObjValidMask = new FastBitMask128(SimpleSyncSettings.FrameCount);

			if (!_rigidbody)
				_rigidbody = transform.GetNestedComponentInChildren<Rigidbody>();

			if (!_rigidbody)
				_rigidbody2D = transform.GetNestedComponentInChildren<Rigidbody2D>();

#if PUN_2_OR_NEWER
			pv = GetComponent<PhotonView>();
			if (pv == null)
				pv = gameObject.AddComponent<PhotonView>();
#endif
			CollectAndReorderInterfaces();

			this.IndexColliders();

			GetComponentsInChildren(true, onAwakeCallbacks);

			/// OnAwake Callbacks
			int cnt = onAwakeCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onAwakeCallbacks[i].OnAwake();
		}

		private void Start()
		{
			GetComponentsInChildren(true, onStartCallbacks);

			/// OnStart Callbacks
			int cnt = onStartCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onStartCallbacks[i].OnStart();

#if PUN_2_OR_NEWER

			if (PhotonNetwork.IsConnectedAndReady)
				OnChangeAuthority(pv.IsMine, PhotonNetwork.IsMasterClient);

			netObjId = pv.ViewID;
#endif
		}

		private void OnEnable()
		{

#if PUN_2_OR_NEWER
			PhotonNetwork.AddCallbackTarget(this);
#endif
			NetMaster.RegisterCallbackInterfaces(this, true);
			activeNetObjs.Add(this);

			/// OnPostEnable Callbacks
			int cnt = onEnableCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onEnableCallbacks[i].OnPostEnable();
		}

		private void OnDisable()
		{

#if PUN_2_OR_NEWER
			PhotonNetwork.RemoveCallbackTarget(this);
#endif
			NetMaster.RegisterCallbackInterfaces(this, false);
			if (activeNetObjs.Contains(this))
				activeNetObjs.Remove(this);

			/// OnPostDisable Callback
			int cnt = onDisableCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onDisableCallbacks[i].OnPostDisable();
		}

		public void OnPreQuit()
		{
			/// OnQuit Callbacks
			int cnt = onPreQuitCallbacks.Count;
			for (int i = 0; i < onPreQuitCallbacks.Count; ++i)
				onPreQuitCallbacks[i].OnPreQuit();
		}

		private void OnDestroy()
		{
			GetComponentsInChildren(true, onDestroyCallbacks);

			/// OnDestroy Callbacks
			int cnt = onDestroyCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onDestroyCallbacks[i].OnPostDestroy();

			if (activeNetObjs.Contains(this))
				activeNetObjs.Remove(this);
		}

		/// TODO: likely not needed if moving UnmountAll to OnDisable
		/// <summary>
		/// Destroy an object in a way that respects Simple.
		/// </summary>
		public virtual void PrepareForDestroy()
		{
			var mounts = GetComponent<MountsLookup>();
			if (mounts)
				mounts.UnmountAll();
		}

		#region PUN Callbacks

#if PUN_2_OR_NEWER

		
		public void OnOwnershipRequest(PhotonView targetView, Photon.Realtime.Player requestingPlayer) { }

		public void OnOwnershipTransfered(PhotonView targetView, Photon.Realtime.Player previousOwner)
		{
			/// Only respond if this pv changed owners
			if (targetView != pv)
				return;

#if UNITY_EDITOR
			Debug.Log(Time.time + " " + name + " " + pv.ViewID + " <color=red>Ownership Changed</color>");
#endif

			OnChangeAuthority(pv.IsMine, Photon.Pun.PhotonNetwork.IsMasterClient);
		}

		public void OnFriendListUpdate(List<FriendInfo> friendList) { }
		public void OnCreatedRoom() { }
		public void OnCreateRoomFailed(short returnCode, string message) { }
		public void OnJoinedRoom()
		{

			GetComponentsInChildren(true, onJoinedRoomCallbacks);

			/// OnAwake Callbacks
			int cnt = onJoinedRoomCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onJoinedRoomCallbacks[i].OnJoinedRoom();

			OnChangeAuthority(pv.IsMine, PhotonNetwork.IsMasterClient);
		}

		public void OnJoinRoomFailed(short returnCode, string message) { }
		public void OnJoinRandomFailed(short returnCode, string message) { }
		public void OnLeftRoom() { }

#endif

#endregion

		public void OnChangeAuthority(bool isMine, bool asServer)
		{
			//this.isMine = isMine;
			/// OnAuthorityChanged Callbacks
			int cnt = onAuthorityChangedCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onAuthorityChangedCallbacks[i].OnAuthorityChanged(isMine, asServer);

			/// Owner assumes all objects are ready.
			if (isMine)
			{
				//Debug.LogError(Time.time + " " + name + " AllReady " + _allSyncObjsAreReady);

				//if (_allPackObjsAreReady == false)
				//{
				//	packObjReadyMask.SetAllTrue();
				//	_allPackObjsAreReady = true;
				//}

				//if (_allSyncObjsAreReady == false)
				//{
				//	syncObjReadyMask.SetAllTrue();
				//	_allSyncObjsAreReady = true;
				//}

				AllObjsAreReady = true;

			}
		}

		#endregion

		#region Outgoing Callbacks

		private static List<Component> reusableComponents = new List<Component>();

		private static readonly List<IOnJoinedRoom> onJoinedRoomCallbacks = new List<IOnJoinedRoom>();
		private static readonly List<IOnAwake> onAwakeCallbacks = new List<IOnAwake>();
		private static readonly List<IOnStart> onStartCallbacks = new List<IOnStart>();
		private static readonly List<IOnDestroy> onDestroyCallbacks = new List<IOnDestroy>();


		private readonly List<IOnEnable> onEnableCallbacks = new List<IOnEnable>();
		private readonly List<IOnDisable> onDisableCallbacks = new List<IOnDisable>();
		public readonly List<IOnPreUpdate> onPreUpdateCallbacks = new List<IOnPreUpdate>();

		public readonly List<IOnAuthorityChanged> onAuthorityChangedCallbacks = new List<IOnAuthorityChanged>();

		public readonly List<IOnNetSerialize> onNetSerializeCallbacks = new List<IOnNetSerialize>();
		public readonly List<IOnNetDeserialize> onNetDeserializeCallbacks = new List<IOnNetDeserialize>();
		public readonly List<IOnIncrementFrame> onIncrementFramesCallbacks = new List<IOnIncrementFrame>();
		public readonly List<IOnSnapshot> onSnapshotCallbacks = new List<IOnSnapshot>();
		public readonly List<IOnQuantize> onQuantizeCallbacks = new List<IOnQuantize>();
		public readonly List<IOnInterpolate> onInterpolateCallbacks = new List<IOnInterpolate>();
		public readonly List<IOnCaptureState> onCaptureCurrentStateCallbacks = new List<IOnCaptureState>();
		public readonly List<IOnPreSimulate> onPreSimulateCallbacks = new List<IOnPreSimulate>();
		public readonly List<IOnPostSimulate> onPostSimulateCallbacks = new List<IOnPostSimulate>();
		public readonly List<IOnPreQuit> onPreQuitCallbacks = new List<IOnPreQuit>();

		private readonly List<IOnNetObjReady> onNetObjReadyCallbacks = new List<IOnNetObjReady>();

		private readonly List<ISyncObject> indexedSyncObjs = new List<ISyncObject>();
		private readonly List<Component> indexedPackObjs = new List<Component>();


		/// <summary>
		/// Find all of the callback interfaces on children, and add them to the callback list, respecting the ApplyOrder value.
		/// </summary>
		private void CollectAndReorderInterfaces()
		{

			/// Collect all components to avoid doing this over and over
			//GetComponentsInChildren(true, reusableFindSyncObjs);
			transform.GetNestedComponentsInChildren(reusableComponents);

			int cnt = reusableComponents.Count;
			for (int order = 0; order <= ApplyOrderConstants.MAX_ORDER_VAL; ++order)
			{
				for (int index = 0; index < cnt; ++index)
				{
					var comp = reusableComponents[index];

					/// Don't include self, or you will stack overflow hard.
					if (comp == this)
						continue;

					var iApplyOrder = comp as IApplyOrder;

					/// Apply any objects without IApplyOrder to the middle timing of 5
					if (ReferenceEquals(iApplyOrder, null))
					{
						if (order == 5)
						{
							AddInterfaces(comp);
							AddPackObjects(comp);
						}
					}
					else
					if (iApplyOrder.ApplyOrder == order)
					{
						AddInterfaces(comp);
					}
				}
			}

			syncObjReadyMask = new FastBitMask128(indexedSyncObjs.Count);
			packObjReadyMask = new FastBitMask128(indexedPackObjs.Count);

			for (int i = 0; i < indexedSyncObjs.Count; ++i)
			{
				var so = indexedSyncObjs[i];
				so.SyncObjIndex = i;

				///// Add NetObj to ReadyStateChange callback
				///// and Simulate firing of ReadyState changes on syncObjs that we may have missed due to order of exec.
				//so.onReadyCallbacks += OnSyncObjReadyChange;
				OnSyncObjReadyChange(so, so.ReadyState);
			}

		}



		/// <summary>
		/// Remove a component from all NetObj callback lists.
		/// </summary>
		/// <param name="comp"></param>
		public void RemoveInterfaces(Component comp) { AddInterfaces(comp, true); }

		private void AddInterfaces(Component comp, bool remove = false)
		{
			AddInterfaceToList(comp, onEnableCallbacks, remove);
			AddInterfaceToList(comp, onDisableCallbacks, remove);
			AddInterfaceToList(comp, onPreUpdateCallbacks, remove);

			AddInterfaceToList(comp, onAuthorityChangedCallbacks, remove);

			AddInterfaceToList(comp, onCaptureCurrentStateCallbacks, remove);

			AddInterfaceToList(comp, onNetSerializeCallbacks, remove, true);
			AddInterfaceToList(comp, onNetDeserializeCallbacks, remove, true);
			AddInterfaceToList(comp, onQuantizeCallbacks, remove, true);
			AddInterfaceToList(comp, onIncrementFramesCallbacks, remove, true);
			AddInterfaceToList(comp, onSnapshotCallbacks, remove, true);
			AddInterfaceToList(comp, onInterpolateCallbacks, remove, true);

			AddInterfaceToList(comp, onPreSimulateCallbacks, remove);
			AddInterfaceToList(comp, onPostSimulateCallbacks, remove);
			AddInterfaceToList(comp, onPreQuitCallbacks, remove);

			AddInterfaceToList(comp, onNetObjReadyCallbacks, remove);
			AddInterfaceToList(comp, indexedSyncObjs, remove);
		}

		private void AddInterfaceToList<T>(object comp, List<T> list, bool remove, bool checkSerializationOptional = false) where T : class
		{
			T cb = comp as T;
			if (!ReferenceEquals(cb, null))
			{
				/// Check if this syncObj is flagged to be excluded from serialization
				if (checkSerializationOptional)
				{
					var optionalCB = cb as ISerializationOptional;
					if (!ReferenceEquals(optionalCB, null))
					{
						if (optionalCB.IncludeInSerialization == false)
							return;
					}
				}

				T tcomp = comp as T;
				if (remove && list.Contains(tcomp))
					list.Remove(tcomp);
				else
					list.Add(tcomp);
			}

		}

		#region PackObjects

		private class PackObjRecord
		{
			public Component component;
			public Compression.Internal.PackObjectDatabase.PackObjectInfo info;
			public Compression.Internal.PackFrame[] packFrames;
			public FastBitMask128 prevReadyMask;
			public FastBitMask128 readyMask;
			public IPackObjOnReadyChange onReadyCallback;
		}

		List<PackObjRecord> packObjRecords = new List<PackObjRecord>();

		/// <summary>
		/// Check if passed Component has a PackObject attribute, if so add it to the callback list for this NetObj
		/// </summary>
		private void AddPackObjects(Component comp)
		{
			if (comp == null)
				return;

			/// Add PackObjRecord
			System.Type compType = comp.GetType();
			if (comp.GetType().GetCustomAttributes(typeof(PackObjectAttribute), false).Length != 0)
			{
				var packObjInfo = Compression.Internal.PackObjectDatabase.GetPackObjectInfo(compType);
				if (packObjInfo == null)
					return;

				var newrecord = new PackObjRecord()
				{
					component = comp,
					onReadyCallback = comp as IPackObjOnReadyChange,
					info = packObjInfo,
					packFrames = packObjInfo.FactoryFramesObj(comp, SimpleSyncSettings.FrameCount),
					prevReadyMask = new FastBitMask128(packObjInfo.fieldCount),
					readyMask = new FastBitMask128(packObjInfo.fieldCount)
				};

				packObjRecords.Add(newrecord);

				// set any readyMask bits that are triggers to true - they are always ready.

				packObjIndexLookup.Add(comp, indexedPackObjs.Count);
				indexedPackObjs.Add(comp);
			}
		}

		#endregion PackObjs

		#endregion Interfaces



		public void OnPreUpdate()
		{
			/// OnPreUpdate Callbacks
			int cnt = onPreUpdateCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onPreUpdateCallbacks[i].OnPreUpdate();
		}

		#region Logging



#if TICKS_TO_UICONSOLE && (DEBUG || UNITY_EDITOR || DEVELOPMENT_BUILD)

		/// Virtual console logging

		int lastConsoleTick;
		static bool consoleRefreshing;

		private void Update()
		{
			consoleRefreshing = false;
		}

		private void LateUpdate()
		{
			if (!pv.IsMine)
			{
				if (lastConsoleTick == NetMaster.CurrentFrameId)
					return;

				if (!consoleRefreshing)
					Debugging.UIConsole.Clear();

				consoleRefreshing = true;
				lastConsoleTick = NetMaster.CurrentFrameId;

				//Debugging.UIConsole.Single._(validFrames.PrintMask(currTargFrameId, -1))._(" netid:")._(noa.NetId)._("\n");

				for (int i = 0; i < TickManager.connections.Count; ++i)
				{
					var offsetinfo = TickManager.perConnOffsets[TickManager.connections[i]];
					Debugging.UIConsole.Single._(offsetinfo.validFrames.PrintMask(NetMaster.CurrentFrameId, null))._(" connid:")._(TickManager.connections[i])._("\n");

				}
				Debugging.UIConsole.Refresh();
			}
		}

#endif

		#endregion

		#region Serialization

		/// <summary>
		/// Generate a state tick.
		/// </summary>
		/// <param name="frameId"></param>
		public SerializationFlags GenerateMessage(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			OnCaptureCurrentState(frameId, Realm.Primary);

			OnQuantize(frameId, Realm.Primary);

			/// Begin SyncObject Serialization content
			var flags = OnSerialize(frameId, buffer, ref bitposition, writeFlags);
			return flags;
		}

#if SNS_WARNINGS
		/// <summary>
		/// Storage for frame arrival times for comparison against consumption time. Ridiculous long, but its just getting the max Enum value as the size of the buffer.
		/// </summary>
		private readonly float?[] bufferAddTime =
			new float?[(int)((SimpleSyncSettings.FrameCountEnum[])System.Enum.GetValues(typeof(SimpleSyncSettings.FrameCountEnum)))[((SimpleSyncSettings.FrameCountEnum[])System.Enum.GetValues(typeof(SimpleSyncSettings.FrameCountEnum))).Length - 1] + 1];
#endif

		/// <summary>
		/// Serialize all SyncObjs on this NetObj
		/// </summary>
		public SerializationFlags OnSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			//if ((writeFlags & SerializationFlags.NewConnection) != 0)
			//	Debug.LogError("NEW CONN " +writeFlags);

			SerializationFlags flags = 0;

#if SNS_WARNINGS
			/// Integrity check
			buffer.Write(111, ref bitposition, 8);
#endif

			/// Serialize Pack Objects
			int prevFrameId = ((frameId == 0) ? SimpleSyncSettings.FrameCount : frameId) - 1;
			int pcnt = packObjRecords.Count;
			for (int i = 0; i < pcnt; ++i)
			{
				var p = packObjRecords[i];
				var pframe = p.packFrames[frameId];
				
				/// make placeholder for mask bits (we don't know them yet)
				int mcnt = p.info.fieldCount;
				int maskpos = bitposition;
				bitposition += mcnt;

				int maskOffset = 0;
				flags |= p.info.PackFrameToBuffer(pframe, p.packFrames[prevFrameId], ref pframe.mask, ref maskOffset, buffer, ref bitposition, frameId, writeFlags);

				/// go back and write the mask bits
				for (int m = 0; m < mcnt; ++m)
					buffer.WriteBool(pframe.mask[m], ref maskpos);

			}

#if SNS_WARNINGS
			/// Integrity check
			buffer.Write(123, ref bitposition, 8);
#endif

			/// Serialize SyncComponents
			int cnt = onNetSerializeCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
			{

#if SNS_REPORTS && (UNITY_EDITOR || DEVELOPMENT_BUILD)
				int holdpos = bitposition;
				flags |= onNetSerialize[i].OnNetSerialize(frameId, buffer, ref bitposition);
				SimpleDataMonitor.AddData(onNetSerialize[i] as ISyncObject, bitposition - holdpos);
#else
				flags |= onNetSerializeCallbacks[i].OnNetSerialize(frameId, buffer, ref bitposition, writeFlags);
#endif
			}

#if SNS_WARNINGS
			/// Integrity check
			buffer.Write(234, ref bitposition, 8);
#endif

			return flags;
		}

		bool processedInitialBacklog;
		float firstDeserializeTime;


		public void OnDeserialize(int sourceFrameId, int originFrameId, int localframeId, byte[] buffer, ref int bitposition, bool hasData)
		{

#if SNS_WARNINGS

			bufferAddTime[localframeId] = Time.time;
#endif

			SerializationFlags flags = SerializationFlags.None;

			if (hasData)
			{

#if SNS_WARNINGS
				/// Integrity check
				if (buffer.Read(ref bitposition, 8) != 111)
					Debug.LogError("Failed Integrity check pre PackObjs.");
#endif
				FrameArrival arrival = NetMaster.CheckFrameArrival(localframeId);

				/// Deserialize Pack Objects
				//int prevFrameId = ((localframeId == 0) ? SimpleSyncSettings.FrameCount : localframeId) - 1;
				int pcnt = packObjRecords.Count;
				for (int i = 0; i < pcnt; ++i)
				{
					var p = packObjRecords[i];
					var pframe = p.packFrames[localframeId];

					int mcnt = p.info.fieldCount;
					for (int m = 0; m < mcnt; ++m)
						pframe.mask[m] = buffer.ReadBool(ref bitposition);

					int maskOffset = 0;

					Debug.Log("PRE");
					var flag = p.info.UnpackFrameFromBuffer(pframe, ref pframe.mask, ref pframe.isCompleteMask, ref maskOffset, buffer, ref bitposition, originFrameId, SerializationFlags.None);
					flags |= flag;

					//Debug.Log(localframeId + " Des READY? flg: " + flag + "  " + p.readyMask.AllAreTrue + " " + p.readyMask.PrintMask(null) + " "
					//	+ p.readyMask.Seg1 + ":" + p.readyMask.AllTrue1 + " " + p.readyMask.Seg2 + ":" + p.readyMask.AllTrue2);

					/// Experimental - Apply valid values as they arrive if pack object isn't fully ready. Ensure even a late arriving full update counts toward Ready
					if (!p.readyMask.AllAreTrue /*&& (flag & SerializationFlags.IsComplete) != 0*/)
					{
						/// Add any always ready bits (Triggers)
						p.readyMask.OR(p.info.defaultReadyMask);
						p.readyMask.OR(pframe.isCompleteMask);
						/// Only write to syncvars that are not already marked as valid
						FastBitMask128 newchangesmask = !p.readyMask & pframe.isCompleteMask;
						maskOffset = 0;
						p.info.CopyFrameToObj(pframe, p.component, ref newchangesmask, ref maskOffset);

						Debug.Log(localframeId + "<b> Des PRE COMPLETE? </b>" + p.readyMask.AllAreTrue + " " + p.readyMask.PrintMask(null) + " "
							+ p.readyMask.Seg1 + ":" + p.readyMask.AllTrue1 + " " + p.readyMask.Seg2 + ":" + p.readyMask.AllTrue2);
					}

					

				}

				packObjValidMask[localframeId] = true;

#if SNS_WARNINGS
				/// Integrity check
				if (buffer.Read(ref bitposition, 8) != 123)
					Debug.LogError(name + " Failed Integrity check post PackObjs. " + localframeId);
#endif
				/// Deserialize SyncObjs
				int cnt = onNetDeserializeCallbacks.Count;
				for (int i = 0; i < cnt; ++i)
				{
					var flag = onNetDeserializeCallbacks[i].OnNetDeserialize(originFrameId, localframeId, buffer, ref bitposition, arrival);
					flags |= flag;

					/// Experimental - immediately apply complete frames to unready sync objects.
					//if (arrival == FrameArrival.IsLate && (flag & SerializationFlags.IsComplete) != 0 && !syncObjReadyMask[i])
					//	Debug.Log("Call an early Apply here when a method exists for that.");

				}

#if SNS_WARNINGS
				/// Integrity check
				if (buffer.Read(ref bitposition, 8) != 234)
					Debug.LogError(name + " Failed Integrity check post SyncObjs. " + localframeId);
#endif
			}
		}

		#endregion

		#region NetMaster Events

		public void OnPreSimulate(int frameId, int _currSubFrameId)
		{
			int cnt = onPreSimulateCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onPreSimulateCallbacks[i].OnPreSimulate(frameId, _currSubFrameId);
		}

		public void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
		{
			int cnt = onPostSimulateCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onPostSimulateCallbacks[i].OnPostSimulate(frameId, subFrameId, isNetTick);
		}

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
#if PUN_2_OR_NEWER
			//Debug.Log(name + " OnNetSerialize NO " + pv.IsMine + " / " + pv.IsMine);
			if (!pv.IsMine)
			{
				return SerializationFlags.None;
			}

			//if ((writeFlags & SerializationFlags.Force) != 0)
			//	Debug.LogError("FORCE " + writeFlags);

			if (pv.Group != 0)

			{
				/// TODO: Ideally objects will not be individually serializing themselves into their own send.
				/// Serialize and Send this netobj state if this is a net tick.
				buffer = NetMsgSends.reusableNetObjBuffer;
				int localbitposition = 0;

				/// Write FrameId
				buffer.Write((uint)frameId, ref localbitposition, SimpleSyncSettings.FrameCountBits);

				/// Write not end of netObjs bool
				//buffer.WriteBool(true, ref localbitposition);

				/// Write netid
				buffer.WritePackedBytes((uint)netObjId, ref localbitposition, 32);

				/// Placeholder for data size. False means this is a contentless heartbea
				int holdHasDataPos = localbitposition;
				buffer.WriteBool(true, ref localbitposition);

				/// Placeholder for data size. False means this is a contentless heartbeat
				int holdDataSizePos = localbitposition;
				localbitposition += NetMaster.BITS_FOR_NETOBJ_SIZE;

				SerializationFlags lclflags = GenerateMessage(frameId, buffer, ref localbitposition, writeFlags);

				if (lclflags == SerializationFlags.None)
				{
					if (skipWhenEmpty)
						return SerializationFlags.None;
					else
					{
						/// revise the hasData bool to be false and rewind the bitwriter.
						localbitposition = holdHasDataPos;
						buffer.WriteBool(false, ref bitposition);
					}
				}

				if (lclflags != SerializationFlags.None || !SkipWhenEmpty)
				{
					/// Revise the data size now that we know it
					buffer.Write((uint)(localbitposition - holdDataSizePos), ref holdDataSizePos, NetMaster.BITS_FOR_NETOBJ_SIZE);

					/// Write end of netObjs marker
					buffer.WritePackedBytes(0, ref localbitposition, 32);

					NetMsgSends.Send(buffer, localbitposition, gameObject, lclflags, false);
				}

				/// We sent this object at the netObj level, so we report back to the NetMaster that nothing has been added to the master byte[] send.
				return SerializationFlags.None;
			}
#endif
			var flags = GenerateMessage(frameId, buffer, ref bitposition, writeFlags);
			return flags;
		}

		public void OnNetDeserialize(int sourceFrameId, int originFrameId, int localFrameId, byte[] buffer, ref int bitposition)
		{
			throw new System.NotImplementedException();
		}

		public void OnCaptureCurrentState(int frameId, Realm realm = Realm.Primary)
		{
			/// Capture PackObjs
			int pcnt = packObjRecords.Count;
			for (int i = 0; i < pcnt; ++i)
			{
				var p = packObjRecords[i];
				p.info.CaptureObj(p.component, p.packFrames[frameId]);
			}

			/// Capture SyncObjs
			int cnt = onCaptureCurrentStateCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onCaptureCurrentStateCallbacks[i].OnCaptureCurrentState(frameId, realm);
		}

		public void OnQuantize(int frameId, Realm realm = Realm.Primary)
		{
			int cnt = onQuantizeCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onQuantizeCallbacks[i].OnQuantize(frameId, realm);
		}

		#endregion

		#region Snapshot / Interpolate Events

		public void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId)
		{
			int cnt = onIncrementFramesCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onIncrementFramesCallbacks[i].OnIncrementFrame(newFrameId, newSubFrameId, previousFrameId, prevSubFrameId);
		}

		public bool OnSnapshot(int frameId)
		{

#if !PUN_2_OR_NEWER
			return false;
#else
			if (!pv)
				return false;

			if (pv.IsMine)
				return false;

			if (!pv.enabled)
				return false;

			/// TODO: Cache this properly
			ConnectionTick offsets;
			if (!TickManager.perConnOffsets.TryGetValue(pv.ControllerActorNr, out offsets))
				return false;

			if (!offsets.hadInitialSnapshot)
				return false;

			if (offsets.advanceCount == 0)
				return false;

			int snapFid = NetMaster.PreviousFrameId;
			int targFid;

			//Debug.Log(GetType().Name + " OnSnap - passed  - advance count: " + offsets.advanceCount);

			int frameCount = SimpleSyncSettings.FrameCount;

			for (int a = 0; a < offsets.advanceCount; ++a)
			{
				targFid = frameId + a;
				if (targFid >= frameCount)
					targFid -= frameCount;

				int invalidateFId = targFid - SimpleSyncSettings.HalfFrameCount;
				if (invalidateFId < 0)
					invalidateFId += frameCount;


				/// Snap Pack Objects
				bool packSnapIsValid = packObjValidMask[snapFid];

				//if (packSnapIsValid/* || packTargIsValid*/)
				{
					bool packTargIsValid = packObjValidMask[targFid];

					int pcnt = packObjRecords.Count;
					for (int i = 0; i < pcnt; ++i)
					{
						PackObjRecord p = packObjRecords[i];
						var snapf = p.packFrames[snapFid];
						var targpf = p.packFrames[targFid];

						/// update readymask with any new valid fields
						p.readyMask.OR(p.info.defaultReadyMask);
						p.readyMask.OR(snapf.isCompleteMask);

						Debug.Log(snapFid + " - " + p.readyMask.PrintMask(null) + " : " + p.info.defaultReadyMask.PrintMask(null) + " : " + snapf.isCompleteMask.PrintMask(null));

						

						/// TODO: when extrapolation is implemented, it will replace this basic copy
						if (!packTargIsValid)
							p.info.CopyFrameToFrame(snapf, targpf);

						/// Snapshot callbacks - fire every net tick changed or not
						int maskOffset = 0;
						p.info.SnapObject(snapf, targpf, p.component, ref p.readyMask, ref maskOffset);
					
						/// Apply new Snap value / Callback only fires on changes
						if (packSnapIsValid)
						{
							maskOffset = 0;
							p.info.CopyFrameToObj(snapf, p.component, ref snapf.mask, ref maskOffset);
						}

						/// Ready Mask has Changed - Issue callback
						if (p.readyMask.Compare(p.prevReadyMask) == false)
						{

							Debug.Log("Ready change " + p.readyMask.AllAreTrue);

							OnPackObjReadyChange(p.component, p.readyMask.AllAreTrue ? ReadyStateEnum.Ready : ReadyStateEnum.Unready);

							IPackObjOnReadyChange onReadyCallback = p.onReadyCallback;
							if (!ReferenceEquals(onReadyCallback, null))
								onReadyCallback.OnPackObjReadyChange(p.readyMask, p.readyMask.AllAreTrue);

							p.prevReadyMask.Copy(p.readyMask);
						}
					}
				}
				/// TODO: Needs a better home
				packObjValidMask[invalidateFId] = false;

				//Debug.Log(GetType().Name + " OnSnap - callback count " + onSnapshot.Count);

				/// Snap SyncObjs
				int cnt = onSnapshotCallbacks.Count;
				for (int i = 0; i < cnt; ++i)
					onSnapshotCallbacks[i].OnSnapshot(targFid);

				snapFid = targFid;

#if SNS_WARNINGS
				//Debug.Log(currTargFrameId + " New Target Time on Buffer " + (bufferAddTime[currTargFrameId].HasValue ? (Time.time - bufferAddTime[currTargFrameId]).ToString() : "NULL"));
				bufferAddTime[targFid] = null;
#endif
			}

			return true;
#endif
		}

		public bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
#if !PUN_2_OR_NEWER
			return false;
#else
			/// TODO: Cache this properly
			ConnectionTick offsets;
			if (!TickManager.perConnOffsets.TryGetValue(pv.ControllerActorNr, out offsets))
				return false;

			
			if (!offsets.hadInitialSnapshot)
				return false;

			/// Interpolate Pack Objects - only interpolate currently if both snap and targ are valid.
			/// TODO: This will change if/when extrapolate is added to Pack Object system
			if (packObjValidMask[targFrameId])
			{
				//Debug.Log("NOBJ Interp " + snapFId + " : " + targFId + packObjValidMask[snapFId] + " : " + packObjValidMask[targFId]);

				int pcnt = packObjRecords.Count;
				for (int i = 0; i < pcnt; ++i)
				{
					var p = packObjRecords[i];
					var snappf = p.packFrames[snapFrameId];
					var targpf = p.packFrames[targFrameId];
					int maskOffset = 0;
					p.info.InterpFrameToObj(snappf, targpf, p.component, t, ref p.readyMask, ref maskOffset);
				}
			}

			///  Interpolation Sync Obj
			int cnt = onInterpolateCallbacks.Count;
			for (int i = 0; i < cnt; ++i)
				onInterpolateCallbacks[i].OnInterpolate(snapFrameId, targFrameId, t);

			return true;
#endif
		}

		

		#endregion

	}


#if UNITY_EDITOR
	[CustomEditor(typeof(NetObject))]
	public class NetObjectEditor : NetCoreHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Network entity component used by Simple Network Sync. Collects all networking interfaces from child components," +
				" and relays network callbacks, serialization, and events between the NetMaster and synced components.";
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();

			SimpleSyncSettings.Single.DrawGui(target, true, false, false);
		}
	}

#endif
}
