using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace SonicOrca.HiroUtils.Graphics.Frames
{
    public sealed class FlxAtlasFrames
    {
        public string ImagePath { get; private set; }
        public List<AtlasFrame> Frames { get; } = new List<AtlasFrame>();

        public static FlxAtlasFrames FromSparrowXml(string xmlText)
        {
            if (string.IsNullOrWhiteSpace(xmlText))
                throw new ArgumentException("XML text is empty.", nameof(xmlText));

            XDocument doc = XDocument.Parse(xmlText);
            XElement root = doc.Root;
            if (root == null || root.Name.LocalName != "TextureAtlas")
                throw new InvalidOperationException("Expected root element TextureAtlas.");

            var atlas = new FlxAtlasFrames
            {
                ImagePath = root.Attribute("imagePath")?.Value
            };

            int idx = 0;
            foreach (XElement sub in root.Elements().Where(e => e.Name.LocalName == "SubTexture"))
            {
                int w = ParseInt(sub.Attribute("width")?.Value);
                int h = ParseInt(sub.Attribute("height")?.Value);
                int fw = ParseOptionalInt(sub.Attribute("frameWidth")?.Value, w);
                int fh = ParseOptionalInt(sub.Attribute("frameHeight")?.Value, h);
                if (fw <= 0)
                    fw = w;
                if (fh <= 0)
                    fh = h;

                var frame = new AtlasFrame
                {
                    Name = sub.Attribute("name")?.Value ?? string.Empty,
                    X = ParseInt(sub.Attribute("x")?.Value),
                    Y = ParseInt(sub.Attribute("y")?.Value),
                    Width = w,
                    Height = h,
                    SourceWidth = fw,
                    SourceHeight = fh,
                    FrameX = ParseInt(sub.Attribute("frameX")?.Value),
                    FrameY = ParseInt(sub.Attribute("frameY")?.Value),
                    Index = idx++
                };
                atlas.Frames.Add(frame);
            }

            return atlas;
        }

        private static int ParseInt(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        private static int ParseOptionalInt(string s, int defaultValue)
        {
            if (string.IsNullOrEmpty(s))
                return defaultValue;
            return int.Parse(s, CultureInfo.InvariantCulture);
        }

        public IReadOnlyList<int> GetFrameIndicesByPrefix(string namePrefix)
        {
            if (namePrefix == null)
                namePrefix = string.Empty;

            return Frames
                .Where(f => f.Name.StartsWith(namePrefix, StringComparison.Ordinal))
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .Select(f => f.Index)
                .ToList();
        }

        public int GetFrameIndexByName(string exactName)
        {
            for (int i = 0; i < Frames.Count; i++)
            {
                if (Frames[i].Name == exactName)
                    return Frames[i].Index;
            }
            return -1;
        }
    }
}