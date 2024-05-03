using static MelonCreamSoda.Pages.ArtCanvas;

namespace MelonCreamSoda.ImagePostProcessing
{
    public class ColorInverter : IPostProcesser
    {
        public byte[] Process(byte[] bytes, int _width, int _height, Dictionary<string, CanvasEffectOption> _)
        {
            for (int i = 0; i < bytes.Length; i += 4)
            {
                bytes[i + 0] = (byte)(255 - bytes[i + 0]);
                bytes[i + 1] = (byte)(255 - bytes[i + 1]);
                bytes[i + 2] = (byte)(255 - bytes[i + 2]);
            }

            return bytes;
        }
    }
}
