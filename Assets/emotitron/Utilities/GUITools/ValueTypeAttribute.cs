﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.GUIUtilities
{
	/// <summary>
	/// Shortens the field rect, and adds the labeltag to the right.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class ValueTypeAttribute : PropertyAttribute
	{
		public string labeltag;
		public float width;

		public ValueTypeAttribute(string labeltag, float width = 48)
		{
			this.labeltag = labeltag;
			this.width = width;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ValueTypeAttribute))]
	public class ValueTypeAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			var _attr = attribute as ValueTypeAttribute;

			float width = _attr.width;

			var proprect = new Rect(r) { width = r.width - width - 2 };
			var tagrect = new Rect(r) { xMin = r.xMax - width };

			EditorGUI.PropertyField(proprect, property, label);
			EditorGUI.LabelField(tagrect, _attr.labeltag);

			//switch (property.propertyType)
			//{
			//	case SerializedPropertyType.Float:
			//		break;

			//	case SerializedPropertyType.Integer:
			//		EditorGUI.PropertyField(tagrect, property);
			//		EditorGUI.LabelField(tagrect, _attr.tag);
			//		break;

			//	default:
			//		Debug.LogWarning(typeof(ValueTypeAttribute).Name + " is cannot be an attribute on property type " + property.propertyType + ". Path: " + property.propertyPath);
			//		EditorGUI.PropertyField(r, property);
			//		break;
			//}

			////_property.intValue = System.Convert.ToInt32(EditorGUI.EnumMaskPopup(_position, _label, (SendCullMask)_property.intValue));
			//property.intValue = EditorGUI.MaskField(r, label, property.intValue, names);
		}
	}
#endif
}
