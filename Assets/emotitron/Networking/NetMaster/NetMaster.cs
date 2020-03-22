// Copyright 2019, Davin Carten, All rights reserved
// This code may only be used in game development, but may not be used in any tools or assets that are sold or made publicly available to other developers.


using UnityEngine;

using System.Collections.Generic;
using emotitron.Utilities.CallbackUtils;
using emotitron.Compression;
using emotitron.Utilities.Networking;
using emotitron.Networking.Internal;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

#if UNITY_EDITOR
using UnityEditor;
using emotitron.Utilities;
#endif

namespace emotitron.Networking
{

	public class NetMaster : MonoBehaviour

	{
		/// <summary>
		/// Singleton instance of the NetMaster. "There can be only one."
		/// </summary>
		public static NetMaster single;

		public static bool isShuttingDown;

		/// <summary>
		/// Value used in Update() timing to generate the t value for OnInterpolate calls.
		/// </summary>
		protected static float lastSentTickTime;

		#region Properties

		private static int _currFrameId, _currSubFrameId, _prevFrameId, _prevSubFrameId;
		public static int CurrentFrameId { get { return _currFrameId; } }
		/// <summary>
		/// When Every X Tick is being sent, ticks are numbered by the sent interval. 
		/// Simulation ticks between these maintain the same FrameId, and increment the SubFrameId.
		/// Frames are sent when the SubFrameId equals flips back to zero.
		/// </summary>
		public static int CurrentSubFrameId { get { return _currSubFrameId; } }
		public static int PreviousFrameId { get { return _prevFrameId; } }
		public static int PreviousSubFrameId { get { return _prevSubFrameId; } }

		public static float NormTimeSinceFixed { get; private set; }

		protected static float rtt;
		public static float RTT { get { return rtt; } }

		#endregion

		#region Outgoing Callbacks

		public static List<IOnPreUpdate> onPreUpdates = new List<IOnPreUpdate>();
		public static List<IOnPostUpdate> onPostUpdates = new List<IOnPostUpdate>();

		public static List<IOnPreLateUpdate> onPreLateUpdates = new List<IOnPreLateUpdate>();
		public static List<IOnPostLateUpdate> onPostLateUpdates = new List<IOnPostLateUpdate>();

		public static List<IOnIncrementFrame> onIncrementFrames = new List<IOnIncrementFrame>();
		//public static List<IOnCaptureInputs> onCaptureInputs = new List<IOnCaptureInputs>();

		public static List<IOnPreSimulate> onPreSimulates = new List<IOnPreSimulate>();
		public static List<IOnPostSimulate> onPostSimulates = new List<IOnPostSimulate>();

		public static List<IOnNetSerialize> onNetSerializes = new List<IOnNetSerialize>();
		public static List<IOnSnapshot> onSnapshots = new List<IOnSnapshot>();
		public static List<IOnInterpolate> onInterpolates = new List<IOnInterpolate>();

		public static List<IOnPreQuit> onPreQuits = new List<IOnPreQuit>();

		public static Queue<object> pendingRegistrations = new Queue<object>();
		public static Queue<object> pendingDeregistrations = new Queue<object>();
		/// <summary>
		/// Find callback interfaces used by NetMaster in this class, and add/remove them from the callback lists.
		/// </summary>
		public static void RegisterCallbackInterfaces(object comp, bool register = true, bool delay = false)
		{
			if (delay)
			{
				if (register)
					pendingRegistrations.Enqueue(comp);
				else
				{
					//if (comp as Object)
					//	Debug.Log((comp as Object).name + "  DELAY UNREGISTER");
					pendingDeregistrations.Enqueue(comp);
				}
				return;
			}
			//if ((comp as Object) && register == false)
			//	Debug.Log((comp as Object).name + "  UNREGISTER");

			CallbackUtilities.RegisterInterface(onPreUpdates, comp, register);
			CallbackUtilities.RegisterInterface(onPostUpdates, comp, register);

			CallbackUtilities.RegisterInterface(onPreLateUpdates, comp, register);
			CallbackUtilities.RegisterInterface(onPostLateUpdates, comp, register);

			CallbackUtilities.RegisterInterface(onIncrementFrames, comp, register);
			//onCaptureInputsCount = CallbackUtilities.RegisterInterface(onCaptureInputs, comp, register);

			CallbackUtilities.RegisterInterface(onPreSimulates, comp, register);
			CallbackUtilities.RegisterInterface(onPostSimulates, comp, register);

			CallbackUtilities.RegisterInterface(onNetSerializes, comp, register);
			CallbackUtilities.RegisterInterface(onSnapshots, comp, register);
			CallbackUtilities.RegisterInterface(onInterpolates, comp, register);
			CallbackUtilities.RegisterInterface(onPreQuits, comp, register);
		}

