using System.Collections;
using System.Collections.Generic;
using emotitron.Utilities;
using emotitron.Utilities.GenericHitscan;
using emotitron.Utilities.Networking;
using UnityEngine;

namespace emotitron.Networking
{
	/// <summary>
	/// Starting class for Generic Hitscan component implementation.
	/// </summary>
	public abstract class HitscanComponent : NetComponent
		, IOnPreSimulate
	{
		#region Inspector Items

		public HitscanDefinition hitscanDefinition = new HitscanDefinition();

		public GameObject origin;
		[Tooltip("Ignore any collider hits that are nested children of the same NetObject this Hitscan is on.")]
		public bool ignoreSelf = true;

//#if UNITY_EDITOR || DEVELOPMENT_BUILD
		public bool visualize = false;
//#endif

		#endregion Inspector Items

		protected bool triggerQueued;
		//protected List<NetworkHit> networkHits = new List<NetworkHit>();

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();

			if (!origin)
				origin = gameObject;
		}
#endif

		public override void OnAwake()
		{
			base.OnAwake();

			if (!origin)
				origin = gameObject;
		}

		public virtual void OnPreSimulate(int frameId, int subFrameId)
		{
			if (!triggerQueued)
				return;

			int nearest = -1;
			Collider[] hits;
			RaycastHit[] rayhits;

			int hitcount = hitscanDefinition.GenericHitscanNonAlloc(transform, out rayhits, out hits, ref nearest, Utilities.GhostWorlds.Realm.Primary);

//#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if (visualize)
				hitscanDefinition.VisualizeHitscan(transform);
//#endif
			triggerQueued = false;

			ProcessHits(hits, hitcount);
		}


		public virtual void ProcessHits(Collider[] hits, int hitcount)
		{
			for (int i = 0; i < hitcount; i++)
			{
				var hit = hits[i];

				if (ignoreSelf)
				{
					var hitNetObj = hit.transform.GetNestedComponentInParents<NetObject>();
					if (hitNetObj == netObj)
					{
						Debug.Log("Ignoring self "+ name + " hit: " + (hit? hit.name : "null") + " hitnetobj: " + (hitNetObj ? hitNetObj.name : "null"));
						continue;

					}
				}

				bool terminateProcessing = ProcessHit(hit);
				if (terminateProcessing)
					return;
			}
		}

		/// <summary>
		/// Handling for processing each hit returned by the hitscan.
		/// </summary>
		/// <param name="hit"></param>
		/// <returns>Return true to terminate processing any more hits.</returns>
		public abstract bool ProcessHit(Collider hit);

	}
}
