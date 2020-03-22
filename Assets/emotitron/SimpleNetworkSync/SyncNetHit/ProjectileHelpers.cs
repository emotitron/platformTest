using System.Collections;
using UnityEngine;

namespace emotitron.Networking
{
	public static class ProjectileHelpers
	{
		public static GameObject prefab;

		public static GameObject GetPlaceholderProj()
		{
			if (prefab != null)
				return prefab;

			var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			go.name = "Projectile Placeholder Prefab";

			go.GetComponent<Renderer>().material.color = Color.yellow;
			go.transform.localScale = new Vector3(.1f, .1f, .1f);
			go.gameObject.SetActive(false);
			var rb = go.AddComponent<Rigidbody>();
			rb.useGravity = false;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			go.GetComponent<Collider>().isTrigger = true;
			go.AddComponent<Projectile>();
			go.AddComponent<ContactTrigger>();

			prefab = go;
			return go;
		}
	}
}
