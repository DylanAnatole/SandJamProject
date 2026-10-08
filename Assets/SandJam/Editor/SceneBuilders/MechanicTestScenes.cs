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
            Create("MechanicTest_ChainHalf", "Test: Khối nối + Nửa khối", new[]
            {
                E("Nối dọc", "112x84_Accessories_2", "Hai khối có dây nối liền nhau trong cùng hàng được chọn cùng lúc và lên hai ô chờ cạnh nhau. Cần đủ 2 ô trống liền kề."),
                E("Nối dọc (2)", "112x84_Accessories_5", "Như trên: chọn khối trên hoặc khối dưới đều kéo cả cặp lên."),
                E("Nối ngang", "112x84_AnimalPortrait_11", "Cặp nối nằm ở hai hàng cạnh nhau cùng vị trí. Chỉ chọn được khi cả hai cùng ở đầu hàng; lên ô chờ cùng nhau."),
                E("Nối ngang (2)", "112x84_AnimalPortrait_14", "Cặp nối ngang kết hợp khối đóng băng."),
                E("Nửa khối", "112x84_EventFigures_1", "Khối hình nêm là một nửa. Lên ô chờ thì NGỦ (zzz) và không bắn. Khi một nửa CÙNG MÀU khác lên ô chờ, hai nửa ghép thành một khối đầy (cộng lượng cát) rồi bắn."),
                E("Nửa khối (2)", "112x84_EventFigures_11", "Nửa khác màu không ghép được; để quá nhiều nửa ngủ sẽ hết chỗ."),
            });
            Create("MechanicTest_FreezeLock", "Test: Đóng băng + Ổ khóa", new[]
            {
                E("Đóng băng", "112x84_AnimalPortrait_1", "Khối bọc băng hiện số. Mỗi lần đưa khối khác lên ô chờ, số giảm 1 (đưa cặp nối lên giảm 2). Về 0 băng vỡ và khối chọn được."),
                E("Đóng băng (2)", "112x84_AnimalPortrait_10", "Nhiều khối đóng băng; ưu tiên chọn khối khác để phá băng."),
                E("Ổ khóa", "112x84_Halloween_1", "Vùng sọc có ổ khóa và số. Chỉ khối có CHÌA KHÓA (cùng màu) mới bắn vào ổ khóa. Số về 0 thì ổ khóa vỡ, vùng mở và khối thường tô vào."),
                E("Ổ khóa (2)", "112x84_ModernPosters_1", "Ổ khóa trong tranh khác; khối chìa khóa không tô vùng thường."),
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
