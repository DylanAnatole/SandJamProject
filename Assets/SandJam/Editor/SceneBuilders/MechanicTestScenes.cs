using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SandJamTest.Scene3D;

namespace SandJamTest.Editor
{
    // Creates the two mechanic sandbox scenes using real original levels that the solver can win.
    public static class MechanicTestScenes
    {
        const string Folder = "Assets/SandJam/Scenes/Tests";

        [MenuItem("Sand Jam/Test scenes/Create mechanic test scenes")]
        public static void CreateAll()
        {
            Create("MechanicTest_ChainHalf", "Test: Chained + Half cubes", new[]
            {
                E("Vertical chain", "112x84_Accessories_2", "Two chained cubes in one lane are picked together and land in two neighbouring slots. Needs 2 free slots side by side."),
                E("Vertical chain (2)", "112x84_Accessories_5", "Same as above: tapping either cube sends the whole pair."),
                E("Horizontal chain", "112x84_AnimalPortrait_11", "The pair sits in neighbouring lanes at the same row. It can be picked only when both are at the front; they move to the stash together."),
                E("Horizontal chain (2)", "112x84_AnimalPortrait_14", "Horizontal pair combined with frozen cubes."),
                E("Half cubes", "112x84_EventFigures_1", "Wedge cubes are halves. In the stash they SLEEP (zzz) and do not pour. When another half of the SAME COLOR arrives, the two merge into one full cube and pour."),
                E("Half cubes (2)", "112x84_EventFigures_11", "Halves of different colors do not merge; too many sleeping halves fill the stash."),
            });
            Create("MechanicTest_FreezeLock", "Test: Frozen + Padlock", new[]
            {
                E("Frozen", "112x84_AnimalPortrait_1", "Frozen cubes show a number. Each other cube sent to the stash lowers it by 1 (a chained pair by 2). At 0 the ice breaks and the cube can be picked."),
                E("Frozen (2)", "112x84_AnimalPortrait_10", "Several frozen cubes; pick other cubes first to break the ice."),
                E("Padlock", "112x84_Halloween_1", "Striped areas carry a padlock and a number. Only KEY cubes of the same color pour into the padlock. At 0 the padlock breaks and normal cubes can paint the area."),
                E("Padlock (2)", "112x84_ModernPosters_1", "Padlock in another picture; key cubes never paint normal areas."),
            });
            AssetDatabase.SaveAssets();
            Debug.Log("Mechanic test scenes created in " + Folder);
        }

        static MechanicTestBootstrap.Entry E(string title, string id, string rule) { return new MechanicTestBootstrap.Entry { Title = title, LevelId = id, Rule = rule }; }

        static void Create(string name, string title, MechanicTestBootstrap.Entry[] levels)
        {
            Directory.CreateDirectory(Folder);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Mechanic Test").AddComponent<MechanicTestBootstrap>();
            bootstrap.SceneTitle = title; bootstrap.Levels = levels;
            EditorSceneManager.SaveScene(scene, Folder + "/" + name + ".unity");
        }
    }
}
