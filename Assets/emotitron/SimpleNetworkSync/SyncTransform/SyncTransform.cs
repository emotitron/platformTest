// Copyright 2019, Davin Carten, All rights reserved
// This code may only be used in game development, but may not be used in any tools or assets that are sold or made publicly available to other developers.

using UnityEngine;
using System.Collections.Generic;

using emotitron.Compression;
using emotitron.Utilities.Networking;
using emotitron.Utilities.GhostWorlds;

namespace emotitron.Networking
{
	public interface ISyncTransform
	{

	}

	public interface ITransformController
	{
#if UNITY_EDITOR
		bool AutoSync { get; }
#endif
		bool HandlesInterpolation { get; }
		bool HandlesExtrapolation { get; }
	}

	[DisallowMultipleComponent]
	public class SyncTransform : SyncObject<SyncTransform.Frame>
		, ISyncTransform
		, IOnSnapshot
		, IOnNetSerialize
		, IOnNetDeserialize
		, IOnAuthorityChanged
		, IReadyable
		, IUseKeyframes
		, IDeltaFrameChangeDetect
		//, IAdjustableApplyOrder
		, IOnInterpolate
		, IOnCaptureState
		, IOnTeleport
	{
		#region Inspector Fields

		[Tooltip("How lerping between tick states is achieved. 'Standard' is Linear. 'None' holds the previous state until t = 1. " +
			"'Catmull Rom' is experimental.")]
		public Interpolation interpolation = Interpolation.Linear;

		[Tooltip("Percentage of extrapolation from previous values. [0 = No Extrapolation] [.5 = 50% extrapolation] [1 = Undampened]. " +
			"This allows for gradual slowing down of motion when the buffer runs dry.")]
		[Range(0f, 1f)]
		public float extrapolateRatio = .5f;
		protected int extrapolationCount;

		[Tooltip("If the distance delta between snapshots exceeds this amount, object will move to new location without lerping. Set this to zero or less to disable (for some tiny CPU savings). You can manually flag a teleport by setting the HasTeleported property to True.")]
		public float teleportThreshold = 5f;
		private float teleportThresholdSqrMag;

		public Dictionary<int, TransformCrusher> masterSharedCrushers = new Dictionary<int, TransformCrusher>();
		public TransformCrusher transformCrusher = new TransformCrusher()
		{
			PosCrusher = new ElementCrusher(TRSType.Position, false)
			{
				hideFieldName = true,
				XCrusher = new FloatCrusher(Axis.X, TRSType.Position, true) { BitsDeterminedBy = BitsDeterminedBy.HalfFloat, AccurateCenter = true },
				YCrusher = new FloatCrusher(Axis.Y, TRSType.Position, true) { BitsDeterminedBy = BitsDeterminedBy.HalfFloat, AccurateCenter = true },
				ZCrusher = new FloatCrusher(Axis.Z, TRSType.Position, true) { BitsDeterminedBy = BitsDeterminedBy.HalfFloat, AccurateCenter = true },
			},
			RotCrusher = new ElementCrusher(TRSType.Quaternion, false)
			{
				hideFieldName = true,
				XCrusher = new FloatCrusher(Axis.X, TRSType.Euler, true) { Bits = 12, AccurateCenter = true },
				YCrusher = new FloatCrusher(Axis.Y, TRSType.Euler, true) { Bits = 12, AccurateCenter = true },
				ZCrusher = new FloatCrusher(Axis.Z, TRSType.Euler, true) { Bits = 12, AccurateCenter = true },
				QCrusher = new QuatCrusher(44, true, false),

				//QCrusher = new QuatCrusher(CompressLevel.uint64Hi, false, false)
			},
			SclCrusher = new ElementCrusher(TRSType.Scale, false)
			{
				hideFieldName = true,
				uniformAxes = ElementCrusher.UniformAxes.NonUniform,
				//UCrusher = new FloatCrusher(Axis.Uniform, TRSType.Scale, true) { BitsDeterminedBy = BitsDeterminedBy.HalfFloat, axis = Axis.Uniform, TRSType = TRSType.Scale }
				XCrusher = new FloatCrusher(BitPresets.Bits8, -1, 1, Axis.X, TRSType.Scale, true) { TRSType = TRSType.Scale, AccurateCenter = true, BitsDeterminedBy = BitsDeterminedBy.SetBits },
				YCrusher = new FloatCrusher(BitPresets.Bits8, -1, 1, Axis.Y, TRSType.Scale, true) { TRSType = TRSType.Scale, AccurateCenter = true, BitsDeterminedBy = BitsDeterminedBy.SetBits, Enabled = false },
				ZCrusher = new FloatCrusher(BitPresets.Bits8, -1, 1, Axis.Z, TRSType.Scale, true) { TRSType = TRSType.Scale, AccurateCenter = true, BitsDeterminedBy = BitsDeterminedBy.SetBits, Enabled = false },
			}
		};

		#endregion

		#region Teleport

		/// <summary>
		///When IsMine: OnTeleport sets this true to indicate the next outgoing serialization should be flagged as a teleport.
		///When !IsMine: Is set during Snapshot to indicate that interpolation should not occur.
		/// </summary>
		protected bool hasTeleported;
		//protected bool parentChanged;
		protected int teleNewParentId;
		protected Matrix preTeleportM = new Matrix();
		protected CompressedMatrix preTeleportCM = new CompressedMatrix();

		/// <summary>
		/// Be sure to call this in the Capture segment BEFORE you reparent the object. Captures and holds the current transform prior to
		/// changes you are about to make.
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="rot"></param>
		/// <param name="scl"></param>
		public void OnTeleport()
		{
			if (IsMine)
			{
				if (!hasTeleported)
					CaptureCurrent(preTeleportM, preTeleportCM/*, Realm.Primary, true*/);

				//Debug.Log(name + " OnTele " + pos + " " + this.telePos + " preCap: " + preTeleportM.position);
				this.hasTeleported = true;
			}
		}

		/// <summary>
		/// Internal. StateSync uses this method to tell the SyncTransform what the parent object is. SyncTransform needs to know about parent changes
		/// to avoid interpolating/extrapolating across parent changes.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="newParent"></param>
		public void UpdateParent(ObjState state, Transform newParent)
		{
			teleNewParentId = newParent ? newParent.GetInstanceID() :
				(state & ObjState.Attached) != 0 ? -2 : -1;

			//if (GetComponent<SyncState>())
			//	Debug.Log(Time.time + " <b><color=green>UpdateParent </color></b>" + teleNewParentId);
		}

		#endregion

		// Cached
		private Rigidbody rb;
		private Rigidbody2D rb2d;
		private List<ITransformController> iTransformControllers = new List<ITransformController>(1);

		protected bool allowInterpolation;
		public override bool AllowInterpolation { get { return allowInterpolation; } }

		protected bool allowReconstruction;
		public override bool AllowReconstruction { get { return allowReconstruction; } }

		protected override void Reset()
		{
			base.Reset();
			/// Default TransformSync to happen before AnimatorSync
			_applyOrder = ApplyOrderConstants.TRANSFORM;
			/// Set the crushers to use local, so that re/deparenting is doable by default.
			transformCrusher.PosCrusher.local = true;
			transformCrusher.RotCrusher.local = true;
			transformCrusher.RotCrusher.TRSType = TRSType.Euler;
			transformCrusher.SclCrusher.local = true;

			/// Default alwaysReady based on root or not.
			_alwaysReady = transform.parent != null;
		}

		public static Stack<Frame[]> framePool = new Stack<Frame[]>();
		public static List<IAutoKinematic> autoKinematicFindList = new List<IAutoKinematic>();

		public override void OnAwake()
		{
			base.OnAwake();

			rb = GetComponent<Rigidbody>();
			rb2d = GetComponent<Rigidbody2D>();
			GetComponents(iTransformControllers);

			teleportThresholdSqrMag = teleportThreshold <= 0 ? 0 : teleportThreshold * teleportThreshold;

			ConnectSharedCaches();

			allowInterpolation = true;
			allowReconstruction = true;

			// cache whether this syncTransform is allowed to reconstruct missing frames
			for (int i = 0; i < iTransformControllers.Count; ++i)
			{
				var controller = iTransformControllers[i];
				allowInterpolation &= !controller.HandlesInterpolation;
				allowReconstruction &= !controller.HandlesExtrapolation;
			}
			//Debug.LogWarning(name + " overrides " + allowInterpolation + " " + allowReconstruction);
		}

		private void ConnectSharedCaches()
		{
			if (masterSharedCrushers.ContainsKey(prefabInstanceId))
				transformCrusher = masterSharedCrushers[prefabInstanceId];
			else
				masterSharedCrushers.Add(prefabInstanceId, transformCrusher);
		}

		private void OnDestroy()
		{
			framePool.Push(frames);
		}

#if PUN_2_OR_NEWER

		private bool rbDefaultKinematic;
		private RigidbodyInterpolation rbDefaultInterp;
		private RigidbodyInterpolation2D rb2dDefaultInterp;
#endif

		#region Frames

		public class Frame : FrameBase
		{
			public bool hasTeleported;
			public Matrix m;
			public CompressedMatrix cm;
			public SyncTransform owner;
			public Matrix telem;
			public CompressedMatrix telecm;
			public int parentHash;
			public int telePparentHash;

			public Frame() : base()
			{
				m = new Matrix();
				cm = new CompressedMatrix();
				telem = new Matrix();
				telecm = new CompressedMatrix();
				parentHash = -2;
			}

			public Frame(SyncTransform sst, int frameId) : base(frameId)
			{
				m = new Matrix();
				cm = new CompressedMatrix();
				telem = new Matrix();
				telecm = new CompressedMatrix();
				sst.transformCrusher.Capture(sst.transform, cm, m);
				var par = sst.transform.parent;
				parentHash = par ? par.GetInstanceID() : -1;
			}

			public Frame(Frame srcFrame, int frameId) : base(frameId)
			{
				m = new Matrix();
				cm = new CompressedMatrix();
				telem = new Matrix();
				telecm = new CompressedMatrix();
				CopyFrom(srcFrame);
			}

			public void Set(SyncTransform sst, int frameId)
			{
				sst.transformCrusher.Capture(sst.transform, cm, m);
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);
				Frame src = sourceFrame as Frame;

				/// When copying a teleport frame, we use the tele values.
				if (src.hasTeleported)
				{
					m.CopyFrom(src.telem);
					cm.CopyFrom(src.telecm);
				}
				else
				{
					m.CopyFrom(src.m);
					cm.CopyFrom(src.cm);
				}

				hasTeleported = false; // src.hasTeleported;
				parentHash = src.parentHash;
			}

			//static readonly StringBuilder strb = new StringBuilder();
			/// <summary>
			/// Compares only the compressed values for equality
			/// </summary>
			public bool FastCompareCompressed(Frame other)
			{
				bool match = cm.Equals(other.cm);

				if (match)
					return true;

				return false;
			}
			/// <summary>
			/// Compares only the compressed values for equality
			/// </summary>
			public bool FastCompareUncompressed(Frame other)
			{
				return
					m.position == other.m.position &&
					m.rotation == other.m.rotation &&
					m.scale == other.m.scale;
			}

			public override void Clear()
			{
				base.Clear();
				hasTeleported = false;
				parentHash = -2;
			}
			public override string ToString()
			{
				return "[" + frameId + " " + m.position + " / " + m.rotation + "]";
			}
		}

