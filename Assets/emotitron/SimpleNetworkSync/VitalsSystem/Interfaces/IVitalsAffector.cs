
namespace emotitron.Networking
{

	public interface IVitalsAffector
	{
		VitalNameType VitalNameType { get; }
		float Value { get; }
		bool AllowOverload { get; }
		bool OnlyPickupIfUsed { get; }
	}
}