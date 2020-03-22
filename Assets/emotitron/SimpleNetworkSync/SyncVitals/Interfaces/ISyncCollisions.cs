//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//using emotitron.Utilities.Networking;
//using emotitron.Compression;
//using emotitron.Utilities;

//namespace emotitron.Networking
//{
//	public interface ISyncCollisions
//	{
//		TriggerOn TriggerOn { get; }

//		List<uint> EntrNetIds { get; }
//		List<uint> StayNetIds { get; }
//		List<uint> ExitNetIds { get; }
//	}

//	public static class ExtendISyncCollision
//	{
//		/// <summary>
//		/// Constant that defines the serialization flag to use when a collision is being synced.
//		/// </summary>
//		public const SerializationFlags TRIGGER_FLAGS = SerializationFlags.HasContent | SerializationFlags.ForceReliable;

//		public static SerializationFlags Serialize(this ISyncCollisions iSyncTrigger, byte[] buffer, ref int bitposition)
//		{
//			SerializationFlags flags = SerializationFlags.None;
//			TriggerOn triggerOn = iSyncTrigger.TriggerOn;

//			if ((triggerOn & TriggerOn.Enter) != 0)
//				flags |= SerializeTriggerOn(iSyncTrigger.EntrNetIds, buffer, ref bitposition);

//			if ((triggerOn & TriggerOn.Stay) != 0)
//				flags |= SerializeTriggerOn(iSyncTrigger.StayNetIds, buffer, ref bitposition);

//			if ((triggerOn & TriggerOn.Exit) != 0)
//				flags |= SerializeTriggerOn(iSyncTrigger.ExitNetIds, buffer, ref bitposition);


//			return flags;
//		}

//		private static SerializationFlags SerializeTriggerOn(List<uint> netIds, byte[] buffer, ref int bitposition)
//		{
//			int cnt = netIds.Count;
//			if (cnt == 0)
//			{
//				buffer.WriteBool(false, ref bitposition);
//				return SerializationFlags.None;
//			}

//			for (int i = 0; i < cnt; ++i)
//			{
//				buffer.WriteBool(true, ref bitposition);
//				buffer.WritePackedBytes(netIds[i], ref bitposition, 32);
//			}

//			buffer.WriteBool(false, ref bitposition);

//			return TRIGGER_FLAGS;
//		}

//		public static SerializationFlags Deserialize(this ISyncCollisions iSyncTrigger, byte[] buffer, ref int bitposition)
//		{
//			SerializationFlags flags = SerializationFlags.None;
//			TriggerOn triggerOn = iSyncTrigger.TriggerOn;

//			if ((triggerOn & TriggerOn.Enter) != 0)
//				flags |= DeserializeTriggerOn(iSyncTrigger.EntrNetIds, buffer, ref bitposition);

//			if ((triggerOn & TriggerOn.Stay) != 0)
//				flags |= SerializeTriggerOn(iSyncTrigger.StayNetIds, buffer, ref bitposition);

//			if ((triggerOn & TriggerOn.Enter) != 0)
//				flags |= SerializeTriggerOn(iSyncTrigger.ExitNetIds, buffer, ref bitposition);

//			return flags;
//		}

//		private static SerializationFlags DeserializeTriggerOn(List<uint> netIds, byte[] buffer, ref int bitposition)
//		{
//			netIds.Clear();

//			while (buffer.ReadBool(ref bitposition))
//			{
//				netIds.Add(buffer.ReadPackedBytes(ref bitposition, 32));
//			}

//			return (netIds.Count == 0) ? SerializationFlags.None : TRIGGER_FLAGS;
//		}
//	}
//}

