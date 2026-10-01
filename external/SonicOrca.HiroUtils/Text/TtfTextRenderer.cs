using System;
using System.IO;
using System.Runtime.InteropServices;
using SDL2;
using SonicOrca.Graphics;

namespace SonicOrca.HiroUtils.Text
{
    public sealed class TtfTextRenderer : IDisposable
    {
        private static readonly object SdlTtfInitSync = new object();
        private static bool _sdlTtfInitialised;

        private readonly IntPtr _font;
        private bool _disposed;

        public TtfTextRenderer(string ttfPath, int pointSize)
        {
            if (string.IsNullOrEmpty(ttfPath))
                throw new ArgumentException("TTF path is empty.", nameof(ttfPath));
            if (!File.Exists(ttfPath))
                throw new FileNotFoundException("TTF file not found.", ttfPath);
            if (pointSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pointSize));

            EnsureSdlTtfInitialised();
            _font = SDL_ttf.TTF_OpenFont(ttfPath, pointSize);
            if (_font == IntPtr.Zero)
                throw new InvalidOperationException("TTF_OpenFont failed: " + SDL.SDL_GetError());
        }

        public ITexture RenderTextTexture(IGraphicsContext graphics, string text, Colour colour)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (string.IsNullOrEmpty(text))
                text = " ";

            SDL.SDL_Color sdlColor = new SDL.SDL_Color
            {
                r = colour.Red,
                g = colour.Green,
                b = colour.Blue,
                a = colour.Alpha
            };

            IntPtr surface = SDL_ttf.TTF_RenderUTF8_Blended(_font, text, sdlColor);
            if (surface == IntPtr.Zero)
                throw new InvalidOperationException("TTF_RenderUTF8_Blended failed: " + SDL.SDL_GetError());

            IntPtr converted = IntPtr.Zero;
            try
            {
                converted = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                if (converted == IntPtr.Zero)
                    throw new InvalidOperationException("SDL_ConvertSurfaceFormat failed: " + SDL.SDL_GetError());

                SDL.SDL_Surface s = Marshal.PtrToStructure<SDL.SDL_Surface>(converted);
                int width = s.w;
                int height = s.h;
                int pitch = s.pitch;
                if (width <= 0 || height <= 0)
                    return graphics.CreateTexture(1, 1);

                bool locked = false;
                if (SDL.SDL_MUSTLOCK(converted))
                {
                    if (SDL.SDL_LockSurface(converted) != 0)
                        throw new InvalidOperationException("SDL_LockSurface failed: " + SDL.SDL_GetError());
                    locked = true;
                    s = Marshal.PtrToStructure<SDL.SDL_Surface>(converted);
                    pitch = s.pitch;
                }

                try
                {
                    byte[] tight = new byte[width * height * 4];
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr srcRow = s.pixels + y * pitch;
                        Marshal.Copy(srcRow, tight, y * width * 4, width * 4);
                    }
                    return graphics.CreateTexture(width, height, 4, tight);
                }
                finally
                {
                    if (locked)
                        SDL.SDL_UnlockSurface(converted);
                }
            }
            finally
            {
                if (converted != IntPtr.Zero)
                    SDL.SDL_FreeSurface(converted);
                SDL.SDL_FreeSurface(surface);
            }
        }

        private static void EnsureSdlTtfInitialised()
        {
            lock (SdlTtfInitSync)
            {
                if (_sdlTtfInitialised)
                    return;
                if (SDL_ttf.TTF_WasInit() == 0 && SDL_ttf.TTF_Init() != 0)
                    throw new InvalidOperationException("TTF_Init failed: " + SDL.SDL_GetError());
                _sdlTtfInitialised = true;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_font != IntPtr.Zero)
                SDL_ttf.TTF_CloseFont(_font);
        }
    }
}