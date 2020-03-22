//using UnityEngine;
//using emotitron.Compression;
//using emotitron.Utilities.GhostWorlds;
//using emotitron.Utilities.Networking;
//using System.Collections.Generic;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Networking
//{

//	/// <summary>
//	/// Basic automatic transform mover for objects for network testing. Will only run if object has local authority.
//	/// </summary>
//	[System.Obsolete("Has been broken into two classes. SyncNodeMover and SyncAdditiveMover")]
//	public class SyncMover : SyncMoverBase<SyncMover.Frame> // SyncObject<SyncMover.Frame> // MonoBehaviour
//		, ITransformController
//		, IOnPreUpdate
//		, IOnPreSimulate
//		, IOnCaptureState
//		, IOnNetSerialize
//		, IOnNetDeserialize
//		, IOnSnapshot
//		, IOnInterpolate
//		, IReadyable
//	{
		
//		public enum Movement { Additive, Oscillate, Trigger }
//		[System.Serializable]
//		public class Node
//		{
//			public Vector3[] trs = new Vector3[3] { new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1) };
//			public Vector3 Pos { get { return trs[0]; } set { trs[0] = value; } }
//			public Vector3 Rot { get { return trs[1]; } set { trs[1] = value; } }
//			public Vector3 Scl { get { return trs[2]; } set { trs[2] = value; } }
//		}

//		[System.Serializable]
//		public class TRSDefinition
//		{
//			[HideInInspector] public AxisMask includeAxes = AxisMask.XYZ;
//			[HideInInspector] public MovementRelation relation = MovementRelation.Relative;
//			[HideInInspector] public bool local = true;
//			[HideInInspector] public Vector3 addVector = new Vector3(0, 0, 0);

//			// cache
//			[System.NonSerialized] public Vector3 fixedAddVector;
//			[System.NonSerialized] public Vector3 tickAddVector;
//		}

//		#region Interface Requirements

//		/// Suppress the automatic adding of a NetObject
//		public override bool AutoAddNetObj { get { return false; } }

//		/// TODO: This may need a different reply for Trigger - unless I can make that deterministic.
//		public virtual bool OverridesExtrapolation { get { return true; } }

//		#endregion

//		#region Inspector

//		[HideInInspector] public TRSDefinition posDef = new TRSDefinition();
//		[HideInInspector] public TRSDefinition rotDef = new TRSDefinition();
//		[HideInInspector] public TRSDefinition sclDef = new TRSDefinition() { includeAxes = AxisMask.None };

//		[HideInInspector] public List<Node> nodes = new List<Node>() { new Node(), new Node() };
//		public Node StartNode { get { return nodes[0]; } }
//		public Node EndNode { get { return nodes[nodes.Count - 1]; } }

//		[HideInInspector] public Movement movement = Movement.Oscillate;
//		[HideInInspector] public float oscillatePeriod = 1;
//		[HideInInspector] public AnimationCurve oscillateCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.5f, 1),new Keyframe(1, 0));
//		[HideInInspector] public LiteFloatCrusher floatCrusher = new LiteFloatCrusher(LiteFloatCompressType.Bits10, LiteFloatCrusher.Normalization.Positive);

//		// AutoSyncTransform Requirements

//#if UNITY_EDITOR
//		protected static List<ITransformController> foundTransformControllers = new List<ITransformController>();

//		[Tooltip("Automatically Adds/Removes SyncTransform as needed, and makes a best guess at settings. Ideally disable this once things are working and tweak the SyncTransform settings yourself.")]
//		[HideInInspector] public bool autoSync = true;
//		public bool AutoSync
//		{
//			get { return autoSync; }
//			set { autoSync = value; }
//		}

//#endif
//		#endregion

//		// Cached items
//		protected Rigidbody rb;
//		protected Rigidbody2D rb2d;
//		protected TransformCrusher tc;
//		protected float lastUpdateTime;
//		protected double oscMultiplier;
//		protected Matrix additiveMatrix;
//		[System.NonSerialized]
//		public SyncTransform syncTransform;

//		// State
//		protected float currentPhase;
//		protected int queuedTargetNode;
//		protected int targetNode;
//		protected float timeSinceTrigger;

