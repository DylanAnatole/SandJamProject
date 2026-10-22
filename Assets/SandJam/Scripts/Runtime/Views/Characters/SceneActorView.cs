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
        public Vector3 Destination { get { return destination; } }
        // Juice: a damped spring for squash & stretch (+ = stretch up) and a short sideways shake.
        float squash, squashVelocity, shakeTime;
        bool wasRunning, wasSliding;
        public void Punch(float strength) { squashVelocity += strength * 18f; }
        // A cube that cannot go: a light left-right wobble, fading out (ShakeSwings full swings in ShakeSeconds).
        const float ShakeSeconds = .34f, ShakeSwings = 2, ShakeAmount = .035f;
        public void Shake() { shakeTime = ShakeSeconds; HapticsManager.Refused(); }
        Quaternion baseRotation;
        // Original camera looks down the lanes like a floor: rows nearer the bottom of the screen are nearer the
        // camera, so they look bigger and the lanes fan out. Rows below the front row are pulled toward the camera.
        public static float DepthOriginY = -2.98f, DepthPerUnit = .6f;
        float laneZ; bool laneZKnown;
        public static float DepthOffset(float y) { return Mathf.Min(0, y - DepthOriginY) * DepthPerUnit; }
        float travel, duration, exitDelay, leaveAge;
        // An emptied box turns white as it leaves (recording 2, 158.38-158.55 s), then runs off in its colour.
        const float EmptyWhiteSeconds=.18f;
        public float EmptyWhite { get { return Departing ? Mathf.Clamp01(1-leaveAge/EmptyWhiteSeconds) : 0; } }
        int shownAmmo = int.MinValue;
        // Motion recovered from the gameplay video (27 fps): queue cubes slide forward in ~0.25 s without legs;
        // a selected cube sprouts legs and runs to its slot in ~0.5 s, bobbing and turned toward its path;
        // an emptied cube pauses briefly, then runs sideways off the nearest screen edge in ~0.45 s.
        enum Gait { Slide, Run }
        Gait gait;
        float yaw;
        // Run to the stash: the original takes ~0.75 s from the right lane to the leftmost slot (recording 2, 48.6-49.4 s).
        const float SlideSeconds=.38f, RunSpeed=3.5f, MinRun=.45f, MaxRun=.8f, ExitSeconds=.6f, ExitPause=.12f, ExitHop=.25f;
        // The box faces where it runs (its number is on its back): straight up the screen it barely turns (recording 2,
        // 155.6-155.9 s), running sideways it turns ~65 deg so its side shows and the legs stride that way (48.6-49.2 s).
        // Heading = angle of the run from straight up times RunTurnGain (the original turns a little past its path, its
        // number almost at the box's right edge on a run to the left), capped at MaxYaw. Turned about the true vertical the pitched box
        // also leans over; RunUpright rolls it back about the view axis (per degree of turn) so it stays upright.
        const float RunUpright=-.5f;
        const float MaxYaw=80f, RunTurnGain=1.25f;
        // An emptied box leaving turns fully side-on toward its exit and strides off in profile (recording 2,
        // 158.55-158.9 s), like a run straight sideways.
        const float ExitYaw=90f, ExitBehind=.5f;
        // Walk clip (0.875 s, two steps) played at RunAnimSpeed: ~0.23 s a step, as the original's legs open and close
        // ~4-5 times a second (recording 2, 48.6-49.2 s). The box itself barely bobs there (under 2% of its height).
        const float WalkClipSeconds=.875f, RunAnimSpeed=1.9f, StepsPerSecond=2*RunAnimSpeed/WalkClipSeconds, StepBob=.012f;
        // The rig is pitched toward the camera (its own up axis points nearly at the camera, so turning about it only
        // spins the box flat on screen): running and pouring both turn about the true vertical through its centre.
        Transform pose; Quaternion poseBase; Vector3 posePosition; bool poseCaptured;
        Vector3 labelRest; Quaternion labelRestRotation; bool labelMoved;
        const float LabelSlide = .14f, LabelTurnShare = .45f;
        Renderer legs;
        // Only the legs turn toward where the cube runs (the body keeps facing the camera): after the animator has
        // posed the rig, both thighs are swung around the hips' up axis.
        public static float LegTurnSign = 1;
        // The body itself turns MaxYaw while running, the legs a little further; they also swing down out from under
        // the block so they show below it (recording 2, 48.8-49.1 s: legs ~60% of the block height below it).
        // Where the sand from this box is going (set on every shot) and how it turns and rocks while pouring.
        // Recording 2 (58.30-59.26 s, every 0.04 s, zoomed): a pouring box turns ~30 deg about the vertical through
        // its centre, its front toward the side away from the region it fills (its other side and top show), and
        // holds that turn while it pours, tipping only very slightly; it turns back when it stops.
        Vector3 pourTarget; float roll, aimYaw; Camera viewCamera;
        const float PourYaw = 30f, PourTilt = 3f;
        static readonly Vector3 PourPivot = new Vector3(0, .42f, 0); // block centre in Visual space
        public void Pour(Vector3 target) { pourTarget = target; }
        // Recording 2 (155.6-155.9 s and 48.6-49.3 s, zoomed): both legs come out of the middle of the block's base,
        // thick, ~0.6 of its height, with bent knees; turned toward the run they hid behind each other.
        const float RunLegYaw = 0f, RunLegDrop = 35f, RunLegStretch = 1.45f;
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
            float target = !running || Mathf.Abs(dx) < .05f ? 0 : RunLegYaw * Mathf.Sign(dx) * Mathf.Clamp01(Mathf.Abs(dx) / .8f);
            legYaw = Mathf.MoveTowards(legYaw, target, Time.deltaTime * 600f);
            legDrop = Mathf.MoveTowards(legDrop, running ? RunLegDrop : 0, Time.deltaTime * 400f);
            if (Mathf.Abs(legYaw) < .01f && Mathf.Abs(legDrop) < .01f)
            {
                if (leftThigh.localScale != thighScale) leftThigh.localScale = rightThigh.localScale = thighScale; // back to rest
                return;
            }
            var frame = pose ? pose : hips;
            // The legs swing down about the screen's horizontal (the rig's right at rest): about the rig's own right,
            // once the box had turned toward its run they swung out behind it instead of below it.
            var dropAxis = new Vector3(Mathf.Sign(frame.right.x), 0, 0);
            var turn = Quaternion.AngleAxis(legYaw * LegTurnSign, Vector3.up) * Quaternion.AngleAxis(legDrop, dropAxis);
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
                pose.localRotation = poseBase;
                pose.localPosition = posePosition;
                var label = AmmoLabel ? AmmoLabel.transform : null;
                bool turned = yaw != 0 || aimYaw != 0 || roll != 0;
                if (label && !turned)
                {
                    if (labelMoved) { label.localPosition = labelRest; label.localRotation = labelRestRotation; labelMoved = false; }
                    else { labelRest = label.localPosition; labelRestRotation = label.localRotation; }
                }
                // Running and pouring turn about the true vertical through the block's centre (about its feet it swung
                // off the pad), then pouring adds the slight tip about the camera's view axis (it looks down at the
                // board, so world Z would tip the box over).
                if (turned)
                {
                    var centre = Visual.TransformPoint(PourPivot);
                    if (!viewCamera) viewCamera = SpriteFactory.CameraForLayer(gameObject.layer);
                    var axis = viewCamera ? (centre - viewCamera.transform.position).normalized : Vector3.forward;
                    pose.RotateAround(centre, Vector3.up, yaw + aimYaw);
                    pose.RotateAround(centre, axis, roll + yaw * RunUpright);
                    // The number is printed on the box's back face, but it is not part of the rig: as the box turns it
                    // slides toward the face's far edge (recording 2, 48.9 s: ~3/4 across on a run to the left) and
                    // turns part of the way with it.
                    if (label)
                    {
                        float turn = yaw + aimYaw;
                        label.localPosition = labelRest; label.localRotation = labelRestRotation;
                        label.position += Vector3.right * (-Mathf.Sin(turn * Mathf.Deg2Rad) * LabelSlide * size);
                        label.rotation = Quaternion.AngleAxis(turn * LabelTurnShare, Vector3.up) * label.rotation;
                        labelMoved = true;
                    }
                }
                transform.localRotation = Quaternion.identity;
            }
            else transform.localRotation = Quaternion.Euler(0, yaw, 0);
        }
        // Legs only exist while running; sitting in the lane or on a waiting slot the block rests on its base.
        float ExitTarget { get { return Mathf.Sign(destination.x - from.x) * ExitYaw; } }
        void ShowLegs(bool visible) { if (legs && legs.enabled != visible) legs.enabled = visible; }
        // After running onto its pad the box still stands on its legs a moment (recording 2, 156.0-156.15 s).
        const float LegLinger = .15f; float landedAge;

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
            gait = kind; travel = 0; duration = seconds; landedAge = 0;
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
            exitDelay=ExitPause; leaveAge=0;
            if(Motion)Motion.SetWalking(false);
            AmmoLabel.gameObject.SetActive(false);
            ClickCollider.enabled = false;
        }

        public void Advance(float delta, bool firing)
        {
            if (!gameObject.activeSelf || Shooter == null) return;
            if (Departing) leaveAge += delta;
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
                    // Queue advance (recording 2, 48.63-49.0 s): slow start, quickest midway, slow finish, flat (no hop).
                    float ease = t * t * (3 - 2 * t);
                    transform.position = Vector3.Lerp(from, destination, ease);
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
                    float target = Departing ? ExitTarget : Mathf.Clamp(RunTurnGain * Mathf.Atan2(dir.x, Mathf.Max(.01f, dir.y)) * Mathf.Rad2Deg, -MaxYaw, MaxYaw);
                    float settle = t > .8f && !Departing ? (t - .8f) / .2f : 0;
                    yaw = Mathf.Lerp(Mathf.MoveTowards(yaw, target, delta * (Departing ? 900 : 420)), 0, settle);
                    if(Motion && Motion.Animator) Motion.Animator.speed = RunAnimSpeed;
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
                float side = pourTarget.x >= transform.position.x ? 1 : -1; // region to the right / left
                yaw = Mathf.MoveTowards(yaw, 0, delta * 420);
                aimYaw = Mathf.MoveTowards(aimYaw, pouring ? side * PourYaw : 0, delta * 300);
                roll = Mathf.MoveTowards(roll, pouring ? -side * PourTilt : 0, delta * 60);
                ApplyYaw();
                landedAge += delta;
                ShowLegs(gait == Gait.Run && !Departing && landedAge < LegLinger);
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
            // Leaving, the box runs along behind the boxes still on their pads (recording 2, 158.6-158.9 s).
            var p = transform.position; p.z = laneZ + DepthOffset(p.y) + (Departing && exitDelay <= 0 ? ExitBehind : 0); transform.position = p;
        }
        const float SpringStep = 1f / 120f;
        void ApplyJuice(float delta)
        {
            if (baseScale == Vector3.zero || delta <= 0) return;
            // Critically-damped-ish spring: snappy overshoot, settles in ~0.3 s. Integrated in steps of at most
            // SpringStep: in one long frame (a hitch, a slow phone) a single step overshot and flipped the box between
            // squashed and stretched every frame.
            for (float left = delta; left > 0; left -= SpringStep)
            {
                float step = Mathf.Min(left, SpringStep);
                squashVelocity += (-260f * squash - 16f * squashVelocity) * step; squash += squashVelocity * step;
            }
            squash = Mathf.Clamp(squash, -.35f, .35f);
            if (Mathf.Abs(squash) < .0005f && Mathf.Abs(squashVelocity) < .01f) { squash = 0; squashVelocity = 0; }
            size = Mathf.MoveTowards(size, sizeTarget, delta * 1.2f); // about the length of the run to the pad
            float hop = gait == Gait.Run && !AtRest && !Departing && duration > 0 ? 1 + RunBulge * Mathf.Sin(Mathf.PI * travel / duration) : 1;
            Visual.localScale = size * hop * new Vector3(baseScale.x * (1 - squash * .5f), baseScale.y * (1 + squash), baseScale.z * (1 - squash * .5f));
            float shake = 0;
            if (shakeTime > 0) { shakeTime = Mathf.Max(0, shakeTime - delta); float left = shakeTime / ShakeSeconds; shake = Mathf.Sin((1 - left) * ShakeSwings * 2 * Mathf.PI) * ShakeAmount * left; }
            Visual.localPosition = basePosition + new Vector3(shake, 0, 0);
        }
    }
}