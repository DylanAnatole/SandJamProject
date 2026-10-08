using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class VideoConfetti : MonoBehaviour
    {
        void Update()
        {
            for(int i=0;i<transform.childCount;i++)
            {
                var t=transform.GetChild(i);var p=t.localPosition;
                p.y-=Time.unscaledDeltaTime*(.5f+(i%7)*.1f);p.x+=Mathf.Sin(Time.unscaledTime*2+i)*Time.unscaledDeltaTime*.1f;
                if(p.y < -5.5f)p.y=5.5f;t.localPosition=p;t.Rotate(0,0,Time.unscaledDeltaTime*(i%2==0?60:-60));
            }
        }
    }
}
