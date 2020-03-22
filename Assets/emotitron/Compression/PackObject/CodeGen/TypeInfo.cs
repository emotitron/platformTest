#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace emotitron.Compression.Internal
{
	[System.Serializable]
	public class TypeInfo
	{
		public long hashcode;
		public string filepath;
		public long codegenFileWriteTime;
		public int localFieldCount;
		public int totalFieldCount;

		public TypeInfo(System.Type type)
		{
			hashcode = type.TypeToHash64();
		}
	}
}

#endif