//Copyright 2019, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	[System.Serializable]
	public class Vital/* : IVital*/
	{
		[SerializeField]
		private VitalDefinition vitalDef;
		public VitalDefinition VitalDef { get { return vitalDef; } }

		[System.NonSerialized]
		private VitalData vitalData;
		public VitalData VitalData { get { return vitalData; } private set { vitalData = value; } }
		
		public float Value
		{
			get { return vitalData.Value; }
			set
			{
				if (value == float.NegativeInfinity)
					return;

				int prev = vitalData.IntValue;
				float clamped = Mathf.Clamp(value, 0, vitalDef.MaxValue);
				vitalData.Value = clamped;

				/// Only send out OnVitalChange notices on whole number changes - so we are using intvalue for our check, rather than value
				if (prev != clamped)
				{
					for (int i = 0; i < onValueChangeCallbacks.Count; ++i)
						onValueChangeCallbacks[i].OnValueChange(this);
				}
			}
		}

		public int TicksUntilDecay { get { return vitalData.ticksUntilDecay; } set { vitalData.ticksUntilDecay = value; } }
		public int TicksUntilRegen { get { return vitalData.ticksUntilRegen; } set { vitalData.ticksUntilRegen = value; } }
		
		public Vital(VitalDefinition vitalDef)
		{
			this.vitalDef = vitalDef;
		}

		public void Initialize(float tickDuration)
		{
			vitalDef.Initialize(tickDuration);
			ResetValues();
		}

		public void ResetValues()
		{
			vitalData = vitalDef.GetDefaultData();

			/// Notify UI and other subscribed components of change
			for (int i = 0; i < onValueChangeCallbacks.Count; ++i)
				onValueChangeCallbacks[i].OnValueChange(this);

		}

		#region Outgoing OnChange Callbacks

		public List<IOnVitalChange> onValueChangeCallbacks = new List<IOnVitalChange>();

		public void AddIOnVitalChange(IOnVitalChange cb)
		{
			onValueChangeCallbacks.Add(cb);
		}
		public void RemoveIOnVitalChange(IOnVitalChange cb)
		{
			onValueChangeCallbacks.Remove(cb);
		}

		#endregion

		public void Apply(VitalData vdata)
		{
			Value = vdata.Value;
			vitalData.ticksUntilDecay = vdata.ticksUntilDecay;
			vitalData.ticksUntilRegen = vdata.ticksUntilRegen;
		}

		/// <summary>
		/// Apply damage to vital, accounting for absortion (returning unabsorbed damage to pass through).
		/// </summary>
		public float ApplyDamage(float dmg)
		{
			float mitigatedDmg = dmg * vitalDef.Absorbtion;

			// mitigated damage exceeds the entirety of this vital - take all of it.
			if (mitigatedDmg > vitalData.Value)
			{
				dmg -= vitalData.Value;
				Value = 0;
			}
			else
			{
				dmg -= mitigatedDmg;
				Value = vitalData.Value - mitigatedDmg;
			}

			vitalData.ticksUntilRegen = vitalDef.RegenDelayInTicks;

			return dmg;
		}

		/// <summary>
		/// Is this vital currently at ita max (can its value NOT be increased)
		/// </summary>
		/// <param name="allowOverload">Whether to check against Max value or Full value</param>
		/// <returns></returns>
		public bool IsFull(bool allowOverload)
		{
			return Value >= ((allowOverload) ? vitalDef.MaxValue : vitalDef.FullValue);
		}

		/// <summary>
		/// Add to the Value.
		/// </summary>
		/// <param name="change"></param>
		/// <param name="allowOverload">Allow new value to be greater than Full Health (still clamped to Max Health)</param>
		/// <returns>Returns unused gains.</returns>
		public float ApplyChange(float change, bool allowOverload)
		{

			float oldval = vitalData.Value;
			float newval = vitalData.Value + change;

			if (allowOverload)
			{
				if (newval > vitalDef.MaxValue)
					newval = vitalDef.MaxValue;
			}
			else
			{
				if (newval > vitalDef.FullValue)
					newval = vitalDef.FullValue;
			}

			Value = newval;
			
			float diff = vitalData.Value - oldval;

			/// reset regen/decay based on gain/loss
			if (diff > 0)
				vitalData.ticksUntilDecay = vitalDef.DecayDelayInTicks;
			else if (diff < 0)
				vitalData.ticksUntilRegen = vitalDef.RegenDelayInTicks;

			return change - diff;
		}

		/// <summary>
		/// Get the results of ApplyChange without actually applying them.
		/// </summary>
		public float TestApplyChange(float change, bool allowOverload)
		{
			float oldval = vitalData.Value;
			float newval = vitalData.Value + change;

			if (allowOverload)
			{
				float maxval = vitalDef.MaxValue;

				if (oldval >= maxval)
					return change;

				if (newval > maxval)
					newval = maxval;
			}
			else
			{
				float fullval = vitalDef.MaxValue;

				if (oldval >= fullval)
					return change;

				if (newval > fullval)
					newval = fullval;
			}

			float diff = newval - oldval;

			return change - diff;
		}

		
		/// <summary>
		/// Apply Regeneration and Decay to current state
		/// </summary>
		public void Simulate()
		{
			/// Regeneration
			if (vitalData.ticksUntilRegen > 0)
				vitalData.ticksUntilRegen--;
			else if (vitalData.Value < vitalDef.FullValue)
				Value = Mathf.Min(vitalData.Value + vitalDef.RegenPerTick, vitalDef.FullValue);

			/// Overload decay
			if (vitalData.ticksUntilDecay > 0)
				vitalData.ticksUntilDecay--;
			else if (vitalData.Value > vitalDef.FullValue)
				Value = Mathf.Max(vitalData.Value - vitalDef.DecayPerTick, vitalDef.FullValue);


		}
	}
}

