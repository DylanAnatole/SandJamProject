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
        Color mainColor;
        int labelState=-1;
        int lastState=-1; float lastAmount=-1; Color lastColor;
        static readonly int Width=Shader.PropertyToID("_OutlineWidth"),ColorId=Shader.PropertyToID("_Color");
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
            var color=Skin.sharedMaterial.GetColor(ColorId);
            bool running=Actor.Moving;
            int state=(front?1:0)|(docked?2:0)|(running?4:0);
            if(state==lastState && amount==lastAmount && color==lastColor && !Actor.Shooter.Half && !vessel.Settling)return;
            lastState=state;lastAmount=amount;lastColor=color;
            vessel.Apply(front,docked,running);
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
        const float LabelOutline=.055f, BackTint=.3f, BackInkShade=.5f; // thin edge as in the original
        void StyleLabel(bool highlighted,Color color)
        {
            int state=highlighted?1:0;
            if(state==labelState || !Actor.AmmoLabel)return;
            labelState=state;
            if(labels==null)mainColor=Actor.AmmoLabel.color;
            // Re-read the outline copies when some were replaced (a legacy outline hands over to OutlinedText when its
            // cube is first shown, destroying the old copies).
            if(labels==null || System.Array.Exists(labels,l=>!l))labels=Actor.AmmoLabel.GetComponentsInChildren<TextMesh>(true);
            var outline=Actor.AmmoLabel.GetComponent<OutlinedText>();
            if(outline)outline.SetThickness(LabelOutline);
            var ink=outline?outline.Ink:TextOutliner.Ink;
            Color backInk=color*BackInkShade;backInk.a=1;
            if(outline)outline.ShowInk(highlighted?ink:backInk); // the outline is one mesh, recoloured as a whole
            foreach(var label in labels)
            {
                bool main=label==Actor.AmmoLabel;
                if(!main && outline)continue;
                // Back rows (recording 2, 150 s): pale digits tinted by the block with an edge in a darker shade of the
                // block itself (dark yellow on yellow, dark green on green), not black; still readable on light blocks.
                label.color=main?(highlighted?mainColor:Color.Lerp(Color.white,color,BackTint)):highlighted?ink:backInk;
            }
        }
    }
}
