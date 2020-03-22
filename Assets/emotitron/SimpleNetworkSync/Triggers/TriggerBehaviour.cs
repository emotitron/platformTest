//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using emotitron.Utilities;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Networking
//{
//	/// <summary>
//	/// Component automatically added to NetObjects with ITriggeringComponent on them. This generates Collision/Trigger callbacks from one place, rather than
//	/// making multiple networking components have to repeat this code.
//	/// </summary>
//	//[AddComponentMenu("")]
//	public class TriggerBehaviour : MonoBehaviour
//	{

//		public List<ITriggeringComponent> triggeringComponents = new List<ITriggeringComponent>();

//		// Use this for initialization
//		void Awake()
//		{
//			GetComponents(triggeringComponents);
//		}

//		#region Trigger Events

//		private void OnEnter(Component otherCollider, Component thisCollider = null)
//		{
//			var trigger = otherCollider.GetComponentInParent<ContactTrigger>();
//			Debug.Log("OnEnter " + otherCollider.name + " " + (trigger == true) + " rb?: " + ((trigger) ? trigger.HasRigidbody : false));

//			if (!ReferenceEquals(trigger, null) /*&& !trigger.HasRigidbody*/)
//				for (int i = 0; i < triggeringComponents.Count; ++i)
//				{
//					Debug.Log("OnEnter " + triggeringComponents[i].ToString());
//					var contactEvent = new ContactEvent(null, triggeringComponents[i], otherCollider, thisCollider ? thisCollider : this, ContactType.Enter);
//					//trigger.OnContactEvent(contactEvent);
//					trigger.Trigger(contactEvent);
//				}

//		}
//		private void OnStay(Component otherCollider, Component thisCollider = null)
//		{
//			var trigger = otherCollider.GetComponentInParent<IOnTriggeringStay>();

//			if (!ReferenceEquals(trigger, null) /*&& !trigger.HasRigidbody*/)
//				for (int i = 0; i < triggeringComponents.Count; ++i)
//					trigger.OnContactEvent(new ContactEvent(null, triggeringComponents[i], otherCollider, thisCollider ? thisCollider : this, ContactType.Stay));
//		}

//		private void OnExit(Component otherCollider, Component thisCollider = null)
//		{
//			var trigger = otherCollider.GetComponentInParent<IOnTriggeringExit>();

//			if (!ReferenceEquals(trigger, null)/* && !trigger.HasRigidbody*/)
//				for (int i = 0; i < triggeringComponents.Count; ++i)
//					trigger.OnContactEvent(new ContactEvent(null, triggeringComponents[i], otherCollider, thisCollider ? thisCollider : this, ContactType.Exit));
//		}

//		// 3d

//		private void OnTriggerEnter(Collider other)
//		{
//			OnEnter(other);
//		}

//		private void OnTriggerStay(Collider other)
//		{
//			OnStay(other);
//		}

//		private void OnTriggerExit(Collider other)
//		{
//			OnExit(other);
//		}
//		private void OnCollisionEnter(Collision collision)
//		{
//			OnEnter(collision.collider);
//		}

//		private void OnCollisionStay(Collision collision)
//		{
//			OnStay(collision.collider);
//		}

//		private void OnCollisionExit(Collision collision)
//		{
//			OnExit(collision.collider);
//		}

//		// 2d 

//		private void OnTriggerEnter2D(Collider2D other)
//		{
//			OnEnter(other);
//		}

//		private void OnTriggerStay2D(Collider2D other)
//		{
//			OnStay(other);
//		}

//		private void OnTriggerExit2D(Collider2D other)
//		{
//			OnExit(other);
//		}
//		private void OnCollisionEnter2D(Collision2D collision)
//		{
//			OnEnter(collision.collider);
//		}

//		private void OnCollisionStay2D(Collision2D collision)
//		{
//			OnStay(collision.collider);
//		}

//		private void OnCollisionExit2D(Collision2D collision)
//		{
//			OnExit(collision.collider);
//		}

//		#endregion

//	}
//}
