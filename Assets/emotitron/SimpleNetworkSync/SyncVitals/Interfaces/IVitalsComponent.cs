using emotitron.Utilities;

namespace emotitron.Networking
{
	
	/// <summary>
	/// Object contains a Vitals class reference.
	/// </summary>
	public interface IVitalsComponent : IContacting
	{
		Vitals Vitals { get; }
	}

	/// <summary>
	/// Interface indicates interest in knowing when there has been a change in which Vitals are owned by this connection.
	/// Used for things like healthbars, that need to determine which netobj's vitals they should be monitoring at runtime.
	/// </summary>
	public interface IOnChangeOwnedVitals
	{
		void OnChangeOwnedVitals(IVitalsComponent added, IVitalsComponent removed);
	}
}
