
using UnityEngine;
using emotitron.Utilities.HitGroups;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	/// <summary>
	/// Base class for the inventory system. You can extend this class using your own T to define how capacity is defined, 
	/// and override any of the virtual methods to customize checks for triggering, pickup and having capacity.
	/// If you are feeling pro level, you can define your own class using the IInventory<> interface yourself.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class Inventory<T> : NetComponent
		, IInventory<T>
	{

		#region Inspector

		[SerializeField]
		protected MountSelector defaultMounting = new MountSelector(0);
		public Mount DefaultMount { get; set; }

		[SerializeField]
		protected HitGroupMaskSelector validHitGroups = new HitGroupMaskSelector();
		public IHitGroupMask ValidHitGroups { get { return validHitGroups; } }

		#endregion Inspector

		//cache
		protected MountsLookup mountsLookup;
		protected int defaultMountingMask;

		public override void OnAwake()
		{
			base.OnAwake();

			mountsLookup = transform.GetNestedComponentInParents<MountsLookup>();
			defaultMountingMask = 1 << (defaultMounting.id);

		}
		public override void OnStart()
		{
			base.OnStart();

			if (mountsLookup)
				DefaultMount = mountsLookup.GetMount(defaultMounting);

			//Debug.Log("DEFMOUNT <b>"+ DefaultMount + " : " + mountIdx + " :</b> " + mountsLookup);
		}

		public virtual bool TryTrigger(IOnTrigger trigger, ref ContactEvent contactEvent, int compatibleMounts)
		{
			//Debug.Log("TryTrigger BAsic Inv. compat with: " + compatibleMounts + " defMountId: " + defaultMounting.id + " defMask: " + defaultMountingMask);

			if (validHitGroups != 0)
			{
				var hga = contactEvent.contacted.GetComponent<HitGroupAssign>();
				int triggermask = hga ? hga.Mask : 0;
				if ((validHitGroups.Mask & triggermask) == 0)
				{
					Debug.Log("Try trigger... HitGroup mismatch");
					return false;
				}
			}

			/// Return if the object being picked up exceeds remaining inventory.
			if (TestCapacity(trigger as IInventoryable<T>) == false)
				return false;

			/// If both are set to 0 (Root) then consider that a match, otherwise zero for one but not the other is a mismatch (for now)
			if ((compatibleMounts == defaultMountingMask) || (compatibleMounts & defaultMountingMask) != 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}


		public virtual Mount TryPickup(IOnPickup trigger, ContactEvent contactEvent)
		{
			return DefaultMount;
		}

		/// <summary>
		/// Return if the object being picked up exceeds remaining inventory. Default implementation always just returns true. Override to create real tests.
		/// </summary>
		/// <param name="inventoryable"></param>
		/// <returns></returns>
		public virtual bool TestCapacity(IInventoryable<T> inventoryable)
		{
			return true;
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(Inventory<>), true)]
	[CanEditMultipleObjects]
	public class BasicInventoryEditor : NetComponentEditor
	{
		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.s0p3khn22xau";
			}
		}
		protected override string TextTexturePath
		{
			get
			{
				return "Header/InventoryText";
			}
		}
		protected override string Instructions
		{
			get
			{
				return "Associates a Mount with an Inventory. Picked up <i>IInventoryable</i> will attach to the associated mount.";
			}
		}
	}
#endif
}

