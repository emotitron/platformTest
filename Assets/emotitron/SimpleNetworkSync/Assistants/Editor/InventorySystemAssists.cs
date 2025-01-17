﻿
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace emotitron.Networking.Assists
{
	public static class InventorySystemAssists
	{

		[MenuItem(AssistHelpers.ADD_TO_OBJ_FOLDER + "System/Inventory")]
		public static void AddInventorySystem()
		{
			var go = NetObjectAssists.GetSelectedGameObject();
			if (!go)
				return;

			AddSystem(go, true);

			///// Add AutoMountHitscan if has rb and doesn't exist yet
			//if (go.transform.GetNestedComponentInParents<Rigidbody>() || go.transform.GetNestedComponentInParents<Rigidbody2D>())
			//	if (!go.transform.GetNestedComponentInChildren<AutoMountHitscan>())
			//		AddAutoMountHitscan();
		}

		private static List<BasicInventory> reusableBasicInvList = new List<BasicInventory>();

		public static SystemPresence GetSystemPresence(this GameObject go, params MonoBehaviour[] depends)
		{

			var ml = go.transform.GetNestedComponentInParents<MountsLookup>();


			var bi = go.transform.GetNestedComponentInChildren<BasicInventory>();
			if (bi == null)
				bi = go.transform.GetNestedComponentInParents<BasicInventory>();

			/// We have an Inventory, but not mount system...
			if (bi)
			{
				if (!ml)
					return SystemPresence.Incomplete;

				if (bi.gameObject == go)
					return SystemPresence.Complete;
				else
					return SystemPresence.Nested;

			}

			return SystemPresence.Absent;

		}

		public static void AddSystem(this GameObject go, bool add, params MonoBehaviour[] depends)
		{
			var netObj = go.transform.GetParentNetObject();
			
			if (add)
			{
				/// Make sure we have a MountsLookup on the root
				if (netObj)
					netObj.gameObject.EnsureComponentExists<MountsLookup>();
				else
					go.EnsureComponentExists<MountsLookup>();

				go.EnsureComponentExists<BasicInventory>();
			}
			else
			{
				go.transform.GetNestedComponentsInChildren(reusableBasicInvList);
				for (int i = reusableBasicInvList.Count - 1; i >= 0; --i)
					Object.DestroyImmediate(reusableBasicInvList[i]);
			}
		}



	}

}
