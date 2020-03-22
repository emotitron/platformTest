//Copyright 2018, Davin Carten, All rights reserved

using emotitron.Utilities;
using UnityEngine;

namespace emotitron.Networking
{
	public class ConnectionTick
	{
		public FastBitMask128 validFrames;
		public int originToLocal, localToOrigin;
		public int numOfSequentialFramesWithTooSmallBuffer;
		public int numOfSequentialFramesWithTooLargeBuffer;
		public bool frameArrivedTooLate;
		public bool hadInitialSnapshot;
		public int advanceCount;
		public float[] frameArriveTime;
		public float[] frameConsumeTime;

		// cached values
		public static int validFrameLookAhead;
		public static int frameCount;
		public static int frameCountBits;
		public static int quaterFrameCount;
		public static int ticksBeforeGrow;
		public static int ticksBeforeShrink;
		public static int targetBufferSize;

		static ConnectionTick()
		{
			frameCount = SimpleSyncSettings.FrameCount;
			frameCountBits = SimpleSyncSettings.FrameCountBits;
			quaterFrameCount = SimpleSyncSettings.QuaterFrameCount;
			ticksBeforeGrow = SimpleSyncSettings.TicksBeforeGrow;
			ticksBeforeShrink = SimpleSyncSettings.TicksBeforeShrink;
			targetBufferSize = SimpleSyncSettings.TargetBufferSize;
			validFrameLookAhead = Mathf.Min(SimpleSyncSettings.maxBufferSize * 2, quaterFrameCount - 1);
		}

		public ConnectionTick(int originToLocal, int localToOrigin)
		{
			this.originToLocal = originToLocal;
			this.localToOrigin = localToOrigin;

			validFrames = new FastBitMask128(SimpleSyncSettings.FrameCount + 1);
			frameArriveTime = new float[frameCount];
		}

		/// <summary>
		/// Checks the state of the buffer, and returns the number of snapshots to advance to keep the buffer happy.
		/// </summary>
		public void SnapshotAdvance()
		{
			int currFrameId = NetMaster.CurrentFrameId;

			/// TODO: May be able to reduce this in the future to a less aggressive look ahead
			int validCount = validFrames.CountValidRange(currFrameId, quaterFrameCount);

			if (!hadInitialSnapshot)
			{

				if (validCount == 0)
				{
					advanceCount = 0;
					return;
				}
				else if (validCount > targetBufferSize)
				{
					advanceCount = validCount - targetBufferSize;
					return;
				}
			}

			/// Buffer emptied - either means drop/sever connection hang, or way behind.
			if (validCount == 0)
			{
				/// No valid frames, but we just received one late - buffer needs IMMEDIATE HARD correction
				if (frameArrivedTooLate)
				{
					numOfSequentialFramesWithTooLargeBuffer = 0;
					numOfSequentialFramesWithTooSmallBuffer = 0;
					frameArrivedTooLate = false;

#if SNS_WARNINGS && UNITY_EDITOR
					Debug.LogWarning(Time.time + " <b><color=red>Frame arrived late with empty buffer</color> - HOLD</b> " + numOfSequentialFramesWithTooSmallBuffer + "/" + ticksBeforeGrow);
#endif
					advanceCount = 0;
					return;
				
				}
				/// No frames have arrived late, looks like bad packetloss. Don't adjust the buffer in case it corrects.
				else
				{
					numOfSequentialFramesWithTooLargeBuffer = 0;
					advanceCount = 1;
				}

			}
			/// Buffer is too small
			else if (validCount < SimpleSyncSettings.minBufferSize)
			{
				numOfSequentialFramesWithTooLargeBuffer = 0;
				numOfSequentialFramesWithTooSmallBuffer += (frameArrivedTooLate ? 2 : 1);
				frameArrivedTooLate = false;

				if (numOfSequentialFramesWithTooSmallBuffer >= ticksBeforeGrow)
				{
#if SNS_WARNINGS && UNITY_EDITOR
					Debug.LogWarning(Time.time + " <b>Buffer Low</b> - <b>HOLD</b> " + currFrameId + " buffsze: " + validCount);
#endif
					advanceCount = 0;
					return;
				}
				else
					advanceCount = 1;
			}
			/// Buffer is too large
			else if (validCount > SimpleSyncSettings.maxBufferSize)
			{
				numOfSequentialFramesWithTooSmallBuffer = 0;
				if (numOfSequentialFramesWithTooLargeBuffer > ticksBeforeGrow)
				{

					/// Limit advance to only one extra snapshot to shrink the buffer, unless this is startup - then we need to burn all backlog.
					advanceCount = (validCount - targetBufferSize) + 1;

#if SNS_WARNINGS&& UNITY_EDITOR
					Debug.LogWarning(Time.time + " <b>SKIP  </b>Trimming Oversized Buffer advance: " + advanceCount + " validCount: " + validCount
						+ " frameArrivedTooLate:" + frameArrivedTooLate);
#endif
					numOfSequentialFramesWithTooLargeBuffer = 0; // /= 2;
				}
				else
				{
					advanceCount = 1;
					numOfSequentialFramesWithTooLargeBuffer++;
				}
			}
			/// Buffer is happy.
			else
			{
				numOfSequentialFramesWithTooLargeBuffer = 0;
				numOfSequentialFramesWithTooSmallBuffer = (frameArrivedTooLate ? 1 : 0);
				advanceCount = 1;
			}

			frameArrivedTooLate = false;
			return;
		}

		public void PostSnapshot()
		{
			int frameCount = SimpleSyncSettings.FrameCount;
			int currFrameId = NetMaster.CurrentFrameId;

			int invalidate = currFrameId - (quaterFrameCount);
			if (invalidate < 0)
				invalidate += frameCount;

			/// This clear could be a bit more intentional
			validFrames.ClearBitsBefore(invalidate, quaterFrameCount);

			if (advanceCount > 0)
				hadInitialSnapshot = true;

			if (advanceCount != 1)
			{
				localToOrigin += (advanceCount - 1);
				if (localToOrigin < 0)
					localToOrigin += frameCount;
				else if (localToOrigin >= frameCount)
					localToOrigin -= frameCount;

				originToLocal = frameCount - localToOrigin;
				if (originToLocal < 0)
					originToLocal += frameCount;
			}
		}
	}
}

