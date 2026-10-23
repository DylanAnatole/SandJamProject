using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SandJamTest.Editor
{
    // Filter in the editor: the recovered mesh has CPU read access disabled in players.
    public static class CharacterVesselMeshBuilder
    {
        const string Path="Assets/SandJam/Resources/CharacterAnimation/CharacterLegs.asset";
        [MenuItem("Sand Jam/Prepare vessel legs")]
        public static void Generate()
        {
            var prefab=Resources.Load<GameObject>("Original/CharacterVisual");
            var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var source=skin.sharedMesh;var result=UnityEngine.Object.Instantiate(source);
            result.name="Recovered animated legs";
            var legBones=skin.bones.Select(b=>b.name.Contains("Leg") || b.name.Contains("Foot") || b.name.Contains("Toe")).ToArray();
            var keep=source.boneWeights.Select(w=>(legBones[w.boneIndex0]?w.weight0:0)+(legBones[w.boneIndex1]?w.weight1:0)+(legBones[w.boneIndex2]?w.weight2:0)+(legBones[w.boneIndex3]?w.weight3:0)>.75f).ToArray();
            int retainedCount=0;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var indices=source.GetTriangles(sub);var retained=new List<int>();
                for(int i=0;i<indices.Length;i+=3)
                    if(keep[indices[i]] && keep[indices[i+1]] && keep[indices[i+2]])
                        retained.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});
                result.SetTriangles(retained,sub,false);retainedCount+=retained.Count;
            }
            if(retainedCount==0 || retainedCount>=source.triangles.Length)
                throw new InvalidOperationException("Leg isolation did not remove the character body");
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Path);
            if(existing){EditorUtility.CopySerialized(result,existing);UnityEngine.Object.DestroyImmediate(result);EditorUtility.SetDirty(existing);}
            else AssetDatabase.CreateAsset(result,Path);
            AssetDatabase.SaveAssets();
        }
    }
}
