using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace MelonCreamSoda.Pages
{
    [SupportedOSPlatform("browser")]
    public partial class ArtCanvas
    {
        [JSImport("consoleLog", "ArtCanvas")]
        internal static partial void Log(string msg);

        [JSImport("saveImage", "ArtCanvas")]
        internal static partial void SaveImage(string canvasId);

        [JSImport("getPixelDataFromCanvas", "ArtCanvas")]
        internal static partial byte[] GetBytes(string canvasId);

        [JSImport("setPixelDataToCanvas", "ArtCanvas")]
        internal static partial void SetBytes(string canvasId, byte[] bytes);
    }
}
