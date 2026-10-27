using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class CharacterFreezeView : MonoBehaviour
    {
        public SceneActorView Actor;
        public Renderer Shell;
        public TextMesh Counter;
        // The original ice mesh (Ice_Cube) melts from the top down through its blend shape, one step per count (Freeze
        // gif: whole at 5, half at 2, a thin collar at 1, gone at 0). Null: the plain shell just fades out.
        public Mesh Melting;
        // Melt per count gone, fitted on the gif: 4 of 5 left only chips the top, 2 of 5 is half, 1 of 5 a collar.
        const float MeltSpeed = 2.5f, MeltCurve = 1.5f, FlashTime = .3f, FadeSpeed = 3.5f;
        Shooter bound;
        float opacity, melt, flash;
        int start, lastRemaining;
        Vector3 baseScale;
        MeshFilter filter;
        MaterialPropertyBlock properties;
        static readonly int OpacityId = Shader.PropertyToID("_Opacity"), FlashId = Shader.PropertyToID("_Flash");
        void Awake(){baseScale=Shell.transform.localScale;properties=new MaterialPropertyBlock();filter=Shell.GetComponent<MeshFilter>();}
        int shownFreeze=-2; // the number on the ice, rewritten only when it changes
        void LateUpdate()
        {
            if(Actor.Shooter==null)return;
            if(bound!=Actor.Shooter){bound=Actor.Shooter;opacity=bound.IsFrozen?1:0;start=lastRemaining=bound.FreezeRemaining;melt=bound.IsFrozen?0:1;flash=0;}
            float dt=Time.deltaTime;
            int remaining=bound.FreezeRemaining;
            if(remaining<lastRemaining)flash=1;
            lastRemaining=remaining;
            flash=Mathf.MoveTowards(flash,0,dt/FlashTime);
            if(Melting)
            {
                melt=Mathf.MoveTowards(melt,start>0?Mathf.Pow(1-(float)remaining/start,MeltCurve):1,dt*MeltSpeed);
                var mesh=BlendShapeCache.At(Melting,melt);
                if(filter.sharedMesh!=mesh)filter.sharedMesh=mesh;
                // The last of the ice goes once it has melted flat.
                opacity=bound.IsFrozen || melt<1?1:Mathf.MoveTowards(opacity,0,dt*FadeSpeed);
            }
            else
            {
                opacity=bound.IsFrozen?1:Mathf.MoveTowards(opacity,0,dt*FadeSpeed);
                Shell.transform.localScale=baseScale*(1+(1-opacity)*.16f);
            }
            Shell.gameObject.SetActive(opacity>0);
            properties.SetFloat(OpacityId,opacity);properties.SetFloat(FlashId,flash);Shell.SetPropertyBlock(properties);
            Counter.gameObject.SetActive(bound.IsFrozen);
            // Original: the ice shows just the remaining number.
            int shown=bound.IsFrozen?remaining:-1;
            if(shown!=shownFreeze){shownFreeze=shown;Counter.text=shown<0?"":DisplayAmount.Text(shown);}
            Actor.AmmoLabel.gameObject.SetActive(!bound.IsFrozen && !Actor.Departing);
        }
    }
}
