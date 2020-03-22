#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace emotitron.Utilities
{

	[CanEditMultipleObjects]
	public abstract class VersaMaskDrawer : PropertyDrawer
	{
		protected static GUIContent reuseGC = new GUIContent();
		protected abstract bool FirstIsZero { get; }
		protected virtual bool ShowMaskBits { get { return true; } }
		protected abstract string[] StringNames { get; }

		protected const float PAD = 4;
		protected const float LINE_SPACING = 18;
		protected const float BOX_INDENT = 0; //16 - PAD;

		protected static SerializedProperty currentProperty;
		protected bool isExpanded;
		protected int maskValue;

		protected virtual bool Expanded
		{
			get
			{
				var expandedProperty = currentProperty.FindPropertyRelative("expanded");
				if (expandedProperty == null)
					return currentProperty.isExpanded;
				else
					return expandedProperty.boolValue;
			}
			set
			{
				var expandedProperty = currentProperty.FindPropertyRelative("expanded");
				if (expandedProperty == null)
				{
					currentProperty.isExpanded = value;
				}
				else
				{
					expandedProperty.boolValue = value;
				}
			}
		}

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			currentProperty = property;

			bool usefoldout = UseFoldout(label);

			if (usefoldout)
			{
				Expanded = EditorGUI.Toggle(new Rect(r) { xMin = r.xMin, height = LINE_SPACING, width = EditorGUIUtility.labelWidth }, Expanded, (GUIStyle)"Foldout");
			}

			isExpanded = Expanded;

			label = EditorGUI.BeginProperty(r, label, property);

			/// For extended drawer types, the mask field needs to be named mask
			var mask = property.FindPropertyRelative("mask");

			/// ELSE If this drawer is being used as an attribute, then the property itself is the enum mask.
			if (mask == null)
				mask = property;

			maskValue = mask.intValue;

			int tempmask;
			Rect br = new Rect(r) { xMin = r.xMin + BOX_INDENT };
			Rect ir = new Rect(br) { height = LINE_SPACING };

			Rect labelRect = new Rect(r) { xMin = usefoldout ? r.xMin + 14 : r.xMin, height = LINE_SPACING };

			/// Remove Zero value from the array if need be.
			var namearray = new string[FirstIsZero ? StringNames.Length - 1 : StringNames.Length];
			int cnt = namearray.Length;
			for (int i = 0; i < cnt; i++)
				namearray[i] = StringNames[FirstIsZero ? (i + 1) : i];

			if (usefoldout && isExpanded)
			{
				tempmask = 0;

				EditorGUI.LabelField(new Rect(br) { yMin = br.yMin + LINE_SPACING }, "", (GUIStyle)"HelpBox");
				ir.xMin += PAD;
				ir.y += PAD;

				string drawmask = "";

				for (int i = 0; i < cnt; ++i)
				{
					ir.y += LINE_SPACING;

					int offsetbit = 1 << i;
					EditorGUI.LabelField(ir, new GUIContent(namearray[i]));
					if (EditorGUI.Toggle(new Rect(ir) { xMin = r.xMin }, " ", ((mask.intValue & offsetbit) != 0)))
						{
						tempmask |= offsetbit;
						if (ShowMaskBits)
							drawmask = "1" + drawmask;
					}
					else if (ShowMaskBits)
						drawmask = "0" + drawmask;
				}

				reuseGC.text = (ShowMaskBits) ?( "[" + drawmask + "]") : "";
				EditorGUI.LabelField(labelRect, label, reuseGC);
			}
			else
			{
				tempmask = EditorGUI.MaskField(r, usefoldout ? " " : "", mask.intValue, namearray);

				if (usefoldout)
					EditorGUI.LabelField(new Rect(r) { xMin = r.xMin + 14 }, label);
			}

			if (tempmask != mask.intValue)
			{
				Undo.RecordObject(property.serializedObject.targetObject, "Change Mask Selection");
				mask.intValue = tempmask;
				maskValue = tempmask;
				property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}

		protected bool UseFoldout(GUIContent label)
		{
			return label.text != null && label.text != "";
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			currentProperty = property;

			bool expanded = (Expanded && UseFoldout(label));

			return expanded ?
				/*base.GetPropertyHeight(property, label) */LINE_SPACING * (StringNames.Length + (FirstIsZero ? 0 : 1)) + PAD * 2 :
				base.GetPropertyHeight(property, label);
		}
	}

}
#endif
