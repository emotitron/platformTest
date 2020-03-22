using emotitron.Networking;
using Photon.Pun;
using UnityEngine;

namespace Testing
{
	public class Platform : MonoBehaviour
	{
		//public float speed = 5F;

		//private bool turnMovement;

		//public float elapsedTimeReset = 5F;
		//private float elapsedTime;

		PhotonView pv;

		private void Awake()
		{
			pv = GetComponent<PhotonView>();

		}
		void Update()
		{
			//if (!pv.IsMine)
			//	return;

			//      elapsedTime += Time.deltaTime;

			//      if (elapsedTime > elapsedTimeReset)
			//      {
			//          elapsedTime = 0;
			//          turnMovement = !turnMovement;
			//      }

			//      if (turnMovement)
			//      {
			//          transform.Translate(speed * Time.deltaTime, 0, 0);
			//      }
			//      else
			//      {
			//          transform.Translate(-speed * Time.deltaTime, 0, 0);
			//      }
		}

		private void OnCollisionEnter2D(Collision2D col)
		{
			Player player = col.gameObject.GetComponent<Player>();

			/// We only change the parenting on the athority version, otherwise glitchy behaviour will occur
			if (!player || !player.pv.IsMine)
				return;

			if (player.playerRigidbody2D.velocity.y < 0.01)
			{
				player.syncState.SoftMount(GetComponent<Mount>());
			}
		}

		private void OnCollisionExit2D(Collision2D col)
		{

			Player player = col.gameObject.GetComponent<Player>();

			/// We only change the parenting on the athority version, otherwise glitchy behaviour will occur
			if (!player || !player.pv.IsMine)
				return;

			col.gameObject.transform.parent = null;

			player.syncState.SoftMount(null);
		}

	}


}
