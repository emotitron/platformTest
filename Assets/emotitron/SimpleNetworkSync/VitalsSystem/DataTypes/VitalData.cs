//Copyright 2019, Davin Carten, All rights reserved

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class VitalsData
	{
		public Vitals vitals;
		public VitalData[] datas;

		public VitalsData(Vitals vitals)
		{
			this.vitals = vitals;
			datas = new VitalData[vitals.VitalArray.Length];
		}

		public void CopyFrom(VitalsData source)
		{
			var srcdatas = source.datas;
			int cnt = srcdatas.Length;
			for (int i = 0; i < cnt; ++i)
				datas[i] = srcdatas[i];
		}
	}

	public struct VitalData
	{
		// Value and IntValue should always change together. Changing either will change both.
		private float _value;
		public float Value { get { return _value; } set { _value = value; intValue = (int)value; } }
		private int intValue;
		public int IntValue { get { return intValue; } set { intValue = value; _value = value; } }

		public int ticksUntilRegen;
		public int ticksUntilDecay;

		//public float Value { get { return value; } set { this.value = value; } }
		//public int TicksUntilDecay { get { return ticksUntilDecay; } set { ticksUntilDecay = value; } }
		//public int TicksUntilRegen { get { return ticksUntilRegen; } set { ticksUntilRegen = value; } }

		public VitalData(float value, int ticksUntilDecay, int ticksUntilRegen)
		{
			this._value = value;
			this.intValue = (int)value;
			this.ticksUntilDecay = ticksUntilDecay;
			this.ticksUntilRegen = ticksUntilRegen;
		}

		public override string ToString()
		{
			return _value + ":" + intValue + " ticksUntilDecay: " + ticksUntilDecay + " ticksUntilRegen: " + ticksUntilRegen;
		}
	}

}

