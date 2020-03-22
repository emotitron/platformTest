// Copyright 2019, Davin Carten, All rights reserved
// This code may be used for game development, but may not be used in any tools or assets that are sold to other developers.

using UnityEngine;
using emotitron.Utilities.Networking;
using emotitron.Networking.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Networking
{

	/// <summary>
	/// SyncObject base class with handling for frames and frame history.
	/// </summary>
	/// <typeparam name="TFrame">The derived FrameBase class to be used as the frame.</typeparam>
	public abstract class SyncObject<TFrame> : SyncObject
		where TFrame : FrameBase, new()
	{

		[System.NonSerialized] public TFrame[] frames;

		/// Runtime vars
		protected TFrame pre2Frame, pre1Frame, snapFrame, targFrame, nextFrame, offtickFrame;
		protected bool hadInitialSnapshot;
		protected bool hadInitialCompleteSnapshot;

		/// <summary>
		/// When overriding, but sure to keep base.Awake(). Also, frames are created and given indexes, but any other Initialization will still need to be
		/// explictly called in the derived Awake().
		/// </summary>
		public override void OnAwake()
		{
			if (keyframeRate > SimpleSyncSettings.MaxKeyframes)
			{
				keyframeRate = SimpleSyncSettings.MaxKeyframes;
				Debug.LogWarning(name + "/" + GetType().Name + " keyframe setting exceeds max allowed for the current " + SimpleSyncSettings.single.name + ".frameCount setting. Reducing to " + keyframeRate);
			}
			base.OnAwake();

			PopulateFrames();

			offtickFrame = frames[frameCount];
		}

		public override void OnPostDisable()
		{
			base.OnPostDisable();
			///TEST - Reset when disabled so new initialization isn't ignored (for scene objects after disconn and reconn)
			hadInitialSnapshot = false;
			hadInitialCompleteSnapshot = false;
		}

		public override void OnAuthorityChanged(bool isMine, bool asServer)
		{
			base.OnAuthorityChanged(isMine, asServer);

			for (int i = 0; i < frameCount; ++i)
				frames[i].Clear();

			/// TODO: Not entirely sure this reset is useful currently. Was put in place for making item authority changes work.
			if (isMine)
			{
				hadInitialCompleteSnapshot = false;
			}

			///TODO: some logic about when this happens may be useful. Originally was only when changing to IsMine
			hadInitialSnapshot = false;

		}

		/// <summary>
		/// Override this with frame initialization code. The default base just creates the frame instances and assigns them index values.
		/// </summary>
		protected virtual void PopulateFrames()
		{
			/// Basic factory, just gives each frame an index.
			FrameBase.PopulateFrames(ref frames);
		}

		/// <summary>
		/// Set all of the initial current frame caches.
		/// </summary>
		/// <param name="frameId"></param>
		public virtual void InitialSnapshot(TFrame frame)
		{
			//Debug.Log(Time.time + " " + frame.frameId + " " + name + " <b>INIT SNAP</b> ");
			int frameId = frame.frameId;

			int prevprevId = (frameId - 3);
			if (prevprevId < 0) prevprevId += frameCount;

			int prevId = (frameId - 2);
			if (prevId < 0) prevId += frameCount;

			int snapId = (frameId - 1);
			if (snapId < 0) snapId += frameCount;

			int nextId = (frameId + 1);
			if (nextId >= frameCount) nextId -= frameCount;

			pre2Frame = frames[prevprevId];
			pre1Frame = frames[prevId];
			snapFrame = frames[snapId];
			targFrame = frame;
			nextFrame = frames[nextId];

			hadInitialSnapshot = true;
		}

		protected virtual void InitialCompleteSnapshot(TFrame frame)
		{
			
		}


		/// <summary>
		/// Called every simulation tick, right after the frameId has been incremented by NetMaster. Base class advances/increments all of the frame references.
		/// </summary>
		/// <param name="frameId"></param>
		/// <param name="initialize"></param>
		/// <returns>Base will return false if snapshot is not ready.</returns>
		public virtual bool OnSnapshot(int frameId)
		{
			///TODO:should this be active in hierarchy?
			if (!enabled)
				return false;
			
			//if (!hadInitialSnapshot)
			//	return false;

			TFrame newTargFrame = frames[frameId];

			bool isInitial;
			/// Our initial Snapshot needs to initialize the frame numbering
			if (!hadInitialSnapshot)
			{
				InitialSnapshot(newTargFrame);
				isInitial = true;
			}
			else
			{
				isInitial = false;
				/// Invalidate old frames
				pre2Frame.Clear();

				pre2Frame = pre1Frame;
				pre1Frame = snapFrame;
				snapFrame = targFrame;
				targFrame = newTargFrame;

				int nextId = (frameId + 1);
				if (nextId >= frameCount)
					nextId -= frameCount;

				nextFrame = frames[nextId];
				
				/// Non authority connections reconstruct this frame if it's missing.
				if (!IsMine)
				{
					if (AllowReconstruction)
					{
						var offsets = TickManager.perConnOffsets[ControllerActorNr];

						if (!offsets.validFrames[frameId])
						{
							ConstructMissingFrame(frameId);

							//if (targFrame.content != FrameContents.Complete && GetType() == typeof(SyncState))
							//	Debug.LogError(targFrame.frameId + " MISSING " + targFrame.content);
						}
						else
						{
							//if (targFrame.content != FrameContents.Complete && GetType() == typeof(SyncState))
							//	Debug.LogError(targFrame.frameId + " RECON " + targFrame.content);

							switch (targFrame.content)
							{
								case FrameContents.Empty:
									ReconstructEmptyFrame();
									break;

								case FrameContents.Partial:
									ReconstructIncompleteFrame();
									break;

								case FrameContents.NoChange:
									targFrame.CopyFrom(snapFrame);
									break;

								case FrameContents.Complete:
									break;

							}
						}
					}
				}
			}

			var targContent = newTargFrame.content;
			if (!hadInitialCompleteSnapshot && (targContent == FrameContents.Complete || (IsKeyframe(frameId) && targContent != 0)))
			{
				//Debug.Log("<color=green>Initial - ReadyState = ready</color>");
				InitialCompleteSnapshot(newTargFrame);
				hadInitialCompleteSnapshot = true;
				ReadyState = ReadyStateEnum.Ready;
				ApplySnapshot(isInitial, true);
			}
			else
				ApplySnapshot(isInitial, false);

			return hadInitialCompleteSnapshot;
		}

		///TODO: Make this abstract and use everywhere rather than OnSnapshot?
		/// <summary>
		/// Only fires when object is flagged as Ready
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="isInitial"></param>
		protected virtual void ApplySnapshot(bool isInitial, bool isInitialComplete)
		{

		}

		public virtual bool AllowInterpolation { get { return true; } }
		public virtual bool AllowReconstruction { get { return true; } }

		/// <summary>
		/// Handling if a frame arrived, but the frame was flagged as hasConent = false and isComplete = false
		/// </summary>
		protected virtual void ReconstructEmptyFrame()
		{
			targFrame.CopyFrom(snapFrame);
		}

		/// <summary>
		/// Handling if a frame arrived, but the frame was flagged as hasConent = true and isComplete = false
		/// </summary>
		protected virtual void ReconstructIncompleteFrame()
		{
			targFrame.CopyFrom(snapFrame);
		}

		protected virtual void ConstructMissingFrame(int frameId)
		{

			var offsets = TickManager.perConnOffsets[ControllerActorNr];

			/// if we are currently on a valid frame, we can attempt to look forward for another valid frame to reconstruct with a tween.
			bool snapFrameIsValid = snapFrame.content == FrameContents.Complete; // offsets.validFrames[snapFrame.frameId];

			if (snapFrameIsValid)
			{
				const int MAX_LOOKAHEAD = 3;
				for (int i = 2; i <= MAX_LOOKAHEAD; ++i)
				{
					int futureFid = frameId + i;
					if (futureFid >= frameCount)
						futureFid -= frameCount;

					if (offsets.validFrames[futureFid] && frames[futureFid].content == FrameContents.Complete) //  ((validMask & (ulong)1 << futureFid) != 0)
					{
						float t = 1f / i;
						targFrame.content = InterpolateFrame(targFrame, snapFrame, frames[futureFid], t);
						//Debug.LogError(targFrame.frameId + ":" + targFrame.content + " Extrapolated by Interpolation " + snapFrame.frameId + "-->" + frames[futureFid].frameId);
						return;
					}
				}
			}
			
			/// No future valid frame found, just do a regular extrapolation
			targFrame.content = ExtrapolateFrame();

			//if (targFrame.content != 0)
			//	Debug.LogError(targFrame.frameId + ":" + targFrame.content + 
			//		" Extrapolated by Interpolation " + pre1Frame.frameId + ":" + pre1Frame.content + " " + 
			//		snapFrame.frameId + ":" + snapFrame.content);

		}

		/// <summary>
		/// Interpolate between SnapFrame and TargFrame.
		/// </summary>
		/// <param name="t"></param>
		/// <returns>Base will return false if snapshot is not ready. Set to true if interpolation can be done.</returns>
		public virtual bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
			if (!AllowInterpolation)
				return false;

			if (!isActiveAndEnabled)
				return false;

			if (!hadInitialSnapshot)
				return false;

			if (IsMine)
				return false;
			
			return true;
		}

		/// <summary>
		/// Interpolate used to construct a new from from two existing frames. Return true if this should flag that new frame now as having content.
		/// </summary>
		protected virtual FrameContents InterpolateFrame(TFrame targ, TFrame start, TFrame end, float t)
		{
			targ.Clear();
			return FrameContents.Empty;
		}
		/// <summary>
		/// Interpolate a new TargFrame from previous frames. Return true if this should flag that new frame now as having content.
		/// </summary>
		protected virtual FrameContents ExtrapolateFrame()
		{
			targFrame.Clear();
			return FrameContents.Empty;
		}
		
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(SyncObject<>), true)]
	[CanEditMultipleObjects]
	public class SyncObjectTFrameEditor : SyncObjectEditor
	{
		protected override string HelpURL
		{
			get
			{
				return "https://docs.google.com/document/d/1ySmkOBsL0qJnIk7iN9lbXPlfmYTGkN7JFgKDBdqj9e8/edit#bookmark=id.30lk0bcaxud";
			}
		}
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

		}
	}

#endif
}

