using System;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite.Utils;
using Nez.Aseprite;
using Nez.Sprites;
using Nez.Textures;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Nez.Systems;

public static class AsepriteFileExtensions
{
	/// <summary>
	/// Flattens this frame by blending all cel elements on the specified layers into a single image.
	/// </summary>
	/// <param name="onlyVisibleLayers">
	/// Indicates whether only cels that are on visible layers should be included when flattening this frame.
	/// </param>
	/// <param name="includeBackgroundLayer">
	/// Indicates whether the cel on the layer marked as the background layer in Aseprite should be included when
	/// flattening this frame.
	/// </param>
	/// <returns>
	/// A new array of color elements where each element represents the final pixels for this frame once flattened.
	/// Order of color element starts with the top-left most pixel and is read left-to-right from top-to-bottom.
	/// </returns>
	public static Color[] FlattenFrameOnLayers(this AsepriteFrame frame, bool onlyVisibleLayers = true, bool includeBackgroundLayer = false, string[] layers = null)
	{
		Color[] result = new Color[frame.Size.Width * frame.Size.Height];

		for (int c = 0; c < frame.Cels.Length; c++)
		{
			AsepriteCel cel = frame.Cels[c];

			//  Are we only processing cels on visible layers?
			if (onlyVisibleLayers && !cel.Layer.IsVisible) { continue; }

			//  Are we processing cels on background layers?
			if (cel.Layer.IsBackgroundLayer && !includeBackgroundLayer) { continue; }

			//	See if the current cel has the desired layers
			if (layers != null)
			{
				bool layerFound = false;

				foreach (var layer in layers)
				{
					if (cel.Layer.Name.ToLower().Equals(layer.ToLower()))
						layerFound = true;
				}

				if (!layerFound)
				{
					continue;
				}
			}

			//	Only process image cels for now.
			//	Note: Will look into adding tilemap cels in a future PR if it is requested enough or if someone
			//	else wants to add it in.  You can see how I do it in my MonoGame.Aseprite library for reference if
			//	needed.
			CheckCelType:
			if (cel is AsepriteLinkedCel linkedCel)
			{
				cel = linkedCel.Cel;
				goto CheckCelType;
			}

			if (cel is AsepriteImageCel imageCel)
			{
				frame.BlendCel(backdrop: result,
					source: imageCel.Pixels,
					blendMode: imageCel.Layer.BlendMode,
					celX: imageCel.Location.X,
					celY: imageCel.Location.Y,
					celWidth: imageCel.Size.Width,
					celOpacity: imageCel.Opacity,
					layerOpacity: imageCel.Layer.Opacity);
			}
		}

		return result;
	}

	/// <summary>
		/// Flattens this frame by blending all cel elements into a single iamge.
		/// </summary>
		/// <param name="onlyVisibleLayers">
		/// Indicates whether only cels that are on visible layers should be included when flattening this frame.
		/// </param>
		/// <param name="includeBackgroundLayer">
		/// Indicates whether the cel on the layer marked as the background layer in Aseprite should be included when
		/// flattening this frame.
		/// </param>
		/// <returns>
		/// A new array of color elements where each element represents the final pixels for this frame once flattened.
		/// Order of color element starts with the top-left most pixel and is read left-to-right from top-to-bottom.
		/// </returns>
		internal static Color[] FlattenFrameInternal(this AsepriteFrame frame, bool onlyVisibleLayers = true, bool includeBackgroundLayer = false, string layerName = null)
		{
			Color[] result = new Color[frame.Size.Width * frame.Size.Height];

			for (int c = 0; c < frame.Cels.Length; c++)
			{
				AsepriteCel cel = frame.Cels[c];

				//  Are we only processing cels on visible layers?
				if (onlyVisibleLayers && !cel.Layer.IsVisible) { continue; }

				//  Are we processing cels on background layers?
				if (cel.Layer.IsBackgroundLayer && !includeBackgroundLayer) { continue; }

				//	If layer is specified, only select frame cels on that layer
				if (layerName != null && !cel.Layer.Name.ToLower().Equals(layerName.ToLower())) {continue;}

				//	Only process image cels for now.
				//	Note: Will look into adding tilemap cels in a future PR if it is requested enough or if someone
				//	else wants to add it in.  You can see how I do it in my MonoGame.Aseprite library for reference if
				//	needed.
			CheckCelType:
				if(cel is AsepriteLinkedCel linkedCel)
				{
					cel = linkedCel.Cel;
					goto CheckCelType;
				}

				if(cel is AsepriteImageCel imageCel)
				{
					frame.BlendCel(backdrop: result,
							 source: imageCel.Pixels,
							 blendMode: imageCel.Layer.BlendMode,
							 celX: imageCel.Location.X,
							 celY: imageCel.Location.Y,
							 celWidth: imageCel.Size.Width,
							 celOpacity: imageCel.Opacity,
							 layerOpacity: imageCel.Layer.Opacity);
				}
			}

			return result;
		}

