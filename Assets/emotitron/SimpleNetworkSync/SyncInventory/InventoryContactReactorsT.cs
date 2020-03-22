using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	public abstract class InventoryContactReactors<T> : ContactReactorsBase<IInventory<T>>
		, IInventoryable<T>
	{
		public abstract T Size { get; }

		// cache
		protected int volume;
		public int Volume { get { return volume; } }
	}

//#if UNITY_EDITOR

//	[CustomEditor(typeof(InventoryContactReactors<>), true)]
//	[CanEditMultipleObjects]
//	public class InventoryContactReactorsBaseEditor : ReactorHeaderEditor
//	{
		
//	}
//#endif
}