//		#region Frame

//		public class Frame : FrameBase
//		{
//			public int targetNode;
//			public float phase;
//			public uint cphase;

//			public Frame() : base() { }

//			public Frame(int frameId) : base(frameId) { }

//			public override void CopyFrom(FrameBase sourceFrame)
//			{
//				base.CopyFrom(sourceFrame);
//				Frame src = sourceFrame as Frame;

//				this.targetNode = src.targetNode;
//				this.phase = src.phase;
//				this.cphase = src.cphase;
//			}

//			public override void Clear()
//			{
//				base.Clear();

//				this.targetNode = -1;
//				this.phase = -1;
//				this.cphase = 0;
//			}

//			public bool Compare(Frame otherFrame)
//			{
//				if (
//					targetNode != otherFrame.targetNode ||
//					phase != otherFrame.phase ||
//					cphase != otherFrame.cphase
//					)
//					return false;

//				return true;
//			}
//		}

//		#endregion Frame

//		#region Startup/Shutdown

//		protected override void Reset()
//		{
//			base.Reset();
//			oscillateCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.5f, 1), new Keyframe(1, 0));
//		}

//		public void Awake()
//		{
//			/// Handling for if this is not a netobject... ties directly into timing callbacks of NetMaster
//			if (!pv)
//			{
//				NetMaster.onPreSimulates.Add(this);
//				NetMaster.onPreUpdates.Add(this);
//			}
//			syncTransform = GetComponent<SyncTransform>();
//		}
//		/// Handling for if this is not a netobject... ties directly into timing callbacks of NetMaster
//		private void OnDestroy()
//		{
//			if (!pv)
//			{
//				NetMaster.onPreSimulates.Remove(this);
//				NetMaster.onPreUpdates.Remove(this);
//			}
//		}
//		public override void OnAwake()
//		{
//			base.OnAwake();

//			rb = GetComponent<Rigidbody>();
//			rb2d = GetComponent<Rigidbody2D>();

//			/// Force RBs to be kinematic
//			if ((rb && !rb.isKinematic) || (rb2d && !rb2d.isKinematic))
//			{
//				Debug.LogWarning(GetType().Name + " doesn't work with non-kinematic rigidbodies. Setting to kinematic.");
//				if (rb)
//					rb.isKinematic = true;
//				else
//					rb2d.isKinematic = true;
//			}

//			Recalculate();
//		}

//		public override void OnStart()
//		{
//			base.OnStart();

//			InitializeTRS(posDef, TRS.Position);
//			InitializeTRS(rotDef, TRS.Rotation);
//			InitializeTRS(sclDef, TRS.Scale);
//		}

//		public void Recalculate()
//		{
//			if (movement == Movement.Additive)
//			{
//				posDef.fixedAddVector = posDef.addVector * Time.fixedDeltaTime;
//				rotDef.fixedAddVector = rotDef.addVector * Time.fixedDeltaTime;
//				sclDef.fixedAddVector = sclDef.addVector * Time.fixedDeltaTime;

//				posDef.tickAddVector = posDef.addVector * (Time.fixedDeltaTime * SimpleSyncSettings.SendEveryXTick);
//				rotDef.tickAddVector = rotDef.addVector * (Time.fixedDeltaTime * SimpleSyncSettings.SendEveryXTick);
//				sclDef.tickAddVector = sclDef.addVector * (Time.fixedDeltaTime * SimpleSyncSettings.SendEveryXTick);
//			}
//		}

//		protected void InitializeTRS(TRSDefinition def, TRS type)
//		{
//			/// Absolute only applies to oscillate.. make sure its false if we aren't oscillating
//			if (movement != Movement.Oscillate)
//				def.relation = MovementRelation.Relative;

//			if (def.relation == MovementRelation.Relative && movement == Movement.Oscillate || movement == Movement.Trigger)
//			{
//				Vector3 currentVector;

//				switch (type)
//				{
//					case TRS.Position:
//						currentVector = def.local ? transform.localPosition : transform.position;
//						break;

//					case TRS.Rotation:
//						currentVector = def.local ? transform.localEulerAngles : transform.eulerAngles;
//						break;

