using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class ReferenceTextOutline : MonoBehaviour
    {
        TextMesh label; TextMesh[] copies; string previous; Renderer body; Vector3 lastSize; int phase;
        void Awake(){label=GetComponent<TextMesh>();copies=GetComponentsInChildren<TextMesh>(true);body=GetComponent<Renderer>();phase=GetInstanceID()&7;}
        // Reading TextMesh.text makes a new string each time: it is read when the label's size changes, plus a full
        // check every 8th frame for same-width edits.
        void LateUpdate()
        {
            var size=body?body.bounds.size:Vector3.zero;
            if(size==lastSize && ((Time.frameCount+phase)&7)!=0)return;
            lastSize=size;
            var now=label.text;if(now==previous)return;previous=now;foreach(var copy in copies)if(copy!=label)copy.text=previous;
        }
    }
}
