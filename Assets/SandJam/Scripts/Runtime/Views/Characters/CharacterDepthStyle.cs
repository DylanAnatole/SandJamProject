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
        int lastState=-1; float lastAmount=-1; Color lastColor;
        static readonly int Width=Shader.PropertyToID("_OutlineWidth");
        void LateUpdate()
        {
            if(!Skin || Actor.Shooter==null || Controller.Game==null)return;
            if(!vessel){vessel=gameObject.AddComponent<CharacterSandVessel>();vessel.Initialize(Actor,Skin.sharedMaterial);}
            var lane=Controller.Game.Lanes[Actor.SourceLane];
            bool queued=lane.Contains(Actor.Shooter);
            bool front=queued && lane.Peek()==Actor.Shooter;
            bool docked=!queued;
            PauseWhenIdle(queued);
            // Restyle only when something it depends on changed (most cubes sit unchanged in their lanes); a half
            // cube keeps updating for its floating "zZ".
            float amount=Actor.DisplayedFill;
            var color=Skin.sharedMaterial.GetColor("_Color");
            int state=(front?1:0)|(docked?2:0);
            if(state==lastState && amount==lastAmount && color==lastColor && !Actor.Shooter.Half)return;
            lastState=state;lastAmount=amount;lastColor=color;
            vessel.Apply(front,docked);
            if(properties==null)properties=new MaterialPropertyBlock();
            Skin.GetPropertyBlock(properties);
            // Legs only need a thin contour; the block itself carries the bold outline.
            properties.SetFloat(Width,.006f);
            // The shader's fill effect is off for the legs; its range is not computed (reading Skin.bounds forced the
            // skinned mesh to finish skinning right here, ~2 ms a frame for the whole queue).
            properties.SetFloat("_FillEnabled",0);properties.SetFloat("_FillAmount",amount);
            properties.SetColor("_OutlineColor",Color.Lerp(color,new Color(.045f,.025f,.07f),.6f));
            Skin.SetPropertyBlock(properties);
            StyleLabel(front || docked,color);
        }
        // A cube resting in its lane holds a still pose: its Animator (and skinning) is paused until it moves again,
        // which saves ~20 rigs evaluating every frame on mobile. The state is kept across the pause.
        void PauseWhenIdle(bool queued)
        {
            var motion=Actor.Motion;var animator=motion?motion.Animator:null;if(!animator)return;
            bool idle=queued && Actor.AtRest && !Actor.Departing;
            if(animator.enabled!=idle)return;
            animator.keepAnimatorStateOnDisable=true;
            animator.enabled=!idle;
            if(!idle)motion.SetWalking(motion.Walking);
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
                // Back rows: still white digits with a dark edge (only lightly tinted by the block colour), so the count
                // stays readable on white, yellow and grey blocks too.
                var ink=new Color(.17f,.12f,.3f);
                labels[i].color=highlighted?labelColors[i]:main?Color.Lerp(Color.white,color,.18f)*new Color(.93f,.93f,.93f,1):Color.Lerp(ink,color,.2f);
            }
        }
    }
}
