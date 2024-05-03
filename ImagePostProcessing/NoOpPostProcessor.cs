
using static MelonCreamSoda.Pages.ArtCanvas;

namespace MelonCreamSoda.ImagePostProcessing
{
    public class NoOpPostProcessor : IPostProcesser
    {
        public byte[] Process(byte[] _bytes, int _width, int _height, Dictionary<string, CanvasEffectOption> _options)
        {
            return _bytes;
        }
    }
}
