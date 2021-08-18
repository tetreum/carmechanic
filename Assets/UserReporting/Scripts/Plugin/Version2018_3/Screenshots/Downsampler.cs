using System;

namespace Unity.Screenshots
{
    public static class Downsampler
    {
        #region Static Methods

        public static byte[] Downsample(byte[] dataRgba, int stride, int maximumWidth, int maximumHeight,
            out int downsampledStride)
        {
            // Preconditions
            if (stride == 0) throw new ArgumentException("The stride must be greater than 0.");
            if (stride % 4 != 0) throw new ArgumentException("The stride must be evenly divisible by 4.");
            if (dataRgba == null) throw new ArgumentNullException("dataRgba");
            if (dataRgba.Length == 0) throw new ArgumentException("The data length must be greater than 0.");
            if (dataRgba.Length % 4 != 0) throw new ArgumentException("The data must be evenly divisible by 4.");
            if (dataRgba.Length % stride != 0)
                throw new ArgumentException("The data must be evenly divisible by the stride.");

            // Implementation
            var width = stride / 4;
            var height = dataRgba.Length / stride;
            var ratioX = maximumWidth / (float) width;
            var ratioY = maximumHeight / (float) height;
            var ratio = Math.Min(ratioX, ratioY);
            if (ratio < 1)
            {
                var downsampledWidth = (int) Math.Round(width * ratio);
                var downsampledHeight = (int) Math.Round(height * ratio);
                var downsampledData = new float[downsampledWidth * downsampledHeight * 4];
                var sampleWidth = width / (float) downsampledWidth;
                var sampleHeight = height / (float) downsampledHeight;
                var kernelWidth = (int) Math.Floor(sampleWidth);
                var kernelHeight = (int) Math.Floor(sampleHeight);
                var kernelSize = kernelWidth * kernelHeight;
                for (var y = 0; y < downsampledHeight; y++)
                for (var x = 0; x < downsampledWidth; x++)
                {
                    var destinationIndex = y * downsampledWidth * 4 + x * 4;
                    var sampleLowerX = (int) Math.Floor(x * sampleWidth);
                    var sampleLowerY = (int) Math.Floor(y * sampleHeight);
                    var sampleUpperX = sampleLowerX + kernelWidth;
                    var sampleUpperY = sampleLowerY + kernelHeight;
                    for (var sampleY = sampleLowerY; sampleY < sampleUpperY; sampleY++)
                    {
                        if (sampleY >= height) continue;
                        for (var sampleX = sampleLowerX; sampleX < sampleUpperX; sampleX++)
                        {
                            if (sampleX >= width) continue;
                            var sourceIndex = sampleY * width * 4 + sampleX * 4;
                            downsampledData[destinationIndex] += dataRgba[sourceIndex];
                            downsampledData[destinationIndex + 1] += dataRgba[sourceIndex + 1];
                            downsampledData[destinationIndex + 2] += dataRgba[sourceIndex + 2];
                            downsampledData[destinationIndex + 3] += dataRgba[sourceIndex + 3];
                        }
                    }

                    downsampledData[destinationIndex] /= kernelSize;
                    downsampledData[destinationIndex + 1] /= kernelSize;
                    downsampledData[destinationIndex + 2] /= kernelSize;
                    downsampledData[destinationIndex + 3] /= kernelSize;
                }

                var flippedData = new byte[downsampledWidth * downsampledHeight * 4];
                for (var y = 0; y < downsampledHeight; y++)
                for (var x = 0; x < downsampledWidth; x++)
                {
                    var sourceIndex = (downsampledHeight - 1 - y) * downsampledWidth * 4 + x * 4;
                    var destinationIndex = y * downsampledWidth * 4 + x * 4;
                    flippedData[destinationIndex] += (byte) downsampledData[sourceIndex];
                    flippedData[destinationIndex + 1] += (byte) downsampledData[sourceIndex + 1];
                    flippedData[destinationIndex + 2] += (byte) downsampledData[sourceIndex + 2];
                    flippedData[destinationIndex + 3] += (byte) downsampledData[sourceIndex + 3];
                }

                downsampledStride = downsampledWidth * 4;
                return flippedData;
            }
            else
            {
                var flippedData = new byte[dataRgba.Length];
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var sourceIndex = (height - 1 - y) * width * 4 + x * 4;
                    var destinationIndex = y * width * 4 + x * 4;
                    flippedData[destinationIndex] += dataRgba[sourceIndex];
                    flippedData[destinationIndex + 1] += dataRgba[sourceIndex + 1];
                    flippedData[destinationIndex + 2] += dataRgba[sourceIndex + 2];
                    flippedData[destinationIndex + 3] += dataRgba[sourceIndex + 3];
                }

                downsampledStride = width * 4;
                return flippedData;
            }
        }

        #endregion
    }
}