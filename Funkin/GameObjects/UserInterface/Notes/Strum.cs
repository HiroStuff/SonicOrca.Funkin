using System.Collections.Generic;
using SonicOrca.Funkin.Assets;
using SonicOrca.Graphics;
using SonicOrca.HiroUtils;
using SonicOrca.HiroUtils.Graphics.Frames;

namespace SonicOrca.Funkin.GameObjects.UserInterface.Notes
{
    public sealed class Strum : FlxSprite
    {
        public int Id { get; }
        public StrumLine StrumLine { get; internal set; }

        private readonly int _confirmFirstFrameX;
        private readonly int _confirmFirstFrameY;

        public Strum(FunkinSparrowAtlas receptorsAtlas, double x, double y, int id)
        {
            X = x;
            Y = y;
            Id = id;

            Texture = receptorsAtlas.Texture;
            Frames = receptorsAtlas.Frames;

            string direction = Note.Directions[id];
            Animation.AddByPrefix(Frames, "static", direction + " static", 24, true);
            Animation.AddByPrefix(Frames, "press", direction + " press", 24, false);
            Animation.AddByPrefix(Frames, "confirm", direction + " confirm", 24, false);

            IReadOnlyList<int> confirmFrames = Frames.GetFrameIndicesByPrefix(direction + " confirm");
            _confirmFirstFrameX = confirmFrames.Count > 0 ? Frames.Frames[confirmFrames[0]].FrameX : 0;
            _confirmFirstFrameY = confirmFrames.Count > 0 ? Frames.Frames[confirmFrames[0]].FrameY : 0;

            Animation.Finished += OnAnimationFinished;

            PlayAnim("static");
            SetScale(0.7, 0.7);
            SyncFrameOrigin();
        }

        private void OnAnimationFinished(string name)
        {
            if (name == "confirm" && StrumLine != null && StrumLine.Cpu)
                PlayAnim("static");
        }

        public void PlayAnim(string name)
        {
            Animation.Play(name, true);
        }

        public override void Update(double elapsed)
        {
            base.Update(elapsed);
            SyncFrameOrigin();
        }

        protected override void GetDrawTrimOffsets(AtlasFrame f, out double trimX, out double trimY)
        {
            if (Animation.CurrentName == "confirm")
            {
                trimX = -_confirmFirstFrameX * ScaleX;
                trimY = -_confirmFirstFrameY * ScaleY;
            }
            else
            {
                base.GetDrawTrimOffsets(f, out trimX, out trimY);
            }
        }

        public new void Draw(I2dRenderer r2d)
        {
            if (Visible)
                base.Draw(r2d);
        }
    }
}