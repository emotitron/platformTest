#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace emotitron.Networking.Assists
{

	public static class TriggerAssists
	{

		[MenuItem(AssistHelpers.CONVERT_TO_FOLDER + "Zone: Vital Recharge", false, -999)]
		public static void ConvertToVitalRechargeZone()
		{
			var selection = NetObjectAssists.GetSelectedGameObject();
			if (selection != null)
				if (!selection.CheckReparentable())
					return;

			selection.EnsureComponentExists<ContactTrigger>();
			selection.EnsureComponentExists<OnTriggerValueChange>();

			selection.SetAllCollidersAsTriggger(true);
		}

		[MenuItem(AssistHelpers.CONVERT_TO_FOLDER + "Zone: Damage", false, -999)]
		public static void ConvertToDamageZone()
		{
			var selection = NetObjectAssists.GetSelectedGameObject();
			if (selection != null)
				if (!selection.CheckReparentable())
					return;

			selection.EnsureComponentExists<ContactTrigger>();
			selection.EnsureComponentExists<OnTriggerApplyDamage>();

			selection.SetAllCollidersAsTriggger(true);
		}

		public static void SetAllCollidersAsTriggger(this GameObject selection, bool isTrigger = true)
		{
			if (ReferenceEquals(selection, null))
				return;

			var colliders = selection.transform.GetNestedComponentsInChildren<Collider>(null);
			for (int i = 0; i < colliders.Count; ++i)
				colliders[i].isTrigger = true;

			var colliders2D = selection.transform.GetNestedComponentsInChildren<Collider2D>(null);
			for (int i = 0; i < colliders2D.Count; ++i)
				colliders2D[i].isTrigger = true;
		}
	}
}

#endif