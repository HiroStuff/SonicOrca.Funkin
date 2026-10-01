using System.Collections.Generic;

namespace SonicOrca.HiroUtils.Animation
{
    public sealed class FlxAnimation
    {
        public string Name { get; set; }
        public List<int> FrameIndices { get; } = new List<int>();
        public int FrameRate { get; set; }
        public bool Looped { get; set; }
    }
}
