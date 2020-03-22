using emotitron.Networking;
using Photon.Pun;
using UnityEngine;

namespace Testing
{
	public class Player : MonoBehaviour
	{
		public float speed = 4F;

		private Vector3 movement;

		public Rigidbody2D playerRigidbody2D;
		public PhotonView pv;
		public SyncState syncState;

		public bool isGrounded = false;
		[HideInInspector]
		public bool canJump = true;
		[HideInInspector]
		public float canJumpTimer = 0F;
		public float canJumpTimerReset = 1F;

		public float jumpForce;

		private void Awake()
		{
			pv = GetComponent<PhotonView>();
			syncState = GetComponent<SyncState>();
		}

		void FixedUpdate()
		{
			Move(Input.GetAxis("Horizontal"),
				Input.GetAxis("Vertical"));
		}

		private void Update()
		{
			canJumpTimer += Time.deltaTime;
			if (canJumpTimer > canJumpTimerReset)
			{
				canJumpTimer = 0;
				canJump = true;
			}

			if (Input.GetKeyDown(KeyCode.Space))
			{
				Jump();
			}
		}

		public void Move(float inputXNeo, float inputYNeo)
		{
			movement = new Vector2(
							speed * inputXNeo,
							playerRigidbody2D.velocity.y);

			playerRigidbody2D.velocity = movement;
		}

		public void Jump()
		{
			playerRigidbody2D.AddForce(transform.up * jumpForce);
		}
	}

}