//					default:
//						currentVector = def.local ? transform.localScale : transform.lossyScale;
//						break;
//				}

//				nodes[0].trs[(int)type] += currentVector;
//				nodes[1].trs[(int)type] += currentVector;
//			}
//		}

//		#endregion

//		#region Owner Loops

//		public void OnPreSimulate(int frameId, int subFrameId)
//		{
//			if (!isActiveAndEnabled || (pv && !pv.IsMine))
//				return;

//			/// Make sure previous lerp is fully applied to scene so our transform capture is based on the fixed time and not the last update time
//			OwnerInterpolate();
//		}

//		public void OnPreUpdate()
//		{
//			if (!isActiveAndEnabled || (pv && !pv.IsMine))
//				return;

//			OwnerInterpolate();
//		}

//		private void OwnerInterpolate()
//		{
//			/// Oscilation doesn't lerp, it applies a Sin based on time.
//			if (movement == Movement.Oscillate)
//			{
//				currentPhase = TimeToPhase(Time.time); 
//				float t = (float)OscillatePhaseToLerpT(currentPhase);
//				Oscillate(t);
//			}
//			else
//				Additive();
//		}

//		#endregion Owner Loops

//		#region Trigger Handling

//		public void Trigger(int targetNode)
//		{
//			queuedTargetNode = targetNode;
//		}
//		public void TriggerMin()
//		{

//		}

//		public void TriggerMax()
//		{

//		}

//		#endregion

//		private void TriggerLerp()
//		{

//		}

//		/// <summary>
//		/// Movement based on oscilation
//		/// </summary>
//		private void Oscillate(float lerpT)
//		{
//			var start = nodes[0];
//			var end = nodes[1];

//			var posLerped = (posDef.includeAxes == 0) ? new Vector3(0, 0, 0) : Vector3.Lerp(start.trs[0], end.trs[0], lerpT);//  currentLerpT * position.oscillateRange + position.oscillateStart;
//			var rotLerped = (rotDef.includeAxes == 0) ? new Vector3(0, 0, 0) : Vector3.Lerp(start.trs[1], end.trs[1], lerpT); //currentLerpT * rotation.oscillateRange + rotation.oscillateStart;
//			var sclLerped = (sclDef.includeAxes == 0) ? new Vector3(1, 1, 1) : Vector3.Lerp(start.trs[2], end.trs[2], lerpT); //currentLerpT * scale.oscillateRange + scale.oscillateStart;

//			ApplyOscillate(posLerped, rotLerped, sclLerped);
//		}

//		/// <summary>
//		/// Movement based on continuous addition.
//		/// </summary>
//		private void Additive()
//		{
//			/// First run needs to get caught up on time
//			if (lastUpdateTime == 0)
//				lastUpdateTime = Time.time;

//			/// Time delta since last Lerp call.
//			float normalizedDeltaTime = (Time.time - lastUpdateTime) / Time.fixedDeltaTime;
//			lastUpdateTime = Time.time;

//			/// Scale
//			transform.localScale += (sclDef.fixedAddVector * normalizedDeltaTime);

//			/// Rot
//			if (rotDef.local)
//				transform.localEulerAngles += rotDef.fixedAddVector * normalizedDeltaTime;
//			else
//				transform.eulerAngles += rotDef.fixedAddVector * normalizedDeltaTime;
//			//transform.Rotate(rotDef.fixedAddVector * normalizedDeltaTime, rotDef.local ? Space.Self : Space.World);

//			/// Pos
//			transform.Translate(posDef.fixedAddVector * normalizedDeltaTime, posDef.local ? Space.Self : Space.World);
//		}

//		#region Networking Loops

//		public void OnCaptureCurrentState(int frameId, Realm realm)
//		{
//			var frame = frames[frameId];

//			frame.targetNode = targetNode;
//			frame.phase = currentPhase;
//			frame.cphase = (uint)floatCrusher.Encode(currentPhase);
//		}

//		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
//		{
			
//			var frame = frames[frameId];

//			if (movement == Movement.Oscillate)
//				floatCrusher.WriteValue(frame.phase, buffer, ref bitposition);

