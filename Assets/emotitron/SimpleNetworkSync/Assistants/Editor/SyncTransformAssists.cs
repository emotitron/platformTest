#if UNITY_EDITOR


using UnityEditor;
using UnityEngine;

namespace emotitron.Networking.Assists
{
	public static class SyncTransformAssists
	{

		public const string SYNC_TRANS_FOLDER = AssistHelpers.ADD_TO_OBJ_FOLDER + "SyncTransform Defaults/";

		public static SyncTransform AddDefaultSyncTransform()
		{
			var selection = Selection.activeGameObject;

			if (!selection)
			{
				Debug.LogWarning("No Object Selected.");
				return null;
			}

			SyncTransform st = selection.GetComponent<SyncTransform>();
			if (!st)
				st = selection.AddComponent<SyncTransform>();

			return st;
		}

		[MenuItem(SYNC_TRANS_FOLDER + "Defaut 3D", false, 0)]
		public static SyncTransform AddDefaultSyncTransform3D()
		{
			var st = AddDefaultSyncTransform();
			if (!st)
				return null;

			var tc = st.transformCrusher;
			var pc = tc.PosCrusher;
			var rc = tc.RotCrusher;
			var sc = tc.SclCrusher;

			pc.XCrusher.Enabled = true;
			pc.YCrusher.Enabled = true;
			pc.ZCrusher.Enabled = true;

			rc.TRSType = Compression.TRSType.Euler;

			sc.uniformAxes = Compression.ElementCrusher.UniformAxes.XYZ;
			sc.UCrusher.Enabled = true;

			return st;
		}

		[MenuItem(SYNC_TRANS_FOLDER + "Defaut 3D Rigidbody", false, 0)]
		public static SyncTransform AddDefaultSyncTransform3DRigidbody()
		{
			var st = AddDefaultSyncTransform();
			if (!st)
				return null;

			var tc = st.transformCrusher;
			var pc = tc.PosCrusher;
			var rc = tc.RotCrusher;
			var sc = tc.SclCrusher;

			pc.XCrusher.Enabled = true;
			pc.YCrusher.Enabled = true;
			pc.ZCrusher.Enabled = true;

			rc.TRSType = Compression.TRSType.Quaternion;
			rc.Enabled = true;

			sc.uniformAxes = Compression.ElementCrusher.UniformAxes.XYZ;
			sc.UCrusher.Enabled = false;

			return st;
		}

		[MenuItem(SYNC_TRANS_FOLDER + "Defaut 2D", false, 0)]
		public static SyncTransform AddDefaultSyncTransform2D()
		{
			var st = AddDefaultSyncTransform();
			if (!st)
				return null;

			var tc = st.transformCrusher;
			var pc = tc.PosCrusher;
			var rc = tc.RotCrusher;
			var sc = tc.SclCrusher;

			pc.XCrusher.Enabled = true;
			pc.YCrusher.Enabled = true;
			pc.ZCrusher.Enabled = false;

			rc.TRSType = Compression.TRSType.Euler;
			rc.XCrusher.Enabled = false;
			rc.YCrusher.Enabled = false;
			rc.ZCrusher.Enabled = true;

			sc.uniformAxes = Compression.ElementCrusher.UniformAxes.NonUniform;
			sc.XCrusher.Enabled = true;
			sc.YCrusher.Enabled = false;
			sc.ZCrusher.Enabled = false;

			return st;
		}

		[MenuItem(SYNC_TRANS_FOLDER + "3D Position Only", false, 0)]
		public static SyncTransform Add3dPosOnly()
		{
			var st = AddDefaultSyncTransform();
			if (!st)
				return null;

			var tc = st.transformCrusher;
			var pc = tc.PosCrusher;
			var rc = tc.RotCrusher;
			var sc = tc.SclCrusher;

			pc.XCrusher.Enabled = true;
			pc.YCrusher.Enabled = true;
			pc.ZCrusher.Enabled = true;

			rc.TRSType = Compression.TRSType.Euler;
			rc.XCrusher.Enabled = false;
			rc.YCrusher.Enabled = false;
			rc.ZCrusher.Enabled = false;

			sc.uniformAxes = Compression.ElementCrusher.UniformAxes.NonUniform;
			sc.XCrusher.Enabled = false;
			sc.YCrusher.Enabled = false;
			sc.ZCrusher.Enabled = false;

			return st;
		}

		[MenuItem(SYNC_TRANS_FOLDER + "2D Position Only", false, 0)]
		public static SyncTransform Add2dPosOnly()
		{
			var st = AddDefaultSyncTransform();
			if (!st)
				return null;

			var tc = st.transformCrusher;
			var pc = tc.PosCrusher;
			var rc = tc.RotCrusher;
			var sc = tc.SclCrusher;

			pc.XCrusher.Enabled = true;
			pc.YCrusher.Enabled = true;
			pc.ZCrusher.Enabled = false;

			rc.TRSType = Compression.TRSType.Euler;
			rc.XCrusher.Enabled = false;
			rc.YCrusher.Enabled = false;
			rc.ZCrusher.Enabled = false;

			sc.uniformAxes = Compression.ElementCrusher.UniformAxes.NonUniform;
			sc.XCrusher.Enabled = false;
			sc.YCrusher.Enabled = false;
			sc.ZCrusher.Enabled = false;

			return st;
		}
	}

}

#endif
