using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Original styling: the selectable front-row block has a thick black outline and a white
    // number; blocks further back have no outline and a number tinted with their own colour.
    public sealed class CharacterDepthStyle : MonoBehaviour
    {
        public SceneActorView Actor;
        public SandJamSceneController Controller;
        public SkinnedMeshRenderer Skin;
        MaterialPropertyBlock properties;
        CharacterSandVessel vessel;
        TextMesh[] labels;
        Color[] labelColors;
        int labelState=-1;
        static readonly int Width=Shader.PropertyToID("_OutlineWidth");
        void LateUpdate()
        {
            if(!Skin || Actor.Shooter==null || Controller.Game==null)return;
            if(!vessel){vessel=gameObject.AddComponent<CharacterSandVessel>();vessel.Initialize(Actor,Skin.sharedMaterial);}
            var lane=Controller.Game.Lanes[Actor.SourceLane];
            bool queued=lane.Contains(Actor.Shooter);
            bool front=queued && lane.Peek()==Actor.Shooter;
            bool docked=!queued;
            vessel.Apply(front,docked);
            if(properties==null)properties=new MaterialPropertyBlock();
            Skin.GetPropertyBlock(properties);
            float amount=Actor.DisplayedFill;
            // Legs only need a thin contour; the block itself carries the bold outline.
            properties.SetFloat(Width,.006f);
            properties.SetFloat("_FillEnabled",0);properties.SetFloat("_FillAmount",amount);
            properties.SetFloat("_FillBottom",Skin.bounds.min.y+.12f*Skin.bounds.size.y);
            properties.SetFloat("_FillTop",Skin.bounds.max.y);
            var color=Skin.sharedMaterial.GetColor("_Color");
            properties.SetColor("_OutlineColor",Color.Lerp(color,new Color(.045f,.025f,.07f),.6f));
            Skin.SetPropertyBlock(properties);
            StyleLabel(front || docked,color);
        }
        void StyleLabel(bool highlighted,Color color)
        {
            int state=highlighted?1:0;
            if(state==labelState || !Actor.AmmoLabel)return;
            labelState=state;
            if(labels==null){labels=Actor.AmmoLabel.GetComponentsInChildren<TextMesh>(true);labelColors=System.Array.ConvertAll(labels,l=>l.color);}
            for(int i=0;i<labels.Length;i++)
            {
                bool main=labels[i]==Actor.AmmoLabel;
                // Back rows: embossed look, a light tint of the block colour with a slightly darker edge.
                labels[i].color=highlighted?labelColors[i]:main?Color.Lerp(color,Color.white,.62f):Color.Lerp(color,Color.black,.45f);
            }
        }
    }
}