//			return SerializationFlags.HasChanged;
//		}

//		public SerializationFlags OnNetDeserialize(int sourceFrameId, int originFrameId, int localFrameId, byte[] buffer, ref int bitposition)
//		{
//			var frame = frames[localFrameId];

//			//frame.phase = floatCrusher.ReadValue(buffer, ref bitposition);
//			frame.hasChanged = true;
//			if (movement == Movement.Oscillate)
//			{
//				frame.cphase = (uint)floatCrusher.ReadCValue(buffer, ref bitposition);
//				frame.phase = floatCrusher.Decode(frame.cphase);
//			}

//			return SerializationFlags.HasChanged;
//		}

//		protected override void ApplySnapshot(bool isInitial, bool isInitialComplete)
//		{
//			base.ApplySnapshot(isInitial, isInitialComplete);

//			if (snapFrame.hasChanged)
//			{
//				if (movement == Movement.Oscillate)
//				{
//					float lerpT = (float)OscillatePhaseToLerpT(snapFrame.phase);
//					Oscillate(lerpT);
//				}
//			}
//		}
		
//		public override bool OnInterpolate(int snapFrameId, int targFrameId, float t)
//		{
//			if (!base.OnInterpolate(snapFrameId, targFrameId, t))
//				return false;

//			switch (movement)
//			{
//				case Movement.Additive:
//					{
//						if (!snapFrame.hasChanged || !targFrame.hasChanged)
//						{
//							if (t < accumulatedTime)
//								accumulatedTime = 0;

//							float tt = t - accumulatedTime;
//							accumulatedTime = t;

//							/// Time delta since last Lerp call.
//							transform.localScale += (sclDef.tickAddVector * tt);
//							if (rotDef.local)
//								transform.localEulerAngles += rotDef.tickAddVector * tt;
//							else
//								transform.eulerAngles += rotDef.tickAddVector * tt;
//							//transform.Rotate(rotDef.tickAddVector * tt, rotDef.local ? Space.Self : Space.World);
//							transform.Translate(posDef.tickAddVector * tt, posDef.local ? Space.Self : Space.World);
//						}
//						break;
//					}

//				case Movement.Oscillate:
//					{
//						float targphase = targFrame.phase;
//						float snapphase = snapFrame.phase;

//						if (targphase < snapphase)
//							targphase += 1;

//						var phase = Mathf.Lerp(snapphase, targphase, t);

//						if (phase >= 1)
//							phase -= 1;

//						var lerpT = (float)OscillatePhaseToLerpT(phase);
//						Oscillate(lerpT);
//						return true;
//					}

//				case Movement.Trigger:
//					{
//						break;
//					}
//				default:
//					break;
//			}

//			return false;

//		}

//		protected float accumulatedTime;

//		protected override void ConstructMissingFrame(int frameId)
//		{
//			//base.ConstructMissingFrame(frameId);
//			switch (movement)
//			{
//				// Oscilate is deterministic in nature. So for missing frames we can turn the previous t back into a moduls time value, 
//				// and accurately predict t by adding a tick worth of time to that.
//				case Movement.Oscillate:
//					{
//						if (snapFrame.hasChanged)
//						{
//							float restoredTime = snapFrame.phase * oscillatePeriod;
//							float extrapTime = restoredTime + (SimpleSyncSettings.SendEveryXTick * Time.fixedDeltaTime);
//							targFrame.phase = TimeToPhase(extrapTime);
//							targFrame.hasChanged = true;
//							return;
//						}
//						break;
//					}

//				default:
//					break;
//			}
//		}
		
//		/// <summary>
//		/// Takes a normalized 0-1 value and returns a T value from the OscillateCurve.
//		/// </summary>
//		/// <param name="phase"></param>
//		/// <returns></returns>
//		protected float OscillatePhaseToLerpT(float phase)
//		{
//			return oscillateCurve.Evaluate(phase);
//		}

//		protected float TimeToPhase(float time)
//		{
//			return ((time % oscillatePeriod)) / oscillatePeriod;
//		}

//		#endregion Networking Loops

