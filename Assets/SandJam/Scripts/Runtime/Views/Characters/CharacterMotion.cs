using UnityEngine;
namespace SandJamTest.Scene3D
{
    // Drives the recovered rig clips; translation remains owned by SceneActorView.
    public sealed class CharacterMotion : MonoBehaviour
    {
        public Animator Animator;
        public bool WalkContinuously;
        public bool Walking { get; private set; }
        static readonly int WalkingId=UnityEngine.Animator.StringToHash("Walking");
        bool posePending;
        // Characters are re-bound while the gameplay page is hidden (Home/Result), so the Animator can be
        // inactive or not yet initialised; only touch it when it can run, otherwise defer to OnEnable.
        bool Ready { get { return Animator && Animator.isActiveAndEnabled && Animator.runtimeAnimatorController; } }
        void OnEnable(){if(!Ready)return;Animator.Rebind();SetWalking(WalkContinuously);if(posePending)ApplyPose();Animator.Update(0);}
        public void SetWalking(bool value)
        {
            Walking=value||WalkContinuously;
            if(Ready)Animator.SetBool(WalkingId,Walking);
        }
        public void ResetPose()
        {
            if(!Ready){posePending=true;return;}
            Animator.Rebind();SetWalking(WalkContinuously);ApplyPose();Animator.Update(0);
        }
        void ApplyPose(){posePending=false;Animator.Play(WalkContinuously?"Walk":"Sit",0,WalkContinuously?0:1);}
    }
}
