
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class Mount : NetComponent
		, IOnPreQuit
	{
		public const string ROOT_MOUNT_NAME = "Root";

		[Tooltip("A Mount component can be associated with more than one mount name. The first root will always include 'Root'.")]
		[SerializeField] [HideInInspector] public MountSelector mountType = new MountSelector(1);

		[SerializeField] [HideInInspector] public int componentIndex;

		[System.NonSerialized]
		public List<IMountable> mountedObjs = new List<IMountable>();

		[System.NonSerialized]
		public MountsLookup mountsLookup;

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			mountsLookup = MountsLookup.EstablishMounts(gameObject);
			//UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(this, (index == 0));
		}
#endif
		public override void OnAwake()
		{
			base.OnAwake();
			mountsLookup = netObj.GetComponent<MountsLookup>();
		}

		public void OnPreQuit()
		{
			DismountAll();
		}

		private void OnDestroy()
		{
			DismountAll();
		}

		public void DismountAll()
		{
			int cnt = mountedObjs.Count;

			for (int i = cnt - 1; i >= 0; --i)
			{
				//if (Photon.Pun.PhotonNetwork.IsMasterClient)
				//	(mountedObjs[i] as Component).GetComponent<Photon.Pun.PhotonView>().TransferOwnership(Photon.Pun.PhotonNetwork.MasterClient.ActorNumber);

				if (mountedObjs[i] as Component)
					mountedObjs[i].ImmediateUnmount();
			}
		}

		/// <summary>
		/// Removes mountable from the list of objects attached to the current mount, and adds to the list of the new mount.
		/// </summary>
		/// <param name="newmount"></param>
		public static void ChangeMounting(IMountable mountable, Mount newmount)
		{
			var currentMount = mountable.CurrentMount;
			if (!ReferenceEquals(currentMount, null))
			{
				currentMount.mountedObjs.Remove(mountable);
			}

			if (!ReferenceEquals(newmount, null))
			{
				var mountedObjs = newmount.mountedObjs;

				if (!mountedObjs.Contains(mountable))
					mountedObjs.Add(mountable);
			}
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(Mount))]
	[CanEditMultipleObjects]
	public class MountPickupEditor : AccessoryHeaderEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Define equipment mounting points, which can be referenced by index or name.";
			}
		}

		protected override string BackTexturePath
		{
			get
			{
				return "Header/GreenBack";
			}
		}

		static Mount thismount;

		public override void OnEnable()
		{
			base.OnEnable();
		}
		
		
		private static HashSet<int> usedIndexes = new HashSet<int>();
		public override void OnInspectorGUI()
		{

			base.OnInspectorGUI();

			thismount = target as Mount;

			var netObj = thismount.transform.GetComponentInParentEvenIfDisabled<NetObject>();

			if (netObj == null)
			{
				Debug.LogWarning(thismount.name + " Mount is on a non-NetObject.");
				return;
			}

			usedIndexes.Clear();

			MountsLookup mountslookup = netObj.GetComponent<MountsLookup>();
			if (!mountslookup)
			{
				mountslookup = netObj.gameObject.AddComponent<MountsLookup>();
			}

			mountslookup.CollectMounts();

			//EditorGUI.BeginChangeCheck();

			//EditorGUI.BeginChangeCheck();
			EditorGUI.BeginDisabledGroup(thismount.componentIndex == 0);
			EditorGUILayout.PropertyField(serializedObject.FindProperty("mountType"));
			EditorGUI.EndDisabledGroup();

			MountsLookup.DrawAllMountMappings(thismount, mountslookup);

			EditorGUILayout.Space();
			MountSettings.Single.DrawGui(target, true, false, false, false);
		}
	}

#endif
}

