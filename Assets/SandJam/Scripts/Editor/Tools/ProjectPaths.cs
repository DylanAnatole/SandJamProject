namespace SandJamTest.Editor
{
    // Where the editor tools read and write project files (Assets / Prefabs / ScriptableObjects / Scripts layout).
    // Runtime code loads by Resources path, which is the same in every Resources folder below.
    static class ProjectPaths
    {
        public const string Root = "Assets/SandJam";
        public const string Assets = Root + "/Assets";
        public const string Resources = Assets + "/Resources";                         // art, audio, level JSON, shaders
        public const string PrefabResources = Root + "/Prefabs/Resources";             // prefabs loaded by path
        public const string ObjectResources = Root + "/ScriptableObjects/Resources";   // ScriptableObjects loaded by path
        public const string Generated = Assets + "/Generated";
        public const string Scenes = Assets + "/Scenes";
        public const string GameScene = Scenes + "/SandJamGame.unity";
        public const string Levels = Assets + "/Levels";
        public const string Catalog = ObjectResources + "/LevelPack/Catalog.asset";
    }
}