		#endregion

		#region Cached values

		static int frameCount;
		static int frameCountBits;
		static int sendEveryXTick;

		#endregion
		/// <summary>
		/// Startup Bootstrap for finding/Creating NetMaster singleton.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void EnsureExistsInScene()
		{
			/// Some basic singleton enforcement
			GameObject go = null;

			if (single)
			{
				go = single.gameObject;
			}
			else
			{
				/// Use NetMasterLate go if that singleton exists (this will be rare or never, but just here to cover all cases)
				if (NetMasterLate.single)
					go = NetMasterLate.single.gameObject;

				single = FindObjectOfType<NetMaster>();
				if (single)
				{
					go = single.gameObject;
				}
				else
				{
					// No singletons exist... make a new GO
					if (!go)
						go = new GameObject("Net Master");

					single = go.AddComponent<NetMaster>();
				}
			}

			// Enforce singleton for NetMasterLate
			if (!NetMasterLate.single)
			{
				NetMasterLate.single = FindObjectOfType<NetMasterLate>();

				if (!NetMasterLate.single)
					NetMasterLate.single = go.AddComponent<NetMasterLate>();
			}

			// cached values that don't change
			frameCount = SimpleSyncSettings.FrameCount;
			frameCountBits = SimpleSyncSettings.FrameCountBits;
			sendEveryXTick = SimpleSyncSettings.SendEveryXTick;

			NetMsgCallbacks.RegisterCallback(ReceiveMessage);
		}

		private void Awake()
		{

			if (single && single != this)
			{
				/// If a singleton already exists, destroy the old one - TODO: Not sure about this behaviour yet. Allows for settings changes with scene changes.
				Destroy(single);
			}

			single = this;

			DontDestroyOnLoad(this);

			_prevFrameId = frameCount - 1;
			_prevSubFrameId = sendEveryXTick - 1;
		}

		private void OnApplicationQuit()
		{
			isShuttingDown = true;

			int cnt = onPreQuits.Count;
			for (int i = 0; i < cnt; i++)
				onPreQuits[i].OnPreQuit();
		}

		private bool simulationHasRun = false;

		private void FixedUpdate()
		{

#if SNS_DEV
			if (!SimpleSyncSettings.single.enabled)
				return;
#endif
			/// Halt everything if networking isn't ready.
			bool readyToSend = NetMsgSends.ReadyToSend;
			if (!readyToSend)
			{
				DoubleTime.SnapFixed();
				return;
			}

			if (simulationHasRun)
				PostSimulate();

			DoubleTime.SnapFixed();

#if PUN_2_OR_NEWER

			/// Make sure we don't have any incoming messages. PUN checks this pre-Update but not so explicitly at the top of the fixed.
			/// We want to ensure that we are running our simulation with the most current network input/states so best to make sure we have all that is available.
			/// Make sure Photon isn't holding out on us just because a FixedUpdate didn't happen this Update()
			bool doDispatch = true;

			while (PhotonNetwork.IsMessageQueueRunning && doDispatch)
			{
				// DispatchIncomingCommands() returns true of it found any command to dispatch (event, result or state change)
				doDispatch = PhotonNetwork.NetworkingClient.LoadBalancingPeer.DispatchIncomingCommands();
			}

			rtt = PhotonNetwork.GetPing() * .001f;
#endif
			/// Moved to NetMasterLate
			//int cnt = onPreSimulates.Count;
			//for (int i = 0; i < cnt; ++i)
			//	onPreSimulates[i].OnPreSimulate(_currFrameId, _currSubFrameId);

			simulationHasRun = true;
		}

