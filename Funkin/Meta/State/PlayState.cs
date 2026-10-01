using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Graphics;
using SonicOrca.Geometry;
using SonicOrca.Input;
using SonicOrca.Resources;
using SonicOrca.Funkin.Assets;
using SonicOrca.Funkin.GameObjects.UserInterface.Notes;
using SonicOrca.Funkin.Meta.Data;

namespace SonicOrca.Funkin.Meta.State
{
    internal sealed class PlayState : IGameState, IFunkinGameplay, IDisposable
    {
        private readonly FunkinGameContext _context;
        private readonly string _contentRoot;
        private int _lastTick;
        private bool _disposed;
        private bool _chartLoadFailed;

        public string SongFolder { get; }
        public string Difficulty { get; }
        public ChartFormat Chart { get; private set; }
        public StrumLine PlayerStrums { get; private set; }
        public StrumLine CpuStrums { get; private set; }

        private FunkinSparrowAtlas _notesAtlas;
        private FunkinSparrowAtlas _receptorsAtlas;
        private Font _font;

        private SampleInstance _inst;
        private SampleInstance _voices;
        private bool _songPlaybackStarted;
        private bool _requestedSongEnd;

        private Task<(Sample Inst, Sample Voices)> _songAudioDecodeTask;
        private bool _songAudioDecodeStarted;
        private bool _songAudioDecodeApplied;

        public PlayState(FunkinGameContext context, string songFolder, string difficulty)
        {
            _context = context;
            SongFolder = songFolder;
            Difficulty = difficulty;
            _contentRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            _lastTick = Environment.TickCount;
        }

        private void EnsureLoaded()
        {
            if (Chart != null || _chartLoadFailed)
                return;

            try
            {
                Chart = ChartParser.Parse(SongFolder, Difficulty, key =>
                {
                    string p = Path.Combine(_contentRoot, "assets", key + ".json");
                    return File.ReadAllText(p);
                });

                Conductor.MapBpmChanges(Chart);
                Conductor.Bpm = Chart.Bpm;
                Conductor.SongPosition = 0.0;

                IGraphicsContext gx = _context.Window.GraphicsContext;
                string images = Path.Combine(_contentRoot, "assets", "images");
                string dataDir = Path.Combine(_contentRoot, "assets", "data");
                string keybinds = Path.Combine(dataDir, "keybinds.xml");

                _notesAtlas = FunkinSparrowAtlas.Load(gx, images, "notes");
                _receptorsAtlas = FunkinSparrowAtlas.Load(gx, images, "receptors");

                CpuStrums = new StrumLine(_receptorsAtlas, _notesAtlas, keybinds, 1920.0 * 0.25, 50.0, true);
                PlayerStrums = new StrumLine(_receptorsAtlas, _notesAtlas, keybinds, 1920.0 * 0.75, 50.0, false);

                List<ChartNote> playerNotes = Chart.Notes.Where(n => n.MustPress).Select(CloneNote).OrderBy(n => n.Time).ToList();
                List<ChartNote> cpuNotes = Chart.Notes.Where(n => !n.MustPress).Select(CloneNote).OrderBy(n => n.Time).ToList();
                PlayerStrums.SetChartNotes(playerNotes);
                CpuStrums.SetChartNotes(cpuNotes);

                BeginSongAudioDecode();

                FunkinGameplay.Current = this;
            }
            catch
            {
                _chartLoadFailed = true;
                _context.RequestState(new SongSelectState(_context));
            }
        }

        private void BeginSongAudioDecode()
        {
            if (_songAudioDecodeStarted)
                return;
            _songAudioDecodeStarted = true;

            string songsRoot = Path.Combine(_contentRoot, "assets", "songs");
            string instPath = Path.Combine(songsRoot, SongFolder, "Inst.ogg");
            if (!File.Exists(instPath))
                instPath = Path.Combine(songsRoot, SongFolder.ToLowerInvariant(), "Inst.ogg");

            string voicesPath = Path.Combine(songsRoot, SongFolder, "Voices.ogg");
            if (!File.Exists(voicesPath))
                voicesPath = Path.Combine(songsRoot, SongFolder.ToLowerInvariant(), "Voices.ogg");

            string instPathCopy = instPath;
            string voicesPathCopy = voicesPath;

            _songAudioDecodeTask = Task.Run(() =>
            {
                Sample inst = SongSampleLoader.TryLoadOgg(instPathCopy);
                Sample voices = null;
                if (inst != null && File.Exists(voicesPathCopy))
                    voices = SongSampleLoader.TryLoadOgg(voicesPathCopy);
                return (inst, voices);
            });
        }

