//using emotitron.Utilities.Networking;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace emotitron.Networking
//{
//	public enum SyncObjState { Unspawned = 0, Visible = 1, Attached = 2 }
//	public interface ISyncState
//	{
//		SyncObjState State { get; }
//		List<uint> AttachedTo { get; }
//	}

//	public static class ExtendISyncState
//	{
//		public static SerializationFlags Serialize(this ISyncState iSyncTrigger, byte[] buffer, ref int bitposition)
//		{
//			return 0;

//		}

//		public static SerializationFlags Deserialize(this ISyncState iSyncTrigger, byte[] buffer, ref int bitposition)
//		{
//			return 0;
//		}
//	}
//}

