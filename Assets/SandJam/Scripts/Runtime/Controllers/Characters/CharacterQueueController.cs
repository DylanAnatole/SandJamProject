using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Connects model shooters to actor views; owns queue and stash movement.
    public sealed class CharacterQueueController
    {
        readonly Dictionary<Shooter, SceneActorView> actors = new Dictionary<Shooter, SceneActorView>();
        readonly List<ChainLinkView> links = new List<ChainLinkView>();
        readonly SceneActorView[] Characters;
        readonly Transform[] LaneStarts;
        Transform[] StashSlots;
        // Used when the player buys an extra waiting slot mid-level.
        public void SetStashSlots(Transform[] slots) { StashSlots = slots; }
        readonly float QueueSpacing;
        public CharacterQueueController(SceneActorView[] characters, Transform[] lanes, Transform[] slots, float spacing)
        { Characters = characters; LaneStarts = lanes; StashSlots = slots; QueueSpacing = spacing; }
        public SceneActorView ViewFor(Shooter shooter) { return actors[shooter]; }
        public bool CanShoot(SandGame game, int slot)
        { var shooter = game.Slots[slot]; return shooter != null && actors[shooter].AtRest; }
        public void Bind(SandGame Game, LevelData level)
        {
            actors.Clear();
            links.Clear();
            for (int lane = 0; lane < Game.Lanes.Length; lane++)
            {
                var queue = Game.Lanes[lane].ToArray();
                for (int i = 0; i < queue.Length; i++)
                {
                    var actor = Characters.Single(a => a.SourceLane == lane && a.SourceOrder == i);
                    actor.Divider = level.uiDivider; actor.Bind(queue[i], LaneStarts[lane].position + Vector3.down * QueueSpacing * i);
                    actors.Add(queue[i], actor);
                }
            }
            foreach (var actor in Characters)
            {
                foreach (var existing in actor.GetComponentsInChildren<ChainLinkView>(true))
                { existing.gameObject.SetActive(false); UnityEngine.Object.Destroy(existing.gameObject); }
            }
            var linked = new HashSet<Shooter>();
            foreach (var pair in actors)
                if (pair.Key.Partner != null && linked.Add(pair.Key))
                {
                    linked.Add(pair.Key.Partner);
                    // Separate object avoids sharing actor lifecycle or leaving strap children on restart.
                    var linkRoot = new GameObject("Linked pair");
                    linkRoot.transform.SetParent(pair.Value.transform, false);
                    var link = linkRoot.AddComponent<ChainLinkView>();
                    link.First = pair.Value; link.Second = actors[pair.Key.Partner];
                    links.Add(link);
                }
        }
        public void Synchronize(SandGame Game, float delta)
        {
            foreach (var link in links)
                link.SetInQueue(Game.Lanes[link.First.SourceLane].Contains(link.First.Shooter) &&
                    Game.Lanes[link.Second.SourceLane].Contains(link.Second.Shooter));
            for (int lane = 0; lane < Game.Lanes.Length; lane++)
            {
                int order = 0;
                foreach (var shooter in Game.Lanes[lane]) actors[shooter].SlideTo(LaneStarts[lane].position + Vector3.down * QueueSpacing * order++);
            }
            for (int slot = 0; slot < Game.Slots.Length; slot++)
                if (Game.Slots[slot] != null) actors[Game.Slots[slot]].RunTo(StashSlots[slot].position);
            foreach (var pair in actors)
            {
                if (pair.Key.MergedInto != null)
                {
                    pair.Value.MergeInto(actors[pair.Key.MergedInto]);
                    pair.Value.Advance(delta, false);
                    continue;
                }
                if (pair.Key.Ammo == 0)
                {
                    bool wasLeaving = pair.Value.Departing || !pair.Value.gameObject.activeSelf;
                    pair.Value.Leave();
                    if (!wasLeaving && pair.Value.Departing) GameEvents.RaiseCharacterEmptied(pair.Key.Color);
                }
                pair.Value.Advance(delta, Game.Slots.Contains(pair.Key) && Game.Target(pair.Key) >= 0);
            }
        }
    }
}