		void Update()
		{

#if SNS_DEV
			if (!SimpleSyncSettings.single.enabled)
				return;
#endif

			if (simulationHasRun)
				PostSimulate();

			DoubleTime.SnapUpdate();

			NormTimeSinceFixed = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;

			int cnt = onPreUpdates.Count;
			for (int i = 0; i < cnt; ++i)
				onPreUpdates[i].OnPreUpdate();

			float t = (Time.time - lastSentTickTime) / (Time.fixedDeltaTime * sendEveryXTick);

			cnt = onInterpolates.Count;
			for (int i = 0; i < cnt; ++i)
				onInterpolates[i].OnInterpolate(_prevFrameId, _currFrameId, t);
		}

		private void LateUpdate()
		{
			int cnt = onPreLateUpdates.Count;
			for (int i = 0; i < cnt; ++i)
				onPreLateUpdates[i].OnPreLateUpdate();

		}

		public static void ApplyQueuedRegistrations()
		{
			/// Run any delayed registration tasks.
			while (pendingRegistrations.Count > 0)
				RegisterCallbackInterfaces(pendingRegistrations.Dequeue(), true, false);

			while (pendingDeregistrations.Count > 0)
				RegisterCallbackInterfaces(pendingDeregistrations.Dequeue(), false, false);
		}


		/// <summary>
		/// Unity lacks a PostPhysX/PostSimulation callback, so this is the closest we can get to creating one.
		/// If this happens during Update, it is important to sample values from the simulaion (such as rb.position, or your own tick based sim results)
		/// RATHER than from the scene objects, which may be interpolated.
		/// </summary>
		void PostSimulate()
		{
			bool isNetTick = _currSubFrameId == sendEveryXTick - 1;
			int cnt = onPostSimulates.Count;
			for (int i = 0; i < cnt; ++i)
				onPostSimulates[i].OnPostSimulate(_currFrameId, _currSubFrameId, isNetTick);

			if (isNetTick)
				SerializeAndSendNetObjects();

			IncrementFrameId();

			simulationHasRun = false;

		}

		/// <summary>
		/// Increment the current frameId. If we are sending every X simulation tics, the Subframe only gets incremented.
		/// Frames are serialized and sent just before CurrentFrameId is incremented (after all subframes have been simulated).
		/// </summary>
		private static void IncrementFrameId()
		{
			_prevSubFrameId = _currSubFrameId;
			_currSubFrameId++;

			if (_currSubFrameId >= sendEveryXTick)
			{
				_currSubFrameId = 0;
				_prevFrameId = _currFrameId;

				_currFrameId++;
				if (_currFrameId >= frameCount)
					_currFrameId = 0;
			}
			int cnt = onIncrementFrames.Count;
			for (int i = 0; i < cnt; ++i)
				onIncrementFrames[i].OnIncrementFrame(_currFrameId, _currSubFrameId, _prevFrameId, _prevSubFrameId);

			if (_currSubFrameId == 0) // sendEveryXTick - 1)
			{
				///  Insert pre snapshot tick manager test for number of snaps needed per connection.
				TickManager.PreSnapshot();

				cnt = onSnapshots.Count;
				for (int i = 0; i < cnt; ++i)
					onSnapshots[i].OnSnapshot(_currFrameId);

				TickManager.PostSnapshot();

				lastSentTickTime = Time.fixedTime;
			}

			//Debug.Log("NewTick " + _currFrameId + ":" + _currSubFrameId);
		}


