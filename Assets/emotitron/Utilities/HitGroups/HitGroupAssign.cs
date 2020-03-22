using System.Collections;
using System.Collections.Generic;
using emotitron.Utilities.GhostWorlds;
using UnityEngine;
using emotitron.Utilities.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.HitGroups
{

	public class HitGroupAssign : MonoBehaviour
		, ICopyToGhost
		, IHitGroupAssign
		, IHitGroupMask
	{
		public HitGroupMaskSelector hitGroupMask;

		[Tooltip("Will add a HitGroupAssign to any children that have colliders and no HitGroupAssign of their own. ")]
		public bool applyToChildren = true;
		
		// cached
		//public int Index { get { return hitGroupMask.index; } }
		public int Mask { get { return hitGroupMask.Mask; } }

		// Use this for initialization
		void Awake()
		{
			CloneToAllChildrenWithColliders(transform, this);
		}

		// if applyToChildren is checked, this HitGroup component needs to be copied to all applicable gameobjects with colliders
		private void CloneToAllChildrenWithColliders(Transform par, HitGroupAssign parentHitGroupAssign)
		{
			if (!applyToChildren)
				return;

			for (int i = 0; i < par.childCount; i++)
			{
				Transform child = par.GetChild(i);

				// if this child has its own HitGroup with applyToChildren = true then stop recursing this branch, that hg will handle that branch.
				HitGroupAssign hga = child.GetComponent<HitGroupAssign>();
				if (hga != null && hga.applyToChildren)
					continue;

				// Copy the parent HitGroup to this child if it has a collider and no HitGroup of its own
				if (hga == null && child.GetComponent<Collider>() != null)
					parentHitGroupAssign.ComponentCopy(child.gameObject);

				// recurse this on its children
				CloneToAllChildrenWithColliders(child, parentHitGroupAssign);
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(HitGroupAssign))]
	[CanEditMultipleObjects]
	public class HitGroupAssignEditor : AccessoryHeaderEditor
	{
		protected override string Instructions { get {  return "Assign colliders of this object (and any children) to a Hit Group. Hit Groups allows collider specific handling (such as critial hits) of collisions, raycast, or overlap hits."; } }
		protected override string BackTexturePath { get { return "Header/RedBack"; } }

		protected static List<Collider> foundColliders = new List<Collider>();
		protected static List<Collider2D> foundColliders2d = new List<Collider2D>();

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();



			var _target = (target as HitGroupAssign);

			if (_target.applyToChildren)
			{
				_target.GetComponentsInChildren(foundColliders);
				_target.GetComponentsInChildren(foundColliders2d);
			}
			else
			{
				_target.GetComponents(foundColliders);
				_target.GetComponents(foundColliders2d);
			}

			if (foundColliders.Count == 0 && foundColliders2d.Count == 0)
				EditorGUILayout.HelpBox("No colliders found.", MessageType.Warning);
			else
				EditorGUILayout.HelpBox(foundColliders.Count + " Collider(s) found.\n" + foundColliders2d.Count + " Collider2d(s) found.", MessageType.None);

			EditorGUILayout.Space();

			HitGroupSettings.Single.DrawGui(target, true, false, true);
		}
	}

#endif

	
}
