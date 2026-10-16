using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Marks a HUD control by its action id (rocket / swap / select boosters are wired by BoosterController).
    public sealed class ReferenceUiPlaceholder : MonoBehaviour
    {
        public string ActionId;
    }
}