		/// <summary>
		/// We reuse frame buffers for Transforms.
		/// </summary>
		protected override void PopulateFrames()
		{
			/// Get frames from pool or create a new array.
			if (framePool.Count == 0)
			{
				frames = new Frame[frameCount + 1];
				/// Get the offtick frame the slow way, then just copy that for all the other frames.
				frames[frameCount] = new Frame(this, frameCount);
				for (int i = 0; i <= frameCount; ++i)
					frames[i] = new Frame(frames[frameCount], i);
			}
			else
			{
				/// Get pooled frame, and populate with starting values from this
				frames = framePool.Pop();
				/// Get the offtick frame the slow way, then just copy that for all the other frames.
				frames[frameCount].Set(this, frameCount);
				for (int i = 0; i < frameCount; ++i)
					frames[i].CopyFrom(frames[frameCount]);
			}
		}

		#endregion

		protected void CaptureCurrent(Matrix m, CompressedMatrix cm, Realm realm = Realm.Primary, bool forceUseTransform = false)
		{
			if (forceUseTransform)
			{
				transformCrusher.Capture(transform, cm, m);
			}
			else if (rb)
			{
				Rigidbody realmRb = realm == Realm.Primary ? rb : null;

				transformCrusher.Capture(realmRb, cm, m);

			}
			/// TODO: Not currently working
			else if (rb2d)
			{
				Rigidbody2D realmRb = realm == Realm.Primary ? rb2d : null;

				transformCrusher.Capture(realmRb, cm, m);
			}
			else
			{
				transformCrusher.Capture(transform, cm, m);
			}
		}

