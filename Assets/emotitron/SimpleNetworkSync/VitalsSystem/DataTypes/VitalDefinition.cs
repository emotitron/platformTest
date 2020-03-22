//Copyright 2019, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;
using emotitron.Compression;
using emotitron.Utilities.Networking;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	[System.Serializable]
	public class VitalDefinition
	//: IVitalDefinition
	{

		#region Outgoing OnChange Callbacks

		public List<IOnVitalChange> iOnVitalChange = new List<IOnVitalChange>();
		public void AddIOnVitalChange(IOnVitalChange cb)
		{
			iOnVitalChange.Add(cb);
		}
		public void RemoveIOnVitalChange(IOnVitalChange cb)
		{
			iOnVitalChange.Remove(cb);
		}

		#endregion

		#region Inspector Items

		[SerializeField] private VitalNameType vitalName;
		public VitalNameType VitalName { get { return vitalName; } }

		//public string Name { get { return targetVital.name; } }

		[Tooltip("Values greater than this will degrade at the decay rate until this value is reached.")]
		[SerializeField] private float _fullValue;
		public float FullValue { get { return _fullValue; } }

		[Tooltip("The absolute greatest possible value. Values above Full Value are considered overloaded, and will decay down to Full Value.")]
		[SerializeField] private uint _maxValue;
		public float MaxValue { get { return _maxValue; } }

		public float startValue;

		[Tooltip("Number of simulation ticks after damage until regeneration resumes.")]
		[SerializeField] private float regenDelay;


		[Tooltip("Amount per tick values less than Full Value will increase until Full Health is reached.")]
		[SerializeField] private float regenRate;

		[Tooltip("Number of simulation ticks after overload until decay resumes.")]
		[SerializeField] private float decayDelay;

		[Tooltip("Amount per tick overloaded values greater than Full Value will degrade until Full Health is reached.")]
		[SerializeField] private float decayRate;

		[Range(0, 1)]
		[Tooltip("How much of the damage this vital absords, the remainder is passed through to the next lower stat. 0 = None (useless), 0.5 = Half, 1 = Full. The root vital (0) likely should always be 1.")]
		[SerializeField] private float absorption;
		public float Absorbtion { get { return absorption; } }

		#endregion

		int _decayDelayInTicks;
		public int DecayDelayInTicks { get { return _decayDelayInTicks; } }
		int _regenDelayInTicks;
		public int RegenDelayInTicks { get { return _regenDelayInTicks; } }

		float _decayPerTick;
		public float DecayPerTick { get { return _decayPerTick; } }
		float _regenPerTick;
		public float RegenPerTick { get { return _regenPerTick; } }

		// Networking cache
		private int bitsForValue;
		private int bitsForDecayDelay;
		private int bitsForRegenDelay;

		//public VitalDefinition()
		//{
		//	_fullValue = 100;
		//	_maxValue = 125;
		//	startValue = 100;
		//	absorption = 1;
		//	regenDelay = 1;
		//	regenRate = 1;
		//	decayDelay = 1;
		//	decayRate = 1;
		//	vitalName = new VitalNameType(VitalType.None);
		//}

		public VitalDefinition(float fullValue, uint maxValue, float startValue, float absorbtion, float regenDelay, float regenRate, float decayDelay, float decayRate, string name)
		{
			this._fullValue = fullValue;
			this._maxValue = maxValue;
			this.startValue = startValue;
			this.absorption = absorbtion;
			this.regenDelay = regenDelay;
			this.regenRate = regenRate;
			this.decayDelay = decayDelay;
			this.decayRate = decayRate;
			vitalName = new VitalNameType(name);
		}

		public void Initialize(float tickDuration)
		{
			SetTickInterval(tickDuration);
		}

		public void SetTickInterval(float tickInterval)
		{
			// Convert seconds into ticks for a more deterministic and networkable count
			_decayDelayInTicks = (int)(decayDelay / tickInterval);
			_regenDelayInTicks = (int)(regenDelay / tickInterval);
			_decayPerTick = decayRate * tickInterval;
			_regenPerTick = regenRate * tickInterval;

			bitsForValue = FloatCrusher.GetBitsForMaxValue((uint)_maxValue);
			bitsForDecayDelay = FloatCrusher.GetBitsForMaxValue((uint)_decayDelayInTicks);
			bitsForRegenDelay = FloatCrusher.GetBitsForMaxValue((uint)_regenDelayInTicks);
		}

		public VitalData GetDefaultData()
		{
			return new VitalData(startValue, _decayDelayInTicks, _regenDelayInTicks);
		}

		public SerializationFlags Serialize(VitalData vitalData, VitalData prevVitalData, byte[] buffer, ref int bitposition, bool keyframe = true)
		{
			var ticksuntildecay = vitalData.ticksUntilDecay;
			var ticksuntilregen = vitalData.ticksUntilRegen;

			//if (keyframe)
			//{
			//	buffer.Write((ulong)vitalData.Value, ref bitposition, bitsForValue);
			//	return SerializationFlags.HasChanged;
			//}
			//else
			int newval = (int)vitalData.Value;
			int prevval = (int)prevVitalData.Value;

			bool haschanged = newval != prevval;

			if (keyframe)
			{
				buffer.Write((ulong)newval, ref bitposition, bitsForValue);
			}
			else
			{
				buffer.WriteBool(haschanged, ref bitposition);
				if (haschanged)
					buffer.Write((ulong)newval, ref bitposition, bitsForValue);
			}

			/// We always write the ticks until, even if we are marking this as having no content.
			if (ticksuntildecay > 0)
			{
				buffer.WriteBool(true, ref bitposition);
				buffer.Write((ulong)ticksuntildecay, ref bitposition, bitsForDecayDelay);
			}
			else
				buffer.WriteBool(false, ref bitposition);

			if (ticksuntilregen > 0)
			{
				buffer.WriteBool(true, ref bitposition);
				buffer.Write((ulong)ticksuntilregen, ref bitposition, bitsForRegenDelay);
			}
			else
				buffer.WriteBool(false, ref bitposition);

			return (haschanged || keyframe) ? SerializationFlags.HasChanged : SerializationFlags.None;

		}

		public VitalData Deserialize(byte[] buffer, ref int bitposition, bool keyframe = true)
		{
			float val = (keyframe || buffer.ReadBool(ref bitposition)) ? (float)buffer.Read(ref bitposition, bitsForValue) : float.NegativeInfinity;
			VitalData data = new VitalData(
				val,
				(int)(buffer.ReadBool(ref bitposition) ? buffer.Read(ref bitposition, bitsForDecayDelay) : 0),
				(int)(buffer.ReadBool(ref bitposition) ? buffer.Read(ref bitposition, bitsForRegenDelay) : 0)
			);

			return data;
		}

		public VitalData Extrapolate(VitalData prev)
		{
			int ticksuntilregen = prev.ticksUntilRegen > 0 ? prev.ticksUntilRegen - 1 : 0;
			int ticksuntildecay = prev.ticksUntilDecay > 0 ? prev.ticksUntilDecay - 1 : 0;
			float preval = prev.Value;
			float val =
				(preval > FullValue && ticksuntildecay == 0) ? preval - _decayPerTick :
				(preval < FullValue && ticksuntilregen == 0) ? preval + _regenPerTick :
				preval;

			return new VitalData(val, ticksuntildecay, ticksuntilregen);
		}


	}


