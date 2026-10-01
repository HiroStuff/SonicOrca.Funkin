using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Funkin.Meta.State
{
    internal sealed class InitState : IGameState
    {
        private const string EngineResourceKey = "SONICORCA/ENGINE";

        private readonly FunkinGameContext _context;
        private readonly int _startTick;
        private ITexture _engineTexture;
        private ResourceSession _resourceSession;
        private Task _logoLoadTask;
        private bool _switched;
        private bool _logoLoadStarted;
        private bool _logoLoadFailed;

        public InitState(FunkinGameContext context)
        {
            _context = context;
            _startTick = Environment.TickCount;
            _context.BeginUiFontLoad();
        }

        private void StartLogoLoad()
        {
            if (_logoLoadStarted)
                return;
            _logoLoadStarted = true;

            if (_context.ResourceTree[EngineResourceKey]?.Resource == null)
            {
                _logoLoadFailed = true;
                Trace.WriteLine("[Init] Missing resource key: " + EngineResourceKey);
                return;
            }

            _resourceSession = new ResourceSession(_context.ResourceTree);
            _resourceSession.PushDependency(EngineResourceKey);
            _logoLoadTask = _resourceSession.LoadAsync(CancellationToken.None, serial: true);
        }

        private void PollLogoLoad()
        {
            if (_engineTexture != null || _logoLoadTask == null || !_logoLoadTask.IsCompleted)
                return;

            if (_logoLoadTask.IsFaulted)
            {
                _logoLoadFailed = true;
                Exception ex = _logoLoadTask.Exception?.GetBaseException();
                Trace.WriteLine("[Init] Failed loading " + EngineResourceKey + ": " + ex?.Message);
                return;
            }

            if (_context.ResourceTree.TryGetLoadedResource<ITexture>(EngineResourceKey, out ITexture texture))
                _engineTexture = texture;
            else
                _logoLoadFailed = true;
        }

        public IEnumerable<UpdateResult> Update()
        {
            while (true)
            {
                StartLogoLoad();
                PollLogoLoad();

                int elapsed = unchecked(Environment.TickCount - _startTick);
                if (!_switched && elapsed >= 5500)
                {
                    _switched = true;
                    _context.RequestState(new SongSelectState(_context));
                }

                yield return UpdateResult.Next();
            }
        }

        public void Draw()
        {
            I2dRenderer r2d = _context.Renderer.Get2dRenderer();
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
            r2d.BlendMode = BlendMode.Opaque;
            r2d.Colour = Colours.Black;
            r2d.RenderQuad(r2d.Colour, r2d.ClipRectangle);

            if (_engineTexture == null || _logoLoadFailed)
                return;

            r2d.BlendMode = BlendMode.Alpha;
            double width = _engineTexture.Width;
            double height = _engineTexture.Height;
            double x = (1920.0 - width) * 0.5;
            double y = (1080.0 - height) * 0.5;
            r2d.Colour = Colours.White;
            r2d.RenderTexture(_engineTexture, new Rectangle(x, y, width, height));
        }

        public void Dispose()
        {
            _engineTexture = null;
            _resourceSession?.Dispose();
            _resourceSession = null;
        }
    }
}