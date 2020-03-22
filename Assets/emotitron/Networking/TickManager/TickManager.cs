//Copyright 2018, Davin Carten, All rights reserved

using emotitron.Utilities.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking.Internal
{
	
	public class TickManager
	{
		public readonly static Dictionary<int, ConnectionTick> perConnOffsets = new Dictionary<int, ConnectionTick>();
		public readonly static List<int> connections = new List<int>();

		public static TickManager single;

		/// <summary>
		/// Flag indicates that the next update should be flagged as needing to be reliable.
		/// </summary>
		public static bool needToSendInitialForNewConn;

		// Cached values
		static int frameCount ;
		static int halfFrameCount;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Bootstrap()
		{
			single = new TickManager();
			MasterNetAdapter.onClientDisconnectCallback += OnClientDisconnect;
			//Photon.Pun.PhotonNetwork.NetworkingClient.AddCallbackTarget(single);

			frameCount = SimpleSyncSettings.FrameCount;
			halfFrameCount = SimpleSyncSettings.HalfFrameCount;
		}

		/// <summary>
		/// Run this prior to OnSnapshot, to establish if the number of snapshots for connection objects needs to be a value other than 1.
		/// </summary>
		public static void PreSnapshot()
		{
			for (int i = 0; i < connections.Count; ++i)
				perConnOffsets[connections[i]].SnapshotAdvance();
		}

		public static void PostSnapshot()
		{
			for (int i = 0; i < connections.Count; ++i)
				perConnOffsets[connections[i]].PostSnapshot();
		}

		public static void RemoveConnection(int connId)
		{
			if (perConnOffsets.ContainsKey(connId))
			{
				perConnOffsets.Remove(connId);
				connections.Remove(connId);
			}
		}

		/// <summary>
		/// Notify tick manager of an incoming frame, so it can register/modify offsets for that connection.
		/// </summary>
		/// <returns>FrameId translated into localFrameId</returns>
		public static int LogIncomingFrame(int connId, int originFrameId)
		{
			ConnectionTick offsets;

			if (!perConnOffsets.TryGetValue(connId, out offsets))
			{
				LogNewConnection(connId, originFrameId, frameCount, out offsets);
			}

			/// In the future, we should be making use of localframe, for now it is the same as originframe for PUN
			int localFrameId = originFrameId += offsets.originToLocal;
			if (localFrameId >= frameCount)
				localFrameId -= frameCount;

			int currTargFrameId = NetMaster.CurrentFrameId; // offsets.currTargFrameId;

			bool frameIsInFuture;

			if (localFrameId == currTargFrameId)
			{
				frameIsInFuture = false;
			}
			else
			{
				/// Flag frame as valid if it is still in the future
				int frameOffsetFromCurrent = localFrameId - currTargFrameId;
				if (frameOffsetFromCurrent < 0)
					frameOffsetFromCurrent += frameCount;

				frameIsInFuture = frameOffsetFromCurrent !=0 && frameOffsetFromCurrent < halfFrameCount;
			}

#if UNITY_EDITOR

			if (!frameIsInFuture)
			{
				int currSnapFrameId = NetMaster.PreviousFrameId;

				string strframes = " Incoming Frame: " + localFrameId + " Current Interpolation: " + currSnapFrameId + "->" + currTargFrameId;
				const string STR_TAG = "\nSeeing many of these indicates an unstable connection, too small a buffer setting, or too high a tick rate.";

				if (localFrameId == currTargFrameId)
					Debug.Log("<b>Late Update </b>" + strframes + ". Already interpolating to this frame. Not critically late, but getting close." + STR_TAG);
				else if (localFrameId == currSnapFrameId)
					Debug.LogWarning("<b>Critically Late Update</b>" + strframes + " Already applied and now interpolating from this frame. Likely data loss." + STR_TAG);
				else
					Debug.LogWarning("<b>Critically Late Update</b> " + strframes + " Already applied this frame.  Likely data loss." + STR_TAG);
			}

#endif

			offsets.frameArrivedTooLate |= !frameIsInFuture;
			offsets.validFrames.Set(localFrameId, frameIsInFuture);

			return localFrameId;
		}

		private static void LogNewConnection(int connId, int originFrameId, int frameCount, out ConnectionTick offsets)
		{
			int currentFrame = NetMaster.CurrentFrameId;

			/// Apply default offset from current local frame
			int startingFrameId = currentFrame + (SimpleSyncSettings.TargetBufferSize /*+ 1*/);
			while (startingFrameId >= frameCount)
				startingFrameId -= frameCount;

			int originToLocal = startingFrameId - originFrameId;
			if (originToLocal < 0)
				originToLocal += frameCount;

			int localToOrigin = frameCount - originToLocal;
			if (localToOrigin < 0)
				localToOrigin += frameCount;

			/// Curently local and origin are the same.
			/// TODO: Pool these
			offsets = new ConnectionTick(originToLocal, localToOrigin);

			perConnOffsets.Add(connId, offsets);
			connections.Add(connId);

			/// Add this connection to the NetSends list of targets for a reliable update.
			NetMsgSends.reliableTargets.Add(connId);
			needToSendInitialForNewConn = true;
		}

		private static void OnClientDisconnect(object connObj, int connId)
		{
			RemoveConnection(connId);

		}
	}
}

