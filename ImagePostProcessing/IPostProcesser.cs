using static MelonCreamSoda.Pages.ArtCanvas;

namespace MelonCreamSoda.ImagePostProcessing
{
    public interface IPostProcesser
    {
        /// <summary>
        /// Applies post processing to the passed in byte array.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        byte[] Process(byte[] bytes, int width, int height, Dictionary<string, CanvasEffectOption> optionsDict);
    }
}
