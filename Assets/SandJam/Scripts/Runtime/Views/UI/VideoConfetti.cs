using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class VideoConfetti : MonoBehaviour
    {
        // Original confetti are short curled ribbons, not rectangles: each piece gets a curved stroke sprite of
        // about the same length (alternating curl direction).
        static Sprite ribbon;
        // Colours seen in the original's confetti (recording, level 267 win).
        static readonly Color[] Palette={new Color(1f,.9f,.1f),new Color(.1f,.85f,1f),new Color(.2f,.5f,1f),new Color(.55f,.2f,1f),
            new Color(1f,.2f,.75f),new Color(.4f,1f,.2f),new Color(1f,.55f,.1f)};
        void Start()
        {
            if(!ribbon)ribbon=Ribbon();
            for(int i=0;i<transform.childCount;i++)
            {
                var t=transform.GetChild(i);var r=t.GetComponent<SpriteRenderer>();if(!r||!r.sprite)continue;
                float length=Mathf.Max(t.localScale.x,t.localScale.y)*r.sprite.bounds.size.y*1.25f;
                r.sprite=ribbon;r.color=Palette[i%Palette.Length];
                float k=length/ribbon.bounds.size.y;
                t.localScale=new Vector3(i%2==0?k:-k,k,t.localScale.z);
            }
        }
        static Sprite Ribbon()
        {
            const int n=64;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,name="Confetti ribbon"};
            var px=new Color32[n*n];
            // A short, gently bent pill (~45 px long, 16 px thick, round ends), like the original's confetti.
            Vector2 centre=new Vector2(32-70,32);float radius=70,half=8f,a0=-17*Mathf.Deg2Rad,a1=17*Mathf.Deg2Rad;
            Vector2 e0=centre+new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*radius,e1=centre+new Vector2(Mathf.Cos(a1),Mathf.Sin(a1))*radius;
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                var p=new Vector2(x+.5f,y+.5f);var d=p-centre;float ang=Mathf.Atan2(d.y,d.x);
                float dist=ang>=a0&&ang<=a1?Mathf.Abs(d.magnitude-radius):Mathf.Min(Vector2.Distance(p,e0),Vector2.Distance(p,e1));
                float a=Mathf.Clamp01(half+.5f-dist);
                px[y*n+x]=new Color32(255,255,255,(byte)(a*255));
            }
            tex.SetPixels32(px);tex.Apply(false,true);
            return Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),n);
        }
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
