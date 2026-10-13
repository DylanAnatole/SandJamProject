using System.Linq;
using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Development helper: plays the level so mechanics can be watched/recorded. Follows a solver replay
    // when one is given, otherwise the built-in hint. A new cube is sent only once the stash is idle
    // (nothing pouring, every cube at rest, finished regions settled), matching how the solver drains moves.
    public sealed class AutoPlayer : MonoBehaviour
    {
        public float Interval = .6f;
        public int[] Replay;
        // Set while a solver replay is being computed so no hint move slips in first.
        public bool WaitForReplay;
        int step;
        float timer;
        public bool Finished { get { return Replay != null && step >= Replay.Length; } }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < Interval || (WaitForReplay && Replay == null)) return;
            var controller = FindObjectOfType<SandJamSceneController>();
            if (controller == null || controller.Game == null || controller.Game.State != GameState.Playing || !Idle(controller)) return;
            timer = 0;
            int lane = Replay != null ? (step < Replay.Length ? Replay[step] : -1) : controller.Game.HintLane();
            if (lane >= 0 && controller.SelectLane(lane) && Replay != null) step++;
        }

        static bool Idle(SandJamSceneController c)
        {
            var g = c.Game;
            if (g.Slots.Any(s => g.Target(s) >= 0)) return false;
            if (c.Characters.Any(a => a.gameObject.activeInHierarchy && !a.AtRest)) return false;
            return !c.Regions.Where((view, i) => g.Regions[view.PartIndex].Remaining == 0 && !view.IsSettled).Any();
        }
    }
}
