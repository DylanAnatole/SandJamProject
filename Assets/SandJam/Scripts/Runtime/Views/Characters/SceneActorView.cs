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
        bool wasRunning, wasSliding;
        public void Punch(float strength) { squashVelocity += strength * 18f; }
        public void Shake() { shakeTime = .28f; }
        Quaternion baseRotation;
        // Original camera looks down the lanes like a floor: rows nearer the bottom of the screen are nearer the
        // camera, so they look bigger and the lanes fan out. Rows below the front row are pulled toward the camera.
        public static float DepthOriginY = -2.98f, DepthPerUnit = .6f;
        float laneZ; bool laneZKnown;
        public static float DepthOffset(float y) { return Mathf.Min(0, y - DepthOriginY) * DepthPerUnit; }
        float travel, duration, exitDelay;
        int shownAmmo = int.MinValue;
        // Motion recovered from the gameplay video (27 fps): queue cubes slide forward in ~0.25 s without legs;
        // a selected cube sprouts legs and runs to its slot in ~0.5 s, bobbing and turned toward its path;
        // an emptied cube pauses briefly, then runs sideways off the nearest screen edge in ~0.45 s.
        enum Gait { Slide, Run }
        Gait gait;
        float yaw;
        // Run to the stash: the original takes ~0.75 s from the right lane to the leftmost slot (recording 2, 48.6-49.4 s).
        const float SlideSeconds=.32f, RunSpeed=3.5f, MinRun=.45f, MaxRun=.8f, ExitSeconds=.6f, ExitPause=.12f, ExitHop=.25f;
        // The body turns a little toward where it runs (recording 2, ~25 deg); further, the pitched body reads as a
        // lopsided diamond.
        const float MaxYaw=25f, ExitYaw=0f, StepsPerSecond=7f, StepBob=.07f;
        // The rig is pitched toward the camera, so turning happens around the body's own up axis
        // (rotating the root around world Y would roll the tilted body sideways).
        Transform pose; Quaternion poseBase; Vector3 posePosition; bool poseCaptured;
        Renderer legs;
        // Only the legs turn toward where the cube runs (the body keeps facing the camera): after the animator has
        // posed the rig, both thighs are swung around the hips' up axis.
        public static float LegTurnSign = 1;
        // The body itself turns MaxYaw while running, the legs a little further; they also swing down out from under
        // the block so they show below it (recording 2, 48.8-49.1 s: legs ~60% of the block height below it).
        // Where the sand from this box is going (set on every shot) and how it turns and rocks while pouring.
        // Recording 2 (49.35-49.45 s, 58.4-60.35 s, zoomed): a pouring box keeps facing the camera and leans on
        // screen toward the region it fills, more the further sideways that region is (~15-25 deg), rising a little
        // off its pad; it turns back upright between regions.
        Vector3 pourTarget; float roll; Camera viewCamera;
        const float MaxPourTilt = 22f, PourTiltPerDegree = .5f, PourLift = .06f;
        static readonly Vector3 PourPivot = new Vector3(0, .42f, 0); // block centre in Visual space
        public void Pour(Vector3 target) { pourTarget = target; }
        const float RunLegYaw = 30f, ExitLegYaw = 85f, RunLegDrop = 35f, RunLegStretch = 1.6f;
        Transform hips, leftThigh, rightThigh; bool rigSearched; float legYaw, legDrop; Vector3 thighScale;
        void FindRig()
        {
            rigSearched = true;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("Hips")) hips = t;
                else if (t.name.EndsWith("LeftUpLeg")) leftThigh = t;
                else if (t.name.EndsWith("RightUpLeg")) rightThigh = t;
            }
            if (leftThigh) thighScale = leftThigh.localScale;
        }
        void LateUpdate()
        {
            if (!rigSearched) FindRig();
            if (!hips || !leftThigh || !rightThigh || Shooter == null) return;
            bool running = gait == Gait.Run && (!AtRest || (Departing && exitDelay > 0));
            float dx = destination.x - from.x;
            float target = !running ? 0 : Departing ? ExitLegYaw * Mathf.Sign(dx) : Mathf.Abs(dx) < .05f ? 0 : RunLegYaw * Mathf.Sign(dx) * Mathf.Clamp01(Mathf.Abs(dx) / .8f);
            legYaw = Mathf.MoveTowards(legYaw, target, Time.deltaTime * 600f);
            legDrop = Mathf.MoveTowards(legDrop, running && !Departing ? RunLegDrop : 0, Time.deltaTime * 400f);
            if (Mathf.Abs(legYaw) < .01f && Mathf.Abs(legDrop) < .01f)
            {
                if (leftThigh.localScale != thighScale) leftThigh.localScale = rightThigh.localScale = thighScale; // back to rest
                return;
            }
            var frame = pose ? pose : hips;
            var turn = Quaternion.AngleAxis(legYaw * LegTurnSign, frame.up) * Quaternion.AngleAxis(legDrop, frame.right);
            // Longer legs while running (the bones' length axis is their local Y).
            var stretch = new Vector3(1, 1 + (RunLegStretch - 1) * legDrop / RunLegDrop, 1);
            foreach (var thigh in new[] { leftThigh, rightThigh })
            {
                thigh.localScale = Vector3.Scale(thighScale, stretch);
                thigh.position = hips.position + turn * (thigh.position - hips.position);
                thigh.rotation = turn * thigh.rotation;
            }
        }
        void ApplyYaw()
        {
            if (pose)
            {
                pose.localRotation = poseBase * Quaternion.Euler(0, yaw, 0);
                pose.localPosition = posePosition;
                // Pouring: tipped on screen about the block's centre (about its feet it swung off the pad), lifted a little.
                if (roll != 0)
                {
                    var centre = Visual.TransformPoint(PourPivot);
                    if (!viewCamera) viewCamera = SpriteFactory.CameraForLayer(gameObject.layer);
                    // About the camera's own view axis: it looks down at the board, so world Z would tip the box over.
                    var axis = viewCamera ? (centre - viewCamera.transform.position).normalized : Vector3.forward;
                    pose.RotateAround(centre, axis, roll);
                    pose.position += Vector3.up * PourLift * Mathf.Abs(roll) / MaxPourTilt;
                }
                transform.localRotation = Quaternion.identity;
            }
            else transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        // Legs only exist while running; sitting in the lane or on a waiting slot the block rests on its base.
        float ExitTarget { get { return -Mathf.Sign(destination.x - from.x) * ExitYaw; } }
        void ShowLegs(bool visible) { if (legs && legs.enabled != visible) legs.enabled = visible; }

        public const float CubeScale = .9f;
        // Original (recording 2, 48.6-52 s): a cube on its waiting pad reads ~0.96 of its size in the lanes, and on
        // the way there it hops toward the camera a little (with its 25 deg turn it reads ~126 px wide mid-run, as in
        // the original). Full size again in the lanes.
        const float StashSize = .96f, RunBulge = .08f;
        float size = 1, sizeTarget = 1;
        public void Bind(Shooter shooter, Vector3 position, float rowSize = 1)
        {
            Shooter = shooter;
            gameObject.SetActive(true);
            if (baseScale == Vector3.zero) { baseScale = Visual.localScale * CubeScale; // original cubes are ~14% smaller
               baseRotation=Visual.localRotation;basePosition=Visual.localPosition; }
            squash = squashVelocity = shakeTime = 0; wasRunning = false; Visual.localPosition = basePosition; size = sizeTarget = rowSize;
            Visual.localScale = baseScale;
            Visual.localRotation = baseRotation;
            transform.position = destination = from = position;
            if (!laneZKnown) { laneZ = position.z; laneZKnown = true; }
            ApplyDepth();
            if (!poseCaptured && Motion) { pose = Motion.transform; poseBase = pose.localRotation; posePosition = pose.localPosition; poseCaptured = true; }
            if (!legs) { var style = GetComponent<CharacterDepthStyle>(); if (style) legs = style.Skin; }
            yaw = 0; ApplyYaw(); ShowLegs(false);
            Departing = false; travel = duration = 0;
            exitDelay=0; DisplayedFill=1; AmmoLabel.gameObject.SetActive(true);
            if(Motion)Motion.ResetPose();
            ClickCollider.enabled = true;
            shownAmmo = DisplayAmount.Units(shooter.Ammo, Divider); AmmoLabel.text = DisplayAmount.Text(shownAmmo);
        }

        // Queue advance inside a lane: quick slide with a small hop, no legs.
        public void SlideTo(Vector3 position, float rowSize = 1) { sizeTarget = rowSize; Begin(position, Gait.Slide, SlideSeconds); }
        // Lane -> waiting slot: run on legs, duration from distance.
        public void RunTo(Vector3 position)
        {
            float seconds = Mathf.Clamp(Vector3.Distance(transform.position, position) / RunSpeed, MinRun, MaxRun);
            sizeTarget = StashSize;
            Begin(position, Gait.Run, seconds);
        }

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
                    // Running: off at once and easing into the pad (recording 2, 48.5-48.6 s: the front cube clears
                    // its lane spot within 0.1 s, before the cube behind slides up), with a bob on every step.
                    float ease = Mathf.Lerp(t, 1 - (1 - t) * (1 - t), .6f);
                    float bob = Mathf.Abs(Mathf.Sin(travel * StepsPerSecond * Mathf.PI)) * StepBob;
                    // An empty box jumps off its pad before running away (recording 2, 60.7 s).
                    if (Departing) bob += Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 3)) * ExitHop;
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
                // Pouring from its pad: tipped toward the region it fills (positive = counter-clockwise on screen).
                bool pouring = firing && !Departing;
                var toTarget = pourTarget - transform.position;
                float direction = Mathf.Atan2(toTarget.x, Mathf.Max(.01f, toTarget.y)) * Mathf.Rad2Deg; // + = to the right
                yaw = Mathf.MoveTowards(yaw, 0, delta * 420);
                roll = Mathf.MoveTowards(roll, pouring ? -Mathf.Clamp(direction * PourTiltPerDegree, -MaxPourTilt, MaxPourTilt) : 0, delta * 150);
                ApplyYaw();
                ShowLegs(false);
                if(!Motion)Visual.localRotation = Quaternion.Euler(firing ? Mathf.Sin(Time.time * 25) * 3 : 0, 0, 0);
                if(Motion){Motion.SetWalking(false); if(Motion.Animator) Motion.Animator.speed = 1;}
                if (Departing) { gameObject.SetActive(false); return; }
            }
            // Landed in the slot after running: squash on touchdown.
            bool runningNow = gait == Gait.Run && !AtRest;
            if (wasRunning && !runningNow && !Departing) Punch(-.3f);
            // Queue advance: a small settle bounce when the slide ends.
            if (wasSliding && AtRest && !Departing) Punch(-.12f);
            wasSliding = gait == Gait.Slide && !AtRest;
            wasRunning = runningNow;
            ApplyJuice(delta);
            ApplyDepth();
            // Text only when the shown number changes (a new string every frame for every cube adds up on mobile).
            int shown = DisplayAmount.Units(Shooter.Ammo, Divider);
            if (shown != shownAmmo) { shownAmmo = shown; AmmoLabel.text = DisplayAmount.Text(shown); }
        }
        void ApplyDepth()
        {
            if (!laneZKnown) return;
            var p = transform.position; p.z = laneZ + DepthOffset(p.y); transform.position = p;
        }
        void ApplyJuice(float delta)
        {
            if (baseScale == Vector3.zero || delta <= 0) return;
            // Critically-damped-ish spring: snappy overshoot, settles in ~0.3 s.
            float accel = -260f * squash - 16f * squashVelocity;
            squashVelocity += accel * delta; squash += squashVelocity * delta;
            squash = Mathf.Clamp(squash, -.35f, .35f);
            if (Mathf.Abs(squash) < .0005f && Mathf.Abs(squashVelocity) < .01f) { squash = 0; squashVelocity = 0; }
            size = Mathf.MoveTowards(size, sizeTarget, delta * 1.2f); // about the length of the run to the pad
            float hop = gait == Gait.Run && !AtRest && !Departing && duration > 0 ? 1 + RunBulge * Mathf.Sin(Mathf.PI * travel / duration) : 1;
            Visual.localScale = size * hop * new Vector3(baseScale.x * (1 - squash * .5f), baseScale.y * (1 + squash), baseScale.z * (1 - squash * .5f));
            float shake = 0;
            if (shakeTime > 0) { shakeTime = Mathf.Max(0, shakeTime - delta); shake = Mathf.Sin(shakeTime * 70f) * .06f * (shakeTime / .28f); }
            Visual.localPosition = basePosition + new Vector3(shake, 0, 0);
        }
    }
}