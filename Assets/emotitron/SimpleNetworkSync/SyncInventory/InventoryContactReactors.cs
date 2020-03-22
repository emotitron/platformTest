using UnityEngine;

namespace emotitron.Networking
{
	/// <summary>
	/// An implementation of InventoryContactReactors<> that can be used as an inv
	/// </summary>
	public class InventoryContactReactors : InventoryContactReactors<Vector3Int>
	{

		[SerializeField]
		protected Vector3Int size = new Vector3Int(1, 1, 1);
		public override Vector3Int Size { get { return size; } }

		public override void OnAwake()
		{
			base.OnAwake();
			
			volume = size.x * size.y * size.z;
		}
	}
}
