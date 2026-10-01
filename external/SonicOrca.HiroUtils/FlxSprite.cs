using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.HiroUtils.Animation;
using SonicOrca.HiroUtils.Graphics.Frames;

namespace SonicOrca.HiroUtils
{
    public class FlxSprite
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public double Alpha { get; set; } = 1.0;
        public bool Visible { get; set; } = true;

        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }

        public double Width { get; protected set; }
        public double Height { get; protected set; }

        public ITexture Texture { get; set; }
        public FlxAtlasFrames Frames { get; set; }
        public FlxAnimationController Animation { get; } = new FlxAnimationController();

        public void SetScale(double x, double y)
        {
            ScaleX = x;
            ScaleY = y;
            UpdateHitbox();
        }

        public void UpdateHitbox()
        {
            AtlasFrame f = GetCurrentAtlasFrame();
            Width = f.LogicalWidth * ScaleX;
            Height = f.LogicalHeight * ScaleY;
        }

        public void CenterOrigin()
        {
            AtlasFrame f = GetCurrentAtlasFrame();
            OriginX = f.LogicalWidth * 0.5;
            OriginY = f.LogicalHeight * 0.5;
        }

        public void CenterOffsets(bool adjustPosition = false)
        {
            AtlasFrame f = GetCurrentAtlasFrame();
            OffsetX = (f.LogicalWidth - Width) * 0.5;
            OffsetY = (f.LogicalHeight - Height) * 0.5;
            if (adjustPosition)
            {
                X += OffsetX;
                Y += OffsetY;
            }
        }

        protected void SyncFrameOrigin()
        {
            UpdateHitbox();
            CenterOrigin();
            CenterOffsets();
        }

        public virtual void Update(double elapsedSeconds)
        {
            Animation.Update(elapsedSeconds);
        }

        public virtual void Draw(I2dRenderer r2d)
        {
            if (!Visible || Texture == null || Frames == null || Frames.Frames.Count == 0)
                return;

            int atlasIdx = Animation.GetCurrentAtlasFrameIndex(Frames);
            if (atlasIdx < 0 || atlasIdx >= Frames.Frames.Count)
                return;

            AtlasFrame f = Frames.Frames[atlasIdx];
            var source = new Rectangle(f.X, f.Y, f.Width, f.Height);
            double destW = f.Width * ScaleX;
            double destH = f.Height * ScaleY;
            GetDrawTrimOffsets(f, out double trimX, out double trimY);
            double dx = X + OffsetX - OriginX * ScaleX + trimX;
            double dy = Y + OffsetY - OriginY * ScaleY + trimY;
            var dest = new Rectangle(dx, dy, destW, destH);

            Colour prev = r2d.Colour;
            double prevA = prev.Alpha / 255.0;
            r2d.Colour = new Colour(prevA * Alpha, 1.0, 1.0, 1.0);
            r2d.RenderTexture(Texture, source, dest);
            r2d.Colour = prev;
        }

        private AtlasFrame GetCurrentAtlasFrame()
        {
            if (Frames == null || Frames.Frames.Count == 0)
                return new AtlasFrame { Width = 1, Height = 1, SourceWidth = 1, SourceHeight = 1 };
            int idx = Animation.GetCurrentAtlasFrameIndex(Frames);
            if (idx < 0 || idx >= Frames.Frames.Count)
                idx = 0;
            return Frames.Frames[idx];
        }

        protected virtual void GetDrawTrimOffsets(AtlasFrame f, out double trimX, out double trimY)
        {
            trimX = -f.FrameX * ScaleX;
            trimY = -f.FrameY * ScaleY;
        }
    }
}
