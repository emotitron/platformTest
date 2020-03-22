using emotitron.Utilities.Networking;
using UnityEngine;
using emotitron.Utilities.GhostWorlds;
using emotitron.Compression;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	/// <summary>
	/// Automatically generates ChangeState events on the SyncState based on timers and triggers.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SyncState))]
	public class SyncSpawnTimer : SyncObject<SyncSpawnTimer.Frame>
		, ISerializationOptional
		, IUseKeyframes
		, IOnSnapshot
		, IOnCaptureState
		, IOnStateChange
	{

		public override int ApplyOrder { get { return ApplyOrderConstants.STATES - 1; } }

		#region Inspector

		[HideInInspector] [SerializeField] public bool respawnEnable = true;
		[EnumMask(true)]
		[HideInInspector] [SerializeField] public ObjState despawnOn = ObjState.Attached;
		[Tooltip("Number of seconds after respawn trigger before respawn occurs.")]
		[HideInInspector] [SerializeField] public float despawnDelay = 5f;


		[HideInInspector] [SerializeField] public bool despawnEnable = false;
		[EnumMask(true)]
		[HideInInspector] [SerializeField] public ObjState respawnOn = ObjState.Despawned;
		[Tooltip("Number of seconds after respawn trigger before respawn occurs.")]
		[HideInInspector] [SerializeField] public float respawnDelay = 5f;

		#endregion Inspector

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			serializeThis = false;
		}
