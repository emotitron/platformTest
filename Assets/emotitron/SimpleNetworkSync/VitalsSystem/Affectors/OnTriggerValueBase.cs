using emotitron.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

namespace emotitron.Networking
{
	public abstract class OnContactEventBehaviour : MonoBehaviour
		, IOnContactEvent
	{

		public bool OnContactEvent(ref ContactEvent contactEvent)
		{
			switch (contactEvent.contactType)
			{
				case ContactType.Enter:
					{
						return OnEnter(ref contactEvent);
					}

				case ContactType.Stay:
					{
						return OnStay(ref contactEvent);
					}

				case ContactType.Exit:
					{
						return OnExit(ref contactEvent);
					}

				case ContactType.Hitscan:
					{
						return OnHitscan(ref contactEvent);
					}

				default:
					return false;
			}
		}

		protected virtual bool OnEnter(ref ContactEvent contactEvent)
		{
			return ProcessContactEvent(ref contactEvent);
		}

		protected virtual bool OnStay(ref ContactEvent contactEvent)
		{
			return ProcessContactEvent(ref contactEvent);
		}

		protected virtual bool OnExit(ref ContactEvent contactEvent)
		{
			return ProcessContactEvent(ref contactEvent);
		}

		protected virtual bool OnHitscan(ref ContactEvent contactEvent)
		{
			return ProcessContactEvent(ref contactEvent);
		}

		protected abstract bool ProcessContactEvent(ref ContactEvent contactEvent);


	}

	public abstract class OnTriggerValueBase : OnContactEventBehaviour
		, IOnContactEvent
	{
		[SerializeField] protected float valueOnEnter = 20;
		[SerializeField] protected float valuePerSec = 20;
		[SerializeField] protected float valueOnExit = 0;
		[SerializeField] protected float valueOnScan = 0;

		protected float valuePerFixed;

		protected virtual void Awake()
		{
			valuePerFixed = valuePerSec * Time.fixedDeltaTime;
		}

		protected float GetValueForTriggerType(ContactType collideType)
		{

			float value;
			switch (collideType)
			{
				case ContactType.Enter:
					{
						value = valueOnEnter;
						break;
					}
				case ContactType.Stay:
					{
						value = valuePerFixed;
						break;
					}
				case ContactType.Exit:
					{
						value = valueOnExit;
						break;
					}
				case ContactType.Hitscan:
					{
						value = valueOnScan;
						break;
					}
				default:
					value = 0;
					break;
			}
			return value;
		}

	}

}