		public const int BITS_FOR_NETOBJ_SIZE = 16;
		private static void SerializeAndSendNetObjects()
		{
			byte[] buffer = NetMsgSends.reusableBuffer;
			int bitposition = 0;

			SerializationFlags writeFlags;
			SerializationFlags flags;
			if (TickManager.needToSendInitialForNewConn)
			{
				//Debug.LogError("needToSendInitialForNewConn");
				//Debug.Log("<b>New connection</b> - send forced initial");
				writeFlags = SerializationFlags.NewConnection | SerializationFlags.ForceReliable | SerializationFlags.Force;
				flags = SerializationFlags.HasChanged; // SerializationFlags.ForceReliable;
				TickManager.needToSendInitialForNewConn = false;
			}
			else
			{
				writeFlags = SerializationFlags.None;
				flags = SerializationFlags.None;
			}

			/// Write frameId
			buffer.Write((uint)_currFrameId, ref bitposition, frameCountBits);

			int cnt = onNetSerializes.Count;
			for (int i = 0; i < cnt; ++i)
			{
				IOnNetSerialize icb = onNetSerializes[i];

				/// Not end of netobjs write bool
				int holdStartPos = bitposition;

				/// Write netid
				buffer.WritePackedBytes((uint)(icb as NetObject).netObjId, ref bitposition, 32);

				/// Write hadData bool
				int holdHasDataPos = bitposition;
				buffer.WriteBool(true, ref bitposition);

				/// Log the data size write position and write a placeholder.
				int holdDataSizePos = bitposition;
				bitposition += BITS_FOR_NETOBJ_SIZE;

				var objflags = icb.OnNetSerialize(_currFrameId, buffer, ref bitposition, writeFlags);

				/// Skip netobjs if they had nothing to say
				if (objflags == SerializationFlags.None)
				{
					/// Rewind if this is a no-data write.
					if (icb.SkipWhenEmpty)
					{
						bitposition = holdStartPos;
						//Debug.Log((icb as Component).name + " SLEEP OUT");
					}
					else
					{
						bitposition = holdHasDataPos;
						buffer.WriteBool(false, ref bitposition);
					}
				}
				else
				{
					/// Revise the data size now that we know it.
					flags |= objflags;
					int bitcount = bitposition - holdDataSizePos;
					buffer.Write((uint)bitcount, ref holdDataSizePos, BITS_FOR_NETOBJ_SIZE);
				}

				//Debug.Log(objflags + " / flg: " + (onNetSerialize[i]  as Component).name + " " + flags);
			}

			if (flags == SerializationFlags.None)
				return;

			/// End of NetObject write bool
			buffer.WritePackedBytes(0, ref bitposition, 32);


			NetMsgSends.Send(buffer, bitposition, null, flags, true);
		}


		/// <summary>
		/// Incoming message receiver.
		/// </summary>
		public static void ReceiveMessage(object conn, int connId, byte[] buffer)
		{
			int bitposition = 0;
			/// Read frameId
			int frameId = (int)buffer.Read(ref bitposition, frameCountBits);

			int localFrameId = TickManager.LogIncomingFrame(connId, frameId);

			/// Read all netobjs
			while (true)
			{
				/// Read next netid
				int netid = (int)buffer.ReadPackedBytes(ref bitposition, 32);

				if (netid == 0)
					break;

				/// Read hasData bool
				bool hasData = buffer.ReadBool(ref bitposition);

				/// No data... this is just a heartbeat
				if (!hasData)
					continue;

				/// Read data size
				int holdDataSizePos = bitposition;
				int bitcount = (int)buffer.Read(ref bitposition, BITS_FOR_NETOBJ_SIZE);

				var netobj = netid.FindComponentByNetId<NetObject>();
				//Debug.Log("Incoming " + netid + " " + netobj.name);

				/// If netobj can't be found, jump to the next object in the stream
				if (ReferenceEquals(netobj, null))
				{
					bitposition = holdDataSizePos + bitcount;
					continue;
				}

				netobj.OnDeserialize(frameId, frameId, localFrameId, buffer, ref bitposition, hasData);
			}
		}

		public static FrameArrival CheckFrameArrival(int incomingFrame)
		{
			int delta = incomingFrame - _prevFrameId;

			if (delta == 0)
				return FrameArrival.IsSnap;

			// change negative values into positive to check for wrap around.
			if (delta < 0)
				delta += SimpleSyncSettings.FrameCount;

			if (delta == 1)
				return FrameArrival.IsTarget;

			if (delta >= SimpleSyncSettings.HalfFrameCount)
				return FrameArrival.IsLate;

			return FrameArrival.IsFuture;

		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(NetMaster))]
	public class NetMasterEditor : NetCoreHeaderEditor
	{
		protected override string BackTexturePath
		{
			get
			{
				return "Header/RedBack";
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Early Timing singleton used by all Simple Network Sync components. " +
			"Effectively a tiny networking specific Update Manager.\n\n" +
			"This component will be added automatically at runtime if one does not exist in your scene.\n\n" +
			"This component should be operating on the earliest possible Script Execution timing, " +
			"in order to produce Fixed/Late/Update callbacks before all other components have run. "
			, MessageType.None);

		}
	}

#endif
}

