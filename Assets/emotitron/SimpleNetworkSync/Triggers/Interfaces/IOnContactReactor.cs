namespace emotitron.Networking
{

	/// <summary>
	/// Defines a class as being able to interact (trigger/pickup) with the T IContacting type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IContactReactor<T> where T : class, IContacting
	{
		
	}
}
