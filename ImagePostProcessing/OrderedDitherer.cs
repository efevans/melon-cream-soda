using MelonCreamSoda.Pages;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MelonCreamSoda.ImagePostProcessing
{
    public class OrderedDitherer : IPostProcesser
    {
        public byte[] Process(byte[] bytes, int width, int _height, Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            var options = ParseOptions(optionsDict);

            var (selectedMatrix, length, size) = options.Strength switch
            {
                1 => (_2Matrix, 2, 2*2),
                2 => (_4Matrix, 4, 4*4),
                3 => (_8Matrix, 8, 8*8),
                4 => (_16Matrix, 16, 16*16),
                _ => (_2Matrix, 1, 2),
            };

            List<List<float>> normalizedThesholdMatrix = selectedMatrix.Select(
                list => list.Select(
                    val => ((float)val / size) - 0.5f).ToList()).ToList();

            DitherDimensions dimensions = new(normalizedThesholdMatrix, length, size);

            if (options.Strength > 0)
            {
                Apply(bytes, width, dimensions, options);
                return bytes;
            }

            ApplyNoDither(bytes, options);
            return bytes;
        }

        private static byte[] Apply(byte[] bytes, int width, DitherDimensions dimensions, Options options)
        {
            for (int i = 0; i < bytes.Length; i += 4)
            {
                var (x, y) = CommonProcessingMethods.GetCoordinatesForIndex(i, width);
                var xPos = x % dimensions.Length;
                var yPos = y % dimensions.Length;
                var thresholdValue = dimensions.NormalizedThresholdMatrix[xPos][yPos];
                
                if (options.Grayscale)
                {
                    var normalizedValue = (bytes[i + 0] + bytes[i + 1] + bytes[i + 2]) / 3.0f;
                    bytes[i + 0] = bytes[i + 1] = bytes[i + 2] = GetClosestValue(normalizedValue,
                                                                                 thresholdValue,
                                                                                 options.ColorStep,
                                                                                 options.ColorBits);
                }
                else
                {
                    bytes[i + 0] = GetClosestValue(bytes[i + 0], thresholdValue, options.ColorStep, options.ColorBits);
                    bytes[i + 1] = GetClosestValue(bytes[i + 1], thresholdValue, options.ColorStep, options.ColorBits);
                    bytes[i + 2] = GetClosestValue(bytes[i + 2], thresholdValue, options.ColorStep, options.ColorBits);
                }
            }
            return bytes;
        }

        private record DitherDimensions(List<List<float>> NormalizedThresholdMatrix, int Length, int Size);

        private static byte[] ApplyNoDither(byte[] bytes, Options options)
        {
            for (int i = 0; i < bytes.Length; i += 4)
            {
                if (options.Grayscale)
                {
                    var normalizedValue = (bytes[i + 0] + bytes[i + 1] + bytes[i + 2]) / 3.0f;
                    bytes[i + 0] = bytes[i + 1] = bytes[i + 2] = GetClosestValueNoDither(normalizedValue, options.ColorStep);
                }
                else
                {
                    bytes[i + 0] = GetClosestValueNoDither(bytes[i + 0], options.ColorStep);
                    bytes[i + 1] = GetClosestValueNoDither(bytes[i + 1], options.ColorStep);
                    bytes[i + 2] = GetClosestValueNoDither(bytes[i + 2], options.ColorStep);
                }
            }
            return bytes;
        }

        private static byte GetClosestValue(float value, float thresholdValue, int colorStep, int colorBits)
        {
            var adjustedThresholdValue = thresholdValue / colorBits;
            var normalizedRGB = (value + 1) / colorStep;
            var addedThreshold = normalizedRGB + adjustedThresholdValue;
            var roundedTotal = Math.Round(addedThreshold);
            var unnormalizedRGB = roundedTotal * colorStep;
            var clampedRGB = (byte)Math.Max(0, Math.Min(255, unnormalizedRGB));
            return clampedRGB;
        }

        private static byte GetClosestValueNoDither(float value, int colorStep)
        {
            var normalizedRGB = value / colorStep;
            var roundedTotal = Math.Round(normalizedRGB);
            var unnormalizedRGB = (roundedTotal * colorStep);
            byte clampedRGB = (byte)Math.Max(0, Math.Min(255, unnormalizedRGB));
            return clampedRGB;
        }

        private static readonly List<List<byte>> _2Matrix =
        [
            [0, 2],
            [3, 1]
        ];

        private static readonly List<List<byte>> _4Matrix =
        [
            [0, 8, 2, 10],
            [12, 4, 14, 6],
            [3, 11, 1, 9],
            [15, 7, 13, 5]
        ];

        private static readonly List<List<byte>> _8Matrix =
        [
            [0, 32, 8, 40, 2, 34, 10, 42],
            [48, 16, 56, 24, 50, 18, 58, 26],
            [12, 44, 4, 36, 14, 46, 6, 38],
            [60, 28, 52, 20, 62, 30, 54, 22],
            [3, 35, 11, 43, 1, 33, 9, 41],
            [51, 19, 59, 27, 49, 17, 57, 25],
            [15, 47, 7, 39, 13, 45, 5, 37],
            [63, 31, 55, 23, 61, 29, 53, 21]
        ];

        private static readonly List<List<byte>> _16Matrix =
        [
            [0, 191, 48, 239, 12, 203, 60, 251, 3, 194, 51, 242, 15, 206, 63, 254],
            [127, 64, 175, 112, 139, 76, 187, 124, 130, 67, 178, 115, 142, 79, 190, 127],
            [32, 223, 16, 207, 44, 235, 28, 219, 35, 226, 19, 210, 47, 238, 31, 222],
            [159, 96, 143, 80, 171, 108, 155, 92, 162, 99, 146, 83, 174, 111, 158, 95],
            [8, 199, 56, 247, 4, 195, 52, 243, 11, 202, 59, 250, 7, 198, 55, 246],
            [135, 72, 183, 120, 131, 68, 179, 116, 138, 75, 186, 123, 134, 71, 182, 119],
            [40, 231, 24, 215, 36, 227, 20, 211, 43, 234, 27, 218, 39, 230, 23, 214],
            [167, 104, 151, 88, 163, 100, 147, 84, 170, 107, 154, 91, 166, 103, 150, 87],
            [2, 193, 50, 241, 14, 205, 62, 253, 1, 192, 49, 240, 13, 204, 61, 252],
            [129, 66, 177, 114, 141, 78, 189, 126, 128, 65, 176, 113, 140, 77, 188, 125],
            [34, 225, 18, 209, 46, 237, 30, 221, 33, 224, 17, 208, 45, 236, 29, 220],
            [161, 98, 145, 82, 173, 110, 157, 94, 160, 97, 144, 81, 172, 109, 156, 93],
            [10, 201, 58, 249, 6, 197, 54, 245, 9, 200, 57, 248, 5, 196, 53, 244],
            [137, 74, 185, 122, 133, 70, 181, 118, 136, 73, 184, 121, 132, 69, 180, 117],
            [42, 233, 26, 217, 38, 229, 22, 213, 41, 232, 25, 216, 37, 228, 21, 212],
            [169, 106, 153, 90, 165, 102, 149, 86, 168, 105, 152, 89, 164, 101, 148, 85],
        ];

        private record Options(bool Grayscale, int ColorBits, int ColorStep, int Strength);

        private static Options ParseOptions(Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            bool grayscale = optionsDict["grayscale"].BinaryValue;
            int colorBits = optionsDict["colorBits"].Value;
            int colorStep = (int)(256f / colorBits);
            int strength = optionsDict["strength"].Value;

            return new(grayscale, colorBits, colorStep, strength);
        }
    }
}
