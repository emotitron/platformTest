#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace emotitron.Compression.Internal
{
	[System.Serializable]
	public class TypeInfoDict
	{
		[SerializeField] private List<string> keys = new List<string>();
		[SerializeField] private List<TypeInfo> vals = new List<TypeInfo>();

		public bool Add(System.Type type, TypeInfo val)
		{
			return Add(type.FullName, val);
		}

		public bool Add(string key, TypeInfo val)
		{
			int index = keys.IndexOf(key);
			if (index != -1)
				return false;

			keys.Add(key);
			vals.Add(val);

			return true;
		}

		public bool Remove(string key)
		{
			int index = keys.IndexOf(key);
			if (index == -1)
				return false;

			keys.RemoveAt(index);
			vals.RemoveAt(index);

			return true;
		}

		public void RemoveAt(int index)
		{
			keys.RemoveAt(index);
			vals.RemoveAt(index);
		}

		public TypeInfo GetTypeInfo(System.Type type)
		{
			int index = keys.IndexOf(type.FullName);

			if (index == -1)
				return null;
			else
				return vals[index];
		}

		public int TryGetValue(string key, out TypeInfo val)
		{
			int index = keys.IndexOf(key);
			if (index == -1)
			{
				val = null;
				return index;
			}

			val = vals[index];
			return index;
		}

		public void Clear()
		{
			keys.Clear();
			vals.Clear();
		}
	}
}

#endif