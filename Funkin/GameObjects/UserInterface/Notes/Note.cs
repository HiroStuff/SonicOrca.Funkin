using SonicOrca.Funkin.Assets;
using SonicOrca.Funkin.Meta.Data;
using SonicOrca.Graphics;
using SonicOrca.HiroUtils;

namespace SonicOrca.Funkin.GameObjects.UserInterface.Notes
{
    public sealed class Note : FlxSprite
    {
        public static readonly string[] Directions = { "left", "down", "up", "right" };

        public int Id { get; }
        public double Time { get; }
        public double Length { get; }
        public bool IsPlayer { get; }
        public bool IsSustain { get; }
        public StrumLine Owner { get; set; }

        public Note(FunkinSparrowAtlas notesAtlas, int id, double time, double length, bool isPlayer)
        {
            X = -9999;
            Y = -9999;

            Id = id;
            Time = time;
            Length = length;
            IsPlayer = isPlayer;
            IsSustain = length > 0;

            Texture = notesAtlas.Texture;
            Frames = notesAtlas.Frames;

            string direction = Directions[id];
            Animation.AddByPrefix(Frames, "main", direction + "0", 24, true);
            Animation.AddByPrefix(Frames, "hold", direction + " hold piece0", 24, false);
            Animation.AddByPrefix(Frames, "hold end", direction + " hold end0", 24, false);

            PlayAnim("main");
            SetScale(0.7, 0.7);
            SyncFrameOrigin();
        }

        public override void Update(double elapsed)
        {
            base.Update(elapsed);
            SyncFrameOrigin();

            IFunkinGameplay game = FunkinGameplay.Current;
            if (game?.Chart == null)
                return;

            StrumLine strumLine = IsPlayer ? game.PlayerStrums : game.CpuStrums;
            if (strumLine == null || Id < 0 || Id >= strumLine.Strums.Count)
                return;

            Strum strum = strumLine.Strums[Id];
            double scroll = game.Chart.ScrollSpeed;
            double distance = (Time - Conductor.SongPosition) * (0.45 * scroll);

            X = strum.X;
            Y = strum.Y + distance;

            if (!IsPlayer && Time < Conductor.SongPosition)
            {
                Destroy();
                strum.PlayAnim("confirm");
                return;
            }

            if (Time < Conductor.SongPosition - 500.0 / scroll)
                Destroy();
        }

        public void PlayAnim(string name)
        {
            Animation.Play(name, true);
        }

        public void Destroy()
        {
            Visible = false;
            Owner?.RemoveNote(this);
        }

        public new void Draw(I2dRenderer r2d)
        {
            if (Visible)
                base.Draw(r2d);
        }
    }
}