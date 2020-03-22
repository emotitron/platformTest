using emotitron.Utilities.Networking;

#if PUN_2_OR_NEWER
using Photon.Pun;
#endif

namespace emotitron.Networking
{

	public interface IProjectileLauncher
	{
#if PUN_2_OR_NEWER
		PhotonView PV { get; }
#endif
		NetObject NetObj { get; }
		IContactTrigger ContactTrigger { get; }
		int NetObjId { get; }
		void QueueHit(NetworkHit hit);
	}
}
