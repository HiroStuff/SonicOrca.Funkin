using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Drawing;
using SonicOrca.Drawing.LevelRendering;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;
// Funkin
using SonicOrca.Funkin.Meta.State;

namespace SonicOrca.Funkin
{
    internal sealed class FunkinGameContext : SonicOrcaGameContext
    {
        private IFramebuffer _canvas;
        private FunkinGameSettings _settings;
        private IGameState _rootGameState;
        private Updater _gameStateUpdater;
        private IGameState _pendingState;
        private ResourceSession _uiResourceSession;
        private string _uiFontKey;
        private bool _uiFontLoadStarted;
        private Task _uiFontLoadTask;
        internal string UiFontResourceKey => _uiFontKey;

        public FunkinGameContext(IPlatform platform) : base(platform)
        {
        }

        public override IAudioSettings AudioSettings => _settings;

        public override IVideoSettings VideoSettings => _settings;

        public override void Initialise()
        {
            base.Initialise();
            Configuration = Program.Configuration;
            UserDataDirectory = Program.UserDataDirectory;
            _canvas = Window.GraphicsContext.CreateFrameBuffer(1920, 1080);
            SonicOrcaGameContext.IsMaxPerformance = Configuration.GetPropertyBoolean("graphics", "max_performance");
            Audio.Volume = Configuration.GetPropertyDouble("audio", "volume", 1.0);
            Audio.MusicVolume = Configuration.GetPropertyDouble("audio", "music_volume", 0.5);
            Audio.SoundVolume = Configuration.GetPropertyDouble("audio", "sound_volume", 1.0);
            Input.IsVibrationEnabled = Configuration.GetPropertyBoolean("input", "vibration", true);
            _settings = new FunkinGameSettings(Configuration, Audio, Window);
            _settings.Apply();
            Window.WindowTitle = "Friday Night Funkin'";
            Window.AspectRatio = new Vector2i(16, 9);
            string contentRoot = GetContentRootDirectory();
            LoadResourceFiles(Path.Combine(contentRoot, "data"));
            if (bool.Parse(Configuration.GetProperty("general", "use_mods", "true")))
                LoadResourceFiles(Path.Combine(contentRoot, "mods"));
            _rootGameState = new InitState(this);
            _gameStateUpdater = new Updater(_rootGameState.Update());
        }

        internal void RequestState(IGameState nextState)
        {
            if (nextState == null)
                throw new ArgumentNullException(nameof(nextState));
            _pendingState = nextState;
        }

        public override void Dispose()
        {
            _rootGameState?.Dispose();
            _uiResourceSession?.Dispose();
            _uiResourceSession = null;
            _uiFontKey = null;
            base.Dispose();
        }

        private async Task LoadUiFontResourcesAsync(string contentRoot)
        {
            string[] candidateKeys =
            {
                "SONICORCA/FONTS/HUD"
            };

            foreach (string key in candidateKeys)
            {
                if (ResourceTree[key]?.Resource == null)
                    continue;
                if (await TryLoadFontWithSessionAsync(key, key).ConfigureAwait(false))
                    return;
            }

            string looseFontXml = Path.Combine(contentRoot, "assets", "fonts", "ui.font.xml");
            if (File.Exists(looseFontXml))
            {
                const string looseKey = "FUNKIN/FONTS/UI";
                ResourceTree.SetOrAddFromFile(looseKey, looseFontXml);
                if (await TryLoadFontWithSessionAsync(looseKey, looseFontXml).ConfigureAwait(false))
                    return;
            }

            Trace.WriteLine("[Funkin] No UI font loaded. Copy Sonic Orca data/*.dat next to the exe, or add assets/fonts/ui.font.xml (+ shape PNG paths in that XML).");
        }

