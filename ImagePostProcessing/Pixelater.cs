using MelonCreamSoda.Pages;

namespace MelonCreamSoda.ImagePostProcessing
{
    public class Pixelater : IPostProcesser
    {
        public byte[] Process(byte[] bytes, int width, int height, Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            return new AveragePixelater().Process(bytes, width, height, optionsDict);
        }

        private class AveragePixelater : IPostProcesser
        {
            public byte[] Process(byte[] bytes, int width, int height, Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
            {
                Options options = ParseOptions(optionsDict);
                Dictionary<int, RGBTriple> storedPixelValues = new Dictionary<int, RGBTriple>();

                for (int i = 0; i < bytes.Length; i += 4)
                {
                    var (r, g, b) = GetAveragePixelationValueForIndex(bytes,
                                                                          i,
                                                                          options,
                                                                          storedPixelValues,
                                                                          width,
                                                                          height);
                    bytes[i + 0] = r;
                    bytes[i + 1] = g;
                    bytes[i + 2] = b;
                }
                return bytes;
            }

            private (byte, byte, byte) GetAveragePixelationValueForIndex(byte[] bytes, int index, Options options, Dictionary<int, RGBTriple> storedIndexValues, int width, int height)
            {
                var (x, y) = CommonProcessingMethods.GetCoordinatesForIndex(index, width);
                var topLeftX = x - (x % options.PixelationLength);
                var topLeftY = y - (y % options.PixelationLength);
                var topLeftIndex = CommonProcessingMethods.GetIndexForCoordinates(topLeftX, topLeftY, width);

                if (storedIndexValues.TryGetValue(topLeftIndex, out RGBTriple rgb))
                {
                    return (rgb.R, rgb.G, rgb.B);
                }

                var farthestX = topLeftX + options.PixelationLength - 1;
                var farthestY = topLeftY + options.PixelationLength - 1;

                if (farthestX >= width || farthestY >= height)
                {
                    return (127, 127, 127);
                }

                List<int> indicesInNewPixel = [];
                for (int j = 0; j < options.PixelationLength; j++)
                {
                    var startingX = topLeftIndex + (width * 4 * j);

                    for (int i = startingX; i < startingX + (options.PixelationLength * 4); i += 4)
                    {
                        indicesInNewPixel.Add(i);
                    }
                }

                int r = 0, g = 0, b = 0;
                foreach (var i in indicesInNewPixel)
                {
                    r += bytes[i + 0];
                    g += bytes[i + 1];
                    b += bytes[i + 2];
                }

                r /= indicesInNewPixel.Count;
                g /= indicesInNewPixel.Count;
                b /= indicesInNewPixel.Count;

                storedIndexValues.Add(topLeftIndex, new RGBTriple((byte)r, (byte)g, (byte)b));
                return ((byte)r, (byte)g, (byte)b);
            }

            private record RGBTriple(byte R, byte G, byte B);

            private record Options(int PixelationLength, int PixelationArea);

            private static Options ParseOptions(Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
            {
                int strength = optionsDict["strength"].Value;
                int pixelationLength = (int)Math.Pow(2, strength);

                return new(pixelationLength, pixelationLength * pixelationLength);
            }
        }
    }
}
