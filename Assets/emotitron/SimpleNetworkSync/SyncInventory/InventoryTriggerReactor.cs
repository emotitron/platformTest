//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using emotitron.Utilities;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Networking
//{
//	public class InventoryTriggerReactor : NetComponent
//		, IOnTrigger
//	{

//		// cache
//		private int compatibleMountsMask;

//		public override void OnAwake()
//		{
//			base.OnAwake();

//			var syncState = GetComponent<SyncState>();

//			if (syncState)
//				compatibleMountsMask = syncState.mountableMask;
//		}

//		public Mount OnTrigger(ref ContactEvent triggerEvent)
//		{
//			var inventory = (triggerEvent.itc as IInventory);

//			/// Update the triggerEvent with the specific triggering object
//			triggerEvent.triggeringObj = inventory;

//			if (ReferenceEquals(inventory, null))
//				return null;

//			return inventory.TryTrigger(triggerEvent, compatibleMountsMask);
//		}
//	}

//#if UNITY_EDITOR

//	[CustomEditor(typeof(InventoryTriggerReactor))]
//	[CanEditMultipleObjects]
//	public class InventoryTriggerReactorEditor : ReactorHeaderEditor
//	{
//		protected override string Instructions
//		{
//			get
//			{
//				return "Responds to OnTrigger(TriggerEvent) callbacks, and will return a Mount if the collision produced a valid trigger interaction.";
//			}
//		}
//	}
//#endif
//}