	private static void BlendCel(this AsepriteFrame frame, Color[] backdrop, ReadOnlySpan<Rgba32> source, AsepriteBlendMode blendMode, int celX, int celY, int celWidth, int celOpacity, int layerOpacity)
	{
		for (int i = 0; i < source.Length; i++)
		{
			int x = (i % celWidth) + celX;
			int y = (i / celWidth) + celY;
			int index = y * frame.Size.Width + x;

			//  Sometimes a cel can have a negative x and/or y value.  This is caused by selecting an area in within
			//  Aseprite and then moving a portion of the selected pixels outside the canvas. We don't care about
			//	these pixels, so if the index is outside the range of the array to store them in, then we'll just
			//	ignore them.
			if (index < 0 || index >= backdrop.Length) { continue; }

			Color b = backdrop[index];
			Color s = source[i].ToXnaColor();
			byte opacity = AsepriteColorUtils.MUL_UN8(celOpacity, layerOpacity);
			backdrop[index] = AsepriteColorUtils.Blend(blendMode, b, s, opacity);
		}
	}


	/// <summary>
	/// Translates the data in this aseprite file to a sprite atlas that can be used in a sprite animator component.
	/// </summary>
	/// <param name="onlyVisibleLayers">
	/// Indicates whether only layers that are visible in the Aseprite file should be included when generating the
	/// texture.
	/// </param>
	/// <param name="borderPadding">
	/// Indicates the amount of padding, in transparent pixels, to add to the edge of the generated texture.
	/// </param>
	/// <param name="spacing">
	/// Indicates the amount of padding, in transparent pixels, to add between each frame in the generated texture.
	/// </param>
	/// <param name="innerPadding">
	/// indicates the amount of padding, in transparent pixels, to add around the edges of each frame in the
	/// generated texture.
	/// </param>
	/// <param name="spriteOrigin">
	/// make the sprite origin something other than sourceRect.GetHalfSize()
	/// </param>
	/// <returns>
	/// A new instance of hte <see cref="SpriteAtlas"/> class initialized with the data generated from this Aseprite
	/// file.
	/// </returns>
	public static SpriteAtlas ToSpriteAtlas(this AsepriteFile file, bool onlyVisibleLayers = true, int borderPadding = 0, int spacing = 0, int innerPadding = 0, Vector2? spriteOrigin = null, string layerName = null)
	{
		SpriteAtlas atlas = new SpriteAtlas
		{
			Names = new string[file.Frames.Length],
			Sprites = new Sprite[file.Frames.Length],
			SpriteAnimations = new SpriteAnimation[file.Tags.Length],
			AnimationNames = new string[file.Tags.Length]
		};

		Color[][] flattenedFrames = new Color[file.Frames.Length][];

		for (var i = 0; i < file.Frames.Length; i++)
		{
			flattenedFrames[i] = file.Frames[i].FlattenFrameInternal(onlyVisibleLayers, false, layerName);
		}

		double sqrt = Math.Sqrt(file.Frames.Length);
		int columns = (int)Math.Ceiling(sqrt);
		int rows = (file.Frames.Length + columns - 1) / columns;

		int imageWidth = (columns * file.CanvasWidth)
						 + (borderPadding * 2)
						 + (spacing * (columns - 1))
						 + (innerPadding * 2 * columns);

		int imageHeight = (rows * file.CanvasHeight)
						  + (borderPadding * 2)
						  + (spacing * (rows - 1))
						  + (innerPadding * 2 * rows);

		Color[] imagePixels = new Color[imageWidth * imageHeight];
		Rectangle[] regions = new Rectangle[file.Frames.Length];

		for (int i = 0; i < flattenedFrames.GetLength(0); i++)
		{
			int column = i % columns;
			int row = i / columns;
			Color[] frame = flattenedFrames[i];

			int x = (column * file.CanvasWidth)
					+ borderPadding
					+ (spacing * column)
					+ (innerPadding * (column + column + 1));

			int y = (row * file.CanvasHeight)
					 + borderPadding
					 + (spacing * row)
					 + (innerPadding * (row + row + 1));

			for (int p = 0; p < frame.Length; p++)
			{
				int px = (p % file.CanvasWidth) + x;
				int py = (p / file.CanvasWidth) + y;

				int index = py * imageWidth + px;
				imagePixels[index] = frame[p];

			}

			regions[i] = new Rectangle(x, y, file.CanvasWidth, file.CanvasHeight);
		}

		Texture2D texture = new Texture2D(Core.GraphicsDevice, imageWidth, imageHeight);
		texture.SetData<Color>(imagePixels);

		for (int i = 0; i < file.Frames.Length; i++)
		{
			atlas.Sprites[i] = new Sprite(texture, regions[i], spriteOrigin ?? regions[i].GetHalfSize());
		}

		for (int tagNum = 0; tagNum < file.Tags.Length; tagNum++)
		{
			AsepriteTag tag = file.Tags[tagNum];
			Sprite[] sprites = new Sprite[tag.To - tag.From + 1];
			float[] durations = new float[sprites.Length];

			for (int spriteIndex = 0, lookupIndex = tag.From; spriteIndex < sprites.Length; spriteIndex++, lookupIndex++)
			{
				sprites[spriteIndex] = atlas.Sprites[lookupIndex];
				durations[spriteIndex] = 1.0f / (file.Frames[lookupIndex].Duration.Milliseconds / 1000.0f);
			}

			atlas.SpriteAnimations[tagNum] = new SpriteAnimation(sprites, durations);
			atlas.AnimationNames[tagNum] = tag.Name;
		}

		return atlas;
	}

