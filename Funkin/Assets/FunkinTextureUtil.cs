using System;
using System.IO;
using Hjg.Pngcs;
using SonicOrca.Graphics;

namespace SonicOrca.Funkin.Assets
{
    internal static class FunkinTextureUtil
    {
        public static ITexture LoadPng(IGraphicsContext graphics, string path)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            using FileStream fs = File.OpenRead(path);
            PngReader reader = new PngReader(fs);
            try
            {
                int width = reader.ImgInfo.Cols;
                int height = reader.ImgInfo.Rows;
                int channels = reader.ImgInfo.Channels;
                byte[] argb = new byte[width * height * 4];
                for (int nrow = 0; nrow < height; nrow++)
                {
                    ImageLine imageLine = reader.ReadRowByte(nrow);
                    Buffer.BlockCopy(imageLine.ScanlineB, 0, argb, nrow * width * channels, width * channels);
                }
                reader.End();
                return graphics.CreateTexture(width, height, channels, argb);
            }
            finally
            {
                reader.End();
            }
        }
    }
}