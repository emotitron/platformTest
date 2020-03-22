using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking.Assists
{
	public static class MountAssists
	{


		public static SystemPresence GetSystemPresence(this GameObject go, params MonoBehaviour[] depends)
		{
			var netobj = go.transform.GetParentNetObject();
			var m = go.GetComponent<Mount>();
			var ml = netobj ? netobj.transform.GetNestedComponentInChildren<MountsLookup>() : null;

			/// Destroy an ML that is not paired with a netobj
			if (ml)
				if (!netobj || ml.gameObject != netobj.gameObject)
					Object.DestroyImmediate(ml);

			//if (m)
			//	var ml = MountsLookup.EstablishMounts(go);

			//var m = go.transform.GetNestedComponentInParents<Mount>();
			//if (!m)
			//	m = go.transform.GetNestedComponentInChildren<Mount>();

			///// Missing NetObj is an immediate fail.
			//if (!netobj)
			//{
			//	/// Without an NetObj, we shouldn't have an ML
			//	if (ml)
			//		Object.DestroyImmediate(ml);

			//	if (m)
			//		return SystemPresence.Incomplete;
			//	else
			//		return SystemPresence.Missing;
			//}

			///// We have a NetObj and we have a mount - Make sure our Lookup on the NetObj.
			//else
			//{
			//	/// Destroy any Lookup on on the NetObj root.
			//	if (ml && ml.gameObject != netobj)
			//	{
			//		Object.DestroyImmediate(ml);
			//	}
			//	/// Make sure we have a MountsLookup on the root if we have a mount.
			//	if (m)
			//		ml = netobj.gameObject.EnsureComponentExists<MountsLookup>();
			//}

			///// If we have a mount and no Lookup, add a Lookup
			//if (!ml && m)
			//	ml = netobj ? netobj.gameObject.AddComponent<MountsLookup>() : go.AddComponent<MountsLookup>();

			if (m)
			{
				if (!ml || !netobj)
					return SystemPresence.Incomplete;
				else
					return SystemPresence.Complete;
			}
			else
			{
				if (ml)
					return
						SystemPresence.Nested;
			}
			
			return SystemPresence.Absent;
		}

		public static void AddSystem(this GameObject go, bool add, params MonoBehaviour[] depends)
		{
			var netobj = go.transform.GetParentNetObject();

			if (add)
			{
				/// Make sure ML is with NetObject root
				if (netobj)
					netobj.gameObject.EnsureComponentExists<MountsLookup>();
				else
					go.EnsureComponentExists<MountsLookup>();

				go.EnsureComponentExists<Mount>();
			}
			else
			{
				var mount = go.GetComponent<Mount>();
				if (mount)
					Object.DestroyImmediate(mount);

				if (netobj.gameObject == go)
				{
					var ml = go.GetComponent<MountsLookup>();
					if (ml)
						Object.DestroyImmediate(ml);
				}
			}
			

		}
	}
}