		public virtual void OnCaptureCurrentState(int frameId, Realm realm)
		{
			//Debug.LogError(" CAP " + ReferenceEquals(transformCrusher.PosCrusher.XCrusher, WorldBoundsSO.single.worldBoundsGroups[0].crusher.XCrusher) 
			//	+ " " + transformCrusher.PosCrusher.XCrusher.Resolution + " " + WorldBoundsSO.single.worldBoundsGroups[0].crusher.XCrusher.Resolution);

			Frame frame = frames[frameId];
			frame.hasTeleported = hasTeleported;

			if (hasTeleported)
			{
				//Debug.LogError(frameId + " <color=blue>SST HasTeleported</color> m: " + frame.m.position + " -> tm: " + frame.telem.position + " "
				//	+ (transform.parent ? transform.parent.name : "null"));


				/// We want to use the captured values for the m and cm, as they were captured before possible parent change post teleport.
				frame.cm.CopyFrom(preTeleportCM);
				frame.m.CopyFrom(preTeleportM);
				CaptureCurrent(frame.telem, frame.telecm, Realm.Primary, true);
				transformCrusher.Apply(transform, frame.telem);

				//if (GetComponent<SyncPickup>())
				//	Debug.Log(Time.time + " " + name + " " + frameId + " <b>TELE</b> " + frame.telem.position + " : " + frame.m.position + " " + (transform.parent ? transform.parent.name : "null"));

				hasTeleported = false;
			}
			else
			{
				CaptureCurrent(frame.m, frame.cm, realm);

				//if (GetComponent<SyncPickup>())
				//	Debug.Log(Time.time + " " + name + " " + frameId + " <b>CAP</b> " + frame.telem.position + " : " + frame.m.position + " " + (transform.parent ? transform.parent.name : "null"));
			}

		}

