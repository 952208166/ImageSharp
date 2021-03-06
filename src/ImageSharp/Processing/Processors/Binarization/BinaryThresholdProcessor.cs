﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;

namespace SixLabors.ImageSharp.Processing.Processors
{
    /// <summary>
    /// An <see cref="IImageProcessor{TPixel}"/> to perform binary threshold filtering against an
    /// <see cref="Image{TPixel}"/>. The image will be converted to grayscale before thresholding occurs.
    /// </summary>
    /// <typeparam name="TPixel">The pixel format.</typeparam>
    internal class BinaryThresholdProcessor<TPixel> : ImageProcessor<TPixel>
        where TPixel : struct, IPixel<TPixel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryThresholdProcessor{TPixel}"/> class.
        /// </summary>
        /// <param name="threshold">The threshold to split the image. Must be between 0 and 1.</param>
        public BinaryThresholdProcessor(float threshold)
        {
            // TODO: Check thresholding limit. Colors should probably have Max/Min/Middle properties.
            Guard.MustBeBetweenOrEqualTo(threshold, 0, 1, nameof(threshold));
            this.Threshold = threshold;

            // Default to white/black for upper/lower.
            this.UpperColor = NamedColors<TPixel>.White;
            this.LowerColor = NamedColors<TPixel>.Black;
        }

        /// <summary>
        /// Gets the threshold value.
        /// </summary>
        public float Threshold { get; }

        /// <summary>
        /// Gets or sets the color to use for pixels that are above the threshold.
        /// </summary>
        public TPixel UpperColor { get; set; }

        /// <summary>
        /// Gets or sets the color to use for pixels that fall below the threshold.
        /// </summary>
        public TPixel LowerColor { get; set; }

        /// <inheritdoc/>
        protected override void BeforeApply(ImageFrame<TPixel> source, Rectangle sourceRectangle, Configuration configuration)
        {
            new GrayscaleBt709Processor<TPixel>(1F).Apply(source, sourceRectangle, configuration);
        }

        /// <inheritdoc/>
        protected override void OnApply(ImageFrame<TPixel> source, Rectangle sourceRectangle, Configuration configuration)
        {
            float threshold = this.Threshold;
            TPixel upper = this.UpperColor;
            TPixel lower = this.LowerColor;

            int startY = sourceRectangle.Y;
            int endY = sourceRectangle.Bottom;
            int startX = sourceRectangle.X;
            int endX = sourceRectangle.Right;

            // Align start/end positions.
            int minX = Math.Max(0, startX);
            int maxX = Math.Min(source.Width, endX);
            int minY = Math.Max(0, startY);
            int maxY = Math.Min(source.Height, endY);

            // Reset offset if necessary.
            if (minX > 0)
            {
                startX = 0;
            }

            if (minY > 0)
            {
                startY = 0;
            }

            Parallel.For(
                minY,
                maxY,
                configuration.ParallelOptions,
                y =>
                {
                    Span<TPixel> row = source.GetPixelRowSpan(y - startY);

                    for (int x = minX; x < maxX; x++)
                    {
                        ref TPixel color = ref row[x - startX];

                        // Any channel will do since it's Grayscale.
                        color = color.ToVector4().X >= threshold ? upper : lower;
                    }
                });
        }
    }
}