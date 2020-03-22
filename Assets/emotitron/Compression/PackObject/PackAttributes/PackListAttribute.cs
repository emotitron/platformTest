using emotitron.Compression.Internal;
using emotitron.Utilities.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Compression
{
	public class PackListAttribute : PackBaseAttribute
	, IPackList<Int32>
	{
		#region List<Int32>

		public SerializationFlags Pack(ref List<Int32> value, List<Int32> prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			bool isKeyframe = IsKeyframe(frameId);
			bool forced = (writeFlags & (SerializationFlags.Force | SerializationFlags.ForceReliable | SerializationFlags.NewConnection)) != 0;

			//bool notforced = !IsForced(frameId, writeFlags);

			SerializationFlags flags = SerializationFlags.None;
			int holdpos = bitposition;

			int cnt = value.Count;

			for (int i = 0; i < cnt; ++i)
			{
				var val = value[i];

				if (!isKeyframe)
				{
					if (!forced && val == prevValue[i])
					{
						buffer.WriteBool(false, ref bitposition);
						continue;
					}
					else
						buffer.WriteBool(true, ref bitposition);
				}

				buffer.WriteSignedPackedBytes(val, ref bitposition, bitCount);
				flags |= SerializationFlags.HasChanged;
			}

			if (flags == SerializationFlags.None)
				bitposition = holdpos;

			//Debug.LogError(cnt + " SER " + frameId + " " + value[1] + " flgs: " + flags);

			return flags;
		}

		public SerializationFlags Unpack(ref List<Int32> value, BitArray isCompleteMask, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{

			//bool notforced = !IsForced(frameId, writeFlags);
			bool isKeyframe = IsKeyframe(frameId);

			SerializationFlags flags = SerializationFlags.None;

			int cnt = value.Count;

			var isComplete = SerializationFlags.IsComplete;

			for (int i = 0; i < cnt; ++i)
			{
				if (!isKeyframe)
				{
					if (!buffer.ReadBool(ref bitposition))
					{
						isComplete = SerializationFlags.None;
						isCompleteMask[i] = false;
						//value[i] = prevValue[i];
						continue;
					}
				}

				isCompleteMask[i] = true;

				value[i] = buffer.ReadSignedPackedBytes(ref bitposition, bitCount);

				flags |= SerializationFlags.HasChanged;
			}

			Debug.LogError("Unpack List DES " + frameId + " <b>" + value[1] + "</b> " + " flgs: " + (flags | isComplete));
			if (isComplete == SerializationFlags.IsComplete)
				Debug.LogError("Complete Synclist");

			return flags | isComplete;
		}

		#endregion

		#region List<UInt32>



		#endregion

		/// <summary>
		/// Only copies elements when their bit in the associated mask == true.
		/// </summary>
		public static void Copy<T>(List<T> src, List<T> trg, BitArray mask) where T : struct
		{
			int cnt = src.Count;
			for (int i = 0; i < cnt; ++i)
			{
				if (mask.Get(i))
					trg[i] = src[i];
			}
		}

		public static void Capture<T>(List<T> src, List<T> trg) where T : struct
		{
			int cnt = src.Count;
			for (int i = 0; i < cnt; ++i)
			{
				trg[i] = src[i];
			}
		}

#if UNITY_EDITOR
		public override string GetFieldDeclareCodeGen(Type fieldType, string fulltypename, string fname)
		{
			/// Add a BitArray mask for Lists to the PackFrame
			return base.GetFieldDeclareCodeGen(fieldType, fulltypename, fname) + " public BitArray " + fname + "_mask;";

		}

		public override string GetCaptureCodeGen(Type fieldType, string fieldName, string s, string t)
		{
			return "{ int cnt = "+ s + "." + fieldName + ".Count; for (int i = 0; i < cnt; ++i) { " + t +"." + fieldName + "[i] = " + s + "." + fieldName + "[i]; } }";

		}
		public override string GetCopyCodeGen(Type fieldType, string fieldName, string s, string t)
		{
			//var genfield = fieldType.GetGenericArguments()[0];

			return "{ int cnt = " + s + "." + fieldName + ".Count; " +
				"for (int i = 0; i < cnt; ++i) { " +
					"if ("+ s + "." + fieldName + "_mask.Get(i)) " + t + "." + fieldName + "[i] = " + s + "." + fieldName + "[i]; } } ";
		}
#endif

	}
}

