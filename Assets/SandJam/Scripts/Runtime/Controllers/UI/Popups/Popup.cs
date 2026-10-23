using UnityEngine;

namespace SandJamTest.Scene3D
{
    // One popup / page built by PopupScreens from the Level Complete card: its root and the card parts popups reuse.
    public sealed class Popup
    {
        public GameObject Root;
        public TextMesh Header, Title, Big, Caption, Left, Right;
        public SpriteRenderer Icon, Border, Ribbon;
        public VideoUiButton LeftButton, RightButton;
    }
}
