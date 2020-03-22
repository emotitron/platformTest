//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using System.Text;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// Base class for Vital UI components that can find source.
	/// </summary>
	public abstract class VitalsUISrcBase : MonoBehaviour
		, IOnChangeOwnedVitals
	{

		public enum MonitorSource { Auto, Owned, Self, GameObject }

		[Tooltip("Where this VitalUI will look for Vitals data.")]
		[HideInInspector] public MonitorSource monitor = MonitorSource.Auto;

		[Tooltip("Object that this VitalUI will search for an IVitalsComponent vitals data source.")]

		[HideInInspector]
		[SerializeField]
		public Object vitalsSource;

		[System.NonSerialized] public Vitals vitals;

		public abstract void OnChangeOwnedVitals(IVitalsComponent added, IVitalsComponent removed);

		protected virtual void Reset()
		{
			ApplyVitalsSource(null);
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{

		}
#endif

		#region VitalsSource inspector field and Property

		public virtual IVitalsComponent ApplyVitalsSource(Object srcObj)
		{
			GameObject vitalsSrcGO;
			Component vitalsSrcComp;

			if (monitor == MonitorSource.Auto)
			{
				if (srcObj == null)
				{
					srcObj = (Object)GetComponentInParent<IVitalsComponent>();
					monitor = MonitorSource.Self;
				}

				if (srcObj == null)
				{
					srcObj = (Object)OwnedIVitals.LastItem;
					monitor = MonitorSource.Owned;
				}
			}

			/// Override the value if it doesn't conform to the type being monitored.
			if (monitor == MonitorSource.Owned)
			{
				var ownedVitals = OwnedIVitals.LastItem;
				vitalsSrcComp = ownedVitals as Component;
				vitalsSrcGO = null;
			}
			else if (monitor == MonitorSource.Self)
			{
				vitalsSrcGO = gameObject;
				vitalsSrcComp = null;
			}
			// Auto - try self first - if no vitals found fall back to owned
			else
			{
				vitalsSource = srcObj;
				vitalsSrcGO = srcObj as GameObject;
				vitalsSrcComp = srcObj as Component;
			}

			IVitalsComponent ivc;

			/// Get the actual value we want
			if (vitalsSrcGO)
			{
				ivc = FindIVitalComponentOnGameObj(vitalsSrcGO);

				if (ivc != null)
					vitalsSource = (ivc as Component).gameObject;

			}
			else if (vitalsSrcComp)
			{
				ivc = vitalsSrcComp as IVitalsComponent;

				if (monitor == MonitorSource.GameObject)
					vitalsSource = vitalsSrcComp.gameObject;
			}
			else
			{
				ivc = null;
				vitalsSource = null;
			}

			return ivc;

		}

		#endregion

		private static IVitalsComponent FindIVitalComponentOnGameObj(GameObject go)
		{
			/// May be null because vitalsSource is a gameoject, need to turn that into a vitalcomp
			if (go)
			{
				IVitalsComponent ivitalcomp = go.GetComponentInParent<IVitalsComponent>();
				if (ivitalcomp == null)
					ivitalcomp = go.GetComponentInChildren<IVitalsComponent>();
				return ivitalcomp;
			}
			return null;
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(VitalsUISrcBase))]
	[CanEditMultipleObjects]
	public class VitalsUISrcBaseEditor : ReactorHeaderEditor
	{
		public static StringBuilder strb = new StringBuilder(64);

		protected VitalsUISrcBase _target;

		SerializedProperty monitor, vitalsSource;
		bool vitalSourceExpanded = true;

		public override void OnEnable()
		{
			base.OnEnable();
			_target = (VitalsUISrcBase)target;
			_target.ApplyVitalsSource(_target.vitalsSource);

			monitor = serializedObject.FindProperty("monitor");
			vitalsSource = serializedObject.FindProperty("vitalsSource");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			vitalSourceExpanded = EditorGUILayout.Foldout(vitalSourceExpanded, "Vitals Data Source");

			if (vitalSourceExpanded)
			{

				BeginVerticalBox();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(monitor);
				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
					if (!Application.isPlaying)
						_target.ApplyVitalsSource(_target.vitalsSource);
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.ObjectField(vitalsSource, typeof(Object));
				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
					if (!Application.isPlaying)
						_target.ApplyVitalsSource(_target.vitalsSource);
				}


				EndVerticalBox();
			}

		}

	}

#endif
}