        private async Task<bool> TryLoadFontWithSessionAsync(string resourceKey, string logLabel)
        {
            ResourceSession session = null;
            try
            {
                _uiResourceSession?.Dispose();
                _uiResourceSession = null;
                _uiFontKey = null;

                session = new ResourceSession(ResourceTree);
                session.PushDependency(resourceKey);
                await session.LoadAsync(CancellationToken.None, serial: true).ConfigureAwait(false);
                if (ResourceTree.TryGetLoadedResource<Font>(resourceKey, out _))
                {
                    _uiResourceSession = session;
                    session = null;
                    _uiFontKey = resourceKey;
                    Trace.WriteLine("[Funkin] UI font loaded: " + logLabel);
                    return true;
                }

                session.Dispose();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[Funkin] UI font load failed (" + logLabel + "): " + ex.Message);
                session?.Dispose();
            }

            return false;
        }

        internal bool TryGetUiFont(out Font font)
        {
            font = null;
            if (string.IsNullOrEmpty(_uiFontKey))
                return false;
            return ResourceTree.TryGetLoadedResource<Font>(_uiFontKey, out font);
        }

        internal void BeginUiFontLoad()
        {
            if (_uiFontLoadStarted || _uiFontLoadTask != null)
                return;
            _uiFontLoadStarted = true;
            _uiFontLoadTask = LoadUiFontResourcesAsync(GetContentRootDirectory());
        }

        internal bool IsUiFontLoadInProgress => _uiFontLoadTask != null && !_uiFontLoadTask.IsCompleted;

        protected override void OnUpdate()
        {
            if (!Input.CurrentState.Keyboard[226] && !Input.CurrentState.Keyboard[230] || !Input.Pressed.Keyboard[40])
                return;
            Window.FullScreen = !Window.FullScreen;
        }

        protected override void OnUpdateStep()
        {
            if (_uiFontLoadTask != null && _uiFontLoadTask.IsCompleted)
            {
                if (_uiFontLoadTask.IsFaulted)
                {
                    Exception ex = _uiFontLoadTask.Exception?.GetBaseException();
                    if (ex != null)
                        Trace.WriteLine("[Funkin] UI font load task faulted: " + ex.Message);
                }
                _uiFontLoadTask = null;
            }

            Console.Update();
            NetworkManager.Update();
            if (_pendingState != null)
            {
                _rootGameState?.Dispose();
                _rootGameState = _pendingState;
                _pendingState = null;
                _gameStateUpdater = new Updater(_rootGameState.Update());
            }
            if (!_gameStateUpdater.Update())
                Finish = true;
            if (Input.Pressed.Keyboard[41])
                Finish = true;
            foreach (Controller controller in Controllers)
                controller.Update();
            Input.OutputState.GamePad = Output.ToArray<GamePadOutputState>();
        }

        protected override void OnDraw()
        {
            I2dRenderer r2d = Renderer.Get2dRenderer();
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
            if (ForceHD)
                _canvas.Activate();
            else
                Window.GraphicsContext.RenderToBackBuffer();
            Window.GraphicsContext.ClearBuffer();
            _rootGameState.Draw();
            Renderer.DeativateRenderer();
            Console.Draw(Renderer);
            Renderer.DeativateRenderer();
            if (!ForceHD)
                return;
            Window.GraphicsContext.RenderToBackBuffer();
            r2d.BlendMode = BlendMode.Opaque;
            r2d.Colour = Colours.White;
            Vector2i clientSize = Window.ClientSize;
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, clientSize.X, clientSize.Y);
            r2d.RenderTexture(_canvas.Textures[0], new Rectangle(0.0, 0.0, clientSize.X, clientSize.Y), flipy: true);
            r2d.Deactivate();
        }

        private static string GetContentRootDirectory()
        {
            string loc = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(loc))
                loc = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(loc) ?? string.Empty;
        }

        private void LoadResourceFiles(string inputDirectory)
        {
            if (!Directory.Exists(inputDirectory))
                return;
            foreach (string file in Directory.GetFiles(inputDirectory, "*.dat", SearchOption.AllDirectories))
                ResourceTree.MergeWith(new ResourceFile(file).Scan());
        }

        protected override Renderer CreateRenderer() => new TheRenderer(Window);

        protected override ILevelRenderer CreateLevelRenderer(Level level) =>
            new LevelRenderer(level, VideoSettings);
    }
}
