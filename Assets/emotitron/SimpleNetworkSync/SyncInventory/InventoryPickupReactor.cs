//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using emotitron.Utilities;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Networking
//{
//	public class InventoryPickupReactor : MonoBehaviour
//		, IOnPickup
//	{
//		public Mount OnPickup(ContactEvent triggerEvent)
//		{
//			var inventory = triggerEvent.itc as IInventory;

//			if (ReferenceEquals(inventory, null))
//				return null;

//			return inventory.TryPickup(triggerEvent);
//		}
//	}

//#if UNITY_EDITOR

//	[CustomEditor(typeof(InventoryPickupReactor))]
//	[CanEditMultipleObjects]
//	public class InventoryPickupReactorEditor : ReactorHeaderEditor
//	{
//		protected override string Instructions
//		{
//			get
//			{
//				return "Responds to " + typeof(IOnPickup).Name + " events. Tests if the hit event was with an " 
//					+ typeof(IInventory).Name + " and if so, calls test if a pickup would be valid. Returns a Mount if so.";
//			}
//		}

//	}
//#endif
//}
