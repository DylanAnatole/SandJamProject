using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Original win presentation (walkthrough videos): once the painting is complete the stage camera
    // pushes in so the finished picture fills the screen and the stash/lanes slide out below.
    // Presentation only: polls the game state, restores the camera when a new attempt starts.
    public sealed class WinZoom : MonoBehaviour
    {
        public float Duration = .9f, WidthMargin = 1.06f, ScreenCenterY = .58f;
        SandJamSceneController controller;
        Camera cam;
        Vector3 startPosition, targetPosition;
        float startFov, targetFov, progress = -1;

        public void Bind(SandJamSceneController owner) { controller = owner; cam = owner.GameCamera; }

        void LateUpdate()
        {
            if (!controller || !cam || controller.Game == null) return;
            bool won = controller.Game.State == GameState.Won;
            if (!won) { Restore(); return; }
            if (progress < 0 && !Begin()) return;
            progress = Mathf.Min(1, progress + Time.unscaledDeltaTime / Duration);
            float t = Mathf.SmoothStep(0, 1, progress);
            cam.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, t);
        }

        bool Begin()
        {
            var board = controller.Board ? controller.Board.Renderer : null;
            if (!board) return false;
            startPosition = cam.transform.position; startFov = cam.fieldOfView;
            var bounds = board.bounds;
            var local = cam.transform.InverseTransformPoint(bounds.center);
            float distance = Mathf.Max(.1f, local.z);
            // Fit the picture's width (plus a small margin) to the viewport width.
            float halfWidth = bounds.extents.x * WidthMargin;
            targetFov = 2 * Mathf.Atan(halfWidth / (distance * cam.aspect)) * Mathf.Rad2Deg;
            targetFov = Mathf.Min(targetFov, startFov);
            float halfHeight = distance * Mathf.Tan(targetFov * .5f * Mathf.Deg2Rad);
            // Slide the camera in its own plane so the picture sits slightly above the screen centre.
            targetPosition = startPosition + cam.transform.right * local.x
                + cam.transform.up * (local.y - (ScreenCenterY - .5f) * 2 * halfHeight);
            progress = 0;
            return true;
        }

        void Restore()
        {
            if (progress < 0) return;
            cam.transform.position = startPosition; cam.fieldOfView = startFov;
            progress = -1;
        }
    }
}
