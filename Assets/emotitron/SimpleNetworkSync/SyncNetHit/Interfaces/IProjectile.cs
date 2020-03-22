
using UnityEngine;


namespace emotitron.Networking
{
	public enum RespondTo { HitSelf = 1, HitNetObj = 2, HitNonNetObj = 4 }

	public interface IProjectile
	{
		IProjectileLauncher Owner { get; set; }
		void Initialize(IProjectileLauncher owner, int frameId, int subFrameId, Vector3 velocity, RespondTo terminateOn, RespondTo damageOn);
	}

}
