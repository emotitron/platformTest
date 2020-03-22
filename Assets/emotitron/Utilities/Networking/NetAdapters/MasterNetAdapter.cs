//Copyright 2018, Davin Carten, All rights reserved


using emotitron.Compression;
using System.Collections;
using System.Collections.Generic;
using emotitron.Utilities.CallbackUtils;
using UnityEngine;

#if PUN_2_OR_NEWER
using Photon.Realtime;
using Photon.Pun;
#endif


namespace emotitron.Utilities.Networking
{

	/// <summary>
	/// PUN2 specific stuff tucked away.
	/// </summary>
	public class MasterNetAdapter
#if PUN_2_OR_NEWER
		: IConnectionCallbacks, IInRoomCallbacks, IMatchmakingCallbacks
#endif
	{
		public const int BITS_FOR_NETID = 32;
		public const int BITS_FOR_CLIENTID = 32;

		/// <summary>
		/// Number of bits used for NetObj write length. This dictates the max number of bits that can be writeen each update per NetObject. 13 bits = 8191 max bitcount
		/// </summary>
#if STITCH_WHOLE_BYTES
		public const int WRITE_SIZE_BITS = 10; // (2 ^ BYTE_CNT_BYTES) must produce a larger nuber than MAX_BUFFER_BYTES
#else
		public const int WRITE_SIZE_BITS = 13;
#endif
		public const int MAX_BUFFER_BYTES = 1020;
		public const int MAX_BUFFER_BITS = MAX_BUFFER_BYTES * 8;

		public const int SVR_CONN_TO_SELF_ID = 0;
		public const int CLNT_CONN_TO_SVR_ID = -1;

		public static int MasterConnId
		{
			get
			{
#if PUN_2_OR_NEWER
				return (PhotonNetwork.MasterClient != null) ? PhotonNetwork.MasterClient.ActorNumber : -1;
#else
				return 0;
#endif
			}
		}

		public static MasterNetAdapter single;
		public static int overflowBitPos;
		public static ICollection<object> connections;

		// Static Constructor
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void ForceAwake()
		{
			if (single == null)
				single = new MasterNetAdapter();

			RegisterNetworkCallbacks();
		}

#region Outgoing Callback Interfaces

		public interface IConnectionEvents
		{
			void OnClientConnect(object connObj, int connId);
			void OnClientDisconnect(object connObj, int connId);
			void OnServerConnect(object connObj, int connId);
			void OnServerDisconnect(object connObj, int connId);
		}

		public static List<IConnectionEvents> iConnectionEvents = new List<IConnectionEvents>();

		public interface IOnClientConnect { void OnClientConnect(object connObj, int connId); }
		public static List<IOnClientConnect> iOnClientConnect = new List<IOnClientConnect>();
		public delegate void OnClientConnectCallback(object connObj, int connId);
		public static OnClientConnectCallback onClientConnectCallback;

		public interface IOnClientDisconnect { void OnClientDisconnect(object connObj, int connId); }
		public static List<IOnClientDisconnect> iOnClientDisconnect = new List<IOnClientDisconnect>();
		public delegate void OnClientDisconnectCallback(object connObj, int connId);
		public static OnClientDisconnectCallback onClientDisconnectCallback;

		public interface IOnServerConnect { void OnServerConnect(object connObj, int connId); }
		public static List<IOnServerConnect> iOnServerConnect = new List<IOnServerConnect>();
		public delegate void OnServerConnectCallback(object connObj, int connId);
		public static OnServerConnectCallback onServerConnectCallback;

		public interface IOnServerDisconnect { void OnServerDisconnect(object connObj, int connId); }
		public static List<IOnServerDisconnect> iOnServerDisconnect = new List<IOnServerDisconnect>();
		public delegate void OnServerDisconnectCallback(object connObj, int connId);
		public static OnServerDisconnectCallback onServerDisconnectCallback;

		public static void RegisterCallbackInterfaces(Object obj, bool register = true)
		{
			CallbackUtilities.RegisterInterface(iConnectionEvents, obj, register);
			CallbackUtilities.RegisterInterface(iOnClientConnect, obj, register);
			CallbackUtilities.RegisterInterface(iOnClientDisconnect, obj, register);
			CallbackUtilities.RegisterInterface(iOnServerConnect, obj, register);
			CallbackUtilities.RegisterInterface(iOnServerDisconnect, obj, register);
		}

#endregion

#region  Messages

#if PUN_2_OR_NEWER

		/// <summary>
		/// Room callbacks
		/// </summary>
		/// <param name="newPlayer"></param>
		public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
		{
			foreach (var cb in iConnectionEvents)
				cb.OnServerConnect(newPlayer, newPlayer.ActorNumber);

			foreach (var cb in iOnServerConnect)
				cb.OnServerConnect(newPlayer, newPlayer.ActorNumber);

			if (onServerConnectCallback != null)
				onServerConnectCallback.Invoke(newPlayer, newPlayer.ActorNumber);

			foreach (var cb in iConnectionEvents)
				cb.OnClientConnect(newPlayer, newPlayer.ActorNumber);

			foreach (var cb in iOnClientConnect)
				cb.OnClientConnect(newPlayer, newPlayer.ActorNumber);

			if (onClientConnectCallback != null)
				onClientConnectCallback.Invoke(newPlayer, newPlayer.ActorNumber);
		}

		public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
		{
			foreach (var cb in iConnectionEvents)
				cb.OnServerDisconnect(otherPlayer, otherPlayer.ActorNumber);

			foreach (var cb in iOnServerDisconnect)
				cb.OnServerDisconnect(otherPlayer, otherPlayer.ActorNumber);

			if (onServerDisconnectCallback != null)
				onServerDisconnectCallback.Invoke(otherPlayer, otherPlayer.ActorNumber);


			foreach (var cb in iConnectionEvents)
				cb.OnClientDisconnect(otherPlayer, otherPlayer.ActorNumber);

			foreach (var cb in iOnClientDisconnect)
				cb.OnClientDisconnect(otherPlayer, otherPlayer.ActorNumber);

			if (onClientDisconnectCallback != null)
				onClientDisconnectCallback.Invoke(otherPlayer, otherPlayer.ActorNumber);
		}

		public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }

		public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }

		public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) { }

		/// Matchmaking callbacks

		public void OnFriendListUpdate(List<FriendInfo> friendList) { }

		public void OnCreatedRoom() { }

		public void OnCreateRoomFailed(short returnCode, string message) { }

		public void OnJoinedRoom()
		{
			foreach (var cb in iConnectionEvents)
				cb.OnClientConnect(PhotonNetwork.LocalPlayer, PhotonNetwork.LocalPlayer.ActorNumber);

			foreach (var cb in iOnClientConnect)
				cb.OnClientConnect(PhotonNetwork.LocalPlayer, PhotonNetwork.LocalPlayer.ActorNumber);

			if (onClientConnectCallback != null)
				onClientConnectCallback.Invoke(PhotonNetwork.LocalPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
		}

		public void OnJoinRoomFailed(short returnCode, string message) { }

		public void OnJoinRandomFailed(short returnCode, string message) { }

		public void OnLeftRoom()
		{
			foreach (var cb in iConnectionEvents)
				cb.OnClientDisconnect(PhotonNetwork.LocalPlayer, PhotonNetwork.LocalPlayer.ActorNumber);

			foreach (var cb in iOnClientDisconnect)
				cb.OnClientDisconnect(PhotonNetwork.LocalPlayer, PhotonNetwork.LocalPlayer.ActorNumber);

			if (onClientDisconnectCallback != null)
				onClientDisconnectCallback.Invoke(PhotonNetwork.LocalPlayer, PhotonNetwork.LocalPlayer.ActorNumber);
		}


		/// <summary>
		/// Connection callbacks
		/// </summary>

		public void OnConnected() { }

		public void OnConnectedToMaster() { }

		public void OnDisconnected(DisconnectCause cause) { }

		public void OnRegionListReceived(RegionHandler regionHandler) { }

		public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }

		public void OnCustomAuthenticationFailed(string debugMessage) { }
#endif
		/// <summary>
		/// Force Photon to dispatch any pending incoming events. This may be needed on Updates where no FixedUpdate fired.
		/// </summary>
		private static void RegisterNetworkCallbacks()
		{
#if PUN_2_OR_NEWER

			if (PhotonNetwork.NetworkingClient == null)
				return;

			if (Application.isPlaying)
			{
				PhotonNetwork.NetworkingClient.AddCallbackTarget(single);
			}
#endif
		}

#endregion
	}
}
