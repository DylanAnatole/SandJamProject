using UnityEngine;

namespace SandJamTest.Scene3D
{
    // What a tap at the pointer hits on a layer (cubes on 8, UI buttons on 9). Physics only serves these taps (no
    // rigidbodies), so it does not simulate every fixed step (Project Settings > Physics: Script); colliders are
    // brought up to date here, once per tap, instead of every 20 ms for every moving cube.
    public static class ScreenTap
    {
        public const int CubeLayer = 8, ButtonLayer = 9;

        public static Collider Hit(Camera camera, int layer)
        {
            if (!camera) return null;
            Physics.SyncTransforms();
            RaycastHit hit;
            return Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, 100, 1 << layer) ? hit.collider : null;
        }

        public static SceneActorView Cube(Camera camera)
        {
            var hit = Hit(camera, CubeLayer);
            return hit ? hit.GetComponentInParent<SceneActorView>() : null;
        }
    }
}
