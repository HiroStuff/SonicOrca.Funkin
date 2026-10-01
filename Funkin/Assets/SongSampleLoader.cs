using System;
using System.IO;
using SonicOrca.Audio;
using SonicOrca.HelperLibraries.OggVorbis;

namespace SonicOrca.Funkin.Assets
{
    internal static class SongSampleLoader
    {
        public static Sample TryLoadOgg(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return null;

            try
            {
                using (FileStream fs = File.OpenRead(absolutePath))
                using (Stream decoded = new OggDecodeStream(fs, false))
                using (var ms = new MemoryStream())
                {
                    decoded.CopyTo(ms);
                    ms.Position = 0;
                    return ParseWavPcm(ms);
                }
            }
            catch
            {
                return null;
            }
        }

        private static Sample ParseWavPcm(Stream stream)
        {
            using (var binaryReader = new BinaryReader(stream))
            {
                if (binaryReader.ReadInt32() != 1179011410)
                    throw new InvalidDataException("Invalid wav signature.");
                binaryReader.ReadInt32();
                if (binaryReader.ReadInt32() != 1163280727)
                    throw new InvalidDataException("Invalid wav signature.");
                if (binaryReader.ReadInt32() != 544501094)
                    throw new InvalidDataException("Invalid wav signature.");
                binaryReader.ReadInt32();
                if (binaryReader.ReadInt16() != 1)
                    throw new InvalidDataException("Non-PCM wav.");
                short channels = binaryReader.ReadInt16();
                int sampleRate = binaryReader.ReadInt32();
                binaryReader.ReadInt32();
                binaryReader.ReadInt16();
                short bitsPerSample = binaryReader.ReadInt16();
                if (binaryReader.ReadInt32() != 1635017060)
                    throw new InvalidDataException("Invalid wav format.");
                byte[] pcm = binaryReader.ReadBytes(binaryReader.ReadInt32());
                return new Sample(pcm, bitsPerSample, sampleRate, channels);
            }
        }
    }
}