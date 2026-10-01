using System;
using System.IO;
using SonicOrca.Graphics;
using SonicOrca.HiroUtils.Graphics.Frames;

namespace SonicOrca.Funkin.Assets
{
    public sealed class FunkinSparrowAtlas
    {
        public FunkinSparrowAtlas(ITexture texture, FlxAtlasFrames frames)
        {
            Texture = texture ?? throw new ArgumentNullException(nameof(texture));
            Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        }

        public ITexture Texture { get; }
        public FlxAtlasFrames Frames { get; }

        public static FunkinSparrowAtlas Load(IGraphicsContext graphics, string directory, string baseName)
        {
            string xmlPath = Path.Combine(directory, baseName + ".xml");
            if (!File.Exists(xmlPath))
                throw new FileNotFoundException("Sparrow atlas XML not found.", xmlPath);
            string xmlText = File.ReadAllText(xmlPath);
            FlxAtlasFrames frames = FlxAtlasFrames.FromSparrowXml(xmlText);
            string pngFileName = frames.ImagePath ?? baseName + ".png";
            string pngPath = Path.Combine(directory, Path.GetFileName(pngFileName));
            if (!File.Exists(pngPath))
                throw new FileNotFoundException("Sparrow atlas PNG not found.", pngPath);
            ITexture texture = FunkinTextureUtil.LoadPng(graphics, pngPath);
            return new FunkinSparrowAtlas(texture, frames);
        }
    }
}