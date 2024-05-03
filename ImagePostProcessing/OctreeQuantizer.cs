using MelonCreamSoda.Pages;
using static MelonCreamSoda.ImagePostProcessing.OctreeQuantizer.OctreeQuantization;

namespace MelonCreamSoda.ImagePostProcessing
{
    public class OctreeQuantizer : IPostProcesser
    {
        public byte[] Process(byte[] bytes, int _width, int _height, Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            var options = ParseOptions(optionsDict);
            int targetPaletteSize = options.PaletteSize;

            var colorCounts = GetRGBColorCount(bytes);

            OctreeQuantization quantizationTree = new();
            quantizationTree.AddColors(colorCounts);
            var palette = quantizationTree.MakePalette(targetPaletteSize);

            //Parallel.For(0, bytes.Length / 4, pixelIndex =>
            //{
            //    var i = pixelIndex * 4;
            //    var pixelPaletteColor = palette.GetPaletteColorFromColor(new RGBColor(bytes[i + 0], bytes[i + 1], bytes[i + 2]));
            //    bytes[i + 0] = pixelPaletteColor.Red;
            //    bytes[i + 1] = pixelPaletteColor.Green;
            //    bytes[i + 2] = pixelPaletteColor.Blue;
            //});

            for (int i = 0; i < bytes.Length; i += 4)
            {
                var pixelPaletteColor = palette.GetPaletteColorFromColor(new RGBColor(bytes[i + 0], bytes[i + 1], bytes[i + 2]));
                bytes[i + 0] = pixelPaletteColor.Red;
                bytes[i + 1] = pixelPaletteColor.Green;
                bytes[i + 2] = pixelPaletteColor.Blue;
            }

            //int i = 0;
            //foreach (var pixelColor in orderedPixelList)
            //{
            //    var pixelPaletteColor = palette.GetPaletteColorFromColor(pixelColor);
            //    bytes[i + 0] = pixelPaletteColor.Red;
            //    bytes[i + 1] = pixelPaletteColor.Green;
            //    bytes[i + 2] = pixelPaletteColor.Blue;
            //    i += 4;
            //}

            return bytes;
        }

        private record Options(int PaletteSize);

        private static Options ParseOptions(Dictionary<string, ArtCanvas.CanvasEffectOption> optionsDict)
        {
            int paletteSize = optionsDict["paletteSize"].Value;

            return new(paletteSize);
        }

        private static Dictionary<RGBColor, int> GetRGBColorCount(byte[] bytes)
        {
            var colorCount = new Dictionary<RGBColor, int>();

            for (var i = 0; i < bytes.Length; i += 4)
            {
                var rgb = new RGBColor(bytes[i + 0], bytes[i + 1], bytes[i + 2]);
                if (!colorCount.TryGetValue(rgb, out int value))
                {
                    value = 0;
                    colorCount.Add(rgb, value);
                }
                colorCount[rgb] = ++value;
            }

            return colorCount;
        }


        public interface IPalette
        {
            RGBColor GetPaletteColorFromColor(RGBColor color);
        }

        public interface IQuantization
        {
            void AddColors(Dictionary<RGBColor, int> colorPixelCounts);
            IPalette MakePalette(int targetPaletteSize);
        }

        public class OctreeQuantization : IQuantization, IPalette
        {
            // The normal Color struct is included in the System.Drawing package which is not available for Blazor Client
            public record RGBColor(byte Red, byte Green, byte Blue);

            public OctreeQuantization()
            {
                for (int i = 0; i < MAXDEPTH; i++)
                {
                    Levels.Add([]);
                }

                Palettizer = new SquarePalettizer();

                Root = new Node();
            }

            public void AddColors(Dictionary<RGBColor, int> colorPixelCounts)
            {
                foreach (var color in colorPixelCounts)
                {
                    AddColor(color.Key, color.Value);
                }
            }

            private void AddColor(RGBColor color, int count)
            {
                Root.AddColor(color, count, this.Levels, 0);
            }

            public IPalette MakePalette(int targetPaletteSize)
            {
                int paletteSize = Root.LeafCount();

                if (paletteSize > targetPaletteSize)
                {
                    int i;
                    for (i = MAXDEPTH - 2; i >= 0; i--)
                    {
                        foreach (var node in Levels[i])
                        {
                            int removed = node.Reduce();
                            paletteSize -= removed;
                        }
                        if (Levels[i].Count > targetPaletteSize && (i > 0 ? Levels[i - 1].Count : 1) <= targetPaletteSize)
                        {
                            break;
                        }
                    }
                    var leafReduceQueue = GetOrderedNodesByPixelCount(Levels[i]);

                    foreach (var nodeToMerge in leafReduceQueue)
                    {
                        bool paletteSizeWasReduced = nodeToMerge.MergeToParent();
                        if (paletteSizeWasReduced)
                        {
                            paletteSize--;
                        }

                        if (paletteSize <= targetPaletteSize)
                        {
                            break;
                        }
                    }
                }

                Root.ConstructPalette(Palette);

                return this;
            }

            public RGBColor GetPaletteColorFromColor(RGBColor color) => Palettizer.GetPaletteColorFromColor(this, color);

