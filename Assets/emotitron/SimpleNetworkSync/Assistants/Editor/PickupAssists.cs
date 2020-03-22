
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using emotitron.Utilities.HitGroups;

namespace emotitron.Networking.Assists
{

	public static class PickupAssists
	{

		public const string PICKUP_FOLDER = AssistHelpers.ADD_TO_SCENE_TXT + "Pickup/";

		[MenuItem(PICKUP_FOLDER + "Item: Static")]
		public static void CreateItemPickup3DStatic()
		{
			GameObject selection = new GameObject("Pickup Item");
			ConvertToItemPickup(selection, Space_XD.SPACE_3D, Dynamics.Static);
		}

		[MenuItem(PICKUP_FOLDER + "Item: Dynamic")]
		public static void CreateItemPickup3DDynamic()
		{
			GameObject selection = new GameObject("Pickup Item");
			ConvertToItemPickup(selection, Space_XD.SPACE_3D, Dynamics.Variable);
		}

		[MenuItem(PICKUP_FOLDER + "Vital: Static")]
		public static void CreateVitalPickup3DStatic()
		{
			GameObject selection = new GameObject("Pickup Vital");
			ConvertToVitalPickup(selection, Space_XD.SPACE_3D, Dynamics.Static);
		}

		[MenuItem(PICKUP_FOLDER + "Vital: Dynamic")]
		public static void CreateVitalPickup3DVDynamic()
		{
			GameObject selection = new GameObject("Pickup Vital");
			ConvertToVitalPickup(selection, Space_XD.SPACE_3D, Dynamics.Variable);
		}

		//public static GameObject CreateBasePickup(string name)
		//{
		//	GameObject selection = new GameObject(name);
		//	ConvertToPickup(selection);

		//	selection.CreateChildStatePlaceholders();
		//	return selection;
		//}

		//[MenuItem("GameObject/Simple/Convert To/Pickup : Item", false, -100)]
		public static void ConvertToItemPickup(GameObject selection, Space_XD space, Dynamics dynamics)
		{
			selection = ConvertToPickup(selection, space, dynamics);

			selection.EnsureComponentExists<InventoryContactReactors>();

			var sst = selection.EnsureComponentExists<SyncSpawnTimer>();
			sst.despawnEnable = false;

			Selection.activeGameObject = selection;
		}

		//[MenuItem("GameObject/Simple/Convert To/Pickup : Vital", false, -100)]
		public static void ConvertToVitalPickup(GameObject selection, Space_XD space, Dynamics dynamics)
		{
			selection = ConvertToPickup(selection, space, dynamics);

			selection.EnsureComponentExists<VitalsContactReactors>();

			var sst = selection.EnsureComponentExists<SyncSpawnTimer>();
			sst.despawnEnable = true;
			sst.despawnOn = ObjState.Attached;

			Selection.activeGameObject = selection;
		}


		/// <summary>
		/// Add the core components needed for all Pickup types, and add toggles to existing children.
		/// </summary>
		public static GameObject ConvertToPickup(GameObject selection, Space_XD space, Dynamics dynamics)
		{
			selection = NetObjectAssists.ConvertToBasicNetObject(selection);

			if (dynamics != Dynamics.Static)
			{
				selection.AddRigidbody(space);
				var st = selection.EnsureComponentExists<SyncTransform>();
				st.transformCrusher.SclCrusher.Enabled = false;

			}

			selection.EnsureComponentExists<ContactTrigger>();
			selection.EnsureComponentExists<SyncPickup>();
			var ss = selection.EnsureComponentExists<SyncState>();
			ss.mountableTo.mask = (1 << MountSettings.single.mountNames.Count) - 1;

			if (dynamics != Dynamics.Static)
				selection.EnsureComponentExists<OnStateChangeKinematic>();

			/// Add OnStateChangeToggle to existing children before creating placeholder children
			selection.EnsureComponentOnNestedChildren<OnStateChangeToggle>(false);

			/// Add HitGroups, and set to default
			var hga = selection.EnsureComponentExists<HitGroupAssign>();
			hga.hitGroupMask.Mask = 0;

			selection.CreateChildStatePlaceholders(space, dynamics);

			return selection;
		}
	}
}
#endif