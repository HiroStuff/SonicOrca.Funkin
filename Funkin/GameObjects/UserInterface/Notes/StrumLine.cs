using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Funkin.Assets;
using SonicOrca.Funkin.Meta.Data;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace SonicOrca.Funkin.GameObjects.UserInterface.Notes
{
    public sealed class StrumLine
    {
        public const float Spacing = 112f;

        public double X { get; set; }
        public double Y { get; set; }
        public bool Cpu { get; }

        public List<Strum> Strums { get; } = new List<Strum>();
        public List<Note> Notes { get; } = new List<Note>();
        public List<ChartNote> QueuedNotes { get; } = new List<ChartNote>();

        private readonly int[][] _keybinds;
        private readonly bool[] _keysHeld = { false, false, false, false };
        private readonly FunkinSparrowAtlas _notesAtlas;
        private int _curSpawnNote;

        public StrumLine(
            FunkinSparrowAtlas receptorsAtlas,
            FunkinSparrowAtlas notesAtlas,
            string keybindsXmlPath,
            double centerX,
            double centerY,
            bool cpu)
        {
            _notesAtlas = notesAtlas;
            Cpu = cpu;
            X = centerX;
            Y = centerY;

            _keybinds = KeybindXml.Load(keybindsXmlPath);

            double totalWidth = Spacing * 4;
            double left = centerX - totalWidth / 2.0;
            for (int id = 0; id < 4; id++)
            {
                var strum = new Strum(receptorsAtlas, left + Spacing * id, centerY, id) { StrumLine = this };
                Strums.Add(strum);
            }
        }

        public void SetChartNotes(IEnumerable<ChartNote> notes)
        {
            QueuedNotes.Clear();
            QueuedNotes.AddRange(notes);
            _curSpawnNote = 0;
        }

        internal void RemoveNote(Note note)
        {
            Notes.Remove(note);
        }

        public void Update(double elapsed, InputContext input)
        {
            foreach (Strum s in Strums)
                s.Update(elapsed);

            IFunkinGameplay game = FunkinGameplay.Current;
            if (game?.Chart == null)
                return;

            double scroll = game.Chart.ScrollSpeed;

            while (_curSpawnNote < QueuedNotes.Count
                && QueuedNotes[_curSpawnNote] != null
                && QueuedNotes[_curSpawnNote].Time - Conductor.SongPosition < 1500.0 / scroll)
            {
                ChartNote q = QueuedNotes[_curSpawnNote];
                var note = new Note(_notesAtlas, q.Id, q.Time, q.Length, !Cpu) { Owner = this };
                Notes.Add(note);
                _curSpawnNote++;
            }

            for (int i = Notes.Count - 1; i >= 0; i--)
            {
                Note n = Notes[i];
                n.Update(elapsed);
            }

            if (!Cpu && input != null)
                UpdateInput(input, scroll);
        }

        private void UpdateInput(InputContext input, double scroll)
        {
            for (int lane = 0; lane < 4 && lane < _keybinds.Length; lane++)
            {
                bool pressedEdge = false;
                bool releasedEdge = false;
                foreach (int code in _keybinds[lane])
                {
                    if (input.Pressed.Keyboard[code])
                        pressedEdge = true;
                    if (input.Released.Keyboard[code])
                        releasedEdge = true;
                }

                if (pressedEdge)
                    KeyDown(lane, scroll);
                if (releasedEdge)
                    KeyUp(lane);
            }
        }

        private void KeyDown(int strumIndex, double scroll)
        {
            if (strumIndex < 0 || strumIndex > 3 || _keysHeld[strumIndex] || Cpu)
                return;
            _keysHeld[strumIndex] = true;

            string queuedAnim = "press";
            double hitzone = 500.0 / scroll;

            List<Note> possible = Notes
                .Where(note => note != null && note.Visible
                    && note.Id == strumIndex
                    && Math.Abs(Conductor.SongPosition - note.Time) < hitzone)
                .OrderBy(note => note.Time)
                .ToList();

            if (possible.Count > 0)
            {
                double t0 = possible[0].Time;
                foreach (Note note in possible.Where(n => Math.Abs(t0 - n.Time) < 5.0).ToList())
                {
                    note.Destroy();
                }
                queuedAnim = "confirm";
            }

            Strums[strumIndex].PlayAnim(queuedAnim);
        }

        private void KeyUp(int strumIndex)
        {
            if (strumIndex < 0 || strumIndex > 3 || !_keysHeld[strumIndex] || Cpu)
                return;
            _keysHeld[strumIndex] = false;
            Strums[strumIndex].PlayAnim("static");
        }

        public void Draw(I2dRenderer r2d)
        {
            foreach (Strum s in Strums)
                s.Draw(r2d);

            foreach (Note n in Notes.OrderBy(n => n.Y))
                n.Draw(r2d);
        }
    }
}