using UnityEngine;

namespace SandJamTest
{
    // A flip-book recovered from the original "gif" AnimationClips (feature and booster tutorials).
    public sealed class SpriteSequence : ScriptableObject
    {
        public Sprite[] Frames;
        public float[] Times;
        public float Length;
        public bool Loop = true;

        public Sprite FrameAt(float time)
        {
            if (Frames == null || Frames.Length == 0) return null;
            if (Loop && Length > 0) time %= Length;
            int index = 0;
            while (index + 1 < Times.Length && Times[index + 1] <= time) index++;
            return Frames[index];
        }
    }
}
