using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using SonicOrca;
using SonicOrca.Graphics;
using SonicOrca.Geometry;
using SonicOrca.HiroUtils.Text;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace SonicOrca.Funkin.Meta.State
{
    internal sealed class SongSelectState : IGameState
    {
        private readonly FunkinGameContext _context;
        private readonly string _contentRoot;
        private readonly List<SongChartEntry> _entries = new List<SongChartEntry>();
        private int _selected;
        private string _loadingText = "";
        private Font _font;
        private bool _loggedDrawOnce;
        private TtfTextRenderer _ttf;
        private readonly Dictionary<string, ITexture> _ttfTextureCache = new Dictionary<string, ITexture>();
        private string _lastLoadingText;

        private struct SongChartEntry
        {
            public string SongFolder;
            public string Difficulty;
            public string DisplayLabel;
        }

        public SongSelectState(FunkinGameContext context)
        {
            _context = context;
            _contentRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            Trace.WriteLine("[SongSelect] Content root: " + _contentRoot);
            ScanCharts();
            TryCreateTtfRenderer();
        }

        private void ScanCharts()
        {
            _entries.Clear();
            string songsRoot = Path.Combine(_contentRoot, "assets", "songs");
            if (!Directory.Exists(songsRoot))
            {
                Trace.WriteLine("[SongSelect] No songs folder at: " + songsRoot);
                return;
            }

            foreach (string folder in Directory.GetDirectories(songsRoot))
            {
                string folderName = Path.GetFileName(folder);
                foreach (string file in Directory.GetFiles(folder, "*.json"))
                {
                    string diff = Path.GetFileNameWithoutExtension(file);
                    _entries.Add(new SongChartEntry
                    {
                        SongFolder = folderName,
                        Difficulty = diff,
                        DisplayLabel = folderName + "/" + Path.GetFileName(file)
                    });
                }
            }

            Trace.WriteLine("[SongSelect] Scanned " + _entries.Count + " chart(s) under " + songsRoot);
            for (int i = 0; i < Math.Min(_entries.Count, 8); i++)
                Trace.WriteLine("[SongSelect]   " + i + ": " + _entries[i].DisplayLabel);
            if (_entries.Count > 8)
                Trace.WriteLine("[SongSelect]   ... and " + (_entries.Count - 8) + " more");
        }

        private Font GetFont()
        {
            if (_ttf != null)
                return null;
            if (_font != null)
                return _font;
            if (_context.TryGetUiFont(out Font f))
            {
                _font = f;
                Trace.WriteLine("[SongSelect] Font: " + _context.UiFontResourceKey);
            }
            return _font;
        }

        private void TryCreateTtfRenderer()
        {
            string ttfPath = Path.Combine(_contentRoot, "assets", "fonts", "vcr.ttf");
            if (!File.Exists(ttfPath))
            {
                Trace.WriteLine("[SongSelect] No TTF at " + ttfPath + " (using resource font fallback)");
                return;
            }

            try
            {
                _ttf = new TtfTextRenderer(ttfPath, 30);
                Trace.WriteLine("[SongSelect] TTF loaded: " + ttfPath);
            }
            catch (Exception ex)
            {
                _ttf = null;
                Trace.WriteLine("[SongSelect] TTF load failed: " + ex.Message);
            }
        }

        private ITexture GetTtfTexture(string text, bool selected)
        {
            if (_ttf == null)
                return null;

            string key = (selected ? "sel|" : "norm|") + text;
            if (_ttfTextureCache.TryGetValue(key, out ITexture cached))
                return cached;

            Colour c = selected ? new Colour(1.0, 0.0, 1.0, 64.0 / 255.0) : Colours.White;
            ITexture tex = _ttf.RenderTextTexture(_context.Window.GraphicsContext, text, c);
            _ttfTextureCache[key] = tex;
            return tex;
        }

        public IEnumerable<UpdateResult> Update()
        {
            while (true)
            {
                if (_entries.Count == 0)
                {
                    yield return UpdateResult.Next();
                    continue;
                }

                InputContext input = _context.Input;
                if (input.Pressed.Keyboard[KeyboardState.KEY_UP])
                    ChangeSelection(-1);
                if (input.Pressed.Keyboard[KeyboardState.KEY_DOWN])
                    ChangeSelection(1);

                if (input.Pressed.Keyboard[KeyboardState.KEY_RETURN])
                {
                    SongChartEntry e = _entries[_selected];
                    string jsonPath = Path.Combine(_contentRoot, "assets", "songs", e.SongFolder, e.Difficulty + ".json");
                    if (!File.Exists(jsonPath))
                    {
                        _loadingText = "Missing: " + jsonPath;
                    }
                    else
                    {
                        _loadingText = "Loading " + e.SongFolder.ToUpperInvariant() + " (" + e.Difficulty.ToUpperInvariant() + ")";
                        _context.RequestState(new PlayState(_context, e.SongFolder, e.Difficulty));
                    }
                }

                yield return UpdateResult.Next();
            }
        }

        private void ChangeSelection(int delta)
        {
            if (_entries.Count == 0)
                return;
            _selected = Wrap(_selected + delta, 0, _entries.Count - 1);
        }

        private static int Wrap(int value, int min, int max)
        {
            int range = max - min + 1;
            if (range <= 0)
                return min;
            int v = (value - min) % range;
            if (v < 0)
                v += range;
            return min + v;
        }

        public void Draw()
        {
            I2dRenderer r2d = _context.Renderer.Get2dRenderer();
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
            r2d.BlendMode = BlendMode.Opaque;
            r2d.Colour = Colours.Black;
            r2d.RenderQuad(r2d.Colour, r2d.ClipRectangle);

            if (_ttf != null)
            {
                DrawWithTtf(r2d);
                return;
            }

            Font font = GetFont();
            if (!_loggedDrawOnce)
            {
                _loggedDrawOnce = true;
                Trace.WriteLine("[SongSelect] Draw: entries=" + _entries.Count + ", font=" + (font == null ? "null (no text)" : "ok"));
            }

            if (font == null)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                bool sel = i == _selected;
                var tri = new TextRenderInfo
                {
                    Font = font,
                    Text = _entries[i].DisplayLabel,
                    Bounds = new Rectangle(20.0, 20.0 + 28.0 * i, 1800.0, 32.0),
                    Colour = sel ? new Colour(1.0, 0.0, 1.0, 64.0 / 255.0) : Colours.White,
                    SizeMultiplier = 1.5f
                };
                r2d.RenderText(tri);
            }

            if (!string.IsNullOrEmpty(_loadingText))
            {
                var loadTri = new TextRenderInfo
                {
                    Font = font,
                    Text = _loadingText,
                    Bounds = new Rectangle(0.0, 700.0, 1920.0, 40.0),
                    Colour = Colours.White,
                    Alignment = FontAlignment.Centre,
                    SizeMultiplier = 1f
                };
                r2d.RenderText(loadTri);
            }
        }

        private void DrawWithTtf(I2dRenderer r2d)
        {
            if (!_loggedDrawOnce)
            {
                _loggedDrawOnce = true;
                Trace.WriteLine("[SongSelect] Draw: entries=" + _entries.Count + ", font=ttf");
            }

            r2d.BlendMode = BlendMode.Alpha;
            r2d.Colour = Colours.White;

            for (int i = 0; i < _entries.Count; i++)
            {
                bool sel = i == _selected;
                ITexture tex = GetTtfTexture(_entries[i].DisplayLabel, sel);
                if (tex == null)
                    continue;
                r2d.RenderTexture(tex, new Rectangle(20.0, 20.0 + 28.0 * i, tex.Width, tex.Height));
            }

            if (!string.IsNullOrEmpty(_loadingText))
            {
                if (_lastLoadingText != _loadingText)
                {
                    if (!string.IsNullOrEmpty(_lastLoadingText))
                    {
                        string oldKey = "norm|" + _lastLoadingText;
                        if (_ttfTextureCache.TryGetValue(oldKey, out ITexture oldTex))
                        {
                            oldTex.Dispose();
                            _ttfTextureCache.Remove(oldKey);
                        }
                    }
                    _lastLoadingText = _loadingText;
                }

                ITexture loadTex = GetTtfTexture(_loadingText, false);
                if (loadTex != null)
                {
                    double x = (1920.0 - loadTex.Width) * 0.5;
                    r2d.RenderTexture(loadTex, new Rectangle(x, 700.0, loadTex.Width, loadTex.Height));
                }
            }
        }

        public void Dispose()
        {
            foreach (ITexture texture in _ttfTextureCache.Values)
                texture.Dispose();
            _ttfTextureCache.Clear();
            _ttf?.Dispose();
            _ttf = null;
        }
    }
}