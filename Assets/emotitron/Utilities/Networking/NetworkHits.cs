// Copyright 2019, Davin Carten, All rights reserved
// This code may only be used in game development, but may not be used in any tools or assets that are sold or made publicly available to other developers.


using emotitron.Compression;
using System.Collections.Generic;

namespace emotitron.Utilities.Networking
{
	/// <summary>
	/// A reusable collection of NetworkHit that can be serialized.
	/// </summary>
	public class NetworkHits
	{
		public readonly List<NetworkHit> hits = new List<NetworkHit>();
		public bool nearestOnly;
		public int nearestIndex = -1;
		public int bitsForHitGroupMask;

		public NetworkHits(bool nearestOnly, int bitsForHitGroupMask)
		{
			this.nearestOnly = nearestOnly;
			this.bitsForHitGroupMask = bitsForHitGroupMask;
		}

		public void Reset(bool nearestOnly, int bitsForHitGroupMask)
		{
			this.nearestOnly = nearestOnly;
			this.bitsForHitGroupMask = bitsForHitGroupMask;
			hits.Clear();
			nearestIndex = -1;
		}

		public void Clear()
		{
			hits.Clear();
			nearestIndex = -1;
		}

		public SerializationFlags Serialize(byte[] buffer, ref int bitposition, int bitsForColliderId)
		{
			SerializationFlags flags = SerializationFlags.None;

			if (nearestOnly)
			{
				if (nearestIndex != -1)
				{
					buffer.WriteBool(true, ref bitposition);
					hits[nearestIndex].Serialize(buffer, ref bitposition, bitsForHitGroupMask, bitsForColliderId);
					flags = SerializationFlags.HasChanged;
				}
				else
					buffer.WriteBool(false, ref bitposition);
			}
			else
			{
				int cnt = hits.Count;
				for (int i = 0; i < hits.Count; ++i)
				{
					buffer.WriteBool(true, ref bitposition);
					hits[i].Serialize(buffer, ref bitposition, bitsForHitGroupMask, bitsForColliderId);
					flags = SerializationFlags.HasChanged;

				}
				buffer.WriteBool(false, ref bitposition);
			}

			return flags;
		}

		public SerializationFlags Deserialize(byte[] buffer, ref int bitposition, int bitsForColliderId)
		{
			hits.Clear();
			SerializationFlags flags = SerializationFlags.None;

			//bool nearestOnly = definition.hitscanType.IsCast() && definition.nearestOnly;
			if (nearestOnly)
			{
				if (buffer.ReadBool(ref bitposition))
				{
					hits.Add(NetworkHit.Deserialize(buffer, ref bitposition, bitsForHitGroupMask, bitsForColliderId));
					flags = SerializationFlags.HasChanged;
					nearestIndex = 0;
				}
				else
					nearestIndex = -1;
			}
			else
			{
				while (buffer.ReadBool(ref bitposition))
				{
					hits.Add(NetworkHit.Deserialize(buffer, ref bitposition, bitsForHitGroupMask, bitsForColliderId));
					flags = SerializationFlags.HasChanged;
				}
			}

			return flags;
		}

		public override string ToString()
		{
			string str = GetType().Name;
			for (int i = 0; i < hits.Count; ++i)
				str += "\nObj:" + hits[i].netObjId + " Mask:" + hits[i].hitMask;
			
			return str;
		}
	}
}

