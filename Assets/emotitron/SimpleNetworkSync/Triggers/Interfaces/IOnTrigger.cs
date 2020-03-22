
using emotitron.Utilities.HitGroups;

namespace emotitron.Networking
{
	public interface IOnTrigger
	{
		bool OnTrigger(ref ContactEvent contactEvent);
	}
}


