using System;
using System.Linq;
using UnityEngine;
namespace SandJamTest
{
    [Serializable] public sealed class LevelEntry
    {
        public int Number;
        public string SourceId, SceneName, PrefabResource;
        public TextAsset Json, TestReplay;
    }
    [CreateAssetMenu(menuName="Sand Jam/Level catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public string ProgressId="original-first-three-v1";
        public LevelEntry[] Levels;
        public LevelEntry Get(int index)
        {
            if(Levels==null || index<0 || index>=Levels.Length)throw new ArgumentOutOfRangeException("index");
            return Levels[index];
        }
        public void Validate()
        {
            if(Levels==null || Levels.Length==0 || string.IsNullOrEmpty(ProgressId))throw new ArgumentException("Empty level catalog");
            if(Levels.Any(l=>l==null || l.Number<1 || !l.Json || string.IsNullOrEmpty(l.SceneName) || string.IsNullOrEmpty(l.SourceId)))throw new ArgumentException("Incomplete level entry");
            if(Levels.Select(l=>l.Number).Distinct().Count()!=Levels.Length || Levels.Select(l=>l.SceneName).Distinct().Count()!=Levels.Length)throw new ArgumentException("Duplicate level number or scene");
        }
    }
    [Serializable] public sealed class LevelReplay { public int[] lanes; }
}