#endif

		// Current state
		[System.NonSerialized] protected int ticksUntilRespawn = -1;
		[System.NonSerialized] protected int ticksUntilDespawn = -1;

		// Cached values
		[System.NonSerialized] protected int respawnWaitAsTicks;
		[System.NonSerialized] protected int despawnWaitAsTicks;

		protected int bitsForTicksUntilRespawn;
		protected int bitsForTicksUntilDespawn;

		#region Frame

		public class Frame : FrameBase
		{
			public int ticksUntilRespawn;
			public int ticksUntilDespawn;

			public Frame() : base() { }

			public Frame(int frameId) : base(frameId) { }

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);
				Frame src = sourceFrame as Frame;
				ticksUntilRespawn = src.ticksUntilRespawn;
				ticksUntilDespawn = src.ticksUntilDespawn;
			}

			public bool Compare(Frame otherFrame)
			{
				return 
					ticksUntilRespawn == otherFrame.ticksUntilRespawn &&
					ticksUntilDespawn == otherFrame.ticksUntilDespawn;
			}
		}

		#endregion Frame

		#region Startup

		public override void OnAwake()
		{
			base.OnAwake();

			respawnWaitAsTicks = ConvertSecsToTicks(respawnDelay);
			despawnWaitAsTicks = ConvertSecsToTicks(despawnDelay);

			bitsForTicksUntilRespawn =  FloatCrusher.GetBitsForMaxValue((uint)respawnWaitAsTicks);
			bitsForTicksUntilDespawn =  FloatCrusher.GetBitsForMaxValue((uint)despawnWaitAsTicks);
		}

		public override void OnStart()
		{
			base.OnStart();
		}

		#endregion Startup

		protected ObjState prevState = ObjState.Despawned;
		/// <summary>
		/// Responds to State change from SyncState
		/// </summary>
		public void OnStateChange(ObjState state, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
		{
			
			if (state == prevState)
				return;

			if (IsMine)
			{
				if (respawnEnable)
				{
					if (state == ObjState.Despawned)
					{
						ticksUntilRespawn = respawnWaitAsTicks;
					}
					/// Check if the flag we are looking for just changed to true
					else if ((prevState & respawnOn) == 0 && (state & respawnOn) != 0)
					{
						ticksUntilRespawn = respawnWaitAsTicks;
					}
				}

				if (despawnEnable)
				{

					/// Check if the flag we are looking for just changed to true
					if ((prevState & despawnOn) == 0 && (state & despawnOn) != 0)
					{
						ticksUntilDespawn = despawnWaitAsTicks;
					}
				}
			}
			

			prevState = state;

		}

		public virtual void OnCaptureCurrentState(int frameId, Realm realm)
		{
			//if (GetComponent<SyncPickup>())
			//	Debug.LogError(name + " " + ticksUntilRespawn);

			Frame frame = frames[frameId];

			/// First check for a respawn - this may belong in post or pre sim, but here for now
			if (respawnEnable)
			{
				if (ticksUntilRespawn == 0)
				{
					//Debug.Log(Time.time + " " + name + " Respawn");
					syncState.Respawn(false);
				}
				ticksUntilRespawn--;

				frame.ticksUntilRespawn = ticksUntilRespawn;
			}

			if (despawnEnable)
			{
				if (ticksUntilDespawn == 0)
				{
					//Debug.Log(Time.time + " " + name + " Despawn");
					syncState.Despawn(false);
				}
				ticksUntilDespawn--;
				frame.ticksUntilDespawn = ticksUntilDespawn;
			}

		}

		#region Serialization

		public virtual SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			/// TODO: This is ignoring keyframe setting
			
			Frame frame = frames[frameId];
			SerializationFlags flags = SerializationFlags.None;

			bool iskeyframe = IsKeyframe(frameId);

			if (!iskeyframe)
				return flags;

			if (respawnEnable)
			{
				/// Respawn
				int ticks = frame.ticksUntilRespawn;

				if (ticks >= 0)
				{
					/// non -1 counter bool
					buffer.WriteBool(true, ref bitposition);
					buffer.Write((ulong)ticks, ref bitposition, bitsForTicksUntilRespawn);
					flags |= SerializationFlags.HasChanged;
				}
				else
				{
					/// non -1 counter bool
					buffer.WriteBool(false, ref bitposition);
				}
			}

			if (despawnEnable)
			{
				/// Despawn
				int ticks = frame.ticksUntilDespawn;
				if (ticks >= 0)
				{
					/// non -1 counter bool
					buffer.WriteBool(true, ref bitposition);
					buffer.Write((ulong)ticks, ref bitposition, bitsForTicksUntilDespawn);

					flags |= SerializationFlags.HasChanged;
				}
				else
				{
					/// non -1 counter bool
					buffer.WriteBool(false, ref bitposition);
				}
			}

			return flags;
		}

		public virtual SerializationFlags OnNetDeserialize(int sourceFrameId, int originFrameId, int localFrameId, byte[] buffer, ref int bitposition)
		{
			Frame frame = frames[localFrameId];
			SerializationFlags flags = SerializationFlags.None;

			bool iskeyframe = IsKeyframe(originFrameId);
			if (!iskeyframe)
			{
				frame.content = FrameContents.Empty;
				return flags;
			}

			if (respawnEnable)
			{
				/// Read ticksToRespawn
				if (buffer.ReadBool(ref bitposition))
				{
					frame.ticksUntilRespawn = (int)buffer.Read(ref bitposition, bitsForTicksUntilRespawn);
					flags |= SerializationFlags.HasChanged;
				}
				else
					frame.ticksUntilRespawn = -1;
			}

			if (despawnEnable)
			{
				if (buffer.ReadBool(ref bitposition))
				{
					frame.ticksUntilDespawn = (int)buffer.Read(ref bitposition, bitsForTicksUntilDespawn);
					flags |= SerializationFlags.HasChanged;
				}
				else
					frame.ticksUntilDespawn = -1;
			}

			frame.content = FrameContents.Complete;
			return flags;
		}

		#endregion Serialization

		protected override void ApplySnapshot(bool isInitial, bool isInitialComplete)
		{
			/// Predict respawn if we didn't get the frame where it would have happened.
			if (snapFrame.content == FrameContents.Empty)
			{
				//Debug.Log("snap no change");
				if (respawnEnable)
				{
					ticksUntilRespawn--;
					targFrame.ticksUntilRespawn = ticksUntilRespawn;
				}
				if (despawnEnable)
				{
					ticksUntilDespawn--;
					targFrame.ticksUntilDespawn = ticksUntilDespawn;
				}
			}
			else
			{
				//Debug.Log("snap change  tr: " + snapFrame.ticksUntilRespawn + " td:" + snapFrame.ticksUntilDespawn);
				if (respawnEnable)
					ticksUntilRespawn = snapFrame.ticksUntilRespawn;
				if (despawnEnable)
					ticksUntilDespawn = snapFrame.ticksUntilDespawn;
			}

			if (respawnEnable && ticksUntilRespawn == 0)
			{
				syncState.Respawn(false);
				//Debug.Log("snap change  tr: " + snapFrame.ticksUntilRespawn + " td:" + snapFrame.ticksUntilDespawn);
			}

			if (despawnEnable && ticksUntilDespawn == 0)
			{
				syncState.Despawn(false);
				//Debug.Log("snap change  tr: " + ticksUntilRespawn + ":" + snapFrame.ticksUntilRespawn + " td: " + ticksUntilRespawn + " " + snapFrame.ticksUntilDespawn);
			}

			

		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncSpawnTimer))]
	[CanEditMultipleObjects]
	public class SyncSpawnTimerEditor : SyncObjectTFrameEditor
	{

		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=kix.jhzrf1gc51s6";
			}
		}
		
		protected override bool UseThinHeader { get { return true; } }

		protected override string Instructions
		{
			get
			{
				return "Responds to " + typeof(IOnStateChange).Name + " callbacks, and produces Spawn/Despawn calls to " + typeof(SyncState).Name + ".";
			}
		}

		protected const int BOX_PAD = 4;
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (spawnBox == null)
				spawnBox = new GUIStyle("HelpBox") { padding = new RectOffset(BOX_PAD, BOX_PAD, BOX_PAD, BOX_PAD) };

			EditorGUI.BeginChangeCheck();

			DrawBox(new GUIContent("Respawn Trigger"), serializedObject.FindProperty("respawnEnable"), serializedObject.FindProperty("respawnOn"), serializedObject.FindProperty("respawnDelay"));
			DrawBox(new GUIContent("Despawn Trigger"), serializedObject.FindProperty("despawnEnable"), serializedObject.FindProperty("despawnOn"), serializedObject.FindProperty("despawnDelay"));

			if (EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();
		}

		protected static GUIStyle spawnBox;

		protected virtual void DrawBox(GUIContent label, SerializedProperty enabled, SerializedProperty p, SerializedProperty delay)
		{
			var lwidth = GUILayout.MaxWidth((EditorGUIUtility.labelWidth - BOX_PAD) - 4);

			EditorGUILayout.BeginVertical(spawnBox);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, lwidth);
			EditorGUILayout.PropertyField(enabled, GUIContent.none, GUILayout.MinWidth(42));
			EditorGUILayout.EndHorizontal();

			if (enabled.boolValue)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(new GUIContent("Trigger On", p.tooltip), lwidth);
				EditorGUILayout.PropertyField(p, GUIContent.none);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(new GUIContent("Delay", delay.tooltip), lwidth);
				EditorGUILayout.PropertyField(delay, GUIContent.none, GUILayout.MinWidth(42));
				EditorGUILayout.LabelField(new GUIContent("Secs"), GUILayout.Width(42));
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndVertical();
		}

	}
#endif
}
