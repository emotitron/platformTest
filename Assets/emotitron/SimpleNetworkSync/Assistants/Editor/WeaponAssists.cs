#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace emotitron.Networking.Assists
{
	public static class WeaponAssists
	{
		public const string HITSCAN_FOLDER = AssistHelpers.ADD_TO_OBJ_FOLDER + "Hitscan/";
		public const string PROJECTILE_FOLDER = AssistHelpers.ADD_TO_OBJ_FOLDER + "Projectile Launcher/";
		#region Assist Menu

		[MenuItem(HITSCAN_FOLDER + "ContactProxy", false, 0)]
		public static void AddHitscanProxy()
		{
			var comp =  AddContactProxy<SyncHitscan>("Net Hitscan Contact");
			comp.hitscanDefinition = new Utilities.GenericHitscan.HitscanDefinition()
			{
				hitscanType = Utilities.GenericHitscan.HitscanType.OverlapSphere,
				nearestOnly = false,
				radius = 2
			};
			comp.triggerKey = KeyCode.G;
			comp.GetComponent<OnNetHitContact>().validHitGroups = new Utilities.HitGroups.HitGroupMaskSelector(0);
		}

		[MenuItem(HITSCAN_FOLDER + "Weapon", false, 10)]
		public static void AddHitscan()
		{
			AddWeapon<SyncHitscan>("Net Hitscan Weapon");
		}

		[MenuItem(PROJECTILE_FOLDER + "Weapon", false, 10)]
		public static void AddLauncher()
		{
			AddWeapon<SyncLauncher>("Net Projectile Launcher", PrimitiveType.Cylinder);
		}

		#endregion

		public static T AddContactProxy<T>(string name, PrimitiveType primitive = PrimitiveType.Cube) where T : SyncNetHitBase
		{
			var selection = Selection.activeGameObject;

			if (selection == null)
			{
				Debug.LogWarning("No selected GameObject. Cannot add " + name + ".");
				return null;
			}

			var go = selection.transform.CreateEmptyChildGameObject(name);
			var prim = go.CreateNewPrimitiveAsChild(primitive, AssistHelpers.ColliderType.None, "Model Placeholder", .5f);
			prim.transform.localEulerAngles = new Vector3(90, 0, 0);
			if (primitive == PrimitiveType.Cylinder)
				prim.transform.localScale = new Vector3(.2f, .2f, .2f);

			T comp = go.EnsureComponentExists<T>();

			go.EnsureComponentExists<OnNetHitContact>();

			/// Make sure we have a visibility toggle
			if (!go.GetComponentInParent<OnStateChangeToggle>())
				go.AddComponent<OnStateChangeToggle>();

			Selection.activeObject = go;
			return comp;
		}

		public static GameObject AddWeapon<T>(string name, PrimitiveType primitive = PrimitiveType.Cube) where T : SyncNetHitBase
		{
			var comp = AddContactProxy<T>(name, primitive);

			if (comp == null)
				return null;

			var go = comp.gameObject;
			go.EnsureComponentExists<OnNetHitApplyDamage>();

			Selection.activeObject = go;
			return go;
		}
	}
}
#endif

