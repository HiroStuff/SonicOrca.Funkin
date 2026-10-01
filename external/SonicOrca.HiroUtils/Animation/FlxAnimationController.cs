using System;
using System.Collections.Generic;
using SonicOrca.HiroUtils.Graphics.Frames;

namespace SonicOrca.HiroUtils.Animation
{
    public sealed class FlxAnimationController
    {
        private readonly Dictionary<string, FlxAnimation> _animations = new Dictionary<string, FlxAnimation>(StringComparer.Ordinal);
        private string _currentName;
        private int _indexInAnimation;
        private double _timeInFrame;
        private bool _finishFired;

        public string CurrentName => _currentName;

        public event Action<string> Finished;

        public void AddByPrefix(
            FlxAtlasFrames atlas,
            string animationName,
            string frameNamePrefix,
            int frameRate,
            bool looped = true)
        {
            if (atlas == null)
                throw new ArgumentNullException(nameof(atlas));
            IReadOnlyList<int> indices = atlas.GetFrameIndicesByPrefix(frameNamePrefix);
            if (indices.Count == 0)
                return;

            var anim = new FlxAnimation
            {
                Name = animationName,
                FrameRate = Math.Max(1, frameRate),
                Looped = looped
            };
            foreach (int i in indices)
                anim.FrameIndices.Add(i);

            _animations[animationName] = anim;
        }

        public void AddByExplicitFrames(string animationName, IEnumerable<int> atlasFrameIndices, int frameRate, bool looped = true)
        {
            var anim = new FlxAnimation
            {
                Name = animationName,
                FrameRate = Math.Max(1, frameRate),
                Looped = looped
            };
            foreach (int i in atlasFrameIndices)
                anim.FrameIndices.Add(i);
            if (anim.FrameIndices.Count > 0)
                _animations[animationName] = anim;
        }

        public void Play(string name, bool force = true)
        {
            if (string.IsNullOrEmpty(name) || !_animations.ContainsKey(name))
                return;
            if (!force && _currentName == name)
                return;

            _currentName = name;
            _indexInAnimation = 0;
            _timeInFrame = 0.0;
            _finishFired = false;
        }

        public void Update(double elapsedSeconds)
        {
            if (string.IsNullOrEmpty(_currentName) || !_animations.TryGetValue(_currentName, out FlxAnimation anim))
                return;
            if (anim.FrameIndices.Count == 0)
                return;

            double frameTime = 1.0 / anim.FrameRate;
            _timeInFrame += elapsedSeconds;

            while (_timeInFrame >= frameTime)
            {
                _timeInFrame -= frameTime;

                if (_indexInAnimation < anim.FrameIndices.Count - 1)
                {
                    _indexInAnimation++;
                }
                else if (anim.Looped)
                {
                    _indexInAnimation = 0;
                }
                else
                {
                    if (!_finishFired)
                    {
                        _finishFired = true;
                        Finished?.Invoke(_currentName);
                    }
                    break;
                }
            }
        }

        public int GetCurrentAtlasFrameIndex(FlxAtlasFrames atlas)
        {
            if (atlas == null || string.IsNullOrEmpty(_currentName) || !_animations.TryGetValue(_currentName, out FlxAnimation anim))
                return 0;
            if (anim.FrameIndices.Count == 0)
                return 0;
            int clamped = Math.Min(_indexInAnimation, anim.FrameIndices.Count - 1);
            return anim.FrameIndices[clamped];
        }
    }
}