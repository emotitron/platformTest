//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

#endif

namespace emotitron.Utilities.Networking
{

	/// <summary>
	/// Destroys they exist in the scene during startup.
	/// This allows prefab copies to exist in the scene while editing, without having to delete them every time you build out.
	/// </summary>
	[DisallowMultipleComponent]
	public class AutoDestroyUnspawned : MonoBehaviour
	{
		public bool onlyIfPrefab = true;
		[SerializeField] [HideInInspector] public bool hasPrefabParent;

#if UNITY_EDITOR

		public void DetectPrefabParent()
		{
#if UNITY_2018_3_OR_NEWER

			if (gameObject.scene == SceneManager.GetActiveScene())
			{
				hasPrefabParent = PrefabUtility.IsPartOfPrefabAsset(gameObject);
				if (!hasPrefabParent)
				{
					var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);
					hasPrefabParent = prefabStatus == PrefabInstanceStatus.Connected || prefabStatus == PrefabInstanceStatus.Disconnected;
				}
			}
			
#else
			if (gameObject.scene == SceneManager.GetActiveScene())
				hasPrefabParent = PrefabUtility.GetPrefabParent(gameObject);
#endif
		}
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void DestroyUnspawned()
		{
			AutoDestroyUnspawned[] nsts = Object.FindObjectsOfType<AutoDestroyUnspawned>(); // Resources.FindObjectsOfTypeAll<AutoDestroyUnspawned>();

			for (int i = nsts.Length - 1; i >= 0; i--)
			{
				var obj = nsts[i];

				if (obj.onlyIfPrefab)
				{
					if (!obj.hasPrefabParent)
						continue;
				}

				if (obj.gameObject.scene == SceneManager.GetActiveScene())
				{
					Object.Destroy(obj.gameObject);
				}
			}
		}
	}

	/// <summary>
	/// Handlers for finding AutoDestroy components and checking if they have a prefab parent.
	/// Cannot be done at runtime, so this is done on exiting Editor Mode and on PreBuild.
	/// </summary>
#if UNITY_EDITOR

	/// <summary>
	/// Find all autoDestroy components on before building and identify objects that have prefab parents
	/// </summary>
#if UNITY_2018_1_OR_NEWER
	public class DetectPrefabParent : IPreprocessBuildWithReport
#else
	public class DetectPrefabParent : IPreprocessBuild
#endif
	{

		public int callbackOrder { get { return 0; } }

#if UNITY_2018_1_OR_NEWER

		public void OnPreprocessBuild(BuildReport report)
		{
			FindAndSetAll();
		}
#else
		public void OnPreprocessBuild(BuildTarget target, string text)
		{
			FindAndSetAll();
		}
#endif

		/// <summary>
		/// Find all autoDestroy components on before entering playmode and identify objects that have prefab parents
		/// </summary>
		[InitializeOnLoad]
		public static class PlayModeStateChangedExample
		{
			// register an event handler when the class is initialized
			static PlayModeStateChangedExample()
			{
				EditorApplication.playModeStateChanged -= LogPlayModeState;
				EditorApplication.playModeStateChanged += LogPlayModeState;
			}

			private static void LogPlayModeState(PlayModeStateChange state)
			{
				if (state == PlayModeStateChange.ExitingEditMode)
				{
					FindAndSetAll();
				}
			}
		}

		public static void FindAndSetAll()
		{
			AutoDestroyUnspawned[] nsts = Resources.FindObjectsOfTypeAll<AutoDestroyUnspawned>();

			for (int i = nsts.Length - 1; i >= 0; i--)
			{
				var obj = nsts[i];
				obj.DetectPrefabParent();
			}
		}
	}

#endif

#if UNITY_EDITOR

	[CustomEditor(typeof(AutoDestroyUnspawned))]
	[CanEditMultipleObjects]
	public class AutoDestroyUnspawnedEditor : AutomationHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Destroys this gameobject if it exists in the scene at scene load. " +
				"Allows network prefabs to be left in scene at build/play time, as a development convenience.";
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			(target as AutoDestroyUnspawned).DetectPrefabParent();
		}
	}

#endif
}

