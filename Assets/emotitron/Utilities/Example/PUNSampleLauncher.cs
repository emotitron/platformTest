
//using UnityEngine;

//#if PUN_2_OR_NEWER
//using Photon.Pun;
//using Photon.Realtime;
//#endif

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Utilities.Example
//{

//	/// <summary>
//	/// This is a very basic PUN implementation I have supplied to make it easy to quicky get started.
//	/// It doesn't make use of a lobby so it only uses one scene, which eliminates the need to add any
//	/// scenes to the build. Your actual game using PUN likely will want to have multiple scenes and you
//	/// will want to replace all of this code with your own.
//	/// </summary>
//	public class PUNSampleLauncher :

//#if PUN_2_OR_NEWER
//		MonoBehaviourPunCallbacks
//#else
//		MonoBehaviour
//#endif
//	{


//#if UNITY_EDITOR

//		[MenuItem("GameObject/Simple/Add/PUN2 Basic Room Launcher", false, -100)]
//		public static PUNSampleLauncher AddLauncherToScene()
//		{
//			var go = new GameObject("PUN2 Launcher");

//			var comp = go.GetComponent<PUNSampleLauncher>();

//			if (comp == null)
//				comp = go.AddComponent<PUNSampleLauncher>();

//			var selection = Selection.activeGameObject;

//#if PUN_2_OR_NEWER

//			if (selection && selection.GetComponent<PhotonView>())
//			{
//				Debug.Log("Automatically Adding '" + selection.name + "' as PlayerPrefab for " + comp.GetType().Name + " because it was selected.");
//				comp.PlayerPrefab = selection;
//			}
//#endif
//			return comp;
//		}
//#endif

//		[Tooltip("The prefab to use for representing the player")]
//		[SerializeField] protected GameObject playerPrefab;
//		public GameObject PlayerPrefab
//		{
//			get { return playerPrefab; }
//			set
//			{
//#if UNITY_EDITOR
//				playerPrefab = ValidatePrefab(value);
//#else
//				playerPrefab = value;
//#endif
//			}
//		}

//		public bool autoSpawnPlayer = true;
//		public KeyCode spawnPlayerKey = KeyCode.P;
//		public KeyCode unspawnPlayerKey = KeyCode.O;

//		public static GameObject localPlayer;

//#if UNITY_EDITOR
//		protected void OnValidate()
//		{
//			playerPrefab = ValidatePrefab(playerPrefab);
//		}

//		private static GameObject ValidatePrefab(GameObject unvalidated)
//		{
//#if UNITY_2018_3_OR_NEWER

//			GameObject validated = null;

//			if (unvalidated != null)
//			{

//				if (PrefabUtility.IsPartOfPrefabAsset(unvalidated))
//					return unvalidated;

//				var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(unvalidated);
//				var isValidPrefab = prefabStatus == PrefabInstanceStatus.Connected || prefabStatus == PrefabInstanceStatus.Disconnected;

//				if (isValidPrefab)
//					validated = PrefabUtility.GetCorrespondingObjectFromSource(unvalidated) as GameObject;
//				else
//					return null;

//				if (!PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(validated).Contains("/Resources"))
//					Debug.LogWarning("Player Prefab needs to be a Prefab in a Resource folder.");
//			}
//#else
//			GameObject validated = unvalidated;

//			if (unvalidated != null && PrefabUtility.GetPrefabType(unvalidated) != PrefabType.Prefab)
//				validated = PrefabUtility.GetPrefabParent(unvalidated) as GameObject;
//#endif
//			return validated;
//		}

//#endif

//#if PUN_2_OR_NEWER

//		private void Awake()
//		{
//			/// this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
//			PhotonNetwork.AutomaticallySyncScene = true;
//		}


//		void Start()
//		{
//			Connect();
//		}

//		public override void OnConnectedToMaster()
//		{
//			JoinOrCreateRoom();
//		}

//		public override void OnJoinedLobby()
//		{
//			base.OnJoinedLobby();
//		}

//		public override void OnJoinedRoom()
//		{
//			Debug.Log("Joined room");
//			if (autoSpawnPlayer)
//				SpawnLocalPlayer();
//			else
//				Debug.Log("<b>Auto-Create for player is disabled on component '" + this.GetType().Name + "'</b>. Press '" + spawnPlayerKey + "' to spawn a player. '" + unspawnPlayerKey + "' to unspawn.");
//		}

//		public void Update()
//		{
//			if (Input.GetKeyDown(spawnPlayerKey))
//				if (PhotonNetwork.InRoom)
//					SpawnLocalPlayer();

//			if (Input.GetKeyDown(unspawnPlayerKey))
//				UnspawnLocalPlayer();
//		}


//		/// <summary>
//		/// Start the connection process. 
//		/// - If already connected, we attempt joining a random room
//		/// - if not yet connected, Connect this application instance to Photon Cloud Network
//		/// </summary>
//		public void Connect()
//		{
//			// we check if we are connected or not, we join if we are , else we initiate the connection to the server.
//			if (PhotonNetwork.IsConnected)
//			{
//				JoinOrCreateRoom();
//			}
//			else
//			{
//				PhotonNetwork.ConnectUsingSettings();
//			}
//		}

//		private void JoinOrCreateRoom()
//		{
//			PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions() { MaxPlayers = 8 }, null);
//		}

//		private void SpawnLocalPlayer()
//		{
//			// we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
//			if (playerPrefab/* && !localPlayer*/)
//			{
//				//Transform tr = spawnPoints.Count > 0 ? spawnPoints[Random.Range(0, spawnPoints.Count)] : null;
//				Transform tr = emotitron.Utilities.Networking.GenericSpawnPoint.GetSpawnPointFromValue(PhotonNetwork.LocalPlayer.ActorNumber);
//				Vector3 pos = (tr) ? tr.position : new Vector3(PhotonNetwork.LocalPlayer.ActorNumber, 0f, 0f);
//				Quaternion rot = (tr) ? tr.rotation : Quaternion.identity;

//				localPlayer = PhotonNetwork.Instantiate(playerPrefab.name, pos, rot, 0);
//				localPlayer.transform.parent = null;
//			}
//			else
//				Debug.LogWarning("No PlayerPrefab defined in " + this.GetType().Name);
//		}

//		private void UnspawnLocalPlayer()
//		{
//			if (localPlayer.GetComponent<PhotonView>().IsMine && PhotonNetwork.IsConnected)
//			{
//				var no = localPlayer.GetComponent<emotitron.Networking.NetObject>();
//				if (no)
//					no.PrepareForDestroy();

//				PhotonNetwork.Destroy(localPlayer);

//			}
//		}
//#endif

//	}

//#if UNITY_EDITOR

//	[CustomEditor(typeof(PUNSampleLauncher))]
//	[CanEditMultipleObjects]
//	public class PUNSampleLauncherEditor : SampleCodeHeaderEditor
//	{
//		protected override string HelpURL
//		{
//			get
//			{
//				return "https://docs.google.com/document/d/1moPBIt8cNe-h1uG01pvaOZQvrIjZfSBfDmVa793X6AQ/edit#bookmark=id.hp0o6adnoi3e";
//			}
//		}
//		protected override string Instructions
//		{
//			get
//			{
//				return "Convenience component for launching a very basic Photon PUN2 room and a player object.";
//			}
//		}
//	}

//#endif

//}

