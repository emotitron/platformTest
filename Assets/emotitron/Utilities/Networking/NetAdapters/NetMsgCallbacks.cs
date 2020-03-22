//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Debugging;
using emotitron.Compression;

#if PUN_2_OR_NEWER
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
#endif

/// <summary>
/// Generic handelers for routing incoming network messages as byte[] arrays to registered handlers. This abstracts the various netlibs into a
/// standard byte[] format.
/// </summary>
namespace emotitron.Utilities.Networking
{

	public static class NetMsgCallbacks
	{
		public delegate void ByteBufferCallback(object conn, int connId, byte[] buffer);

		private static Dictionary<int, CallbackLists> callbacks = new Dictionary<int, CallbackLists>();

		private class CallbackLists
		{
			public List<ByteBufferCallback> bufferCallbacks;
		}

		public const byte DEF_MSG_ID = 215;

#if PUN_2_OR_NEWER

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void RegisterOnEventListener()
		{

			PhotonNetwork.NetworkingClient.EventReceived += OnEvent;

		}

		/// <summary>
		/// Capture incoming Photon messages here.
		/// </summary>
		public static void OnEvent(EventData photonEvent)
		{
			byte msgId = photonEvent.Code;

			if (!callbacks.ContainsKey(msgId))
				return;

			//// ignore messages from self.
			//if (PhotonNetwork.IsMasterClient && PhotonNetwork.MasterClient.ActorNumber == photonEvent.Sender)
			//{
			//	Debug.Log("Master Client talking to self? Normal occurance for a few seconds after Master leaves the game and a new master is selected.");
			//	return;
			//}

			byte[] buffer = (photonEvent.CustomData as byte[]);

			var cbs = callbacks[msgId];
			if (cbs.bufferCallbacks != null && cbs.bufferCallbacks.Count > 0)
			{
				foreach (var cb in cbs.bufferCallbacks)
					cb(null, photonEvent.Sender, buffer);
			}
		}
#endif

#region Handler Registration

		[System.Obsolete("Removed the asServer from UNET side, killing it here as well.")]
		public static void RegisterCallback(byte msgid, ByteBufferCallback callback, bool asServer)
		{
			if (!callbacks.ContainsKey(msgid))
				callbacks.Add(msgid, new CallbackLists());

			if (callbacks[msgid].bufferCallbacks == null)
				callbacks[msgid].bufferCallbacks = new List<ByteBufferCallback>();

			var cbs = callbacks[msgid].bufferCallbacks;

			if (!cbs.Contains(callback))
				cbs.Add(callback);
		}

		public static void RegisterCallback(ByteBufferCallback callback)
		{
			RegisterCallback(DEF_MSG_ID, callback);
		}
		public static void RegisterCallback(byte msgid, ByteBufferCallback callback)
		{
			if (!callbacks.ContainsKey(msgid))
				callbacks.Add(msgid, new CallbackLists());

			if (callbacks[msgid].bufferCallbacks == null)
				callbacks[msgid].bufferCallbacks = new List<ByteBufferCallback>();

			var cbs = callbacks[msgid].bufferCallbacks;

			if (!cbs.Contains(callback))
				cbs.Add(callback);
		}

		[System.Obsolete("Removed the asServer from UNET side, killing it here as well.")]
		public static void UnregisterCallback(byte msgid, ByteBufferCallback callback, bool asServer)
		{
			if (callbacks.ContainsKey(msgid))
			{
				var cbs = callbacks[msgid];
				cbs.bufferCallbacks.Remove(callback);

				if (cbs.bufferCallbacks.Count == 0)
					callbacks.Remove(msgid);
			}
		}

		public static void UnregisterCallback(ByteBufferCallback callback)
		{
			UnregisterCallback(DEF_MSG_ID, callback);
		}
		public static void UnregisterCallback(byte msgid, ByteBufferCallback callback)
		{
			if (callbacks.ContainsKey(msgid))
			{
				var cbs = callbacks[msgid];
				cbs.bufferCallbacks.Remove(callback);

				if (cbs.bufferCallbacks.Count == 0)
					callbacks.Remove(msgid);
			}
		}

#endregion  // END HANDLERS
	}
}
