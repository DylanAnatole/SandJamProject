using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Legacy component still referenced by the built scene and level prefab (it used to copy the label's text into
    // hand-made outline children). On load it hands the label to OutlinedText, keeping the old outline colour, and
    // removes itself, so there is a single outline system at runtime.
    public sealed class ReferenceTextOutline : MonoBehaviour
    {
        void Awake()
        {
            var label = GetComponent<TextMesh>();
            Color ink = TextOutliner.Ink; bool found = false;
            foreach (Transform child in transform)
            {
                var copy = child.GetComponent<TextMesh>(); if (!copy || child.name != "Text outline") continue;
                if (!found) { ink = copy.color; found = true; }
                Destroy(child.gameObject);
            }
            if (label) OutlinedText.Add(label, ink);
            Destroy(this);
        }
    }
}
