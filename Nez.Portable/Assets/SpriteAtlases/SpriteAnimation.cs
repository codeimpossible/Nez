using System;
using Nez.Textures;

namespace Nez.Sprites
{
	public enum SpriteAnimationTimingMethod
	{
		FramesPerSecond,
		FrameTimeMilliseconds,
	}
	public class SpriteAnimation
	{
		public readonly Sprite[] Sprites;
		public readonly float[] FrameRates;
		public readonly SpriteAnimator.LoopMode LoopMode;
		public readonly SpriteAnimationTimingMethod Timing;

		public SpriteAnimation(
			Sprite[] sprites,
			float frameRate,
			SpriteAnimator.LoopMode loopMode = SpriteAnimator.LoopMode.Loop,
			SpriteAnimationTimingMethod timing = SpriteAnimationTimingMethod.FramesPerSecond)
		{
			Sprites = sprites;
			FrameRates = new float[sprites.Length];
			LoopMode = loopMode;
			Timing = timing;
			for(int i = 0; i < FrameRates.Length; ++i)
			{
				FrameRates[i] = frameRate;
			}
		}

		public SpriteAnimation(
			Sprite[] sprites,
			float[] frameRates,
			SpriteAnimator.LoopMode loopMode = SpriteAnimator.LoopMode.Loop,
			SpriteAnimationTimingMethod timing = SpriteAnimationTimingMethod.FramesPerSecond)
		{
			Sprites = sprites;
			FrameRates = frameRates;
			LoopMode = loopMode;
			Timing = timing;
		}
	}
}
