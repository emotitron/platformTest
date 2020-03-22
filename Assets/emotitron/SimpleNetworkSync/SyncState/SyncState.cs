using emotitron.Compression;
using emotitron.Utilities.GhostWorlds;
using emotitron.Utilities.Networking;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	[DisallowMultipleComponent]

	public class SyncState : SyncObject<SyncState.Frame>
		, IMountable
		, IOnCaptureState
		, IOnNetSerialize
		, IOnNetDeserialize
		, IOnSnapshot
		, IOnNetObjReady
		, IUseKeyframes
	{
		public override int ApplyOrder { get { return ApplyOrderConstants.STATES; } }

		#region Inspector Items

		public ObjState initialState = ObjState.Despawned;
		public ObjState respawnState = ObjState.Visible;
		public ObjState readyState = ObjState.Visible;
		public ObjState unreadyState = ObjState.Despawned;

		[Tooltip("Mount types this NetObject can be attached to.")]
		public MountMaskSelector mountableTo = new MountMaskSelector(0);

		[Tooltip("Automatically return this object to its starting position and attach to original parent when ObjState changes from Despawned to any other state.")]
		public bool autoReset = true;

		[Tooltip("Automatically will request ownership transfer to the owner of NetObjects this becomes attached to.")]
		public bool autoOwnerChange = true;

		#endregion

		[System.NonSerialized] protected Frame currentState = new Frame();
		[System.NonSerialized] protected Mount currentMount = null;
		[System.NonSerialized] protected bool netObjIsReady;

		#region IMountable Requirements

		public Mount CurrentMount { get { return currentMount; } set { currentMount = value; } }
		public bool IsThrowable { get { return true; } }
		public bool IsDroppable { get { return true; } }
		public Rigidbody Rb { get { return netObj.Rb; } }
		public Rigidbody2D Rb2d { get { return netObj.Rb2D; } }

		#endregion

		// Cached Values
		//protected bool foundExternalISpawnControl;
		protected MountsLookup mountsLookup;
		protected SyncTransform syncTransform;

		/// <summary>
		/// [mountTypeId, index]
		/// </summary>
		protected Dictionary<int, int> mountTypeIdToIndex = new Dictionary<int, int>();
		protected int[] indexToMountTypeId;
		protected int bitsForMountType;

		/// <summary>
		/// The state to which this object will be set when Respawn is called.
		/// </summary>
		protected StateChangeInfo respawnStateInfo;

		[System.NonSerialized] public List<IOnStateChange> onStateChangeCallbacks = new List<IOnStateChange>();
		[System.NonSerialized] public List<IOnTeleport> onTeleportCallbacks = new List<IOnTeleport>();

		public class Frame : FrameBase
		{
			public ObjState state;
			//public bool respawn;
			public int? attachedToNetId;
			public int? attachedToMountTypeId;

			public Frame() : base() { }

			public Frame(int frameId) : base(frameId) { }

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);
				Frame src = sourceFrame as Frame;
				state = src.state;
				//respawn = false;
				attachedToNetId = src.attachedToNetId;
				attachedToMountTypeId = src.attachedToMountTypeId;
			}

			public override void Clear()
			{
				base.Clear();
				state = 0;
				attachedToNetId = null;
				attachedToMountTypeId = null;
				//respawn = false;
			}

			public bool Compare(Frame otherFrame)
			{
				if (/*respawn != otherFrame.respawn ||*/
					state != otherFrame.state ||
					attachedToNetId != otherFrame.attachedToNetId ||
					attachedToMountTypeId != otherFrame.attachedToMountTypeId)
					return false;

				return true;
			}
		}

		public override void OnAwake()
		{
			base.OnAwake();

			syncTransform = GetComponent<SyncTransform>();

			respawnStateInfo = new StateChangeInfo(
				respawnState,
				transform.parent ? transform.parent.GetComponent<Mount>() : null,
				transform.localPosition,
				transform.localEulerAngles,
				null, true);

			transform.GetNestedComponentsInChildren(onStateChangeCallbacks);
			transform.GetComponents(onTeleportCallbacks);

			mountsLookup = netObj.GetComponent<MountsLookup>();

		}


		public override void OnStart()
		{

			/// TEST - this code fixed startup rendering, but not fully tested. Likely needs to stay here.
			ChangeState(new StateChangeInfo(initialState, transform.parent ? transform.parent.GetComponent<Mount>() : null, true));

			base.OnStart();

			/// Cache values for mountType serialization. We get the total possible mount options from this objects SyncState
			var mountableToCount = (mountableTo.mask).CountTrueBits(out indexToMountTypeId, MountSettings.Single.mountNames.Count);

			bitsForMountType = FloatCrusher.GetBitsForMaxValue((uint)(mountableToCount));

			for (int i = 0; i < mountableToCount; ++i)
			{
				mountTypeIdToIndex.Add(indexToMountTypeId[i], i);
			}
		}

		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			// Intentionally first. NetObj start may want to change this immediately.
			ChangeState(new StateChangeInfo(initialState, transform.parent ? transform.parent.GetComponent<Mount>() : null, true));

		}

		public override void OnAuthorityChanged(bool isMine, bool asServer)
		{
			base.OnAuthorityChanged(isMine, asServer);

			/// Clear the queue, because there may be some items that never got consumed due to an authority change at startup.
			stateChangeQueue.Clear();
		}

		/// <summary>
		/// Respond to NetObject changes in Ready here.
		/// </summary>
		public virtual void OnNetObjReadyChange(bool ready)
		{

			netObjIsReady = ready;

			if (ready)
			{
				//Debug.Log(name + " <color><b>Ready</b></color> " + readyState + " currState: " +  currentState.state);

				/// We only want to change the state if the state currently matches unready. Otherwise authority changes trigger the default states.
				if (currentState.state == unreadyState)
					ChangeState(new StateChangeInfo(readyState, currentMount, true));
				//ChangeState(new StateChangeInfo(currentState.state | ObjState.Visible, currentMount, false, true));
			}
			else
			{
				//Debug.Log(name + " <b>UnReady</b> " + readyState + " currState: " + currentState.state);
				ChangeState(new StateChangeInfo(unreadyState, currentMount, true));
				//ChangeState(new StateChangeInfo(currentState.state & ~ObjState.Visible, currentMount, false, true));
			}
		}

		#region State Change Shortcuts

		//public void Attach(Mount mount)
		//{
		//	/// Keep the current visibile state, change all others to just Attached
		//	var currentVisibleFlag = (currentState.state & ObjState.Visible);
		//	//Debug.Log(name + "<b> QUE Attach </b>");
		//	stateChangeQueue.Enqueue(new StateChangeInfo(currentVisibleFlag | ObjState.Attached, mount, true));


		//}


		public void SoftMount(Mount attachTo)
		{
			const ObjState MOUNT_ADD_FLAGS = ObjState.Attached;
			const ObjState MOUNT_REM_FLAGS = ObjState.Dropped | ObjState.Transit;

			ObjState newstate = attachTo ? (currentState.state & ~MOUNT_REM_FLAGS) | MOUNT_ADD_FLAGS : currentState.state & ~MOUNT_ADD_FLAGS;
			//QueueStateChange(newstate, attachTo, false);
			ChangeState(new StateChangeInfo(newstate, attachTo, false));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mountTo"></param>
		public void HardMount(Mount mountTo)
		{
			const ObjState MOUNT_ADD_FLAGS = ObjState.Attached | ObjState.Mounted;
			const ObjState MOUNT_REM_FLAGS = ObjState.Dropped | ObjState.Transit;

			ObjState newstate = mountTo ? (currentState.state & ~MOUNT_REM_FLAGS) | MOUNT_ADD_FLAGS : currentState.state & ~MOUNT_ADD_FLAGS;
			//QueueStateChange(newstate, mountTo, false);
			ChangeState(new StateChangeInfo(newstate, mountTo, false));
		}


		public void Spawn()
		{
			// Not Implemented... put a generic just works Spawn call here for making an Object pop back to life where it originated
		}

		public void Respawn(bool immediate)
		{
			//Debug.LogError(Time.time + " Respawn " + immediate);

			//Debug.Log(Time.time + " " + name + "<b> QUE Respawn </b> curr state = " + currentState.state);
			if (immediate)
				ChangeState(respawnStateInfo);
			else
				stateChangeQueue.Enqueue(respawnStateInfo);
		}

		public void Despawn(bool immediate)
		{
			//Debug.LogError(Time.time + " Despawn " + immediate);
			if (immediate)
				ChangeState(new StateChangeInfo(ObjState.Despawned, null, true));
			else
				stateChangeQueue.Enqueue(new StateChangeInfo(ObjState.Despawned, null, true));
		}

		/// <summary>
		/// Handler that unparents this object from any mount immediately (rather than on the next tick Capture).
		/// Put in place for handling despawning of objects, so that mounts on NetObjects can unmount all objects before self-destructing.
		/// </summary>
		public void ImmediateUnmount()
		{
			//Debug.Log(name + "<b> IMMEDIATE UNMOUNT </b>");
			stateChangeQueue.Clear();
			ChangeState(new StateChangeInfo(ObjState.Visible | ObjState.Transit | ObjState.Dropped, null, true));
		}

		public void Drop(Mount newMount, bool force = false)
		{
			const ObjState state = ObjState.Visible | ObjState.Dropped;
			stateChangeQueue.Enqueue(new StateChangeInfo(state, newMount, force));
		}

		public void Throw(Vector3 localOffset, Vector3 velocity)
		{
			const ObjState state = ObjState.Visible | ObjState.Dropped | ObjState.Transit | ObjState.Dropped;
			stateChangeQueue.Enqueue(new StateChangeInfo(state, null, localOffset, velocity, false));
		}

		#endregion


		protected Queue<StateChangeInfo> stateChangeQueue = new Queue<StateChangeInfo>();

		//bool teleportNeeded = false;

		public virtual void QueueStateChange(ObjState newState, Mount newMount, bool force)
		{
			//if (IsMine)
			//	Debug.Log(Time.time + " " + name + "<b> QUE STATE </b>" + newState);

			stateChangeQueue.Enqueue(new StateChangeInfo(newState, newMount, null, null, force));
		}

		public virtual void QueueStateChange(ObjState newState, Mount newMount, Vector3 offset, Vector3 velocity, bool force)
		{
			//if (IsMine)
			//	Debug.Log(Time.time + " " + name + "<b> QUE STATE </b>" + newState);

			stateChangeQueue.Enqueue(new StateChangeInfo(newState, newMount, offset, velocity, force));
		}

		protected virtual void DequeueStateChanges()
		{
			//int newOwnerId = -1;

			while (stateChangeQueue.Count > 0)
			{
				var stateChangeInfo = stateChangeQueue.Dequeue();
				//Debug.Log(Time.time + " DEQUEUE " + stateChangeInfo);
				/*newOwnerId = */
				ChangeState(stateChangeInfo);
			}

			/// TODO: TEST removed, only letting this change on new owners snapshot currently. May be able to readd this safely.
			//if (autoOwnerChange && newOwnerId != -1)
			//{
			//	Debug.LogError("DISABLED");
			//	pv.TransferOwnership(newOwnerId);
			//}
		}

		/// <summary>
		/// Call this method to change the state of this object. This state will be synced over the network,
		/// and callbacks will trigger locally and remotely. Typically it is preferred to call QueueStateChange(), 
		/// which will defer the ChangeState application until the appropriate timing.
		/// </summary>
		protected virtual void ChangeState(StateChangeInfo stateChangeInfo)
		{
			if (!gameObject)
			{
				Debug.LogWarning(name + " has been destroyed. Will not try to change state.");
				return;
			}

			//if (IsMine)
			//	//if (GetComponent<SyncVitals>())
			//	{
			//		if (stateChangeInfo.objState == ObjState.Visible)
			//			Debug.Log(Time.time + " " + stateChangeInfo);
			//		else
			//			Debug.Log(Time.time + " <b>" + stateChangeInfo + "</b>");
			//	}

			var oldState = currentState.state;
			var oldMount = currentMount;
			var newState = stateChangeInfo.objState;
			var newMount = stateChangeInfo.mount;

			bool respawn;
			/// Assuming first Visible after a despawn is a Respawn - this is here to handle lost teleport packets
			if (autoReset && oldState == ObjState.Despawned && newState != ObjState.Despawned)
			{
				stateChangeInfo = new StateChangeInfo(respawnStateInfo) { objState = stateChangeInfo.objState };
				respawn = true;
			}
			else
				respawn = false;

			var force = stateChangeInfo.force;

			bool stateChanged = newState != oldState;
			bool mountChanged = oldMount != newMount;

			var prevParent = transform.parent;

			/// Test nothing has changed
			if (!force && !stateChanged && !mountChanged)
				return;

			bool nowAttached = (newState & ObjState.Attached) != 0;

			/// Handling for attached without a valid Mount
			if (nowAttached && !newMount)
			{
				//Debug.LogError(name + " Attached with a null mount! " + newState);
				InvalidMountHandler(newState, newMount, force);
				return;
			}

			if (IsMine)
				if (mountChanged || respawn)
				{
					////Debug.LogError("TELEPORT");
					//for (int i = 0; i < onTeleportCallbacks.Count; ++i)
					//	onTeleportCallbacks[i].OnTeleport();
				}

			/// The State is attached
			/*else*/
			if (mountChanged)
			{
				/// Attaching to a mount
				// If Attached bit is true
				if (nowAttached)
				{
					//Debug.Log(Time.time + " " + name + " ATTACH " + newMount.name + " ");
					currentState.attachedToNetId = newMount.NetObjId;
					currentState.attachedToMountTypeId = newMount.mountType.id;

					transform.parent = newMount.transform;

					bool nowMounted = (newState & ObjState.Mounted) != 0;

					// If Mounted bit is true
					if (nowMounted)
					{
						//Debug.Log(name + " Hard Mounting Origin");
						transform.localPosition = new Vector3();
						transform.localRotation = new Quaternion();
					}
				}

				/// Detaching from a mount
				else
				{
					//Debug.Log(Time.time + " " + name + " DEATTACH ");

					currentState.attachedToNetId = null;
					currentState.attachedToMountTypeId = null;
					transform.parent = null;
				}

				Mount.ChangeMounting(this, newMount);
			}

			var pos = stateChangeInfo.offsetPos;
			var rot = stateChangeInfo.offsetRot;
			var vel = stateChangeInfo.velocity;

			if (rot.HasValue)
			{
				//Debug.Log(Time.time + " " + name + " STATE ROT APPLY " + rot.Value);

				transform.localEulerAngles = rot.Value;
			}

			if (pos.HasValue)
			{
				if (respawn)
				{
					transform.localPosition = pos.Value;
				}
				else
				{
					var parRot = prevParent ? prevParent.rotation : transform.rotation;
					transform.position = transform.position + parRot * pos.Value;
				}
			}

			if (vel.HasValue)
			{

				var rb = syncState.Rb;
				if (rb)
				{
					if (!rb.isKinematic)
					{
						rb.velocity = ((prevParent) ? prevParent.rotation : rb.rotation) * vel.Value;
					}
				}
				else
				{
					var rb2d = syncState.Rb2d;
					if (rb2d)
					{
						if (!rb2d.isKinematic)
						{
							rb2d.velocity = stateChangeInfo.velocity.Value;
						}
					}
				}
			}

			currentState.state = newState;
			currentMount = newMount;


			///// Apply the vector values
			//this.ApplyVectors(stateChangeInfo, prevParent, onTeleportCallbacks);

			/// Send out callbacks
			for (int i = 0; i < onStateChangeCallbacks.Count; ++i)
				onStateChangeCallbacks[i].OnStateChange(newState, transform, currentMount, netObjIsReady);
		

		}


		/// <summary>
		/// Modify StateChange call if the mount was invalid (Mount likely destroyed).
		/// </summary>
		protected virtual void InvalidMountHandler(ObjState newState, Mount newMount, bool force)
		{
			ChangeState(new StateChangeInfo(ObjState.Visible, null, true));
		}

		/// <summary>
		/// Attempt to change to a different Mount on the same object.
		/// </summary>
		/// <param name="newMountId"></param>
		public virtual bool ChangeMount(int newMountId)
		{
			if (ReferenceEquals(currentMount, null))
			{
				Debug.LogWarning("'" + name + "' is not currently mounted, so we cannot change to a different mount.");
				return false;
			}

			if ((mountableTo & (1 << newMountId)) == 0)
			{
				Debug.LogWarning("'" + name + "' is trying to switch to a mount '" + MountSettings.single.mountNames[newMountId] + "' , but mount is not set as valid in SyncState.");
				return false;
			}

			var lookup = currentMount.mountsLookup.mountIdLookup;

			if (!lookup.ContainsKey(newMountId))
			{
				Debug.LogWarning("'" + name + "' doesn't contain a mount for '" + MountSettings.single.mountNames[newMountId] + "'.");
				return false;
			}
			var attachTo = lookup[newMountId];

			//Debug.Log("New Mount:" + attachTo + ":" + newMountId);

			ChangeState(new StateChangeInfo(currentState.state, attachTo, false));

			return true;
		}

		protected Mount pendingMount;

		public void OnCaptureCurrentState(int frameId, Realm realm)
		{
			DequeueStateChanges();

			Frame frame = frames[frameId];

			frame.CopyFrom(currentState);
			//if (currentState.respawn)
			//{
			//	frame.respawn = true;
			//	currentState.respawn = false;
			//}
		}

		#region Serialization

		protected Frame prevSerializedFrame;

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{

			/// Don't transmit data if this component is disabled. Allows for muting components
			/// Simply by disabling them at the authority side.
			if (!isActiveAndEnabled)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}

			Frame frame = frames[frameId];

			/// Write State - it is cheap enough to send it every tick
			buffer.Write((ulong)frame.state, ref bitposition, 5);

			bool iskeyframe = IsKeyframe(frameId);
			bool hascontent = iskeyframe || ReferenceEquals(prevSerializedFrame, null) || !prevSerializedFrame.Compare(frame);

			var flags = hascontent ? SerializationFlags.HasChanged : SerializationFlags.None;

			if ((frame.state & ObjState.Attached) != 0)
			{

				//if (GetComponent<SyncVitals>())
				//	Debug.Log(frameId + " SER " + frame.state + " hascontent? " + hascontent + " " + iskeyframe + " " + !prevSerializedFrame.Compare(frame));

				if (hascontent)
				{
					if (!iskeyframe)
						buffer.Write(1, ref bitposition, 1);

					//if ((frame.state & ObjState.Attached) != 0)
					{
						buffer.WritePackedBytes((uint)frame.attachedToNetId, ref bitposition, 32);
						if (bitsForMountType > 0)
						{
							//Debug.Log(Time.time + " " + name + " <b>SER Mount Type Id</b> " + frame.attachedToMountTypeId.Value + " "
							//	+ ((!ReferenceEquals(prevSerializedFrame, null) ? prevSerializedFrame.Compare(frame).ToString() : "null")) + " " + (transform.parent ? transform.parent.name : ""));

							var mountidx = mountTypeIdToIndex[frame.attachedToMountTypeId.Value];
							buffer.Write((uint)mountidx, ref bitposition, bitsForMountType);
						}
						//Debug.LogError(frame.frameId + " " + name + " SER " + frame.attachedToNetId + ":" + frame.attachedToMountId + "   " + MountSettings.bitsForMountId);
					}

					//if (GetComponent<SyncVitals>())
					//	Debug.Log(frameId + " <b>SER</b> " + frame.state + " " + frame.attachedToNetId + " : " + frame.attachedToMountTypeId);

					flags |= SerializationFlags.HasChanged;
				}
				else
				{
					buffer.Write(0, ref bitposition, 1);
				}
			}


			prevSerializedFrame = frame;
			return flags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			Frame frame = frames[localFrameId];

			SerializationFlags flags = SerializationFlags.HasChanged;

			/// Read State
			frame.state = (ObjState)buffer.Read(ref bitposition, 5);

			bool isAttached = (frame.state & ObjState.Attached) != 0;

			if (!isAttached)
			{
				frame.content = FrameContents.Complete;
			}
			else if (IsKeyframe(originFrameId) || buffer.Read(ref bitposition, 1) == 1)
			{
				///// Read isRespawn bool
				//frame.respawn = buffer.Read(ref bitposition, 1) == 0 ? false : true;

				/// Read attached
				if ((frame.state & ObjState.Attached) != 0)
				{
					frame.attachedToNetId = (int?)buffer.ReadPackedBytes(ref bitposition, 32);
					if (bitsForMountType > 0)
					{
						int mountidx = (int)buffer.Read(ref bitposition, bitsForMountType);
						int mountTypeId = indexToMountTypeId[mountidx];
						frame.attachedToMountTypeId = (int?)mountTypeId;
					}
					else
						frame.attachedToMountTypeId = 0;
				}

				
				frame.content = FrameContents.Complete;
			}

			/// State is attached, but because this is a delta frame the parent info is missing
			else
			{
				frame.attachedToNetId = null;
				frame.attachedToMountTypeId = null;
				frame.content = FrameContents.Partial;
			}

			//if (GetComponent<SyncPickup>())
			//	Debug.Log(Time.time + " " + name + " <b>[" + frame.frameId + "] DES State </b> " + frame.state + " " +
			//		(frame.isCompleteFrame ? (frame.attachedToNetId + ":" + frame.attachedToMountTypeId) : " empty"));


			return flags;
		}

		#endregion Serialization

		protected override void ApplySnapshot(bool isInitial, bool isInitialComplete)
		{

			/// Notifying the SyncTransform of any parent changes, since they are managed here. Less than ideal, but the alternative is to completely move parent handing to SyncTransform.
			if ((targFrame.content == FrameContents.Complete)) // & FrameContents.Partial) != 0)
			{
				Transform par;
				if (targFrame.attachedToNetId.HasValue)
				{
					var targmount = GetMount(targFrame.attachedToNetId, targFrame.attachedToMountTypeId);
					par = targmount ? targmount.transform.parent : null;
				}
				else
					par = null;

				if (syncTransform)
					syncTransform.UpdateParent(targFrame.state, par);
			}

			if (snapFrame.content == FrameContents.Empty)
			{
				return;
			}

			//if (GetComponent<SyncVitals>())
			//	Debug.Log(snapFrame.frameId + " " + targFrame.frameId + "  content :" + snapFrame.content + " " +
			//		" <b>SNAP STATE snap:</b>" + snapFrame.frameId + " " +  (snapFrame.state + "  " + snapFrame.attachedToNetId + ":" + snapFrame.attachedToMountTypeId) +
			//		 " targ: " + targFrame.frameId + " " + (targFrame.content != 0 ? (targFrame.state + "  " + targFrame.attachedToNetId + ":" + targFrame.attachedToMountTypeId) : " lost"));


			var snapState = snapFrame.state;

			Mount snapMount;

			bool attach = ((snapState & ObjState.Attached) != 0);

			if (attach)
			{
				int? snapAttachedToNetId = snapFrame.attachedToNetId;

				/// attached ID will only be sent on keyframes, so the id may be null even though pickup is attached. Use the old id value if so.
				if (snapAttachedToNetId.HasValue)
				{
					int? snapAttachedToMountId = snapFrame.attachedToMountTypeId;

					var mount = GetMount(snapAttachedToNetId, snapAttachedToMountId);// UnifiedNetTools.FindComponentByNetId<MountsLookup>(snapAttachedToNetId.Value);
					if (mount)
					{
						if (autoOwnerChange)
						{
#if PUN_2_OR_NEWER
							var mountOwnerId = mount.PV.OwnerActorNr;
							if (mount.PV.IsMine && pv.OwnerActorNr != mountOwnerId)
							{
								pv.TransferOwnership(mount.PV.OwnerActorNr);
							}
#endif
						}
						snapMount = mount;
					}
					else
						snapMount = currentMount;

					ReadyState = ReadyStateEnum.Ready;

				}
				/// Because of delta frames and packetloss, we know this is attached, but don't know what to!
				else
				{
					/// Has become attached. Since we don't know
					if (currentMount == null)
					{
						//if (GetComponent<SyncPickup>())
						//	Debug.Log("<color=red>UNREADY</color>");

						ReadyState = ReadyStateEnum.Unready;
						snapMount = null;
					}
					else
					{
						ReadyState = ReadyStateEnum.Ready;
						snapMount = currentMount;
					}
				}
			}
			/// Detached
			else
			{
				ReadyState = ReadyStateEnum.Ready;
				snapMount = null;

				
			}

			//if (syncTransform && attach || snapMount != null)
			//	syncTransform.UpdateParent(targFrame.state, null);

			ChangeState(new StateChangeInfo(snapState, snapMount, isInitialComplete));
		}


		public static Mount GetMount(int? netId, int? mountId)
		{
			if (!netId.HasValue || !mountId.HasValue)
				return null;

			var mounts = UnifiedNetTools.FindComponentByNetId<MountsLookup>(netId.Value);

			if (mounts)
				return mounts.mountIdLookup[mountId.Value];

			return null;
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncState))]
	[CanEditMultipleObjects]
	public class SyncStateEditor : SyncObjectTFrameEditor
	{
		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=kix.aoe7lw6zs4bc";
			}
		}
		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncStateText";
			}
		}

		protected override string Instructions
		{
			get
			{
				return "Manages and syncs the State (Visibility, Attachment, Dropped, etc) of this NetObject. " +
					"Calls to <b>syncState.ChangeState()</b> will replicate, and " +
					"Components with <b>" +

					typeof(IOnStateChange).Name + "</b> will receive callbacks.";
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();
			MountSettings.Single.DrawGui(target, true, false, false, false);
		}

	}
#endif
}

