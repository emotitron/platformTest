using emotitron.Utilities;
using System.Collections;

namespace emotitron.Compression
{
	public interface IPackObjOnReadyChange
	{
		void OnPackObjReadyChange(FastBitMask128 readyMask, bool AllAreReady);
	}
}
