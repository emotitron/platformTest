
using emotitron.Utilities.Networking;
using emotitron.Utilities;
using UnityEngine;
using emotitron.Utilities.HitGroups;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public class OnNetHitApplyDamage : MonoBehaviour
		, IOnNetworkHit
	{
		public float damage = 20f;
		public HitGroupValues multipliers = new HitGroupValues();

		public void OnNetworkHit(NetworkHits results)
		{
			//Debug.Log(name + " OnNetHit " + results);
			var hits = results.hits;
			var bitsformask = results.bitsForHitGroupMask;

			/// if results is nearest only, we won't be looping
			if (results.nearestOnly)
			{
				hits[0].ProcessHit(damage, multipliers, bitsformask);
			}
			else
			{
				int cnt = hits.Count;
				for (int i = 0; i < cnt; ++i)
					hits[i].ProcessHit(damage, multipliers, bitsformask);
			}
		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(OnNetHitApplyDamage))]
	[CanEditMultipleObjects]
	public class OnNetHitApplyDamageEditor : ReactorHeaderEditor
	{
		protected override string Instructions { get { return "Detects " + typeof(IOnNetworkHit).Name + " event, and applies that to any " + typeof(IDamageable).Name + " found on the hit " + typeof(NetObject).Name + "."; } }
		
		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();

			base.OnInspectorGUI();

			if (EditorGUI.EndChangeCheck())
				serializedObject.ApplyModifiedProperties();

			//EditorGUILayout.LabelField("<b>OnNetworkHit()</b>\n{\n  Adjust Value by HitGroup\n  " + typeof(IDamageable).Name + ".ApplyDamage()\n}", richBox);

			EditorGUILayout.Space();

			HitGroupSettings.Single.DrawGui(target, true, false, false);
		}

		//protected override void OnInspectorGUIInjectMiddle()
		//{

		//	base.OnInspectorGUIInjectMiddle();
		//}
	}

#endif
}

