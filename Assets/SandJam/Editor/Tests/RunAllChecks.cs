using System;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // One entry point for every editor check that currently passes. Run it before and after an upgrade:
    // Sand Jam > Tests > Run all checks (or -executeMethod SandJamTest.Editor.RunAllChecks.Run in batch mode).
    public static class RunAllChecks
    {
        [MenuItem("Sand Jam/Tests/Run all checks")]
        public static void Run()
        {
            int passed = 0, failed = 0;
            void Check(string name, Action run)
            {
                try { run(); passed++; Debug.Log("[Checks] PASS " + name); }
                catch (Exception e) { failed++; Debug.LogError("[Checks] FAIL " + name + ": " + e.Message); }
            }
            Check("Sand simulation (Burst)", BurstSandChecks.Run);
            Check("Sand flow", SandFlowChecks.Run);
            Check("Boosters", BoosterChecks.Run);
            Check("Chained cubes", ChainMechanicChecks.Run);
            Check("Frozen cubes", FreezeMechanicChecks.Run);
            Check("Level 201 replay", Video201Checks.Run);
            Check("Level pack", () => LevelPackChecks.Run(Resources.Load<LevelCatalog>("LevelPack/Catalog")));
            Check("Shaders and materials", ShaderMaterialChecks.Validate);
            Debug.Log("[Checks] " + passed + " passed, " + failed + " failed");
            if (failed > 0 && Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
