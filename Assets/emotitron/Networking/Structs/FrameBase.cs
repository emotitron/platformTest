﻿
namespace emotitron.Networking
{
	public enum FrameContents { Empty, Partial, NoChange, Complete }

	/// <summary>
	/// Extend this base class for derived SyncObjectTFrame to include networked variables.
	/// </summary>
	public abstract class FrameBase
	{
		public int frameId;
		public FrameContents content;
		//public bool isCompleteFrame;

		public FrameBase()
		{

		}
		public FrameBase(int frameId)
		{
			this.frameId = frameId;
		}

		public virtual void CopyFrom(FrameBase sourceFrame)
		{
			content = sourceFrame.content;
		}
		//public abstract bool Compare(FrameBase frame, FrameBase holdframe);

		public virtual void Clear()
		{
			content = FrameContents.Empty;
		}

		public static void PopulateFrames<TFrame>(ref TFrame[] frames) where TFrame : FrameBase, new()
		{
			int frameCount = SimpleSyncSettings.FrameCount;
			frames = new TFrame[frameCount + 1];
			for (int i = 0; i <= frameCount; ++i)
			{
				TFrame frame = new TFrame() { frameId = i };
				frames[i] = frame;

			}
		}
	}
}