//		/// <summary>
//		/// Apply a value to the indicated TRS axis, leaving the axes and TRS types not indicated as they are.
//		/// </summary>
//		/// <param name="pos"></param>
//		private void ApplyOscillate(Vector3 pos, Vector3 rot, Vector3 scl)
//		{
//			var posIncludeAxes = posDef.includeAxes;
//			var rotIncludeAxes = rotDef.includeAxes;
//			var sclIncludeAxes = sclDef.includeAxes;


//			// Scale
//			if (sclIncludeAxes != AxisMask.None)
//			{
//				transform.localScale = new Vector3(
//					((sclIncludeAxes & AxisMask.X) != 0) ? scl.x : transform.localScale.x,
//					((sclIncludeAxes & AxisMask.Y) != 0) ? scl.y : transform.localScale.y,
//					((sclIncludeAxes & AxisMask.Z) != 0) ? scl.z : transform.localScale.z);
//			}

//			// Rotation
//			if (rotIncludeAxes != AxisMask.None)
//			{
//				if (rotDef.local)
//				{
//					transform.localEulerAngles = new Vector3(
//						((rotIncludeAxes & AxisMask.X) != 0) ? rot.x : transform.localEulerAngles.x,
//						((rotIncludeAxes & AxisMask.Y) != 0) ? rot.y : transform.localEulerAngles.y,
//						((rotIncludeAxes & AxisMask.Z) != 0) ? rot.z : transform.localEulerAngles.z);
//				}
//				else
//				{
//					transform.eulerAngles = new Vector3(
//						((rotIncludeAxes & AxisMask.X) != 0) ? rot.x : transform.eulerAngles.x,
//						((rotIncludeAxes & AxisMask.Y) != 0) ? rot.y : transform.eulerAngles.y,
//						((rotIncludeAxes & AxisMask.Z) != 0) ? rot.z : transform.eulerAngles.z);
//				}
//			}

//			// Position
//			if (posIncludeAxes != AxisMask.None)
//			{
//				if (posDef.local)
//				{
//					transform.localPosition = new Vector3(
//						((posIncludeAxes & AxisMask.X) != 0) ? pos.x : transform.localPosition.x,
//						((posIncludeAxes & AxisMask.Y) != 0) ? pos.y : transform.localPosition.y,
//						((posIncludeAxes & AxisMask.Z) != 0) ? pos.z : transform.localPosition.z);
//				}
//				else
//				{
//					transform.position = new Vector3(
//						((posIncludeAxes & AxisMask.X) != 0) ? pos.x : transform.position.x,
//						((posIncludeAxes & AxisMask.Y) != 0) ? pos.y : transform.position.y,
//						((posIncludeAxes & AxisMask.Z) != 0) ? pos.z : transform.position.z);
//				}
//			}

//		}

//		#region Auto Set Transform



//#if UNITY_EDITOR

//		public void AutoSetSyncTransform()
//		{
//			if (!autoSync)
//				return;

//			syncTransform = GetComponent<SyncTransform>();

//			switch (movement)
//			{
//				case Movement.Additive:
//					{
//						if (!syncTransform)
//							syncTransform = gameObject.AddComponent<SyncTransform>();

//						AutoSetSyncTransformEnablesAdditive();
//						break;
//					}
//				case Movement.Oscillate:
//					{
//						if (syncTransform)
//						{
//							Object.DestroyImmediate(syncTransform);
//							Debug.LogWarning(GetType().Name + " automatically removing SyncTransform. " + GetType().Name + " handles transform sync internally when Movement is set to Oscillate.");
//						}
//						break;
//					}
//				case Movement.Trigger:
//					{

//						break;
//					}
//				default:
//					break;
//			}
//		}

//		public void AutoSetSyncTransformEnablesAdditive()
//		{
//			var st = syncTransform;

//			// Position
//			{
//				var def = posDef;
//				var c = st.transformCrusher.PosCrusher;
//				bool local = def.local;

//				c.local = local;