	public static SpriteAtlas ToSpriteAtlasFromLayers(this AsepriteFile file, string[] layers = null, bool onlyVisibleLayers = true, int borderPadding = 0, int spacing = 0, int innerPadding = 0, Vector2? spriteOrigin = null)
	{
		var atlas = new SpriteAtlas
		{
			Names = new string[file.Frames.Length],
			Sprites = new Sprite[file.Frames.Length],
			SpriteAnimations = new SpriteAnimation[file.Tags.Length],
			AnimationNames = new string[file.Tags.Length]
		};

		var flattenedFrames = new Color[file.Frames.Length][];

		for (var i = 0; i < file.Frames.Length; i++)
		{
			flattenedFrames[i] = file.Frames[i].FlattenFrameOnLayers(onlyVisibleLayers, false, layers);
		}

		var sqrt = Math.Sqrt(file.Frames.Length);
		var columns = (int)Math.Ceiling(sqrt);
		var rows = (file.Frames.Length + columns - 1) / columns;

		var imageWidth = columns * file.CanvasWidth
						 + borderPadding * 2
						 + spacing * (columns - 1)
						 + innerPadding * 2 * columns;

		var imageHeight = rows * file.CanvasHeight
						  + borderPadding * 2
						  + spacing * (rows - 1)
						  + innerPadding * 2 * rows;

		var imagePixels = new Color[imageWidth * imageHeight];
		var regions = new Rectangle[file.Frames.Length];

		for (var i = 0; i < flattenedFrames.GetLength(0); i++)
		{
			var column = i % columns;
			var row = i / columns;
			var frame = flattenedFrames[i];

			var x = column * file.CanvasWidth
					+ borderPadding
					+ spacing * column
					+ innerPadding * (column + column + 1);

			var y = row * file.CanvasHeight
					+ borderPadding
					+ spacing * row
					+ innerPadding * (row + row + 1);

			for (var p = 0; p < frame.Length; p++)
			{
				var px = p % file.CanvasWidth + x;
				var py = p / file.CanvasWidth + y;

				var index = py * imageWidth + px;
				imagePixels[index] = frame[p];
			}

			regions[i] = new Rectangle(x, y, file.CanvasWidth, file.CanvasHeight);
		}

		var texture = new Texture2D(Core.GraphicsDevice, imageWidth, imageHeight);
		texture.SetData<Color>(imagePixels);

		for (var i = 0; i < file.Frames.Length; i++)
		{
			atlas.Sprites[i] = new Sprite(texture, regions[i], spriteOrigin ?? regions[i].GetHalfSize());
		}

		for (var tagNum = 0; tagNum < file.Tags.Length; tagNum++)
		{
			var tag = file.Tags[tagNum];
			var sprites = new Sprite[tag.To - tag.From + 1];
			var durations = new float[sprites.Length];

			for (int spriteIndex = 0, lookupIndex = tag.From;
				 spriteIndex < sprites.Length;
				 spriteIndex++, lookupIndex++)
			{
				sprites[spriteIndex] = atlas.Sprites[lookupIndex];
				durations[spriteIndex] = 1.0f / (file.Frames[lookupIndex].Duration.Milliseconds / 1000.0f);
			}

			atlas.SpriteAnimations[tagNum] = new SpriteAnimation(sprites, durations);
			atlas.AnimationNames[tagNum] = tag.Name;
		}

		return atlas;
	}