        private void TryApplySongAudioDecodeResult()
        {
            if (_songAudioDecodeApplied || _songAudioDecodeTask == null)
                return;
            if (!_songAudioDecodeTask.IsCompleted)
                return;

            _songAudioDecodeApplied = true;

            try
            {
                if (_songAudioDecodeTask.IsFaulted)
                {
                    Trace.WriteLine("[Funkin] Song audio decode failed: " +
                        _songAudioDecodeTask.Exception?.GetBaseException()?.Message);
                    return;
                }

                if (!_songAudioDecodeTask.IsCompletedSuccessfully)
                    return;

                (Sample instSample, Sample voicesSample) = _songAudioDecodeTask.Result;

                if (_disposed)
                    return;

                if (instSample == null)
                {
                    Trace.WriteLine("[Funkin] Instrumental decode failed or missing — chart runs silent.");
                    return;
                }

                _inst = new SampleInstance(_context, new SampleInfo(instSample))
                {
                    Classification = SampleInstanceClassification.Music,
                    Volume = 1.0
                };

                if (voicesSample != null)
                {
                    _voices = new SampleInstance(_context, new SampleInfo(voicesSample))
                    {
                        Classification = SampleInstanceClassification.Music,
                        Volume = 1.0
                    };
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[Funkin] Song audio task failed: " + ex.Message);
            }
        }

        private static ChartNote CloneNote(ChartNote n)
        {
            return new ChartNote
            {
                Time = n.Time,
                Id = n.Id,
                Length = n.Length,
                MustPress = n.MustPress
            };
        }

        private Font GetFont()
        {
            if (_font != null)
                return _font;
            _context.TryGetUiFont(out _font);
            return _font;
        }

        public IEnumerable<UpdateResult> Update()
        {
            while (true)
            {
                EnsureLoaded();
                if (_chartLoadFailed)
                {
                    yield return UpdateResult.Next();
                    continue;
                }

                int now = Environment.TickCount;
                double elapsed = Math.Min((now - _lastTick) / 1000.0, 0.1);
                if (elapsed <= 0.0)
                    elapsed = 1.0 / 60.0;
                _lastTick = now;

                TryApplySongAudioDecodeResult();

                if (!_songAudioDecodeApplied)
                {
                    Conductor.SongPosition = 0.0;
                    Conductor.UpdateTime();
                }
                else if (_inst != null)
                {
                    if (!_songPlaybackStarted)
                    {
                        _songPlaybackStarted = true;
                        Conductor.SongPosition = 0.0;
                        _inst.SeekToStart();
                        _inst.Play();
                        if (_voices != null)
                        {
                            _voices.SeekToStart();
                            _voices.Play();
                        }
                    }

                    Conductor.SongPosition = _inst.Position * 1000.0;
                    Conductor.UpdateTime();

                    if (_songPlaybackStarted && !_inst.Playing && !_requestedSongEnd)
                    {
                        _requestedSongEnd = true;
                        _context.RequestState(new SongSelectState(_context));
                    }
                }
                else
                    Conductor.Update(elapsed);
                }

                InputContext input = _context.Input;
                if (input.Pressed.Keyboard[KeyboardState.KEY_ESCAPE])
                    _context.RequestState(new SongSelectState(_context));

                if (_songAudioDecodeApplied)
                {
                    CpuStrums?.Update(elapsed, input);
                    PlayerStrums?.Update(elapsed, input);
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

            if (Chart == null)
                return;

            r2d.BlendMode = BlendMode.Alpha;
            CpuStrums?.Draw(r2d);
            PlayerStrums?.Draw(r2d);

            Font font = GetFont();
            if (font != null)
            {
                string dbg = "Position: " + Math.Round(Conductor.SongPosition / 1000.0, 2)
                    + "\nStep: " + Conductor.CurStep
                    + "\nBeat: " + Conductor.CurBeat
                    + "\nMeasure: " + Conductor.CurMeasure
                    + "\nESC: song select";
                var tri = new TextRenderInfo
                {
                    Font = font,
                    Text = dbg,
                    Bounds = new Rectangle(3.0, 3.0, 600.0, 200.0),
                    Colour = new Colour(1.0, 1.0, 1.0, 1.0 / 3.0),
                    SizeMultiplier = 1.25f
                };
                r2d.RenderText(tri);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (ReferenceEquals(FunkinGameplay.Current, this))
                FunkinGameplay.Current = null;
            _inst?.Stop();
            _inst?.Dispose();
            _inst = null;
            _voices?.Stop();
            _voices?.Dispose();
            _voices = null;
            _songAudioDecodeTask = null;
            _notesAtlas?.Texture.Dispose();
            _receptorsAtlas?.Texture.Dispose();
        }
    }
}