//				if (local)
//				{
//					bool iszero = def.addVector.magnitude == 0;
//					c.XCrusher.Enabled = !iszero;
//					c.YCrusher.Enabled = !iszero;
//					c.ZCrusher.Enabled = !iszero;
//				}
//				else
//				{
//					var addVector = def.addVector;
//					c.XCrusher.Enabled = addVector.x != 0;
//					c.YCrusher.Enabled = addVector.y != 0;
//					c.ZCrusher.Enabled = addVector.z != 0;
//				}
//			}

//			/// Rotation
//			{
//				var def = rotDef;
//				var c = st.transformCrusher.RotCrusher;
//				bool local = def.local;

//				c.local = local;
//				bool iszero = def.addVector.magnitude == 0;

//				if (iszero || !rotDef.local)
//				{
//					c.TRSType = TRSType.Quaternion;
//					c.QCrusher.Enabled = !iszero;
//				}
//				else
//				{
//					c.TRSType = TRSType.Euler;
//					var addVector = def.addVector;

//					c.XCrusher.Enabled = local ? addVector.x != 0 : !iszero;
//					c.YCrusher.Enabled = local ? addVector.y != 0 : !iszero;
//					c.ZCrusher.Enabled = local ? addVector.z != 0 : !iszero;
//				}
//			}


//			/// Scale
//			{
//				var def = sclDef;
//				var c = st.transformCrusher.SclCrusher;

//				bool usescl = (sclDef.addVector.sqrMagnitude != 0);
//				if (usescl)
//				{
//					bool iszero = def.addVector.magnitude == 0;

//					bool local = def.local;
//					var addVector = sclDef.addVector;
//					c.uniformAxes = ElementCrusher.UniformAxes.NonUniform;
//					c.XCrusher.Enabled = local ? addVector.x != 0 : !iszero;
//					c.YCrusher.Enabled = local ? addVector.y != 0 : !iszero;
//					c.ZCrusher.Enabled = local ? addVector.z != 0 : !iszero;
//					c.local = def.local;
//				}
//				else
//				{
//					c.uniformAxes = ElementCrusher.UniformAxes.XYZ;
//					c.UCrusher.Enabled = false;
//				}
//			}
//		}
		
//#endif
//		#endregion
//	}

//#if UNITY_EDITOR

//	[System.Obsolete("Broken into two classes.")]
//	[CustomEditor(typeof(SyncMover))]
//	[CanEditMultipleObjects]
//	public class SimpleMoverEditor : SyncObjectTFrameEditor // HeaderEditorBase
//	{
//		protected override bool UseThinHeader { get { return true; } }

//		SerializedProperty posDef, rotDef, sclDef;

//		SerializedProperty
//			floatCrusher,
//			autoSync,
//			nodes,
//			movement,
//			oscillateCurve,
//			oscillatePeriod;

//		protected class TRS_SP
//		{
//			public SerializedProperty
//			addVector,
//			relation,
//			includeAxes,
//			local;
//		}

//		TRS_SP posSPs = new TRS_SP();
//		TRS_SP rotSPs = new TRS_SP();
//		TRS_SP sclSPs = new TRS_SP();

//		readonly GUIContent addVectorContent = new GUIContent("Add", "Applies this vector to the selected TRS type.");

//		const float AXIS_LAB_WID = 14f;

//		public override void OnEnable()
//		{
//			base.OnEnable();

//			nodes = serializedObject.FindProperty("nodes");
//			autoSync = serializedObject.FindProperty("autoSync");
//			movement = serializedObject.FindProperty("movement");
//			floatCrusher = serializedObject.FindProperty("floatCrusher");

//			posDef = serializedObject.FindProperty("posDef");
//			rotDef = serializedObject.FindProperty("rotDef");
//			sclDef = serializedObject.FindProperty("sclDef");
//			oscillatePeriod = serializedObject.FindProperty("oscillatePeriod");
//			oscillateCurve = serializedObject.FindProperty("oscillateCurve");

//			InitSP(posDef, posSPs);
//			InitSP(rotDef, rotSPs);
//			InitSP(sclDef, sclSPs);
//		}

//		protected void InitSP(SerializedProperty trs, TRS_SP trsSP)
//		{
//			trsSP.addVector = trs.FindPropertyRelative("addVector");
//			trsSP.relation = trs.FindPropertyRelative("relation");
//			trsSP.includeAxes = trs.FindPropertyRelative("includeAxes");
//			trsSP.local = trs.FindPropertyRelative("local");
//		}

