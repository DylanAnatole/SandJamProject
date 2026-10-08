using UnityEngine;
namespace SandJamTest.Scene3D
{
    [DefaultExecutionOrder(200)]
    public sealed class OverlayCameraSync : MonoBehaviour
    {
        public Camera WorldCamera;
        Camera overlay;
        void Awake(){overlay=GetComponent<Camera>();}
        void LateUpdate(){overlay.rect=WorldCamera.rect;overlay.aspect=WorldCamera.aspect;}
    }
}