            private const int MAXDEPTH = 8;
            private readonly Node Root;
            private readonly List<List<Node>> Levels = [];
            private readonly List<RGBColor> Palette = [];
            private readonly SquarePalettizer Palettizer;

            private static Queue<Node> GetOrderedNodesByPixelCount(List<Node> nodes)
            {
                var ascendingPixelCountOrderedNodes = new List<Node>(nodes);
                ascendingPixelCountOrderedNodes.Sort((n1, n2) => n1.PixelCount.CompareTo(n2.PixelCount));

                return new Queue<Node>(ascendingPixelCountOrderedNodes);
            }

            public class Node
            {
                public bool HasColorInfo { get; set; } = false;
                public int PixelCount { get; set; }

                public List<RGBColor> ConstructPalette(List<RGBColor> palette)
                {
                    if (HasColorInfo)
                    {
                        palette.Add(GetNormalizedColor());
                    }

                    foreach (var node in Children)
                    {
                        if (node == null)
                        {
                            continue;
                        }

                        node.ConstructPalette(palette);
                    }

                    return palette;
                }

                public void AddColor(RGBColor color, int count, List<List<Node>> levels, int level)
                {
                    if (level >= MAXDEPTH)
                    {
                        PixelCount += count;
                        Red += color.Red * count;
                        Green += color.Green * count;
                        Blue += color.Blue * count;
                        HasColorInfo = true;
                        return;
                    }

                    int index = GetIndexForColor(color, level);
                    if (Children[index] == null)
                    {
                        var newNode = new Node()
                        {
                            Parent = this
                        };
                        Children[index] = newNode;
                        levels[level].Add(newNode);
                    }

                    Children[index]?.AddColor(color, count, levels, level + 1);
                }

                public int Reduce()
                {
                    int results = 0;
                    for (int i = 0; i < Children.Length; i++)
                    {
                        var node = Children[i];
                        if (node == null)
                        {
                            continue;
                        }

                        Red += node.Red;
                        Green += node.Green;
                        Blue += node.Blue;
                        PixelCount += node.PixelCount;
                        results++;
                        Children[i] = null;
                    }
                    HasColorInfo = true;

                    return results - 1;
                }

                public bool MergeToParent()
                {
                    var parent = Parent;
                    if (parent == null)
                    {
                        return false;
                    }

                    parent.Red += Red;
                    parent.Green += Green;
                    parent.Blue += Blue;
                    parent.PixelCount += PixelCount;

                    bool reducedColorInfoNodesCount = parent.HasColorInfo;
                    parent.HasColorInfo = true;

                    for (int i = 0; i < parent.Children.Length; i++)
                    {
                        var sibling = parent.Children[i];
                        if (sibling == this)
                        {
                            parent.Children[i] = null;
                        }
                    }

                    return reducedColorInfoNodesCount;
                }

                public int LeafCount()
                {
                    if (Children.All(c => c == null))
                    {
                        return 1;
                    }

                    int count = 0;

                    foreach (var node in Children)
                    {
                        if (node == null)
                        {
                            continue;
                        }

                        count += node.LeafCount();
                    }

                    return count;
                }

                public RGBColor GetNormalizedColor()
                {
                    return new RGBColor((byte)(Red / PixelCount), (byte)(Green / PixelCount), (byte)(Blue / PixelCount));
                }

                public static int GetIndexForColor(RGBColor color, int level)
                {
                    int index = 0;
                    int mask = 0b10000000 >> level;
                    if ((color.Red & mask) != 0)
                    {
                        index |= 0b100;
                    }
                    if ((color.Green & mask) != 0)
                    {
                        index |= 0b010;
                    }
                    if ((color.Blue & mask) != 0)
                    {
                        index |= 0b001;
                    }

                    return index;
                }

                private Node? Parent { get; set; }
                private Node?[] Children { get; } = new Node?[8];
                private long Red { get; set; }
                private long Green { get; set; }
                private long Blue { get; set; }
            }

            public interface IPalettizer : IQuantizationTreePalettizer { }

            public interface IQuantizationTreePalettizer
            {
                RGBColor GetPaletteColorFromColor(OctreeQuantization tree, RGBColor color);
            }

            private class SquarePalettizer : IPalettizer
            {
                private readonly Dictionary<RGBColor, RGBColor> ColorMap = [];

                public RGBColor GetPaletteColorFromColor(OctreeQuantization tree, RGBColor color)
                {
                    if (ColorMap.TryGetValue(color, out var mapColor))
                    {
                        return mapColor;
                    }

                    int dsqBest = int.MaxValue;
                    RGBColor best = new(0, 0, 0);
                    foreach (var paletteColor in tree.Palette)
                    {
                        int dsq = 0;
                        int v;
                        v = color.Red - paletteColor.Red;
                        dsq += v * v;
                        v = color.Green - paletteColor.Green;
                        dsq += v * v;
                        v = color.Blue - paletteColor.Blue;
                        dsq += v * v;

                        if (dsq < dsqBest)
                        {
                            dsqBest = dsq;
                            best = paletteColor;
                        }
                    }

                    ColorMap.Add(color, best);
                    return best;
                }
            }
        }
    }
}
