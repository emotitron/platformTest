
using UnityEngine;

#if PUN_2_OR_NEWER
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

namespace emotitron.Utilities.Networking
{
	/// <summary>
	/// Tools that work for all currently supported netlibs, using unified methods.
	/// </summary>
	public static class UnifiedNetTools
	{

		/// <summary>
		/// Unified Network Find GameObject using netid, and return the root component of type T. For Photon PhotonView is the return of the Find already.
		/// </summary>
		public static T FindComponentByNetId<T>(this int netid) where T : class
		{
#if PUN_2_OR_NEWER
			PhotonView found = PhotonView.Find((int)netid);
			if (found == null)
			{
				Debug.LogWarning("No Object found for viewID " + netid + ". \nIt is not unsual with PUN2 for messages sometimes to arrive before their target gameobject is ready at startup.");
				return null;
			}
			if (typeof(T) == typeof(PhotonView))
				return (found as T);
			else
				return found.GetComponent<T>();
#else
			return null;
#endif
		}

		/// <summary>
		/// Unitifed Network Find GameObject using netid.
		/// </summary>
		public static GameObject FindGameObjectByNetId(this ulong netid)
		{
#if PUN_2_OR_NEWER
			PhotonView found = PhotonView.Find((int)netid);
			if (found == null)
			{
				Debug.LogWarning("No Object found for netid " + netid);
				return null;
			}
			return found.gameObject;
#else
			return null;
#endif
		}

	}
}


