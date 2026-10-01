namespace SonicOrca.HiroUtils.Graphics.Frames
{
    public sealed class AtlasFrame
    {
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Index { get; set; }
        public int SourceWidth { get; set; }
        public int SourceHeight { get; set; }
        public int FrameX { get; set; }
        public int FrameY { get; set; }
        public int LogicalWidth => SourceWidth > 0 ? SourceWidth : Width;
        public int LogicalHeight => SourceHeight > 0 ? SourceHeight : Height;
    }
}
