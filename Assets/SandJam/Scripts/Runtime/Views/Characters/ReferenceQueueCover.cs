using UnityEngine;
namespace SandJamTest.Scene3D
{
    public sealed class ReferenceQueueCover : MonoBehaviour
    {
        public SceneActorView Actor;
        public SandJamSceneController Controller;
        public GameObject Cover;
        public Renderer[] MaskedRenderers;
        void LateUpdate()
        {
            if(Actor.Shooter==null || Controller.Game==null)return;
            bool hidden=Controller.Game.IsHidden(Actor.Shooter,Actor.SourceLane) && !Actor.Departing;
            Cover.SetActive(hidden); Actor.AmmoLabel.gameObject.SetActive(!hidden);
            if(MaskedRenderers!=null)foreach(var renderer in MaskedRenderers)if(renderer)renderer.enabled=!hidden;
        }
    }
}

