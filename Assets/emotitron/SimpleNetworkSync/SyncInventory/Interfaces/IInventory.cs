
namespace emotitron.Networking
{
	public interface IInventory : IContacting { }
	public interface IInventory<T> : IInventory
	{
		
		bool TestCapacity(IInventoryable<T> inventoryable);
	}

}
