//Copyright 2019, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Networking.Internal;

using UnityEditor;

namespace emotitron.Networking
{
	[CustomEditor(typeof(SyncVitals))]
	[CanEditMultipleObjects]
	public class SyncVitalsEditor : SyncObjectTFrameEditor
	{
		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.coeh99td287o";
			}
		}
		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncVitalsText";
			}
		}

		protected override string TPotTexturePath
		{
			get
			{
				return "Header/TeapotEarthVitals";
			}
		}


		protected override string Instructions
		{
			get
			{
				return "Collection of Vital types used for handling more complex health systems, accounting for layers of vitals. The default creates a base Health, with a Shield and Armor layer. " +
					typeof(SyncNetHitBase).Name + " derived classes can apply damage to this " + typeof(IVitalsComponent).Name + " component. Vitals Triggers and Pickups can also affect these vitals.";
			}
		}

		private readonly static List<Rigidbody> reusableRBList = new List<Rigidbody>();
		private readonly static List<Rigidbody> reusableRB2DList = new List<Rigidbody>();


		protected override void OnInspectorGUIInjectMiddle()
		{
			base.OnInspectorGUIInjectMiddle();

			EditorGUI.BeginChangeCheck();
;
			SyncVitals sshealth = target as SyncVitals;

			sshealth.transform.root.transform.GetNestedComponentsInChildren(reusableRBList);
			sshealth.transform.root.transform.GetNestedComponentsInChildren(reusableRB2DList);

			int rbCount = reusableRBList.Count;
			int rb2dCount = reusableRB2DList.Count;

			bool isRigidBody = rbCount > 0 || rb2dCount > 0;
			bool isOnRigidbody = (rbCount > 0 && sshealth.GetComponent<Rigidbody>()) || (rb2dCount > 0 && sshealth.GetComponent<Rigidbody2D>());

			if (isRigidBody)
			{
				/// SSH is on root of an RB
				if (isOnRigidbody)
				{
					int colliderCount = sshealth.transform.CountChildCollider(false, true);
					if (colliderCount < 1)
						EditorGUILayout.HelpBox("Cannot locate any non-trigger Collider/Collider2D on this GameObject. Will not be able to detect RB collisions.", MessageType.Warning);
				}
				/// SSH is on an RB, but not the root level
				else
				{
					EditorGUILayout.HelpBox(sshealth.GetType().Name +
						" must be on same child as a RigidBody to detect Rigidbody collisions.", MessageType.Warning);
				}
			}

			if (!isOnRigidbody)
			{
				int triggerCount = sshealth.transform.CountChildCollider(true, true);

				if (triggerCount < 1)
					EditorGUILayout.HelpBox("Cannot locate a Collider/Collider2D on this object and/or children. Hitscans and triggers will not work.", MessageType.Warning);

				if (triggerCount > 1)
					EditorGUILayout.HelpBox(triggerCount + " colliders were found on this object and/or children. " +
						"More than one may cause multiple trigger events.", MessageType.Warning);
			}

			if (EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
		}
	}
}


