using System;
using System.Collections;
using System.Collections.Generic;
using emotitron.Utilities.Networking;
using UnityEngine;

namespace emotitron.Compression.Internal
{
	
	public class PackRangedAttribute : PackBaseAttribute
		, IPackSingle
	{
		LiteFloatCrusher crusher = new LiteFloatCrusher();

		public PackRangedAttribute(LiteFloatCompressType compression, Single min, Single max, bool accurateCenter)
		{
			LiteFloatCrusher.Recalculate(compression, min, max, accurateCenter, crusher);
		}
		public SerializationFlags Pack(ref Single value, Single preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			uint cval = (uint)crusher.Encode(value);

			if (!IsForced(frameId, writeFlags) && cval == (uint)crusher.Encode(preValue))
				return SerializationFlags.None;

			crusher.WriteCValue(cval, buffer, ref bitposition);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref Single value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = crusher.ReadValue(buffer, ref bitposition);
			return SerializationFlags.IsComplete;
		}

	}

}
