using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.HitGroups
{
	[System.Serializable]
	public struct HitGroupSelector : IHitGroupMask
	{
		/// <summary>
		/// Default is an index of 0 and a layermask of 0
		/// </summary>
		public int index;

		// TODO: this really should be cached if possible.
		/// <summary>
		/// Default is an index of 0 and a layermask of 0
		/// </summary>
		public int Mask { get { return (index == 0) ? 0 : ((int)1 << (index - 1)); } }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(HitGroupSelector))]
	[CanEditMultipleObjects]
	public class HitGroupSelectorDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			var index = property.FindPropertyRelative("index");
			int newindex = EditorGUI.Popup(r, "Hit Group", index.intValue, HitGroupSettings.Single.hitGroupTags.ToArray());

			if (newindex != index.intValue)
			{
				Undo.RecordObject(property.serializedObject.targetObject, "Change HitGroup Selection");
				index.intValue = newindex;
			}
		}
	}

#endif
}

