using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.GUIUtilities
{

	// Attribute that lets me flag SendCull to use the custom drawer and be a multiselect enum
	[AttributeUsage(AttributeTargets.Field)]
	public class EnumMaskAttribute : PropertyAttribute
	{
		public bool definesZero;

		public EnumMaskAttribute(bool definesZero = false)
		{
			this.definesZero = definesZero;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EnumMaskAttribute))]
	public class EnumMaskAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			var names = property.enumDisplayNames;
			if ((attribute as EnumMaskAttribute).definesZero)
			{
				string[] truncated = new string[names.Length - 1];
				Array.Copy(names, 1, truncated, 0, truncated.Length);
				names = truncated;
			}

			//_property.intValue = System.Convert.ToInt32(EditorGUI.EnumMaskPopup(_position, _label, (SendCullMask)_property.intValue));
			property.intValue = EditorGUI.MaskField(r, label, property.intValue, names);
		}
	}
#endif
}

