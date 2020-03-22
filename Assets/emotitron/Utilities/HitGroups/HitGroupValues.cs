using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.HitGroups
{
	[System.Serializable]
	public class HitGroupValues
	{
		[SerializeField]
		public List<float> values = new List<float>() { 1 };
	}


#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(HitGroupValues))]
	[CanEditMultipleObjects]
	public class HitGroupValuesDrawer : PropertyDrawer
	{
		protected static GUIStyle italicstyle;
		private GUIContent reuseGC = new GUIContent();
		private const float pad = 6;
		const int LINE_HGHT = 18;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			if (ReferenceEquals(italicstyle, null))
				italicstyle = new GUIStyle() { fontStyle = FontStyle.Italic };

			var values = property.FindPropertyRelative("values");

			var array = HitGroupSettings.Single.hitGroupTags.ToArray();

			EditorGUI.BeginChangeCheck();

			GUI.Box(new Rect(r) { yMin = r.yMin + LINE_HGHT + pad }, GUIContent.none, (GUIStyle)"HelpBox");

			r.xMin += pad;
			r.xMax -= pad;
			r.yMin += pad;
			r.yMax += pad;

			EditorGUI.LabelField(new Rect(r) { height = 17 }, "Group", "Multipliers");
			r.y += LINE_HGHT + pad;

			/// Resize our list to match the number of groups in Settings
			while (values.arraySize < array.Length)
			{
				values.InsertArrayElementAtIndex(values.arraySize);
				property.serializedObject.ApplyModifiedProperties();
			}
			while (values.arraySize > array.Length)
			{
				values.DeleteArrayElementAtIndex(values.arraySize - 1);
				property.serializedObject.ApplyModifiedProperties();
			}

			float line = r.yMin;
			r.height = LINE_HGHT;

			/// Draw List
			for (int i = 0; i < values.arraySize; ++i)
			{
				reuseGC.text = array[i];
				EditorGUI.LabelField(r, reuseGC, italicstyle);
				reuseGC.text = " ";
				EditorGUI.PropertyField(r, values.GetArrayElementAtIndex(i), reuseGC);
				r.y += LINE_HGHT;
			}

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 17 + LINE_HGHT * HitGroupSettings.Single.hitGroupTags.Count + pad * 2 + pad;
		}
	}

#endif
}


