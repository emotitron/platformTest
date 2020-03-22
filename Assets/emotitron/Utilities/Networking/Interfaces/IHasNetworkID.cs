using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Utilities.Networking
{
	public interface IHasNetworkID
	{
		uint NetId { get; }
	}
}
