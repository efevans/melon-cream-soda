using MelonCreamSoda.Pages;

namespace MelonCreamSoda.ImagePostProcessing
{
    public class FloydSteinbergDitherer : IPostProcesser
    {
        public byte[] Process(byte[] bytes, int width, int height, Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            var options = ParseOptions(optionsDict);
            var corrections = new float[bytes.Length];

            if (options.Grayscale)
            {
                Console.WriteLine("Starting Grayscale dither");
                return ApplyGrayscaleDither(bytes, width, height, corrections, options);
            }
            Console.WriteLine("Starting RGB dither");
            return ApplyRGBDither(bytes, width, height, corrections, options);
        }

        private const float RightNeighborRatio = 7.0f / 16.0f;
        private const float BottomLeftNeighborRatio = 3.0f / 16.0f;
        private const float BottomNeighborRatio = 5.0f / 16.0f;
        private const float BottomRightNeighborRatio = 1.0f / 16.0f;

        private static byte[] ApplyGrayscaleDither(byte[] bytes, int width, int height, float[] corrections, Options options)
        {
            //void AccrueError(int x, int y, int xOffset, int yOffset, float[] corrections, float newlyAccruedCorrection, float correctionRatio)
            //{
            //    int index = CommonProcessingMethods.GetIndexForCoordinates(x + xOffset, y + yOffset, width);
            //    AccrueErrorForIndex(corrections, index, newlyAccruedCorrection, correctionRatio);
            //}

            //for (int i = 0; i < bytes.Length; i += 4)
            //{
            //    float colorAvg = (bytes[i + 0] + bytes[i + 1] + bytes[i + 2]) / 3.0f;
            //    var (greyValue, correction) = GetClosestValueWithError(colorAvg, corrections[i + 0], options.ColorStep);
            //    bytes[i + 0] = bytes[i + 1] = bytes[i + 2] = greyValue;
            //    var (x, y) = CommonProcessingMethods.GetCoordinatesForIndex(i, width);

            //    if (!CoordinateIsOnRightEdge(x, width))
            //    {
            //        AccrueError(x, y, 1, 0, corrections, correction, RightNeighborRatio);
            //    }
            //    if (!CoordinateIsOnLeftEdge(x) && !CoordinateIsOnBottomEdge(y, height))
            //    {
            //        AccrueError(x, y, -1, 1, corrections, correction, BottomLeftNeighborRatio);
            //    }
            //    if (!CoordinateIsOnBottomEdge(y, height))
            //    {
            //        AccrueError(x, y, 0, 1, corrections, correction, BottomNeighborRatio);
            //    }
            //    if (!CoordinateIsOnRightEdge(x, width) && !CoordinateIsOnBottomEdge(y, height))
            //    {
            //        AccrueError(x, y, 0, 1, corrections, correction, BottomRightNeighborRatio);
            //    }
            //}

            return bytes;
        }

