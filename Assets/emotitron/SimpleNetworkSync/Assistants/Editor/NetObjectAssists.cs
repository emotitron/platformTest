
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using emotitron.Utilities;
using emotitron.Utilities.Networking;
using emotitron.Utilities.Example;
using System.Collections.Generic;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

namespace emotitron.Networking.Assists
{

	public static class NetObjectAssists
	{

		[MenuItem(AssistHelpers.CONVERT_TO_FOLDER + "Player", false, -999)]
		public static void ConvertToPlayer()
		{
			var selection = ConvertToBasicNetObject(null);

			if (selection == null)
				return;


			selection.EnsureComponentExists<SyncTransform>();

			//selection.EnsureComponentExists<OnStateChangeKinematic>();

			if (selection.GetComponent<Animator>())
				selection.EnsureComponentExists<SyncAnimator>();

			VitalsAssists.AddVitalsSystem();


			/// Quality of life components
			selection.EnsureComponentExists<AutoOwnerComponentEnable>();

			/// Inventory system
			InventorySystemAssists.AddInventorySystem();

			StateAssists.AddSystem(selection, true);
			///// State System
			//var syncstate = selection.EnsureComponentExists<SyncState>();
			//syncstate.autoOwnerChange = false;

			//selection.EnsureComponentOnNestedChildren<OnStateChangeToggle>(false);


			//selection.EnsureComponentExists<SyncSpawnTimer>();

			selection.EnsureComponentExists<AutoDestroyUnspawned>();
		}

		[MenuItem(AssistHelpers.CONVERT_TO_FOLDER + "Platform")]
		public static void ConvertToPlatform()
		{
			var go = GetSelectedGameObject();

			if (!go)
				return;

			Debug.Log(go.transform.lossyScale);
			if (go.transform.lossyScale != Vector3.one)
			{
				if (go.transform.parent && go.transform.parent.lossyScale != Vector3.one)
				{
					Debug.LogWarning("Aborted Convert To Platform. Parent object has a scale other than " + Vector3.one + " which would distort any object that mounts to it.");
					return;
				}

				Debug.LogWarning("Selected object as a scale other than " + Vector3.one + ". Creating a parent object for the platform and making the selected object a child.");
				var par = new GameObject("Platform");
				par.transform.position = go.transform.position;
				par.transform.rotation = go.transform.rotation;
				par.transform.parent = go.transform.parent;
				go.transform.parent = par.transform;

				go = par;
			}

			go.EnsureComponentExists<NetObject>(true);
			go.EnsureComponentExists<Mount>();
			var mover = go.EnsureComponentExists<SyncNodeMover>();
			mover.posDef.includeAxes = AxisMask.Y;
			mover.StartNode.Pos = new Vector3(0, -2f, 0);
			mover.EndNode.Pos = new Vector3(0, 2f, 0);
			mover.oscillatePeriod = 3;

			mover.rotDef.includeAxes = AxisMask.None;
			mover.sclDef.includeAxes = AxisMask.None;

			Selection.activeGameObject = go;
		}

		

		#region Vitals System

		#endregion


		[MenuItem(AssistHelpers.ADD_TO_OBJ_FOLDER + "AutoMount Hitscan")]
		public static AutoMountHitscan AddAutoMountHitscan()
		{
			GameObject par = AssistHelpers.GetSelectedGameObject();

			if (!par)
				return null;

			GameObject go = new GameObject("AutoMount");
			go.transform.eulerAngles = new Vector3(90f, 0, 0);
			go.transform.parent = par.transform;
			go.transform.localPosition = new Vector3(0, 0, 0);

			return go.EnsureComponentExists<AutoMountHitscan>();
		}


		public static GameObject ConvertToBasicNetObject(GameObject selection = null)
		{
			if (selection == null)
				selection = Selection.activeGameObject;

			if (selection == null)
			{
				Debug.LogWarning("No selected GameObject. Creating a dummy Player/NPC.");
				selection = new GameObject("Empty Player");
				selection.CreateChildStatePlaceholders(Space_XD.SPACE_3D, Dynamics.Variable, 2);
				//return null;
			}

#if PUN_2_OR_NEWER
			selection.EnsureComponentExists<PhotonView>();
#endif
			selection.EnsureComponentExists<NetObject>();

			return selection;
		}

		public static SystemPresence GetSystemPresence(this GameObject go, params MonoBehaviour[] depends)
		{
			var netObj = go.transform.GetNestedComponentInParents<NetObject>();

#if PUN_2_OR_NEWER

			PhotonView pv = go.transform.GetNestedComponentInParents<PhotonView>();

			if (pv && netObj)
			{
				if (pv.gameObject == go)
					return SystemPresence.Complete;
				else
					return SystemPresence.Nested;
			}

			else if (pv || netObj)
				return SystemPresence.Partial;
			else
				return SystemPresence.Absent;
#else
			return 0;
#endif


		}
		public static void AddSystem(this GameObject go, bool add, params MonoBehaviour[] depends)
		{
			
			if (add)
			{
#if PUN_2_OR_NEWER
				go.EnsureComponentExists<PhotonView>();
#endif
				go.EnsureComponentExists<NetObject>();
			}

			else
			{
				var no = go.transform.GetNestedComponentInParents<NetObject>();
				if (no)
				{
					var ml = go.GetComponent<MountsLookup>();
					if (ml)
						Object.DestroyImmediate(ml);

					Object.DestroyImmediate(no);
				}

#if PUN_2_OR_NEWER
				var pv = go.transform.GetNestedComponentInParents<PhotonView>();
				if (pv)
					Object.DestroyImmediate(pv);
#endif

			}
		}

		public static GameObject GetSelectedGameObject()
		{
			var selection = Selection.activeGameObject;

			if (selection == null)
			{
				Debug.LogWarning("No selected GameObject.");
				return null;
			}

			return selection;
		}

		public static GameObject GetSelectedReparentableGameObject()
		{
			var selection = GetSelectedGameObject();
			if (!selection)
				return null;

			return selection.CheckReparentable() ? selection : null;
		}

		public static bool CheckReparentable(this GameObject go)
		{
#if UNITY_2018_3_OR_NEWER
			if (PrefabUtility.IsPartOfPrefabAsset(go))
#else
			if (PrefabUtility.GetPrefabType(go) == PrefabType.Prefab)
#endif
			{
				Debug.LogWarning("Cannot add/reparent GameObjects on a Prefab Source Object. Make an instance of this prefab in the current scene and run this assistant on that, then save the changes to the prefab, or drag the scene instance into a Resource folder.");
				return false;
			}

			return true;
		}
	}
}

#endif