#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace emotitron.Networking.Assists
{
	public enum Dynamics { Static, Variable, Dynamic }
	public enum Space_XD { SPACE_3D, SPACE_2D }
	public enum SystemPresence { Absent, Incomplete, Partial, Nested, Complete }

	public delegate SystemPresence GetSystemPresenceDelegate(GameObject selection, params MonoBehaviour[] dependencies);
	public delegate void SystemAddDelegate(GameObject selection, bool add, params MonoBehaviour[] dependencies);

	public static class AssistHelpers
	{
		public const string SIMPLE_MENU_ROOT = "GameObject/Simple/";
		public const string TUTORIAL_FOLDER = SIMPLE_MENU_ROOT + "Tutorial/";
		public const string CONVERT_TO_FOLDER = SIMPLE_MENU_ROOT + "Convert To/";
		public const string ADD_TO_OBJ_FOLDER = SIMPLE_MENU_ROOT + "Add To /Object/";
		public const string ADD_TO_SCENE_TXT = SIMPLE_MENU_ROOT + "Add To /Scene/";

		public static GameObject GetSelectedGameObject()
		{
			GameObject selection = Selection.activeGameObject;
			if (!selection)
				Debug.LogWarning("No gameobject selected, cannot Assist aborted.");

			return selection;
		}

		public static GameObject CreateEmptyChildGameObject(this Transform t, string name)
		{
			var go = new GameObject(name);
			go.transform.parent = t;
			go.transform.localPosition = new Vector3(0, 0, 0);
			go.transform.localRotation = new Quaternion(0, 0, 0, 1);
			go.transform.localScale = new Vector3(1, 1, 1);
			return go;
		}

		public static T EnsureComponentExists<T>(this GameObject go, bool checkParents = false) where T : MonoBehaviour
		{
			if (go == null)
				return null;


			T found = checkParents ? go.GetComponentInParent<T>() : go.GetComponent<T>();
			if (found)
				return found;

			return go.AddComponent<T>();
		}

		public static void EnsureComponentOnNestedChildren<T>(this GameObject go, bool allowMultiples, bool recurse = false) where T : Component
		{
			for (int i = 0; i < go.transform.childCount; ++i)
			{
				var child = go.transform.GetChild(i);

				/// Don't touch nests
				if (child.GetComponent<NetObject>())
					continue;

				/// Recurse if applicable
				if (recurse & child.childCount > 0)
					EnsureComponentOnNestedChildren<T>(child.gameObject, allowMultiples, recurse);

				if (!allowMultiples)
					if (child.GetComponent<T>())
						continue;

				child.gameObject.AddComponent<T>();
			}
		}

		public static List<Component> reusableComponents = new List<Component>();
		public static void DestroyComponentOnNestedChildren<T>(this GameObject go, bool recurse = false) where T : Component
		{
			for (int i = 0; i < go.transform.childCount; ++i)
			{
				var child = go.transform.GetChild(i);

				if (child.GetComponent<NetObject>())
					continue;

				/// Recurse if applicable
				if (recurse & child.childCount > 0)
					EnsureComponentOnNestedChildren<T>(child.gameObject, recurse);

				child.GetComponents(reusableComponents);
				for (int c = reusableComponents.Count - 1; c >= 0; --c)
					if (reusableComponents[c] is T)
						Object.DestroyImmediate(reusableComponents[c]);
			}
		}

		public static Component AddRigidbody(this GameObject go, Space_XD space)
		{
			if (space == Space_XD.SPACE_3D)
			{
				var rb = go.GetComponent<Rigidbody>();
				if (!rb)
					rb = go.AddComponent<Rigidbody>();

				rb.isKinematic = true;
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

				return rb;
			}
			else
			{
				var rb = go.GetComponent<Rigidbody2D>();
				if (!rb)
					rb = go.AddComponent<Rigidbody2D>();

				rb.isKinematic = true;
				rb.interpolation = RigidbodyInterpolation2D.Interpolate;
				rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
				return rb;
			}
		}
		
		public enum ColliderType { None, Trigger, Full };

		public static GameObject CreateNewPrimitiveAsChild(this GameObject go, PrimitiveType primitiveType, ColliderType coltype, string name, float scale = 1, params System.Type[] components)
		{
			var prim = GameObject.CreatePrimitive(primitiveType);

			if (primitiveType == PrimitiveType.Sphere)
				prim.transform.localScale = new Vector3(.5f, .5f, .5f) * scale;
			else if (primitiveType == PrimitiveType.Cube)
				prim.transform.localScale = new Vector3(.35f, .35f, .35f) * scale;
			else if (primitiveType == PrimitiveType.Capsule)
				prim.transform.localScale = new Vector3(.3f, .3f, .3f) * scale;
			else if (primitiveType == PrimitiveType.Cylinder)
				prim.transform.localScale = new Vector3(.51f, .1f, .51f) * scale;

			prim.transform.parent = go.transform;
			prim.transform.localPosition = new Vector3(0, 0, 0);
			prim.transform.localRotation = new Quaternion(0, 0, 0, 1);
			prim.name = name;

			switch (coltype)
			{
				case ColliderType.None:
					{
						Object.DestroyImmediate(prim.GetComponent<Collider>());
						break;
					}

				case ColliderType.Trigger:
					{
						prim.GetComponent<Collider>().isTrigger = true;
						break;
					}
				case ColliderType.Full:
					{
						break;
					}
			}

			foreach(var c in components)
			{
				prim.AddComponent(c);
			}

			return prim;
		}

		public static string[] itemStateEnumNames = System.Enum.GetNames(typeof(ObjState));
		public static ObjState[] itemStateEnumValues = (ObjState[])System.Enum.GetValues(typeof(ObjState));

		public static void CreateChildStatePlaceholders(this GameObject go, Space_XD space, Dynamics dynamics, float scale = 1)
		{
			/// Create a placeholder for each State enum value
			for (int i = 0; i < itemStateEnumValues.Length; ++i)
			{
				go.AddStatePlaceholder(i, space, dynamics, scale);
			}
		}

		public static Material blackMat, cyanMat, magentaMat, greenMat, redMat;

		private static void SetUpMaterials()
		{
			if (blackMat == null)
			{
				blackMat = new Material(Shader.Find("Specular"));
				cyanMat = new Material(Shader.Find("Specular"));
				magentaMat = new Material(Shader.Find("Specular"));
				greenMat = new Material(Shader.Find("Specular"));
				redMat = new Material(Shader.Find("Specular"));

				blackMat.color = Color.black;
				cyanMat.color = Color.cyan;
				magentaMat.color = Color.magenta;
				greenMat.color = Color.green;
				redMat.color = Color.red;
			}
		}


		private static void AddStatePlaceholder(this GameObject go, int enumindex, Space_XD space, Dynamics dynamics, float scale = 1)
		{
			ObjState mask = itemStateEnumValues[enumindex];
			string label = itemStateEnumNames[enumindex] + " Model";

			var visObj = new GameObject(label);
			visObj.transform.parent = go.transform;
			var toggle = visObj.AddComponent<OnStateChangeToggle>();
			toggle.stateLogic.operation = enumindex == 0 ? ObjStateLogic.Operator.EQUALS : ObjStateLogic.Operator.AND;
			toggle.stateLogic.stateMask = (int)mask;

			/// Set the collider settings for each State
			ColliderType coltype = 
				(mask == 0) ? ColliderType.None :
				((mask & ObjState.Attached) != 0) ? ColliderType.None :
				((mask & (ObjState.Dropped | ObjState.Transit)) != 0) ? ColliderType.Full : 
				ColliderType.Trigger;

			PrimitiveType primtype = 
				mask == ObjState.Visible ? PrimitiveType.Sphere :
				mask == ObjState.Attached ? PrimitiveType.Cube : 
				mask == ObjState.Dropped ? PrimitiveType.Cylinder :
				PrimitiveType.Capsule;

			if (blackMat == null)
				SetUpMaterials();

			Material material =
				mask == ObjState.Despawned ? blackMat :
				mask == ObjState.Visible ? cyanMat :
				mask == ObjState.Attached ? magentaMat :
				mask == ObjState.Dropped ? redMat :
				greenMat;

			var primitive = visObj.CreateNewPrimitiveAsChild(primtype, coltype, label + " Placeholder", scale);
			primitive.GetComponent<Renderer>().material = material;
		}

	}

}

#endif