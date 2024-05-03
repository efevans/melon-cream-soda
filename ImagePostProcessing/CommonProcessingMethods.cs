namespace MelonCreamSoda.ImagePostProcessing
{
    public static class CommonProcessingMethods
    {
        public static (int, int) GetCoordinatesForIndex(int index, int width)
        {
            var x = (index % (width * 4)) / 4;
            var y = (index - (x * 4)) / (width * 4);
            return (x, y);
        }

        public static int GetIndexForCoordinates(int x, int y, int width)
        {
            return (x + (y * width)) * 4;
        }
    }
}
