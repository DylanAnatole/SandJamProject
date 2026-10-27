using System;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed partial class SandJamSceneController : MonoBehaviour
    {
        [Header("Level and scene references")]
        public TextAsset LevelJson;
        public bool EnableSlotUnlocks;
        public float ViewAspect = .6f;
        public Camera GameCamera;
        public SceneRegionView[] Regions;
        public SceneActorView[] Characters;
        public Transform[] LaneStarts;
        public Transform[] StashSlots;
        public Transform ProjectileRoot;
        public GameObject ProjectilePrefab;
        public Material[] ProjectileMaterials;
        public int[] MaterialColorIds;
        public Font InterfaceFont;
        [Header("Gameplay tuning")]
        public float QueueSpacing = 1.20f;
        [Header("Original sounds")]
        public AudioClip SelectSound;
        public AudioClip VictorySound;
        public SandGame Game { get; private set; }
        public bool BoosterInputBlocked { get; set; }
        void OnDestroy(){if(projectiles!=null)projectiles.Dispose();ReleaseRuntimeAssets();}
        int suppressedInputFrame = -1;
        public void SuppressInputThisFrame() { suppressedInputFrame = Time.frameCount; }
        public bool SwapFrontRows()
        {
            if (paused || Game == null || !Game.SwapFrontRows()) return false;
            Synchronize(0); Play(SelectSound, .25f); return true;
        }
        public bool SelectPriority(Shooter shooter)
        {
            if (paused || Game == null || !Game.SelectPriority(shooter)) return false;
            Synchronize(0); Play(SelectSound, .25f); return true;
        }
        CharacterQueueController characterQueue;
        bool[] regionReported;
        ProjectileManager projectiles;
        SandBoardTextureView boardTexture;
        public SandBoardTextureView Board { get { return boardTexture; } }
        LevelData level;
        AudioSource sound;
        float accumulator;
        bool paused;
        int lastScreenWidth, lastScreenHeight;
        string error;

        // "Play on": buys one more waiting slot after the player ran out of space. Clones the last slot's
        // anchor and pad visuals one step to the right, then re-centres the row so it stays symmetric.
        public bool AddExtraSlot()
        {
            if (Game == null || StashSlots.Length < 2 || !Game.ContinueWithExtraSlot()) return false;
            var parent = StashSlots[0].parent;
            var last = StashSlots[StashSlots.Length - 1];
            float step = last.position.x - StashSlots[StashSlots.Length - 2].position.x;
            foreach (Transform child in parent.Cast<Transform>().ToArray())
            {
                if (Mathf.Abs(child.position.x - last.position.x) > .05f) continue;
                var copy = Instantiate(child.gameObject, parent).transform;
                copy.name = child.name + " (extra)";
                copy.position = child.position + Vector3.right * step;
                if (child == last) StashSlots = StashSlots.Concat(new[] { copy }).ToArray();
            }
            // Keep the row centred: shift every slot visual and the unlock counters half a step left.
            var shift = Vector3.left * step * .5f;
            foreach (Transform child in parent) child.position += shift;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name.Contains("unlock counter") || t.name.StartsWith("Fifth waiting slot")) t.position += shift;
            characterQueue.SetStashSlots(StashSlots);
            Synchronize(0);
            return true;
        }

        public void ConfigureFiveSlots()
        {
            if (StashSlots.Length < 5) throw new InvalidOperationException("Five stash anchors are required.");
            var parent = StashSlots[0].parent;
            foreach (Transform child in parent.Cast<Transform>().ToArray())
            {
                if (child.name == "Slot 6 platform" || child.name == "Slot 6 anchor" || child.name == "Slot index 6") { child.gameObject.SetActive(false); if(Application.isPlaying) Destroy(child.gameObject); else DestroyImmediate(child.gameObject); }
                for (int i=0;i<5;i++)
                    if(child.name=="Slot "+(i+1)+" platform" || child.name=="Slot "+(i+1)+" anchor" || child.name=="Slot index "+(i+1))
                    { var position=child.position; position.x=-3f+i*1.5f; child.position=position; }
            }
            StashSlots=StashSlots.Take(5).ToArray();
            parent.name="Stash - 5 positions";
        }

        void Awake()
        {
            ConfigureFiveSlots();
            MobileDisplay.Configure();
            FitCamera();
            sound = GetComponent<AudioSource>();
            try
            {
                bool dynamicLevel = PendingLevel != null;
                if (dynamicLevel) { LevelJson = PendingLevel; PendingLevel = null; }
                level = LevelDataManager.Load(LevelJson);
                if (dynamicLevel) BuildLevelViews(level);
                if (Regions.Length != level.parts.Length || Characters.Length != level.laneData.Sum(l => l.ColorAmmoDatas.Length) || StashSlots.Length != 5)
                    throw new InvalidOperationException("Scene references do not match the level data.");
                var boardObject=new GameObject("Sand Board - Grid Texture SpriteRenderer");
                boardTexture=boardObject.AddComponent<SandBoardTextureView>();
                boardTexture.Initialize(level,Regions[0].Geometry.transform.parent.parent);
                foreach(var region in Regions) region.AttachBoard(boardTexture);
                var obstacles=Regions[0].Geometry.transform.parent.parent.Find("Obstacle tiles");
                if(obstacles) obstacles.GetComponent<MeshRenderer>().enabled=false;
                projectiles = new ProjectileManager(ProjectileRoot, ProjectilePrefab, ProjectileMaterials, MaterialColorIds);
                Restart();
                // Presentation layers: original-style look and effects (no gameplay rules).
                if(!GetComponent<OriginalLookStyler>()) gameObject.AddComponent<OriginalLookStyler>().Apply(this);
                if(!GetComponent<SandFx>()) gameObject.AddComponent<SandFx>().Bind(this);
                if(!GetComponent<WinZoom>()) gameObject.AddComponent<WinZoom>().Bind(this);
            }
            catch (Exception ex) { error = ex.Message; Debug.LogException(ex); }
        }

        void Start()
        {
            if (error == null) Synchronize(0);
        }

        // Original: every cube pours out in about 2.1 s whatever its size (recording 2, 156-158 s: 20 -> 0 in 2.05 s;
        // recording 3, 83.9-86.1 s: 40 -> 0 in 2.2 s), in small steady ticks.
        const float PourSeconds = 2.1f, PourTick = .02f;
        public void Restart()
        {
            Game = new SandGame(level, StashSlots.Length, r => Regions[Array.IndexOf(level.parts, r.Data)].SettledCount == r.Data.rows.Length, EnableSlotUnlocks);
            foreach (var region in Regions) { region.Divider = level.uiDivider; region.ResetFlow(); }
            regionReported = new bool[Game.Regions.Length];
            characterQueue = new CharacterQueueController(Characters, LaneStarts, StashSlots, QueueSpacing);
            characterQueue.Bind(Game, level);
            projectiles.Reset();
            accumulator = 0; paused = false;
            sound.Stop();
        }

        void Update()
        {
            FitCamera();
            if (error != null || Game == null || BoosterInputBlocked || suppressedInputFrame == Time.frameCount) return;
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectLane(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectLane(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectLane(2);
            if (Input.GetMouseButtonDown(0) && !paused && Game.State == GameState.Playing)
            {
                var tapped = ScreenTap.Cube(GameCamera);
                if (tapped) SelectActor(tapped);
            }
            if (!paused) Advance(Mathf.Min(Time.unscaledDeltaTime, .1f) * (EndgameRush ? 2 : 1));
        }

        // Endgame rush: once every remaining cube is already in the stash there is nothing left to decide,
        // so the rest of the level plays at double speed.
        public bool EndgameRush
        {
            get
            {
                if (Game == null || Game.State != GameState.Playing) return false;
                foreach (var lane in Game.Lanes) if (lane.Count > 0) return false; // plain loop: read every frame, no LINQ allocation
                return true;
            }
        }

        void FitCamera()
        {
            if (lastScreenWidth == Screen.width && lastScreenHeight == Screen.height) return;
            lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            FitView(GameCamera, (float)Screen.width / Mathf.Max(1, Screen.height), ViewAspect);
        }
        // Phones a little wider than the design aspect (e.g. 19.5:9 vs the 0.449 layout) use the whole screen and
        // simply see a bit more of the backdrop at the sides; side bars used to show as pale strips at the edges.
        // Much wider screens (tablets) keep the centred design box, narrower ones are letterboxed.
        public const float MaxExtraWidth = 1.15f;
        public static void FitView(Camera camera, float aspect, float target)
        {
            if (aspect > target && aspect <= target * MaxExtraWidth) { camera.rect = new Rect(0, 0, 1, 1); camera.aspect = aspect; return; }
            if (aspect > target) { float width = target * MaxExtraWidth / aspect; camera.rect = new Rect((1 - width) / 2, 0, width, 1); camera.aspect = target * MaxExtraWidth; return; }
            float height = aspect / target; camera.rect = new Rect(0, (1 - height) / 2, 1, height);
            camera.aspect = target;
        }

        public bool SelectActor(SceneActorView actor)
        {
            if (actor == null || actor.Shooter == null || paused || Game.State != GameState.Playing) return false;
            var lane = Game.Lanes[actor.SourceLane];
            if (actor.Shooter.Partner != null && lane.Count > 0 && lane.Peek() == actor.Shooter.Partner)
                return SelectLane(actor.SourceLane);
            if (lane.Count == 0 || lane.Peek() != actor.Shooter)
            {
                actor.Shake(); return false;
            }
            return SelectLane(actor.SourceLane);
        }

        public bool SelectLane(int lane)
        {
            if (paused || Game.State != GameState.Playing) return false;
            int pickedColor = lane >= 0 && lane < Game.Lanes.Length && Game.Lanes[lane].Count > 0 ? Game.Lanes[lane].Peek().Color : 0;
            if (!Game.SelectLane(lane))
            {
                // Original: a cube that cannot go only shakes; no message on screen.
                var first = lane >= 0 && lane < Game.Lanes.Length && Game.Lanes[lane].Count > 0 ? Game.Lanes[lane].Peek() : null;
                if (first != null) characterQueue.ViewFor(first).Shake();
                return false;
            }
            Play(SelectSound, .25f);
            GameEvents.RaiseCharacterSelected(pickedColor);
            Synchronize(0);
            return true;
        }

        // In the campaign the AudioManager plays sounds from game events; standalone test scenes play locally.
        void Play(AudioClip clip, float volume) { if (clip != null && !GameServices.Ready) sound.PlayOneShot(clip, volume); }
        bool CanShoot(int slot) { return characterQueue.CanShoot(Game, slot); }
        Func<int, bool> canShootCheck;

        SceneActorView[] shooters;
        public void Advance(float delta)
        {
            Synchronize(delta);
            int revision = Game.Revision;
            if (Game.State == GameState.Playing)
            {
                accumulator += delta;
                while (accumulator >= PourTick)
                {
                    accumulator -= PourTick;
                    // Allocation-free per tick (runs ~50 times a second): reused arrays and plain loops.
                    if (shooters == null || shooters.Length != Game.Slots.Length) shooters = new SceneActorView[Game.Slots.Length];
                    for (int s = 0; s < shooters.Length; s++) shooters[s] = Game.Slots[s] == null ? null : characterQueue.ViewFor(Game.Slots[s]);
                    var previousState = Game.State;
                    foreach (var shot in Game.Tick(PourTick / PourSeconds, canShootCheck ?? (canShootCheck = CanShoot)))
                    {
                        if(Game.Regions[shot.Region].Data.ColorType!=shot.Color)throw new InvalidOperationException("Shot colour does not match receiving region");
                        projectiles.Spawn(shooters[shot.Slot], Regions[shot.Region], shot.Color, shot.Slot);
                        if (shooters[shot.Slot]) shooters[shot.Slot].Pour(Regions[shot.Region].Target.position);
                        GameEvents.RaiseSandPoured(shot.Color, shot.Amount, Mathf.Max(1, level.uiDivider));
                    }
                    if (previousState != GameState.Won && Game.State == GameState.Won) Play(VictorySound, .45f);
                    for (int r = 0; r < Game.Regions.Length; r++)
                        if (Game.Regions[r].Remaining == 0 && !regionReported[r]) { regionReported[r] = true; GameEvents.RaiseRegionCompleted(r, Game.Regions[r].Data.ColorType); }
                }
            }
            // Shots, picks and merges bump the revision; without them the views already match (~40 us saved a frame).
            if (Game.Revision != revision) Synchronize(0);
            foreach (var region in Regions) region.BeginAdvance(delta);
            Unity.Jobs.JobHandle.ScheduleBatchedJobs();
            foreach (var region in Regions) region.FinishAdvance();
            projectiles.Advance(delta);
        }

        void Synchronize(float delta)
        {
            characterQueue.Synchronize(Game, delta);
            foreach (var view in Regions) view.Apply(Game.Regions[view.PartIndex]);
        }


    }
}


