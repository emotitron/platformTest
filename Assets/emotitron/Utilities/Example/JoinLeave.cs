﻿//using System.Collections;
//using System.Collections.Generic;

//using UnityEngine;
//#if PUN_2_OR_NEWER
//// using Photon.Pun;
//#elif MIRROR
//using Mirror;
//#elif MLAPI
//using MLAPI;
//#else
//using UnityEngine.Networking;
//#endif

//#pragma warning disable CS0618 // UNET obsolete

//public class JoinLeave : MonoBehaviour
//{

//	public KeyCode join = KeyCode.P;
//	public KeyCode leave = KeyCode.O;

//	void Update()
//	{

//#if PUN_2_OR_NEWER

//#elif MIRROR
//		if (Input.GetKeyDown(join))
//			ClientScene.AddPlayer();

//		if (Input.GetKeyDown(leave))
//			ClientScene.RemovePlayer();
//#elif MLAPI

//		Debug.LogWarning("Not implemented for MLAPI yet.");

//#else

//		if (Input.GetKeyDown(join))
//			ClientScene.AddPlayer(0);

//		if (Input.GetKeyDown(leave))
//			ClientScene.RemovePlayer(0);
//#endif

//	}
//}

//#pragma warning disable CS0618 // UNET obsolete