		#region Serialization

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{

			/// Don't transmit data if this component is disabled. Allows for muting components
			/// Simply by disabling them at the authority side.
			if (!isActiveAndEnabled)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}

			Frame frame = frames[frameId];
			bool isKeyframe = IsKeyframe(frameId);
			bool forceForNewConn = keyframeRate == 0 && (writeFlags & SerializationFlags.NewConnection) != 0;

			/// Only check for changes if we aren't forced to send by a keyframe.
			if (!forceForNewConn && !isKeyframe)
			{

				bool hascontent = useDeltas && (prevSentFrame == null || !frame.cm.Equals(prevSentFrame.cm));

				if (!hascontent)
				{
					buffer.WriteBool(false, ref bitposition);
					prevSentFrame = frame;
					//Debug.LogError("Skipping " + frameId);
					return SerializationFlags.None;
				}
			}

			//Debug.LogError("OUT " + frameId + " " + frame.m.position);

			/// has content bool
			buffer.WriteBool(true, ref bitposition);

			///Teleport handling
			bool _hasTeleported = frame.hasTeleported;
			buffer.WriteBool(_hasTeleported, ref bitposition);
			if (_hasTeleported)
			{
				//Debug.LogError(Time.time + " " + name + " " + frameId + " <b>SER TELE</b>");
				transformCrusher.Write(frame.telecm, buffer, ref bitposition);
			}
			/// TRS handling
			transformCrusher.Write(frame.cm, buffer, ref bitposition);
			transformCrusher.Decompress(frame.m, frame.cm);
			prevSentFrame = frame;

			//if (GetComponent<SyncPickup>())
			//	Debug.Log(Time.time + " " + name + " " + frameId + " <b>ST SER </b>" + frame.m.position + " -> " + frame.telem.position);

