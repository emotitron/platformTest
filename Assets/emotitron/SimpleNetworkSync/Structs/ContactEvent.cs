using emotitron.Utilities;
using UnityEngine;

namespace emotitron.Networking
{

	public struct ContactEvent
	{
		public object triggeringObj;
		public IContacting itc;
		public Component contacted;
		public Component contacter;
		public ContactType contactType;

		public ContactEvent(object triggeringObj, IContacting itc, Component contacted, Component contacter, ContactType contactType)
		{
			this.triggeringObj = triggeringObj;
			this.itc = itc;
			this.contacted = contacted;
			this.contacter = contacter;
			this.contactType = contactType;
		}

		public ContactEvent(ContactEvent contactEvent)
		{
			this.triggeringObj = contactEvent.triggeringObj;
			this.itc = contactEvent.itc;
			this.contacted = contactEvent.contacted;
			this.contacter = contactEvent.contacter;
			this.contactType = contactEvent.contactType;
		}

#if UNITY_EDITOR
		public override string ToString()
		{
			var itcc = (itc as Component);
			return "ITC: " + (itcc ? (itcc.name + " : " + itcc.GetType().Name) : null) + " " + contactType 
				+ " col: " +  ((contacted) ? contacted.name : "null") 
				+ " othrcol: " +  ((contacter) ? contacter.name : "null")
				+ " trigObj: " + ((triggeringObj != null) ? triggeringObj.GetType().Name : "")
				;
		}
#endif
	}
}
