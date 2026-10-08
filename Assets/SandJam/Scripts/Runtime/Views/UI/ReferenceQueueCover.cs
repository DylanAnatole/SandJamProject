using System.Linq;
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
            var queue=Controller.Game.Lanes[Actor.SourceLane];
            // Secret rule: the colour stays hidden until the character reaches the front of its lane.
            bool hidden=Actor.Shooter.Secret && queue.Count>0 && queue.Contains(Actor.Shooter) && queue.Peek()!=Actor.Shooter && !Actor.Departing;
            Cover.SetActive(hidden); Actor.AmmoLabel.gameObject.SetActive(!hidden);
            if(MaskedRenderers!=null)foreach(var renderer in MaskedRenderers)if(renderer)renderer.enabled=!hidden;
        }
    }
}