//		public override void OnInspectorGUI()
//		{

//			base.OnInspectorGUI();

//			(target as SyncMover).AutoSetSyncTransform();
//			serializedObject.Update();

//			EditorGUI.BeginChangeCheck();

//			/// AutoSync
//			EditorGUILayout.PropertyField(autoSync);

//			/// Movement dropdown
//			EditorGUILayout.PropertyField(movement);

//			if (movement.intValue == (int)SyncMover.Movement.Oscillate)
//			{
//				EditorGUILayout.BeginVertical("HelpBox");
//				/// Rate
//				EditorGUILayout.BeginHorizontal(/*GUILayout.Width(100), GUILayout.MinWidth(75)*/);
//				EditorGUILayout.PropertyField(oscillatePeriod);
//				EditorGUILayout.LabelField(" sec(s)", GUILayout.MaxWidth(48));
//				EditorGUILayout.EndHorizontal();

//				/// Curve
//				EditorGUILayout.PropertyField(oscillateCurve);

//				/// normalized float crusher
//				EditorGUILayout.BeginVertical("HelpBox");
//				EditorGUILayout.LabelField(new GUIContent(floatCrusher.displayName, "The compressor used to serialize the normalized lerp T value"));
//				EditorGUILayout.PropertyField(floatCrusher);
//				EditorGUILayout.EndVertical();

//				EditorGUILayout.EndVertical();
//			}

//			DrawWarningBoxes();

//			DrawTRS(posSPs, TRS.Position, "Position:");
//			DrawTRS(rotSPs, TRS.Rotation, "Rotation");
//			DrawTRS(sclSPs, TRS.Scale, "Scale");

//			if (EditorGUI.EndChangeCheck())
//				serializedObject.ApplyModifiedProperties();

//		}

//		protected void DrawTRS(TRS_SP trsSP, TRS type, string label)
//		{
//			const float RANGE_LABEL_WIDTH = 42;

//			EditorGUILayout.LabelField(System.Enum.GetName(typeof(TRS), type) + ":", (GUIStyle)"BoldLabel");

//			EditorGUILayout.BeginVertical("HelpBox");

//			if ((SyncMover.Movement)movement.enumValueIndex == SyncMover.Movement.Additive)
//			{

//				EditorGUILayout.BeginHorizontal();
//				EditorGUILayout.LabelField(addVectorContent, GUILayout.MaxWidth(RANGE_LABEL_WIDTH));
//				DrawAxes(trsSP.addVector, AxisMask.XYZ);
//				EditorGUILayout.LabelField("/sec", GUILayout.MaxWidth(32));
//				EditorGUILayout.EndHorizontal();

//				/// Local
//				EditorGUI.BeginDisabledGroup(type == TRS.Scale);
//				EditorGUILayout.BeginHorizontal();
//				EditorGUILayout.LabelField("Local", GUILayout.MaxWidth(RANGE_LABEL_WIDTH));
//				EditorGUILayout.GetControlRect(GUILayout.MaxWidth(AXIS_LAB_WID));
//				EditorGUILayout.PropertyField(trsSP.local, GUIContent.none);
//				EditorGUILayout.EndHorizontal();
//				EditorGUI.EndDisabledGroup();
//			}
//			else
//			{
//				/// Restrict
//				EditorGUILayout.PropertyField(trsSP.includeAxes);

//				if (trsSP.includeAxes.intValue != 0)
//				{
//					/// Relation
//					EditorGUILayout.PropertyField(trsSP.relation);

//					/// Start
//					EditorGUILayout.BeginHorizontal();
//					EditorGUILayout.LabelField("Start:", GUILayout.MaxWidth(RANGE_LABEL_WIDTH));
//					DrawAxes(nodes.GetArrayElementAtIndex(0).FindPropertyRelative("trs").GetArrayElementAtIndex((int)type), (AxisMask)trsSP.includeAxes.enumValueIndex);
//					EditorGUILayout.EndHorizontal();

