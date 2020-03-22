using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Networking
{
	public enum TRS { Position, Rotation, Scale }
	public enum AxisMask { None = 0, X = 1, Y = 2, XY = 3, Z = 4, XZ = 5, YZ = 6, XYZ = 7 }
	public enum Replication { OwnerSend = 1, MasterSend = 2 }
	public enum Interpolation { None, Linear, CatmullRom }
	public enum LocalApplyTiming { Never, Immediately, OnSend }
}
