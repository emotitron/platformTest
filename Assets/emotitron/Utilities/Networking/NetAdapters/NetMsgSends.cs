using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using emotitron.Compression;


#if PUN_2_OR_NEWER
using Photon;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
#endif

namespace emotitron.Utilities.Networking
{
	public enum ReceiveGroup { Others, All, Master }

	/// <summary>
	/// Unified code for sending network messages across different Network Libraries.
	/// </summary>
	public static class NetMsgSends
	{
		public static byte[] reusableBuffer = new byte[16384];
		public static byte[] reusableNetObjBuffer = new byte[4096];

		public static List<int> reliableTargets = new List<int>();

		public static void Send(this byte[] buffer, int bitposition, UnityEngine.Object refObj, SerializationFlags flags, bool flush = false)
		{
#if PUN_2_OR_NEWER

			var currentRoom = PhotonNetwork.CurrentRoom;

			if (PhotonNetwork.OfflineMode || currentRoom == null || currentRoom.Players == null)
			{
				return;
			}

			// no need to send OnSerialize messages while being alone (these are not buffered anyway)
			if (currentRoom.Players.Count <= 1)
			{
				return;
			}

			ReceiveGroup sendTo = ((flags & SerializationFlags.SendToSelf) != 0) ? ReceiveGroup.All : ReceiveGroup.Others;
			bool forceReliable = (flags & SerializationFlags.ForceReliable) != 0;

			int bytecount = (bitposition + 7) >> 3;

			System.ArraySegment<byte> byteseg = new System.ArraySegment<byte>(buffer, 0, bytecount);

			PhotonNetwork.NetworkingClient.OpRaiseEvent(NetMsgCallbacks.DEF_MSG_ID, byteseg, opts[(int)sendTo], (forceReliable) ? sendOptsReliable : sendOptsUnreliable);

			/// Send reliable copies of the update to new connections
			if (reliableTargets.Count != 0)
			{
				//Debug.LogError("New target connection send " + reliableTargets.Count);
				//if ((flags & SerializationFlags.ForceReliable) == 0)
				//	Debug.LogWarning("New connection target, but update was not flagged as reliable?");

				targetRaiseOpts.TargetActors = reliableTargets.ToArray();
				reliableTargets.Clear();
				PhotonNetwork.NetworkingClient.OpRaiseEvent(NetMsgCallbacks.DEF_MSG_ID, byteseg, targetRaiseOpts, sendOptsReliable);

			}

			if (flush)
				PhotonNetwork.NetworkingClient.Service();
#endif
		}

#if PUN_2_OR_NEWER

		public static bool ReadyToSend { get { return PhotonNetwork.NetworkClientState == ClientState.Joined; } }

		private static RaiseEventOptions[] opts = new RaiseEventOptions[3]
		{
			new RaiseEventOptions() { Receivers = ReceiverGroup.Others },
			new RaiseEventOptions() { Receivers = ReceiverGroup.All },
			new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }
		};

		private static RaiseEventOptions targetRaiseOpts = new RaiseEventOptions() { Receivers = ReceiverGroup.Others };


		private static SendOptions sendOptsUnreliable = new SendOptions() { DeliveryMode = DeliveryMode.UnreliableUnsequenced };
		private static SendOptions sendOptsReliable = new SendOptions() { DeliveryMode = DeliveryMode.ReliableUnsequenced };
#else
		public static bool ReadyToSend { get { return false; } }
#endif
		public static bool AmActiveServer { get { return false; } }
	}

}
