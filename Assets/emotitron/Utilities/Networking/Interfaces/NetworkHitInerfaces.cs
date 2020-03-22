using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Utilities.Networking
{
	public interface IHitGroupAssign
	{
		//int Index { get; }
		int Mask { get; }
	}

	public interface IOnNetworkHit
	{
		void OnNetworkHit(NetworkHits results);
	}

	public interface IOnTerminate
	{
		void OnTerminate();
	}

	public interface IDamageable
	{
		/// <summary>
		/// Apply damage to this object, and return remaining damage.
		/// </summary>
		/// <param name="damage"></param>
		/// <returns>Return the remaining damage if not all was applied.</returns>
		float ApplyDamage(float damage);
		bool IsMine { get; }
		int NetObjId { get; }
	}

	public interface IDamager
	{

	}
	public interface IDamagerOnEnter : IDamager
	{
		void OnEnter(IDamageable iDamageable);
	}
	public interface IDamagerOnStay : IDamager
	{
		void OnStay(IDamageable iDamageable);
	}
	public interface IDamagerOnExit : IDamager
	{
		void OnExit(IDamageable iDamageable);
	}

}

