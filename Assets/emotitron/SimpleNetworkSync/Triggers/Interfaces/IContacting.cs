
using emotitron.Utilities.HitGroups;

namespace emotitron.Networking
{
	public interface IContactable
	{
		
	}

	public interface IContacting
	{
		NetObject NetObj { get; }
		int NetObjId { get; }
		bool IsMine { get; }
		Mount DefaultMount { get; }
		IHitGroupMask ValidHitGroups { get; }

		bool TryTrigger(IOnTrigger trigger, ref ContactEvent contactEvent, int compatibleMounts);
		Mount TryPickup(IOnPickup trigger, ContactEvent contactEvent);
	}

}
