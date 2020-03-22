using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Utilities.GhostWorlds
{
	[AddComponentMenu("")]
	public class GhostCollider : MonoBehaviour
	{
		// We only want to serialized for the editor to make it easy to see what settings its coming up with.
		// These values are runtime only.
#if !UNITY_EDITOR
		[System.NonSerialized]
#endif
		public int sourceLayer;

#if !UNITY_EDITOR
		[System.NonSerialized]
#endif
		public LayerMask sourceLayerMask;

		public void SetLayer(int layer)
		{
			sourceLayer = layer;
			sourceLayerMask = 1 << layer;
		}
	}
}
