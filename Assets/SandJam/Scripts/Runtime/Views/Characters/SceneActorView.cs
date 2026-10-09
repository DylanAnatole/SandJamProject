using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class SceneActorView : MonoBehaviour
    {
        public int SourceLane;
        public int SourceOrder;
        public int ColorId; public int Divider = 40;
        public Transform Visual;
        public CharacterMotion Motion;
        public TextMesh AmmoLabel;
        public BoxCollider ClickCollider;
        public Shooter Shooter { get; private set; }
        public bool AtRest { get { return travel >= duration; } }
        public bool Departing { get; private set; }
        public float DisplayedFill { get; private set; } = 1;
        public Vector3 AimPoint { get { return Visual ? Visual.TransformPoint(new Vector3(0,.81f,0)) : transform.position + Vector3.up * .70f; } }
        Vector3 from, destination, baseScale, basePosition;
        // Juice: a damped spring for squash & stretch (+ = stretch up) and a short sideways shake.
        float squash, squashVelocity, shakeTime;
        bool wasRunning;
        public void Punch(float strength) { squashVelocity += strength * 18f; }
        public void Shake() { shakeTime = .28f; }
        Quaternion baseRotation;
        float travel, duration, exitDelay;
        // Motion recovered from the gameplay video (27 fps): queue cubes slide forward in ~0.25 s without legs;
        // a selected cube sprouts legs and runs to its slot in ~0.5 s, bobbing and turned toward its path;
        // an emptied cube pauses briefly, then runs sideways off the nearest screen edge in ~0.45 s.
        enum Gait { Slide, Run }
        Gait gait;
        float yaw;
        const float SlideSeconds=.24f, RunSpeed=4.6f, MinRun=.36f, MaxRun=.62f, ExitSeconds=.45f, ExitPause=.12f;
        const float MaxYaw=38f, ExitYaw=90f, StepsPerSecond=7f, StepBob=.07f;
        // The rig is pitched toward the camera, so turning happens around the body's own up axis
        // (rotating the root around world Y would roll the tilted body sideways).
        Transform pose; Quaternion poseBase; bool poseCaptured;
        Renderer legs;
        void ApplyYaw()
        {
            if (pose) { pose.localRotation = poseBase * Quaternion.Euler(0, yaw, 0); transform.localRotation = Quaternion.identity; }
            else transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        // Legs only exist while running; sitting in the lane or on a waiting slot the block rests on its base.
        float ExitTarget { get { return -Mathf.Sign(destination.x - from.x) * ExitYaw; } }
        void ShowLegs(bool visible) { if (legs && legs.enabled != visible) legs.enabled = visible; }

        public void Bind(Shooter shooter, Vector3 position)
        {
            Shooter = shooter;
            gameObject.SetActive(true);
            if (baseScale == Vector3.zero) { baseScale = Visual.localScale;baseRotation=Visual.localRotation;basePosition=Visual.localPosition; }
            squash = squashVelocity = shakeTime = 0; wasRunning = false; Visual.localPosition = basePosition;
            Visual.localScale = baseScale;
            Visual.localRotation = baseRotation;
            transform.position = destination = from = position;
            if (!poseCaptured && Motion) { pose = Motion.transform; poseBase = pose.localRotation; poseCaptured = true; }
            if (!legs) { var style = GetComponent<CharacterDepthStyle>(); if (style) legs = style.Skin; }
            yaw = 0; ApplyYaw(); ShowLegs(false);
            Departing = false; travel = duration = 0;
            exitDelay=0; DisplayedFill=1; AmmoLabel.gameObject.SetActive(true);
            if(Motion)Motion.ResetPose();
            ClickCollider.enabled = true;
            AmmoLabel.text = DisplayAmount.Units(shooter.Ammo, Divider).ToString();
        }

        // Queue advance inside a lane: quick slide with a small hop, no legs.
        public void SlideTo(Vector3 position) { Begin(position, Gait.Slide, SlideSeconds); }
        // Lane -> waiting slot: run on legs, duration from distance.
        public void RunTo(Vector3 position)
        {
            float seconds = Mathf.Clamp(Vector3.Distance(transform.position, position) / RunSpeed, MinRun, MaxRun);
            Begin(position, Gait.Run, seconds);
        }
        public void MoveTo(Vector3 position, float seconds = .32f) { Begin(position, Gait.Run, seconds); }

        void Begin(Vector3 position, Gait kind, float seconds)
        {
            if (Departing || (destination - position).sqrMagnitude < .00001f) return;
            from = transform.position; destination = position;
            gait = kind; travel = 0; duration = seconds;
            // Picked: a quick stretch as it jumps off.
            if (kind == Gait.Run && !Departing) Punch(.22f);
            if(Motion)Motion.SetWalking(kind == Gait.Run);
        }

        // Half merge: this half runs into its partner's slot and disappears there (the partner becomes full).
        public void MergeInto(SceneActorView partner)
        {
            if (Departing || !gameObject.activeSelf || partner == null) return;
            Begin(partner.transform.position, Gait.Run, .3f);
            Departing = true; exitDelay = 0;
            if(Motion)Motion.SetWalking(true);
            AmmoLabel.gameObject.SetActive(false);
            ClickCollider.enabled = false;
        }

        public void Leave()
        {
            if (Departing || !gameObject.activeSelf) return;
            float side=transform.position.x<0?-1:1;
            Begin(new Vector3(side*4.8f,transform.position.y,transform.position.z),Gait.Run,ExitSeconds);
            Departing = true;
            exitDelay=ExitPause;
            if(Motion)Motion.SetWalking(false);
            AmmoLabel.gameObject.SetActive(false);
            ClickCollider.enabled = false;
        }

        public void Advance(float delta, bool firing)
        {
            if (!gameObject.activeSelf || Shooter == null) return;
            float targetFill=(float)Shooter.Ammo/Mathf.Max(1,Shooter.InitialAmmo);
            DisplayedFill=Mathf.Lerp(DisplayedFill,targetFill,1-Mathf.Exp(-18*delta));
            if(Mathf.Abs(DisplayedFill-targetFill)<.003f)DisplayedFill=targetFill;
            if(Departing && exitDelay>0)
            {
                // Brief pause: the emptied block turns toward its exit edge before running off.
                exitDelay=Mathf.Max(0,exitDelay-delta);
                yaw=Mathf.MoveTowards(yaw,ExitTarget,delta*900);ApplyYaw();ShowLegs(false);
                if(exitDelay>0)return;
                if(Motion)Motion.SetWalking(true);
            }
            if (!AtRest)
            {
                travel = Mathf.Min(duration, travel + delta);
                float t = duration > 0 ? travel / duration : 1;
                if (gait == Gait.Slide)
                {
                    // Ease-out slide with a small hop, like the reference queue advance.
                    float ease = 1 - (1 - t) * (1 - t) * (1 - t);
                    transform.position = Vector3.Lerp(from, destination, ease) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .05f;
                    yaw = Mathf.MoveTowards(yaw, 0, delta * 300);
                }
                else
                {
                    // Running: near-constant speed (short ease at both ends) with a bob on every step.
                    float ease = Mathf.SmoothStep(0, 1, Mathf.Lerp(t, Mathf.SmoothStep(0, 1, t), .35f));
                    float bob = Mathf.Abs(Mathf.Sin(travel * StepsPerSecond * Mathf.PI)) * StepBob;
                    transform.position = Vector3.Lerp(from, destination, ease) + Vector3.up * bob;
                    // Turn toward the horizontal direction of travel, settle facing the camera on arrival.
                    var dir = destination - from;
                    // Leaving: fully sideways so the legs stride toward the exit edge.
                    float target = Departing ? ExitTarget : Mathf.Abs(dir.x) < .05f ? 0 : -Mathf.Sign(dir.x) * MaxYaw * Mathf.Clamp01(Mathf.Abs(dir.x) / .8f);
                    float settle = t > .8f && !Departing ? (t - .8f) / .2f : 0;
                    yaw = Mathf.Lerp(Mathf.MoveTowards(yaw, target, delta * (Departing ? 900 : 420)), 0, settle);
                    if(Motion && Motion.Animator) Motion.Animator.speed = 1.6f;
                }
                ApplyYaw();
                if(!Motion && gait==Gait.Run)Visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4) * 7);
                bool running = gait == Gait.Run && !AtRest;
                if(Motion)Motion.SetWalking(running);
                ShowLegs(running);
            }
            else
            {
                transform.position = destination;
                yaw = Mathf.MoveTowards(yaw, 0, delta * 420);
                ApplyYaw();
                ShowLegs(false);
                if(!Motion)Visual.localRotation = Quaternion.Euler(firing ? Mathf.Sin(Time.time * 25) * 3 : 0, 0, 0);
                if(Motion){Motion.SetWalking(false); if(Motion.Animator) Motion.Animator.speed = 1;}
                if (Departing) { gameObject.SetActive(false); return; }
            }
            // Landed in the slot after running: squash on touchdown.
            bool runningNow = gait == Gait.Run && !AtRest;
            if (wasRunning && !runningNow && !Departing) Punch(-.3f);
            wasRunning = runningNow;
            ApplyJuice(delta);
            string ammo = DisplayAmount.Units(Shooter.Ammo, Divider).ToString();
            if (AmmoLabel.text != ammo) AmmoLabel.text = ammo;
        }
        void ApplyJuice(float delta)
        {
            if (baseScale == Vector3.zero || delta <= 0) return;
            // Critically-damped-ish spring: snappy overshoot, settles in ~0.3 s.
            float accel = -260f * squash - 16f * squashVelocity;
            squashVelocity += accel * delta; squash += squashVelocity * delta;
            squash = Mathf.Clamp(squash, -.35f, .35f);
            if (Mathf.Abs(squash) < .0005f && Mathf.Abs(squashVelocity) < .01f) { squash = 0; squashVelocity = 0; }
            Visual.localScale = new Vector3(baseScale.x * (1 - squash * .5f), baseScale.y * (1 + squash), baseScale.z * (1 - squash * .5f));
            float shake = 0;
            if (shakeTime > 0) { shakeTime = Mathf.Max(0, shakeTime - delta); shake = Mathf.Sin(shakeTime * 70f) * .06f * (shakeTime / .28f); }
            Visual.localPosition = basePosition + new Vector3(shake, 0, 0);
        }
    }
}