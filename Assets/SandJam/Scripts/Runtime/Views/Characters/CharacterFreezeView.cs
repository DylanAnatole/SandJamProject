using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class CharacterFreezeView : MonoBehaviour
    {
        public SceneActorView Actor;
        public Renderer Shell;
        public TextMesh Counter;
        Shooter bound;
        float opacity;
        Vector3 baseScale;
        MaterialPropertyBlock properties;
        public bool IceVisible { get { return Shell.gameObject.activeSelf; } }
        void Awake(){baseScale=Shell.transform.localScale;properties=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            if(Actor.Shooter==null)return;
            if(bound!=Actor.Shooter){bound=Actor.Shooter;opacity=bound.IsFrozen?1:0;}
            opacity=bound.IsFrozen?1:Mathf.MoveTowards(opacity,0,Time.deltaTime*3.5f);
            Shell.gameObject.SetActive(opacity>0);
            Shell.transform.localScale=baseScale*(1+(1-opacity)*.16f);
            properties.SetFloat("_Opacity",opacity);Shell.SetPropertyBlock(properties);
            Counter.gameObject.SetActive(bound.IsFrozen);
            // Original: the ice shows just the remaining number.
            Counter.text=bound.IsFrozen?bound.FreezeRemaining.ToString():"";
            Actor.AmmoLabel.gameObject.SetActive(!bound.IsFrozen && !Actor.Departing);
        }
    }
}