			//if (_hasTeleported)
			//	return SerializationFlags.HasChanged | SerializationFlags.ForceReliable;
			//else
			return SerializationFlags.HasChanged;
		}

		public Frame prevSentFrame;

		public SerializationFlags OnNetDeserialize(int originFrameId, int localFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			//if (arrival != FrameArrival.IsFuture && snapFrame != null)
			//	Debug.Log("arrival lcl: " + localFrameId + " sn:" + snapFrame.frameId + " " + arrival);

#if PUN_2_OR_NEWER
			/// Needs to ignore any incoming updates that are the server/relay mirroring back what we sent
			var frame = (pv.IsMine) ? offtickFrame : frames[localFrameId];
#else
			Frame frame = null;
#endif
			/// If enabled flag is false, we are done here.
			if (!buffer.ReadBool(ref bitposition))
			{
				frame.content = FrameContents.Empty;

				return SerializationFlags.None;
			}

			frame.content = FrameContents.Complete;

			bool _hasTeleported = buffer.ReadBool(ref bitposition);
			frame.hasTeleported = _hasTeleported;

			if (_hasTeleported)
			{
				//Debug.Log(localFrameId + " RCV TELE : trgf: " + (targFrame != null ? targFrame.frameId.ToString() : "null"));
				transformCrusher.Read(frame.telecm, buffer, ref bitposition);
				transformCrusher.Decompress(frame.telem, frame.telecm);
			}

			transformCrusher.Read(frame.cm, buffer, ref bitposition);
			transformCrusher.Decompress(frame.m, frame.cm);

			//if (GetComponent<SyncPickup>())
			//	Debug.Log(Time.time + " " + name + " <b>" + localFrameId + " DES TRANS</b> " + frame.m.position + " -> " + frame.telem.position + (_hasTeleported ? " <b>tele</b>" : ""));

			return /*frame.hasTeleported ? SerializationFlags.ForceReliable :*/ SerializationFlags.HasChanged;
		}

		#endregion


		protected override void InitialCompleteSnapshot(Frame frame)
		{
			base.InitialCompleteSnapshot(frame);

			//Debug.LogWarning(name + "Inital Complete snap " + frame.m.position + " -> " + frame.telem.position + 
			//	" tele? " + frame.hasTeleported + " " + (transform.parent ? transform.parent.name : "noParent"));

			transformCrusher.Apply(transform, frame.m);

			pre2Frame.CopyFrom(frame);
			pre1Frame.CopyFrom(frame);
			snapFrame.CopyFrom(frame);
		}

		protected bool skipInterpolation;

		public override bool OnSnapshot(int newTargetFrameId)
		{


			bool ready = base.OnSnapshot(newTargetFrameId);

			if (!ready)
			{
				return false;
			}

			bool snapComplete = snapFrame.content == FrameContents.Complete;

			if (!snapComplete)
				return false;

			bool targComplete = targFrame.content == FrameContents.Complete;

			bool snapTeleported = snapFrame.hasTeleported;

			targFrame.parentHash = teleNewParentId;

			/// Clear the teleport flag every tick
			skipInterpolation = false;

			//if (snapFrame.hasTeleported)
			//	Debug.LogWarning(Time.time + " " + name + " TELEFRAME fr: " + newTargetFrameId + " " + snapFrame.m.position + " -> " + snapFrame.telem.position);

			/// Test for need to auto-teleport (excessive distance change)
			if (!snapTeleported && targComplete)
			{
				if (teleportThresholdSqrMag > 0)
				{
					/// If the targF is not a valid frame, we will use the current interpolated scene position for this test.
					var newpos = targFrame.m.position;
					var oldpos = snapTeleported ? snapFrame.telem.position : snapFrame.m.position;

					if (Vector3.SqrMagnitude(newpos - oldpos) > teleportThresholdSqrMag)
					{
#if UNITY_EDITOR
						Debug.LogWarning(Time.time + " " + name + " fr: " + newTargetFrameId + " teleportThreshold distance exceeded. Teleport Distance: " + Vector3.Distance(newpos, oldpos) + " / " + teleportThreshold
							+ "  sqrmag: " + Vector3.SqrMagnitude(newpos - oldpos) + " / " + teleportThresholdSqrMag + " " + newpos + " " + oldpos + " snapComplete? " + snapComplete);
#endif
						skipInterpolation = true;
					}
				}
			}

			if (snapComplete)
			{
				//if (GetComponent<SyncVitals>())
				//{
				//	var str = snapFrame.frameId + ":" + targFrame.frameId + " " + snapFrame.hasTeleported + ":" + targFrame.hasTeleported + " ";
				//	str += (transform.parent ? ("<b>" + transform.parent.name +"</b>") : "<b>null</b>") + " SNAP TRANS";
				//	str += " snappos: <b>" + snapFrame.m.position * 100 + "</b>";
				//	str +=	(snapFrame.hasTeleported ? (" <b>tele: </b>" + snapFrame.telem.position) : "") + " targpos: " + targFrame.m.position;
				//	Debug.Log(str);

				//}

				transformCrusher.Apply(transform, snapTeleported ? snapFrame.telem : snapFrame.m);
			}

			return true;
		}

		public override bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
			if (skipInterpolation)
				return false;

			bool ready = base.OnInterpolate(snapFrameId, targFrameId, t);

			if (!ready)
				return false;

			//Debug.Log(Time.time + "  " + snapFrameId + ":" + targFrameId + "  TRANS Interp");

			if (interpolation == Interpolation.None)
				return false;

			if (ReferenceEquals(targFrame, null))
				return false;

			if (snapFrame.content != FrameContents.Complete)
				return false;

			if (targFrame.content != FrameContents.Complete)
				return false;

			//if (snapFrame.parentHash == targFrame.parentHash == )
			//Debug.Log("<b>Mismatch </b>" + snapFrame.parentHash + " " + targFrame.parentHash + " " + (transform.parent ? transform.parent.GetInstanceID() : -1));
			if (snapFrame.parentHash != targFrame.parentHash)
				return false;

			var snapM = snapFrame.hasTeleported ? snapFrame.telem : snapFrame.m;
			//if (snapFrame.hasTeleported)
			//	Debug.Log("Snap Tele " + snapFrame.m.position + " -> " + snapM.position + " -> " + targFrame.m.position);

			if (interpolation == Interpolation.Linear || pre1Frame.content != FrameContents.Complete)
				Matrix.Lerp(Matrix.reusable, snapM, targFrame.m, t);
			///TODO: teleport handling with Catmul non existant
			else
				Matrix.CatmullRomLerpUnclamped(Matrix.reusable, pre1Frame.m, snapFrame.m, targFrame.m, t);

			//if (transform.GetComponent<SyncPickup>())
			//	Debug.Log(name + " " + transform.localEulerAngles);

			transformCrusher.Apply(transform, Matrix.reusable);

			return true;
		}

		#region Reconstruction


		protected override FrameContents InterpolateFrame(Frame targ, Frame start, Frame end, float t)
		{
			//return FrameContents.Empty;

			/// Don't interpolate if parent has changed - -2 indicates unknown. Checking for -2 so that both being -2 doesn't get treated as "same".
			if (start.parentHash == -2 || start.parentHash != end.parentHash)
			{
				return FrameContents.Empty;
				//targ.CopyFrom(end);
			}
			else
			{
				targ.CopyFrom(end);
				Matrix.Lerp(targ.m, start.hasTeleported ? start.telem : start.m, end.m, t);
				transformCrusher.Compress(targ.cm, targ.m);
			}

			return FrameContents.Complete;
		}

		protected override FrameContents ExtrapolateFrame()
		{

			if (extrapolateRatio == 0)
				return FrameContents.Empty;

			/// TODO: Not tested these uses of .Partial yet.
			
			/// Don't extrapolate if we don't have a valid snapframe
			if (snapFrame.content != FrameContents.Complete)
				return FrameContents.Empty;

			/// Copy Snap to get any teleport info copied over? (I forget what this was for)
			targFrame.CopyFrom(snapFrame);
			targFrame.parentHash = teleNewParentId;

			/// Don't extrapolate if we had a parent change. hash of -2 indicates "Unknown". Check for -2 because if both are that, they would look like match (when they are not).
			if (pre1Frame.content == FrameContents.Complete)
			{
				var pre1par = pre1Frame.parentHash;
				var snappar = snapFrame.parentHash;
				if (pre1par == -2 || pre1par != snappar || pre1Frame.hasTeleported)
				{
					//if (GetComponent<SyncPickup>())
					//	Debug.Log(Time.time + " <b>" + targFrame.frameId + " [" + pre1Frame.parentHash + " : " + snapFrame.parentHash + "]  Extrap</b> by copy snapFrame " + snapFrame.m.position + " -> " + snapFrame.telem.position + (pre1Frame.hasTeleported ? " <b>tele</b>" : ""));

					return FrameContents.Empty;
				}
			}

			/// Don't try to extrapolate if pre1 is invalid, or if this crosses a teleport.
			else
			{
				//if (GetComponent<SyncPickup>())
				//	Debug.Log(Time.time + " <b>" + targFrame.frameId + " Extrap</b> by copy snapFrame " + snapFrame.m.position + " -> " + snapFrame.telem.position);

				return FrameContents.Empty;
			}

			//if (GetComponent<SyncPickup>())
			//	Debug.Log(Time.time + " " + targFrame.frameId + " <b>Extrap</b> by LERP Pre1 " + pre1Frame.frameId + ":" + snapFrame.frameId + " "
			//		+ ((pre1Frame.m.position - snapFrame.m.position).magnitude > 1 ? "<color=red>" : "<color=blue>")
			//		+ pre1Frame.parentHash + ":" + snapFrame.parentHash + " " + pre1Frame.m.position + " " + snapFrame.m.position + "</color>");

			Matrix.LerpUnclamped(targFrame.m, pre1Frame.m, snapFrame.m, 1 + extrapolateRatio);
			transformCrusher.Compress(targFrame.cm, targFrame.m);

			return FrameContents.Complete;
		}

		#endregion


	}
}

