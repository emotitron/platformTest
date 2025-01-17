﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	[System.Serializable]
	public struct MountSelector
	{
		/// <summary>
		/// Default is an index of 0 and a layermask of 0
		/// </summary>
		public int id;
#if UNITY_EDITOR
		public bool expanded;
#endif
		public MountSelector(int index)
		{
			this.id = index;
#if UNITY_EDITOR
			this.expanded = false;
#endif
		}

		public static implicit operator int(MountSelector selector) { return selector.id; }
		public static implicit operator MountSelector(int id) { return new MountSelector(id); }

	}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MountSelector))]
	[CanEditMultipleObjects]
	public class MountSelectorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			var index = property.FindPropertyRelative("id");

			int newindex = EditorGUI.Popup(r, label.text, index.intValue, MountSettings.Single.mountNames.ToArray());

			if (newindex != index.intValue)
			{
				Undo.RecordObject(property.serializedObject.targetObject, "Change Mount Selection");
				index.intValue = newindex;
				property.serializedObject.ApplyModifiedProperties();
				
			}
		}
	}

#endif
}