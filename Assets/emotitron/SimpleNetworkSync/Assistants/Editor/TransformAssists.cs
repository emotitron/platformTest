using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking.Assists
{
	public static class TransformAssists
	{

		public static SystemPresence GetSystemPresence(this GameObject go, params MonoBehaviour[] depends)
		{
			var netobj = go.transform.GetParentNetObject();

			var comp = go.GetComponent<SyncTransform>();
			
			if (comp)
			{
				if (!netobj)
					return SystemPresence.Incomplete;

				if (comp.gameObject.gameObject == go)
					return SystemPresence.Complete;
			}

			return SystemPresence.Absent;
		}

		public static void AddSystem(this GameObject go, bool add, params MonoBehaviour[] depends)
		{
			if (add)
			{
				go.AddComponent<SyncTransform>();
			}
			else
			{
				var st = go.GetComponent<SyncTransform>();
				if (st)
					Object.DestroyImmediate(st);
			}
		}
	}

}
