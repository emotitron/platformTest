using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.Pooling;
using emotitron.Utilities.Networking;
using emotitron.Utilities.GUIUtilities;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	public class SyncLauncher : SyncNetHitBase
		, IProjectileLauncher
	{
		public override int ApplyOrder { get { return ApplyOrderConstants.WEAPONS; } }

		#region Inspector Items

		[SerializeField] public GameObject projPrefab;
		[SerializeField] public Vector3 velocity = new Vector3(0, 0, 10f);

		[SerializeField] [EnumMask] public RespondTo terminateOn = RespondTo.HitNetObj | RespondTo.HitNonNetObj;
		[SerializeField] [EnumMask] public RespondTo damageOn = RespondTo.HitNetObj | RespondTo.HitNonNetObj;

		#endregion

		//cache
		IContactTrigger contactTrigger;
		public IContactTrigger ContactTrigger { get { return contactTrigger; } }

		private Queue<NetworkHit> projectileHitQueue = new Queue<NetworkHit>();

		protected override void PopulateFrames()
		{
			frames = new WeaponFrame[frameCount + 1];
			for (int i = 0; i <= frameCount; ++i)
			{
				frames[i] = new WeaponFrame(this, false, i);

			}
		}

		#region Initialization

		protected override void Reset()
		{
			base.Reset();
			triggerKey = KeyCode.F;
		}

		public override void OnAwake()
		{
			base.OnAwake();

			/// If no prefab was designated, we need a dummy
			if (projPrefab == null)
			{
				projPrefab = ProjectileHelpers.GetPlaceholderProj();
				Pool.AddPrefabToPool(projPrefab, 8, 8, null, true);
			}
			else
				Pool.AddPrefabToPool(projPrefab);

			contactTrigger = transform.GetNestedComponentInParents<IContactTrigger>();
		}

		#endregion

		/// <summary>
		/// Projectiles report hits back to the launcher, and they are queued for networking and local simulation.
		/// </summary>
		/// <param name="hit"></param>
		public void QueueHit(NetworkHit hit)
		{
			if (IsMine)
			{
				projectileHitQueue.Enqueue(hit);
			}
		}

		/// <summary>
		/// Create the projectile instance
		/// </summary>
		protected override void Trigger(WeaponFrame frame, int subFrameId)
		{
			Pool p = Pool.Spawn(projPrefab, origin);
			var ssproj = p.GetComponent<IProjectile>();
			ssproj.Initialize(this, frame.frameId, subFrameId, velocity, terminateOn, damageOn);
			ssproj.Owner = this;
		}

		#region Timings

		public override void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
		{
			/// Base handles the fire triggers. We hae special handling for hits, since they come in from the projectiles and are not instant
			base.OnPostSimulate(frameId, subFrameId, isNetTick);

			WeaponFrame frame = frames[frameId];

			/// Owner log hits to frame/subframe
			if (projectileHitQueue.Count > 0)
			{
				while (projectileHitQueue.Count > 0)
					frame.netHits[subFrameId].hits.Add(projectileHitQueue.Dequeue());

				frame.content = FrameContents.Complete;
				frame.hitmask |= ((uint)1 << subFrameId);

				HitsCallbacks(frame.netHits[subFrameId]);
			}
		}

		#endregion
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncLauncher), true)]
	[CanEditMultipleObjects]
	public class SyncLauncherEditor : SyncNetHitBaseEditor
	{
		protected override string Instructions
		{
			get
			{
				return "Attach this component to any root or child GameObject to define a networkable projectile launcher. " +
					"A NetObject is required on this object or a parent.\n\n" +
					"Initiate a projectile by calling:\n" +
					"this" + typeof(SyncLauncher).Name + ".QueueTrigger()";
			}
		}

		protected override string HelpURL
		{
			get
			{
				return "";
			}
		}

		protected override string TextTexturePath
		{
			get
			{
				return "Header/SyncLauncherText";
			}
		}
	}
#endif
}

