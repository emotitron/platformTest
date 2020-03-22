//Copyright 2019, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{
	public interface IAutoKinematic
	{
		bool AutoKinematicEnabled { get; }
	}

	public enum KinematicSetting { Default, NonKinematic, Kinematic }

	[DisallowMultipleComponent]
	public class OnStateChangeKinematic : NetComponent
		, IOnStateChange
		, IApplyOrder
		, IAutoKinematic
	{
		public int ApplyOrder { get { return 11; } }

		#region IAutoKinematic
		public bool AutoKinematicEnabled { get { return true; } }
		#endregion

		public KinematicSetting onDespawned = KinematicSetting.Kinematic;
		public KinematicSetting onMounted = KinematicSetting.Kinematic;
		public KinematicSetting onTransit = KinematicSetting.NonKinematic;
		public KinematicSetting onDropped = KinematicSetting.NonKinematic;
		public KinematicSetting onAttached = KinematicSetting.Default;
		public KinematicSetting onVisible = KinematicSetting.Default;

		[Tooltip("Destroy this component if no Rigidbodies exist on this GameObject.")]
		public bool autoDestroy = true;

		// cache
		private ObjState currentState;
		private Rigidbody rb;
		private Rigidbody2D rb2d;
		private bool kinematicDefault;
		private int interpolateDefault;

		public override void OnAwake()
		{
			base.OnAwake();
			rb = netObj.Rb;
			rb2d = netObj.Rb2D;

			if (rb)
			{
				kinematicDefault = rb.isKinematic;
				interpolateDefault = (int)rb.interpolation;
			}

			else if (rb2d)
			{
				kinematicDefault = rb2d.isKinematic;
				interpolateDefault = (int)rb2d.interpolation;
			}

			if (autoDestroy && !rb && !rb2d)
				Destroy(this);
		}

		
		/// Owner changes can get dicey, this reapplies the state after an owner change.
		public override void OnAuthorityChanged(bool isMine, bool asServer)
		{
			base.OnAuthorityChanged(isMine, asServer);

			if (isMine)
				SetOwnedKinematics(currentState);
			else
				SetUnownedKinematics();
		}

		public void OnStateChange(ObjState state, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
		{
			if (IsMine)
				SetOwnedKinematics(state);
		}

		protected virtual void SetUnownedKinematics()
		{
			if (RigidbodyType == RigidbodyType.RB)
			{
				rb.isKinematic = true;
				rb.interpolation = RigidbodyInterpolation.None;
			}
			else
			{
				rb2d.isKinematic = true;
				rb2d.interpolation = RigidbodyInterpolation2D.None;
			}
		}

		protected virtual void SetOwnedKinematics(ObjState state)
		{
			if (RigidbodyType == RigidbodyType.None)
				return;

			KinematicSetting kinematicSetting;


			if (state == ObjState.Despawned)
				kinematicSetting = onDespawned;
			else if ((state & ObjState.Mounted) != 0)
				kinematicSetting = onMounted;
			else if ((state & ObjState.Transit) != 0)
				kinematicSetting = onTransit;
			else if ((state & ObjState.Dropped) != 0)
				kinematicSetting = onDropped;
			else if ((state & ObjState.Attached) != 0)
				kinematicSetting = onAttached;
			else if ((state & ObjState.Visible) != 0)
				kinematicSetting = onVisible;
			else
				kinematicSetting = KinematicSetting.Default;

			bool isKinematic =
				(kinematicSetting == KinematicSetting.NonKinematic) ? false :
				(kinematicSetting == KinematicSetting.Kinematic) ? true :
				kinematicDefault;

			if (RigidbodyType == RigidbodyType.RB)
			{
				// setting the rb to the transform is needed here to harden any recent transform.pos changes. Otherwise this switch will negate them
				rb.position = transform.position;
				rb.isKinematic = isKinematic;
				// Be sure to not interpolate attache objects. Even as  kinematic, a moving parent will create position desync
				rb.interpolation = (state != ObjState.Despawned && (state & ObjState.Attached) == 0) ? (RigidbodyInterpolation)interpolateDefault: RigidbodyInterpolation.None;

				/////TEST
				//rb.interpolation = RigidbodyInterpolation.None;
			}
			else
			{
				rb2d.position = transform.position;
				rb2d.isKinematic = isKinematic;
				rb2d.interpolation = (state != ObjState.Despawned && (state & ObjState.Attached) == 0) ? (RigidbodyInterpolation2D)interpolateDefault: RigidbodyInterpolation2D.None;
			}

			currentState = state;
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(OnStateChangeKinematic))]
	[CanEditMultipleObjects]
	public class OnStateChangeKinematicEditor : ReactorHeaderEditor
	{

		private static string help = "Responds to <i>" + typeof(IOnStateChange).Name + "</i> callbacks and if (IsMine == true) sets the rb.isKinematic value." +
					"\n\nStates higher on of the list below have priority (for example if the state has the Mounted bit set, then the kinematic setting for that will be used" +
					" regardless of the " + ObjState.Visible + " or " + ObjState.Dropped + " flags.";

		protected override string Instructions
		{
			get
			{
				return help;
			}

		}

	}

#endif
}