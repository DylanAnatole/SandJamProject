using System;
using System.Collections.Generic;

namespace SandJamTest
{
    // Export flags mark both endpoints; same-lane pairs are consecutive,
    // cross-lane pairs occupy the same row in adjacent lanes.
    public static class ChainPairing
    {
        public struct Pair { public int LaneA, OrderA, LaneB, OrderB; }
        public static List<Pair> Resolve(LevelData data)
        {
            var result = new List<Pair>();
            var used = new HashSet<string>();
            for (int lane = 0; lane < data.laneData.Length; lane++)
                for (int order = 0; order < data.laneData[lane].ColorAmmoDatas.Length; order++)
                {
                    var a = data.laneData[lane].ColorAmmoDatas[order];
                    string key = lane + ":" + order;
                    if (!a.IsChain || used.Contains(key)) continue;
                    int nextLane = a.IsChainSameLane ? lane : lane + 1;
                    int nextOrder = a.IsChainSameLane ? order + 1 : order;
                    string other = nextLane + ":" + nextOrder;
                    if (nextLane >= data.laneData.Length || nextOrder >= data.laneData[nextLane].ColorAmmoDatas.Length)
                        throw new ArgumentException("Chain partner missing at " + key);
                    var b = data.laneData[nextLane].ColorAmmoDatas[nextOrder];
                    if (!b.IsChain || b.IsChainSameLane != a.IsChainSameLane || used.Contains(other))
                        throw new ArgumentException("Invalid chain pair at " + key);
                    used.Add(key); used.Add(other);
                    result.Add(new Pair { LaneA = lane, OrderA = order, LaneB = nextLane, OrderB = nextOrder });
                }
            return result;
        }
    }
}
