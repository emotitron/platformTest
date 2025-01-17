﻿// Copyright 2019, Davin Carten, All rights reserved

using UnityEngine;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

#if UNITY_EDITOR
using emotitron.Utilities;
using UnityEditor;
#endif

namespace emotitron.Networking
{
	/// <summary>
	/// Base class for components that are aware of Networking and the root NetObject, and tie into its startup/shutdown/ownership callbacks.
	/// </summary>
	public abstract class NetComponent : MonoBehaviour
		, IOnJoinedRoom
		, IOnAwake
		, IOnStart
		, IOnEnable
		, IOnDisable
		, IOnAuthorityChanged
	{

		/// <summary>
		/// Used for shared cached items.
		/// </summary>
		[HideInInspector] [SerializeField] protected int prefabInstanceId;

		protected NetObject netObj;
		public NetObject NetObj { get { return netObj; } }

#if UNITY_EDITOR
		public virtual bool AutoAddNetObj { get { return true; } }
#endif

		protected SyncState syncState;
		public SyncState SyncState { get { return syncState; } }

		public RigidbodyType RigidbodyType { get; private set; }

#if PUN_2_OR_NEWER

		private int netObjId;
		public int NetObjId { get { return netObjId; } }
		protected PhotonView pv;
		public PhotonView PV { get { return pv; } }
		public bool IsMine { get { return pv.IsMine; } }
		public int ControllerActorNr { get { return pv.ControllerActorNr; } }

#else
		public int NetObjId { get { return 0; } }
		public bool IsMine { get { return false; } }
		public int ControllerActorNr { get { return 0; } }
#endif

		protected virtual void Reset()
		{

#if UNITY_EDITOR
			/// Only check the instanceId if we are not playing. Once we build out this is set in stone to ensure all instances and prefabs across network agree.
			if (!Application.isPlaying)
				prefabInstanceId = GetInstanceID();

			GetOrAddNetObj();
#endif
		}


		protected virtual void OnValidate()
		{
#if UNITY_EDITOR
			GetOrAddNetObj();
#endif
		}

#if UNITY_EDITOR

		/// <summary>
		/// Connect the NetObject on this gameobject to the netObj cached variabe.
		/// </summary>
		public void GetOrAddNetObj()
		{
			if (netObj)
				return;

			netObj = transform.GetParentNetObject();

			if (netObj == null && AutoAddNetObj)
			{
				Debug.Log("No NetObject yet on " + name + ". Adding one to root now.");
				netObj = transform.root.gameObject.AddComponent<NetObject>();
			}
		}
#endif

		public virtual void OnJoinedRoom()
		{
#if PUN_2_OR_NEWER
			netObjId = netObj.pv.ViewID;
#endif
		}

		public void Awake()
		{
			if (!GetComponentInParent<NetObject>())
				OnAwakeInitialize(false);
		}
		/// <summary>
		/// Be sure to use base.OnAwake() when overriding. 
		/// This is called when the NetObject runs Awake(). All code that depends on the NetObj being initialized should use this
		/// rather than Awake();
		/// </summary>
		public virtual void OnAwake()
		{
			netObj = GetComponentInParent<NetObject>();

			if (netObj)
				syncState = netObj.GetComponent<SyncState>();

			EnsureComponentsDependenciesExist();

			OnAwakeInitialize(true);
		}

		/// <summary>
		/// Awake code that will run whether or not a NetObject Exists
		/// </summary>
		/// <returns>Returns true if this is a NetObject</returns>
		public virtual void OnAwakeInitialize(bool isNetObject)
		{

		}

		protected NetObject EnsureComponentsDependenciesExist()
		{
			if (!netObj)
				netObj = GetComponentInParent<NetObject>();

			if (netObj)
			{
#if PUN_2_OR_NEWER
				pv = netObj.GetComponent<PhotonView>();
#endif
				if (this is IContacting)
					if (ReferenceEquals(netObj.GetComponent<IContactTrigger>(), null))
						netObj.gameObject.AddComponent<ContactTrigger>();

				RigidbodyType = (netObj.Rb) ? RigidbodyType.RB : (netObj.Rb2D) ? RigidbodyType.RB2D : RigidbodyType.None;

				return netObj;
			}
			else
			{
				Debug.LogError("NetComponent derived class cannot find a NetObject on '" + transform.root.name + "'.");
				return null;
			}
		}


		public virtual void Start()
		{
			if (!netObj)
				OnStartInitialize(false);
		}

		public virtual void OnStart()
		{
#if PUN_2_OR_NEWER
			netObjId = netObj.pv.ViewID;
#endif
			OnStartInitialize(true);
		}

		/// <summary>
		/// Awake code that will run whether or not a NetObject Exists
		/// </summary>
		/// <returns>Returns true if this is a NetObject</returns>
		public virtual void OnStartInitialize(bool isNetObject)
		{

		}

		public virtual void OnPostEnable()
		{

		}

		public virtual void OnPostDisable()
		{
			hadFirstAuthorityAssgn = false;
		}

		protected bool hadFirstAuthorityAssgn;

		/// <summary>
		/// Updates authority values on authority changes.
		/// </summary>
		/// <param name="asServer"></param>
		public virtual void OnAuthorityChanged(bool isMine, bool asServer)
		{
			if (!hadFirstAuthorityAssgn)
			{
				OnFirstAuthorityAssign(isMine, asServer);
				hadFirstAuthorityAssgn = true;
			}
		}

		public virtual void OnFirstAuthorityAssign(bool isMine, bool asServer)
		{

		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(NetComponent), true)]
	[CanEditMultipleObjects]
	public class NetComponentEditor : NetUtilityHeaderEditor
	{
		protected readonly System.Text.StringBuilder sb = new System.Text.StringBuilder();

		protected override string BackTexturePath
		{
			get { return "Header/GreenBack"; }
		}

	}
#endif

}
