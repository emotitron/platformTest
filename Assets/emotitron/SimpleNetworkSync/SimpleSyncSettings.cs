using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{


#if UNITY_EDITOR
	[HelpURL(HELP_URL)]
#endif

	public class SimpleSyncSettings : SettingsScriptableObject<SimpleSyncSettings>
	{
		public const string MENU_PATH = "Window/PUN2 Simple Sync/";

		public enum FrameCountEnum { FrameCount12 = 12, FrameCount30 = 30, FrameCount60 = 60, FrameCount120 = 120 }
		public enum BufferCorrection { Manual, Auto }

#if UNITY_EDITOR
		public const string HELP_URL = "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.fd0tg2ybh584";
		public override string HelpURL { get { return HELP_URL; } }
		public static string instructions = "";

		[MenuItem(MENU_PATH + "Simple Sync Settings", false, 1)]
		private static void PingSettings()
		{
			Selection.activeObject = Single;
			EditorGUIUtility.PingObject(single);
		}

		[UnityEditor.InitializeOnLoadMethod]
		public static void InitializeOnLoad()
		{
#if !PUN_2_OR_NEWER
			Debug.LogWarning("<b>PhotonEngine PUN2 NOT INSTALLED!</b>");
#endif
		}

#endif

		#region Inspector Items

#if SNS_DEV
		public bool enabled = true;
#endif


		[Tooltip("The size of the circular buffer.")]
		[SerializeField] [HideInInspector]
		private FrameCountEnum _frameCount = FrameCountEnum.FrameCount30;

		/// cached runtime frame count values
		//private static int frameCount;
		public static int FrameCount { get { return (int)Single._frameCount; } }
		//private static int halfFrameCount;
		public static int HalfFrameCount { get { return (int)Single._frameCount / 2; } }
		//private static int thirdFrameCount;
		public static int ThirdFrameCount { get { return (int)Single._frameCount / 3; } }
		//private static int quaterFrameCount;
		public static int QuaterFrameCount { get { return (int)Single._frameCount / 4; } }
		//private static int frameCountBits;
		public static int FrameCountBits { get { return Compression.FloatCrusher.GetBitsForMaxValue((uint)Single._frameCount); ; } }
		public static float NetTickInterval { get { return Time.fixedDeltaTime * Single._sendEveryXTick; } }
		public static float NetTickIntervalInv { get { return 1f / (Time.fixedDeltaTime * Single._sendEveryXTick); } }
		public static float TargetBufferInterval { get { return Time.fixedDeltaTime * single._sendEveryXTick * TargetBufferSize; } }

		[SerializeField] [HideInInspector]
		private BufferCorrection _bufferCorrection = BufferCorrection.Manual;
		public static BufferCorrection bufferCorrection;

		[Tooltip("Target size of the frame buffer. This is the number of frames in the buffer that is considered ideal. " +
			"The lower the number the less latency, but with a greater risk of buffer underruns that lead to extrapolation/hitching.")]
		[SerializeField] [HideInInspector] private int _targetBufferSize = 2;
		public static int TargetBufferSize { get { return Single._targetBufferSize; } }

		[Tooltip("Buffer sizes above this value wll be considered to be excessive, and will trigger multiple frames being processed to shrink the buffer.")]
		[SerializeField] [HideInInspector] private int _maxBufferSize = 3;
		public static int maxBufferSize;

		[Tooltip("Buffer sizes below this value will trigger the frames to hold for extra ticks in order to grow the buffer.")]
		[SerializeField] [HideInInspector] private int _minBufferSize = 1;
		public static int minBufferSize;

		[Tooltip("The number of ticks a buffer will be allowed to be below the the Min Buffer Size before starting to correct. " +
			"This value prevents overreaction to network hiccups and allows for a few ticks before applying harsh corrections. Ideally this value will be larger than Ticks Before Shrink.")]
		[SerializeField] [HideInInspector] private int _ticksBeforeGrow = 8;
		public static int TicksBeforeGrow {get { return Single._ticksBeforeGrow; } }

		[Tooltip("The number of ticks a buffer will be allowed to exceed Max Buffer Size before starting to correct. " +
			"This value prevents overreaction to network hiccups and allows for a few ticks before applying harsh corrections. Ideally this value will be smaller than Ticks Before Grow.")]
		[SerializeField] [HideInInspector] private int _ticksBeforeShrink = 5;
		public static int TicksBeforeShrink { get { return Single._ticksBeforeShrink; } }

		/// <summary>
		/// Get 1/3rd the value of the current frameCount setting. Do not hotpath this method please.
		/// </summary>
		public static int MaxKeyframes
		{
			get { return (int)Single._frameCount / 3; }
		}

		[Tooltip("States are sent post PhysX/FixedUpdate. Setting this to a value greater than one reduces these sends by only sending every X fixed tick.\n1 = Every Tick\n2 = Every Other\n3 = Every Third, etc.")]
		[SerializeField][HideInInspector]
		private int _sendEveryXTick = 3;
		public static int SendEveryXTick { get { return Single._sendEveryXTick; } }

		[Space(4)]
#if UNITY_EDITOR
		public bool showGUIHeaders = true;
#endif
		[Header("Code Generation")]
		[Tooltip("Enables the codegen for PackObjects / PackAttributes. Disable this if you would like to suspend codegen. Existing codegen will remain, unless it produces errors.")]
		public bool enableCodegen = true;

		[Tooltip("Automatically deletes codegen if it produces any compile errors. Typically you will want to leave this enabled.")]
		public bool deleteBadCode = true;


		#endregion

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Bootstrap()
		{

			var single = Single;

			bufferCorrection = single._bufferCorrection;

			if (bufferCorrection == BufferCorrection.Manual)
			{
				//targetBufferSize = single._targetBufferSize;
				minBufferSize = single._minBufferSize;
				maxBufferSize = single._maxBufferSize;
			}
			//else if	(bufferCorrection == BufferCorrection.Auto)
			//{
			//	//targetBufferSize = single._targetBufferSize;
			//	minBufferSize = single._minBufferSize;
			//	maxBufferSize = single._maxBufferSize;
			//}

			//frameCount = (int)single._frameCount;
			//halfFrameCount = frameCount / 2;
			//thirdFrameCount = frameCount / 3;
			//quaterFrameCount = frameCount / 4;

			//frameCountBits = Compression.FloatCrusher.GetBitsForMaxValue((uint)frameCount);
		}


#if UNITY_EDITOR



		public static void DrawGuiStatic(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{
			Single.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);
		}
		public override bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{
			EditorGUI.BeginChangeCheck();

			bool isexpanded = base.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);

			SerializedObject soTarget = new SerializedObject(Single);

			if (isexpanded)
			{

				if (GUI.Button(EditorGUILayout.GetControlRect(), "Regenerate All Code"))
				{
					Compression.Internal.TypeCatalogue.RebuildSNSCodegen();
				}


				SerializedProperty frameCount = soTarget.FindProperty("_frameCount");
				SerializedProperty _sendEveryXTick = soTarget.FindProperty("_sendEveryXTick");
				SerializedProperty bufferCorrection = soTarget.FindProperty("_bufferCorrection");

				//EditorGUILayout.HelpBox("Global settings for Simple Network Sync.", MessageType.None);

				EditorGUILayout.LabelField("Ring Buffer", (GUIStyle)"BoldLabel");

				/// Limit sendEveryX inspector value to 4 if settings frameCount is 12. 5+ will not factor.
				if (bufferCorrection.enumValueIndex == (int)BufferCorrection.Manual && frameCount.intValue == 12 && _sendEveryXTick.intValue > 4)
				{
					_sendEveryXTick.intValue = 4;
				}

				EditorGUILayout.IntSlider(_sendEveryXTick, 1, 12);
				
				EditorGUILayout.PropertyField(bufferCorrection);

				if (bufferCorrection.enumValueIndex == (int)BufferCorrection.Manual)
				{

					EditorGUILayout.PropertyField(frameCount);
					DrawBufferSizes(soTarget);
				}
				else
				{
					AutoSetBuffer(frameCount, _sendEveryXTick);
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.PropertyField(frameCount);
					DrawBufferSizes(soTarget);
					EditorGUI.EndDisabledGroup();
				}

				float secondsOfBuffer = Time.fixedDeltaTime * _sendEveryXTick.intValue * frameCount.intValue;
				float secondsOfHalfBuffer = secondsOfBuffer * .5f;
				float fixrate = Time.fixedDeltaTime;
				float netrate = Time.fixedDeltaTime * _sendEveryXTick.intValue;
				float bufferTargSecs = netrate * _targetBufferSize;
				EditorGUILayout.HelpBox(
					"Fixed/Sim Tick Rate:\n" +
					fixrate.ToString("G4") + " ms (" + (1 / fixrate).ToString("G4") + " ticks per sec)\n\n" +
					"Net Tick Rate:\n" +
					netrate.ToString("G4") + " ms (" + (1 / netrate).ToString("G4") + " ticks per sec)\n\n" +

					bufferTargSecs.ToString("0.000") + " ms target buffer size\n\n" +
					secondsOfBuffer.ToString("0.000") + " secs Buffer Total\n" +
					secondsOfHalfBuffer.ToString("0.000") + " secs Buffer Look Ahead\n" +
					secondsOfHalfBuffer.ToString("0.000") + " secs Buffer History"
					, MessageType.None);

				if (secondsOfBuffer < 1)
					EditorGUILayout.HelpBox("Warning: Less than 1 Second of buffer may break catastrophically for users with high pings. Increase FrameCount, increase SendEveryX value, or reduce the physics/fixed rate to make the buffer larger.", MessageType.Warning);

			}

			if (EditorGUI.EndChangeCheck())
			{
				soTarget.ApplyModifiedProperties();
				AssetDatabase.SaveAssets();
			}
			return isexpanded;
		}

		private void AutoSetBuffer(SerializedProperty frameCount, SerializedProperty sendEveryX)
		{
			float secondsPerTick = Time.fixedDeltaTime * sendEveryX.intValue;

			float framesNeeded = 1 / secondsPerTick;


			if (framesNeeded < (float)FrameCountEnum.FrameCount12)
				frameCount.intValue = (int)FrameCountEnum.FrameCount12;

			else if (framesNeeded < (float)FrameCountEnum.FrameCount30)
				frameCount.intValue = (int)FrameCountEnum.FrameCount30;

			else if (framesNeeded < (float)FrameCountEnum.FrameCount60)
				frameCount.intValue = (int)FrameCountEnum.FrameCount60;

			else
				frameCount.intValue = (int)FrameCountEnum.FrameCount120;

			//Debug.Log("Frames needed for 1 sec " + framesNeeded + " : " + secondsPerTick + " frameCount: " + FrameCount + " fixed: " + Time.fixedDeltaTime + " " + sendEveryX.intValue);

		}



		private void DrawBufferSizes(SerializedObject soTarget)
		{
			SerializedProperty _trgSize = soTarget.FindProperty("_targetBufferSize");
			SerializedProperty _min = soTarget.FindProperty("_minBufferSize");
			SerializedProperty _max = soTarget.FindProperty("_maxBufferSize");
			SerializedProperty _ticksBeforeGrow = soTarget.FindProperty("_ticksBeforeGrow");
			SerializedProperty _ticksBeforeShrink = soTarget.FindProperty("_ticksBeforeShrink");


			EditorGUILayout.BeginVertical(/*"HelpBox"*/);

			int bufferLimit = (int)_frameCount / 3;
			EditorGUILayout.IntSlider(_trgSize, 0, bufferLimit);

			if (_trgSize.intValue < 1) _trgSize.intValue = 1;
			if (_trgSize.intValue >= bufferLimit) _trgSize.intValue = bufferLimit;

			if (_min.intValue > _trgSize.intValue) _min.intValue = _trgSize.intValue;
			if (_max.intValue < _trgSize.intValue) _max.intValue = _trgSize.intValue;

			if (_max.intValue > bufferLimit) _max.intValue = bufferLimit;

			float min = _min.intValue;
			float max = _max.intValue;

			/// Min/Max slider row
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.MinMaxSlider("Buffer Min/Max", ref min, ref max, 0, bufferLimit);
			min = EditorGUILayout.DelayedIntField(GUIContent.none, (int)min, GUILayout.MaxWidth(23), GUILayout.MinWidth(23));
			max = EditorGUILayout.DelayedIntField(GUIContent.none, (int)max, GUILayout.MaxWidth(23), GUILayout.MinWidth(23));
			EditorGUILayout.EndHorizontal();

			_min.intValue = (int)min;
			_max.intValue = (int)max;

			if (_min.intValue > _trgSize.intValue) _min.intValue = _trgSize.intValue;
			if (_max.intValue < _trgSize.intValue) _max.intValue = _trgSize.intValue;

			EditorGUILayout.IntSlider(_ticksBeforeGrow, 1, 12);
			EditorGUILayout.IntSlider(_ticksBeforeShrink, 1, 12);

			EditorGUILayout.EndVertical();
		}

#endif

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SimpleSyncSettings))]
	[CanEditMultipleObjects]
	public class SimpleSyncSettingsEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			SimpleSyncSettings.Single.DrawGui(target, false, false, true);
		}
	}
#endif
}