        private static byte[] ApplyRGBDither(byte[] bytes, int width, int height, float[] corrections, Options options)
        {
            void AccrueError(int x, int y, int xOffset, int yOffset, float[] corrections, float newlyAccruedRCorrection,
                             float newlyAccruedGCorrection, float newlyAccruedBCorrection, float correctionRatio)
            {
                int index = CommonProcessingMethods.GetIndexForCoordinates(x + xOffset, y + yOffset, width);
                AccrueErrorForIndex(corrections, index + 0, newlyAccruedRCorrection, correctionRatio);
                AccrueErrorForIndex(corrections, index + 1, newlyAccruedGCorrection, correctionRatio);
                AccrueErrorForIndex(corrections, index + 2, newlyAccruedBCorrection, correctionRatio);
            }

            for (int i = 0; i < bytes.Length; i += 4)
            {
                float rCorrection, gCorrection, bCorrection;
                //(bytes[i + 0], rCorrection) = GetClosestValueWithError(bytes[i + 0], corrections[i + 0], options.ColorStep);
                //(bytes[i + 1], gCorrection) = GetClosestValueWithError(bytes[i + 1], corrections[i + 1], options.ColorStep);
                //(bytes[i + 2], bCorrection) = GetClosestValueWithError(bytes[i + 2], corrections[i + 2], options.ColorStep);
                GetClosestValueWithError(bytes[i + 0], corrections[i + 0], options.ColorStep, out bytes[i + 0], out rCorrection);
                GetClosestValueWithError(bytes[i + 1], corrections[i + 1], options.ColorStep, out bytes[i + 1], out gCorrection);
                GetClosestValueWithError(bytes[i + 2], corrections[i + 2], options.ColorStep, out bytes[i + 2], out bCorrection);

                var (x, y) = CommonProcessingMethods.GetCoordinatesForIndex(i, width);

                if (!CoordinateIsOnRightEdge(x, width))
                {
                    AccrueError(x, y, 1, 0, corrections, rCorrection, gCorrection, bCorrection, RightNeighborRatio);
                }
                if (!CoordinateIsOnLeftEdge(x) && !CoordinateIsOnBottomEdge(y, height))
                {
                    AccrueError(x, y, -1, 1, corrections, rCorrection, gCorrection, bCorrection, BottomLeftNeighborRatio);
                }
                if (!CoordinateIsOnBottomEdge(y, height))
                {
                    AccrueError(x, y, 0, 1, corrections, rCorrection, gCorrection, bCorrection, BottomNeighborRatio);
                }
                if (!CoordinateIsOnRightEdge(x, width) && !CoordinateIsOnBottomEdge(y, height))
                {
                    AccrueError(x, y, 0, 1, corrections, rCorrection, gCorrection, bCorrection, BottomRightNeighborRatio);
                }
            }

            return bytes;
        }

        private record Options(bool Grayscale,int ColorStep);

        private static Options ParseOptions(Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            bool grayscale = optionsDict["grayscale"].BinaryValue;
            int colorBits = optionsDict["colorBits"].Value;
            int colorStep = (int)(256f / colorBits);

            return new(grayscale, colorStep);
        }

        private static (byte, float) GetClosestValueWithError(float value, float correction, int colorStep)
        {
            //return ((byte)value, correction);
            var correctedValue = value + correction;
            var normalizedValue = (correctedValue + 1) / colorStep;
            var roundedTotal = (int)Math.Round(normalizedValue);
            var unnormalizedRGB = roundedTotal * colorStep;
            var clampedRGB = (byte)Math.Max(0, Math.Min(255, unnormalizedRGB));
            return (clampedRGB, correctedValue - clampedRGB);
        }

        private static void GetClosestValueWithError(float value, float correction, int colorStep, out byte ditheredValue,
            out float newCorrection)
        {
            //newCorrection = correction;
            //ditheredValue = (byte)value;
            //ditheredValue = 100;
            var correctedValue = value + correction;
            var normalizedValue = (correctedValue + 1) / colorStep;
            var roundedTotal = (int)Math.Round(normalizedValue);
            var unnormalizedRGB = roundedTotal * colorStep;
            var clampedRGB = (byte)Math.Max(0, Math.Min(255, unnormalizedRGB));
            //return (clampedRGB, correctedValue - clampedRGB);
            ditheredValue = clampedRGB;
            newCorrection = correctedValue - clampedRGB;
        }

        private static void AccrueErrorForIndex(float[] errorsArray, int index, float addedValue, float correctionRatio)
        {
            errorsArray[index] = errorsArray[index] + (addedValue * correctionRatio);
        }

        private static bool CoordinateIsOnLeftEdge(int x) => x == 0;

        private static bool CoordinateIsOnRightEdge(int x, int width) => x + 1 == width;

        private static bool CoordinateIsOnBottomEdge(int y, int height) => y + 1 == height;
    }
}
