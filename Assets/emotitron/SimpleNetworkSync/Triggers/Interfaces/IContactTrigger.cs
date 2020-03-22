
using UnityEngine;
using emotitron.Utilities;

namespace emotitron.Networking {

	public interface IContactTrigger
	{
		void Trigger(ContactEvent contactEvent);
		void OnContact(IContactTrigger otherCT, Component otherCollider, ContactType contactType);
		IContactTrigger Proxy { get; set; }
		bool PreventRepeats { get; set; }
		bool TriggerOnNull { get; set; }
	}

}