//					/// End
//					EditorGUILayout.BeginHorizontal();
//					EditorGUILayout.LabelField("End:", GUILayout.MaxWidth(RANGE_LABEL_WIDTH));
//					DrawAxes(nodes.GetArrayElementAtIndex(1).FindPropertyRelative("trs").GetArrayElementAtIndex((int)type), (AxisMask)trsSP.includeAxes.enumValueIndex);
//					EditorGUILayout.EndHorizontal();

//					/// Local
//					EditorGUI.BeginDisabledGroup(type == TRS.Scale);
//					EditorGUILayout.BeginHorizontal();
//					EditorGUILayout.LabelField("Local", GUILayout.MaxWidth(RANGE_LABEL_WIDTH));
//					EditorGUILayout.GetControlRect(GUILayout.MaxWidth(AXIS_LAB_WID));
//					EditorGUILayout.PropertyField(trsSP.local, GUIContent.none);
//					EditorGUILayout.EndHorizontal();
//					EditorGUI.EndDisabledGroup();
//				}
//			}
//			EditorGUILayout.EndVertical();
//		}

//		protected void DrawAxes(SerializedProperty v3, AxisMask axis)
//		{
//			const float FLOAT_WIDTH = 10f;
//			var oldval = v3.vector3Value;

//			float x, y, z;

//			bool usex = (axis & AxisMask.X) != 0;
//			bool usey = (axis & AxisMask.Y) != 0;
//			bool usez = (axis & AxisMask.Z) != 0;

//			/// X
//			EditorGUI.BeginDisabledGroup(!usex);
//			EditorGUILayout.LabelField(" x", GUILayout.MaxWidth(AXIS_LAB_WID));
//			float newx = EditorGUILayout.DelayedFloatField(oldval.x, GUILayout.MinWidth(FLOAT_WIDTH));
//			x = (usex) ? newx : 0;
//			EditorGUI.EndDisabledGroup();

//			/// Y
//			EditorGUI.BeginDisabledGroup(!usey);
//			EditorGUILayout.LabelField(" y", GUILayout.MaxWidth(AXIS_LAB_WID));
//			float newy = EditorGUILayout.DelayedFloatField(oldval.y, GUILayout.MinWidth(FLOAT_WIDTH));
//			y = (usey) ? newy : 0;
//			EditorGUI.EndDisabledGroup();


//			/// Z
//			EditorGUI.BeginDisabledGroup(!usez);
//			EditorGUILayout.LabelField(" z", GUILayout.MaxWidth(AXIS_LAB_WID));
//			float newz = EditorGUILayout.DelayedFloatField(oldval.z, GUILayout.MinWidth(FLOAT_WIDTH));
//			z = (usez) ? newz : 0;
//			EditorGUI.EndDisabledGroup();

//			var newval = new Vector3(x, y, z);
//			if (v3.vector3Value != newval)
//				v3.vector3Value = newval;
//		}

//		protected void DrawWarningBoxes()
//		{
//			var _target = target as SyncMover;

//			#region Warning Boxes

//			var isynctrans = _target.GetComponent<ISyncTransform>();
//			_target.GetOrAddNetObj();

//			if (movement.intValue != (int)SyncMover.Movement.Additive)
//			{
//				if (!ReferenceEquals(isynctrans, null))
//				{
//					EditorGUILayout.HelpBox((isynctrans as Component).GetType().Name + " on GameObject '" + _target.name +
//						"' may conflict with the internal transform syncing of " + target.GetType().Name + ". Be sure it is not syncing any axes this component is controlling.", MessageType.Warning);
//				}
//			}
//			else
//			{
//				if (ReferenceEquals(isynctrans, null) && _target.NetObj)
//				{
//					EditorGUILayout.HelpBox(
//						"Additive motion isn't deterministic and is not synced internally by " + target.GetType().Name + ". A " + typeof(SyncTransform).Name +
//						" should be added.", MessageType.Warning);
//				}
//			}

//			if (!_target.NetObj)
//			{
//				EditorGUILayout.HelpBox(
//					"This GameObject does not have a " + typeof(NetObject).Name + ". Motion will be applied locally without networking.", MessageType.Info);
//			}

//			#endregion
//		}

//	}



//#endif
//}