#if UNITY_EDITOR

	/// <summary>
	/// This vital will draw a vital, and will include add/destroy buttons if it is part of a list and its parent has the IVitals interface.
	/// </summary>
	[CustomPropertyDrawer(typeof(VitalDefinition))]
	[CanEditMultipleObjects]
	public class VitalDefinitionDrawer : PropertyDrawer
	{
#if UNITY_2019_3_OR_NEWER
		public const float HEIGHT = 18f;
#else
		public const float HEIGHT = 16f;
#endif
		public const float FIXED_HGHT = HEIGHT * 9 + 14; // 164f;

		public const float INDENT = 4f;

		public const float PAD = 4f;
		const float TOP_BAR = HEIGHT * 3 + PAD + 2;
		public static string[] vitalTypeEnumNames = System.Enum.GetNames(typeof(VitalType));

		public static GUIStyle miniLabelRight;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			if (miniLabelRight == null)
				miniLabelRight = new GUIStyle("Label") { alignment = TextAnchor.UpperRight };
			//EditorGUI.BeginProperty(r, label, property);

			EditorGUI.BeginChangeCheck();

			int index = PropertyDrawerUtility.GetIndexOfDrawerObject(property);
			bool isAnArray = index >= 0;

			r.xMin += 2;
			r.xMax -= 2;

			Rect outer = r;
			outer.xMin = (outer.xMin - PAD) + INDENT;
			outer.xMax += PAD;

			if (property.isExpanded)
				outer.height = FIXED_HGHT; // TOP_BAR + 2;

			GUI.Box(outer, GUIContent.none, "HelpBox");
			var vitalcolor = index == 0 ? SolidTextures.red2D : SolidTextures.blue2D;
			outer.xMin += 1; outer.yMin += 1;
			outer.width -= 1;
			outer.height = TOP_BAR;
			SolidTextures.DrawTexture(outer, vitalcolor);

			Rect inner = r;

			inner.yMin += PAD;
			inner.height = HEIGHT;

			/// Using the ! of isExpanded, so that the default starting state is true rather than false.
			property.isExpanded = EditorGUI.Toggle(new Rect(inner) { xMin = 4, width = 64 }, "", property.isExpanded, (GUIStyle)"Foldout");

			inner.xMin += INDENT;

			var vitalNameType = property.FindPropertyRelative("vitalName");

			Rect vitalnamerect = new Rect(inner) { yMin = inner.yMin - 1, xMax = (isAnArray && index != 0) ? (inner.xMax - 22) : inner.xMax };
			EditorGUI.PropertyField(vitalnamerect, vitalNameType, new GUIContent(index.ToString()));

			inner.yMin += HEIGHT;
			inner.height = HEIGHT;
			EditorGUI.PropertyField(inner, property.FindPropertyRelative("startValue"));


			inner.yMin += HEIGHT;
			inner.height = HEIGHT;
			DrawValueAndBitsNeeded(inner, property.FindPropertyRelative("_maxValue"));

			inner.yMin += 6;

			if (property.isExpanded)
			{

				inner.yMin += HEIGHT;
				inner.height = HEIGHT;
				EditorGUI.PropertyField(inner, property.FindPropertyRelative("_fullValue"));


				inner.yMin += HEIGHT;
				inner.height = HEIGHT;
				EditorGUI.PropertyField(inner, property.FindPropertyRelative("absorption"));

				inner.yMin += HEIGHT;
				inner.height = HEIGHT;
				EditorGUI.PropertyField(inner, property.FindPropertyRelative("regenDelay"));

				inner.yMin += HEIGHT;
				inner.height = HEIGHT;
				EditorGUI.PropertyField(inner, property.FindPropertyRelative("regenRate"));

				inner.yMin += HEIGHT;
				inner.height = HEIGHT;
				//EditorGUI.PropertyField(inner, property.FindPropertyRelative("decayDelay"));
				DrawValueAndBitsNeeded(inner, property.FindPropertyRelative("decayDelay"), Time.fixedDeltaTime * SimpleSyncSettings.SendEveryXTick);

				inner.yMin += HEIGHT;
				inner.height = HEIGHT;
				EditorGUI.PropertyField(inner, property.FindPropertyRelative("decayRate"));
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(property.serializedObject.targetObject, "Change Vitals");
				property.serializedObject.ApplyModifiedProperties();
				//AssetDatabase.SaveAssets();
			}
			//EditorGUI.EndProperty();
		}

		const int BITS_WIDTH = 64;
		protected void DrawValueAndBitsNeeded(Rect inner, SerializedProperty sp)
		{
			EditorGUI.PropertyField(new Rect(inner) { xMax = inner.xMax - BITS_WIDTH }, sp);
			if (sp.intValue < 1)
			{
				sp.intValue = 1;
				sp.serializedObject.ApplyModifiedProperties();
			}
			int bits = FloatCrusher.GetBitsForMaxValue((uint)sp.intValue);
			EditorGUI.LabelField(new Rect(inner) { xMin = inner.xMax - BITS_WIDTH - 4 }, bits.ToString() + " Bits", miniLabelRight);
		}

		protected void DrawValueAndBitsNeeded(Rect inner, SerializedProperty sp, float tickduration)
		{
			EditorGUI.PropertyField(new Rect(inner) { xMax = inner.xMax - BITS_WIDTH }, sp);
			if (sp.floatValue < 1)
			{
				sp.floatValue = 1;
				sp.serializedObject.ApplyModifiedProperties();
			}
			int bits = FloatCrusher.GetBitsForMaxValue((uint)(sp.floatValue / tickduration));
			EditorGUI.LabelField(new Rect(inner) { xMin = inner.xMax - BITS_WIDTH - 4 }, bits.ToString() + " Bits", miniLabelRight);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			property.serializedObject.Update();
			return (property.isExpanded) ? FIXED_HGHT : 3 * HEIGHT + 8;
		}
	}

#endif

}

