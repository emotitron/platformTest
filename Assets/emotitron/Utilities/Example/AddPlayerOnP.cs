﻿//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//#if MIRROR
//using Mirror;
//#elif MLAPI
//using MLAPI;
//#else
//using UnityEngine.Networking;
//#endif

//namespace emotitron.Utilities.Example
//{
//	public class AddPlayerOnP : MonoBehaviour
//	{

//		void Update()
//		{

//#if PUN_2_OR_NEWER

//#elif MLAPI

//#elif MIRROR

//		if (Input.GetKeyDown(KeyCode.P))
//			ClientScene.AddPlayer();

//		if (Input.GetKeyDown(KeyCode.O))
//			ClientScene.RemovePlayer();
//#else
//#pragma warning disable CS0618 // Type or member is obsolete
//			if (Input.GetKeyDown(KeyCode.P))
//				ClientScene.AddPlayer(0);

//			if (Input.GetKeyDown(KeyCode.O))
//				ClientScene.RemovePlayer(0);
//#pragma warning restore CS0618 // Type or member is obsolete
//#endif
//		}

//	}
//}
