//Copyright 2019, Davin Carten, All rights reserved

using System.Collections.Generic;

namespace emotitron.Networking
{



	public interface IOnVitalChange
	{
		void OnValueChange(Vital vital);
		void OnVitalChange(Vital vital);
	}



	public interface IVitalMonitor
	{

	}


}


