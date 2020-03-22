

namespace emotitron.Networking
{
	


	/// <summary>
	/// Indicates a class that can be added to an IInventory.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IInventoryable<T> : IContactable
	{
		T Size { get; }
	}
}