	/// <summary>
	/// Translates the data in this aseprite file to a sprite atlas that can be used in a sprite animator component.
	/// </summary>
	/// <param name="file">The Aseprite file</param>
	/// <param name="spriteOrigin">
	/// make the sprite origin something other than sourceRect.GetHalfSize()
	/// </param>
	/// <returns>
	/// A new instance of the <see cref="SpriteAtlas"/> class initialized with the data generated from this Aseprite
	/// file.
	/// </returns>
	public static SpriteAtlas ToSpriteAtlasWithOrigin(this AsepriteFile file, Vector2 spriteOrigin)
	{
		return file.ToSpriteAtlas(true, 0, 0, 0, spriteOrigin);
	}

	/// <summary>
	/// generate a Texture2D from a single aseprite frame.
	/// </summary>
	/// <param name="frameNumber">
	/// the number of the frame as show in Aseprite app.
	/// </param>
	/// <returns>
	/// A <see cref="Texture2D"/> instance with the flattened contents of the frame
	/// </returns>
	public static Texture2D GetTextureFromFrameNumber(this AsepriteFile file, int frameNumber, string layerName = null)
	{
		// frameNumber is base-one in the aseprite app,
		// but Frames array is base-zero, so we subtract 1
		AsepriteFrame frame = file.Frames[frameNumber - 1];
		Color[] pixels = frame.FlattenFrameInternal(true, true, layerName);
		Texture2D texture = new Texture2D(Core.GraphicsDevice, frame.Size.Width, frame.Size.Height);
		texture.SetData<Color>(pixels);
		return texture;
	}
}
