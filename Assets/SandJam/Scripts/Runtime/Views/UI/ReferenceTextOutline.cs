using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class ReferenceTextOutline : MonoBehaviour
    {
        TextMesh label; TextMesh[] copies; string previous;
        void Awake(){label=GetComponent<TextMesh>();copies=GetComponentsInChildren<TextMesh>(true);}
        void LateUpdate(){if(label.text==previous)return;previous=label.text;foreach(var copy in copies)if(copy!=label)copy.text=previous;}
    }
}
