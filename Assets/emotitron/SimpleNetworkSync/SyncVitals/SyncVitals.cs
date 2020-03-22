//Copyright 2019, Davin Carten, All rights reserved

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.Networking;
using emotitron.Utilities.GhostWorlds;
using emotitron.Utilities;
using emotitron.Compression;
using emotitron.Utilities.HitGroups;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class SyncVitals : SyncObject<SyncVitals.Frame>
		, IVitalsComponent
		, IOnSnapshot
		, IOnNetSerialize
		, IOnNetDeserialize
		, IOnAuthorityChanged
		, IOnPostSimulate
		, IOnVitalChange
		, IOnCaptureState
		, IDamageable
		, IUseKeyframes
		, IOnStateChange
	{
		public override int ApplyOrder { get { return ApplyOrderConstants.VITALS; } }

		//private List<IOnVitalChange> iOnVitalChange = new List<IOnVitalChange>();

		#region Inspector Items

		public Vitals vitals = new Vitals();
		public Vitals Vitals { get { return vitals; } }

#if UNITY_EDITOR
		[Utilities.GUIUtilities.HideNextX(1, false)]
#endif
		public bool useHitGroups = true;
		[SerializeField]
		protected HitGroupMaskSelector validHitGroups = new HitGroupMaskSelector();
		public IHitGroupMask ValidHitGroups { get { return validHitGroups; } }

		[Tooltip("Vital triggers/pickups must have this as a valid mount type. When pickups will attach to this mount when picked up.")]
		public MountSelector defaultMounting = new MountSelector(0);
		public Mount DefaultMount { get; set; }

		[Tooltip("When root vital <= zero, syncState.Despawn() will be called. This allows for a default handling of object 'death'.")]
		public bool autoDespawn = true;
		[Tooltip("When OnStateChange changes from ObjState.Despawned to any other state, vital values will be reset to their starting defaults.")]
		public bool resetOnSpawn = true;

		#endregion

		[System.NonSerialized]
		private VitalsData lastSentData;

		// Cached Items
		private Vital[] vitalArray;
		protected Vital rootVital;
		private int vitalsCount;
		protected int defaultMountingMask;

		// runtime states
		protected bool isPredicted;

		#region Frame

		public class Frame : FrameBase
		{
			public VitalsData vitalsData;

			public Frame() : base() { }

			public Frame(int frameId) : base(frameId) { }

			public Frame(int frameId, Vitals vitals) : base(frameId)
			{
				vitalsData = new VitalsData(vitals);
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);

				var srcVitalsData = (sourceFrame as Frame).vitalsData;
				vitalsData.CopyFrom(srcVitalsData);
			}
		}

		protected override void PopulateFrames()
		{
			frames = new Frame[frameCount + 1];
			for (int i = 0; i <= frameCount; ++i)
				frames[i] = new Frame(i, vitals);
		}

		#endregion Frame

		public override void OnAwake()
		{
			base.OnAwake();

			vitalArray = vitals.VitalArray;
			vitalsCount = vitals.vitalDefs.Count;
			rootVital = vitalArray[0];

			/// subscribe to callbacks to Vitals changes
			vitals.onVitalChangeCallbacks.Add(this);

			lastSentData = new VitalsData(vitals);
			for (int i = 0; i < vitalsCount; ++i)
				vitalArray[i].ResetValues();

			defaultMountingMask = 1 << (defaultMounting.id);
		}

		public override void OnStart()
		{
			base.OnStart();

			var mountsLookup = GetComponent<MountsLookup>();
			if (mountsLookup)
			{
				if (mountsLookup.mountIdLookup.ContainsKey(defaultMounting.id))
					DefaultMount = mountsLookup.mountIdLookup[defaultMounting.id];
				else
				{
					Debug.LogWarning("Sync Vitals has a Default Mount setting of "
					+ MountSettings.Single.mountNames[defaultMounting.id] +
					" but no such mount is defined yet on GameObject: '" + name + "'. Root mount will be used as a failsafe.");

					/// Invalid default mounting (doesn't exist)... warn and set to Root
					defaultMounting.id = 0;
					DefaultMount = mountsLookup.mountIdLookup[0];

				}
			}
		}

		public override void OnAuthorityChanged(bool isMine, bool asServer)
		{
			base.OnAuthorityChanged(isMine, asServer);
			OwnedIVitals.OnChangeAuthority(this, isMine, asServer);
		}

		public bool TryTrigger(IOnTrigger trigger, ref ContactEvent contactEvent, int compatibleMounts)
		{

			if (!useHitGroups || validHitGroups != 0)
			{
				var hga = contactEvent.contacted.GetComponent<HitGroupAssign>();
				int triggermask = hga ? hga.Mask : 0;
				if ((validHitGroups.Mask & triggermask) == 0)
				{
#if UNITY_EDITOR
					Debug.Log(name + " SyncVitals.TryTrigger() HitGroup Mismatch. Cannot pick up '" + hga.transform.root.name + "' because its has a non-matching HitGroupAssign.");
#endif
					return false;
				}
			}

			///TODO: Make this into an interface rather than this component type
			var otherVitalNameType = (trigger as IVitalsAffector);

			Vital vital = vitals.GetVital(otherVitalNameType.VitalNameType);
			if (vital == null)
			{
				return false;
			}

			/// If both are set to 0 (Root) then consider that a match, otherwise zero for one but not the other is a mismatch (for now)
			if ((compatibleMounts == defaultMountingMask) || (compatibleMounts & defaultMountingMask) != 0)
			{
				contactEvent = new ContactEvent(contactEvent) { triggeringObj = vital };
				return true;
			}
			else
			{
				return false;
			}
		}

		public Mount TryPickup(IOnPickup pickup, ContactEvent contactEvent)
		{

			var vital = contactEvent.triggeringObj as Vital;

			if (ReferenceEquals(vital, null))
				return null;

			var vpr = pickup as IVitalsAffector;
			float value = vpr.Value;
			var defaultMount = DefaultMount;

			/// Apply to vital if vital has authority.
			if (IsMine)
			{
				float remainder = vital.ApplyChange(value, vpr.AllowOverload);
				return (!vpr.OnlyPickupIfUsed || value != remainder) ? defaultMount : null;
			}
			/// Vital does not belong to us, but we want to know IF it would have been consumed for prediction purposes.
			else
			{
				if (vpr.OnlyPickupIfUsed)
				{
					float remainder = vital.TestApplyChange(value, vpr.AllowOverload);
					return value != remainder ? defaultMount : null;
				}
				return defaultMount;
			}
		}

		public void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
		{
			if (isNetTick)
				vitals.Simulate();
		}

		public virtual void OnCaptureCurrentState(int frameId, Realm realm)
		{
			var framedatas = frames[frameId].vitalsData.datas;
			for (int i = 0; i < vitalsCount; ++i)
				framedatas[i] = vitalArray[i].VitalData;
		}

		#region Serialization

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{

			/// Don't transmit data if this component is disabled. Allows for muting components
			/// Simply by disabling them at the authority side.
			if (!enabled)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}

			Frame frame = frames[frameId];
			buffer.WriteBool(true, ref bitposition);

			bool isKeyframe = IsKeyframe(frameId);

			return vitals.Serialize(frame.vitalsData, lastSentData, buffer, ref bitposition, isKeyframe);

		}


		public SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{

			/// Needs to ignore any incoming updates that are the server/relay mirroring back what we sent
			var frame = (IsMine) ? offtickFrame : frames[localFrameId];

			/// If frame is empty, we are done here. Typically means object was disabled.
			if (!buffer.ReadBool(ref bitposition))
			{
				return SerializationFlags.None;
			}

			bool isKeyframe = IsKeyframe(originFrameId);
			var flags = vitals.Deserialize(frame.vitalsData, buffer, ref bitposition, isKeyframe);

			frame.content =
				(flags & SerializationFlags.IsComplete) != 0 ? FrameContents.Complete :
				(flags & SerializationFlags.HasChanged) != 0 ? FrameContents.Partial :
				FrameContents.Empty;

			return flags;

		}

		#endregion Serialization

		public float ApplyDamage(float dmg)
		{
			if (!IsMine)
				return dmg;

			if (dmg == 0)
				return dmg;

			return vitals.ApplyDamage(dmg);
		}

		public void OnValueChange(Vital vital)
		{
			//int cnt = iOnVitalChange.Count;
			//for (int i = 0; i < cnt; ++i)
			//	iOnVitalChange[i].OnValueChange(vital);

			//Debug.Log(transform.root.name + " " + name + " " + vitalArray[0].intValue);

			if (autoDespawn)
				if (syncState)
					if (ReferenceEquals(rootVital, vital))
						if (vital.VitalData.IntValue <= 0)
							syncState.Despawn(false);

		}

		public void OnVitalChange(Vital vital)
		{
			//int cnt = iOnVitalChange.Count;
			//for (int i = 0; i < cnt; ++i)
			//	iOnVitalChange[i].OnVitalChange(vital);
		}

		private bool wasDespawned;

		public void OnStateChange(ObjState state, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
		{
			/// Detect respawn (change from despawned to any other state currently) and reset values when that occurs.
			if (wasDespawned && state != ObjState.Despawned)
			{
				for (int i = 0; i < vitalsCount; ++i)
					vitalArray[i].ResetValues();

			}
			wasDespawned = state == ObjState.Despawned;
		}

		public override bool OnSnapshot(int frameId)
		{
			bool ready = base.OnSnapshot(frameId);

			if (!ready)
				return false;

			vitals.Apply(snapFrame.vitalsData);
			return true;
		}

		protected override FrameContents InterpolateFrame(Frame targ, Frame start, Frame end, float t)
		{
			/// TODO: This isn't really an interpolate. Might want to try to make one.
			targ.CopyFrom(start);
			/// TODO: Maybe should be .Complete?
			return FrameContents.Partial;
		}

		protected override FrameContents ExtrapolateFrame()
		{
			var snapdatas = snapFrame.vitalsData.datas;
			var targdatas = targFrame.vitalsData.datas;

			for (int i = 0; i < vitalsCount; ++i)
				targdatas[i] = vitalArray[i].VitalDef.Extrapolate(snapdatas[i]);

			/// TODO: Maybe should be .Complete?
			return FrameContents.Partial;
		}


	}

	//#if UNITY_EDITOR

	//	[CustomEditor(typeof(SyncVitals))]
	//	[CanEditMultipleObjects]
	//	public class SyncVitalsEditor : SyncObjectTFrameEditor
	//	{

	//		protected override void OnInspectorGUIInjectMiddle()
	//		{
	//			base.OnInspectorGUIInjectMiddle();
	//			EditorGUILayout.LabelField("WTF?");

	//		}
	//		public override void OnInspectorGUI()
	//		{

	//			base.OnInspectorGUI();
	//			//CustomGUIRender(serializedObject.GetIterator());
	//		}
	//	}

	//#endif
}


