using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SandJamTest.Scene3D;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SandJamTest.Editor
{
    public static class BuildGameplayScene
    {
        const string Folder="Assets/SandJam/Generated/Scene3D";
        const string ScenePath="Assets/SandJam/Scenes/SandJamGameplay3D.unity";
        const float Cell=.075f;
        static Font font;
        static Material textBacking, white, dark;
        static readonly int[] Colors={1,2,3,4,7};

        static Color Hex(string s){Color c;ColorUtility.TryParseHtmlString("#"+s,out c);return c;}
        static T Save<T>(T value,string name) where T:UnityEngine.Object
        {
            string path=Folder+"/"+name;
            var previous=AssetDatabase.LoadAssetAtPath<T>(path);
            if(previous){EditorUtility.CopySerialized(value,previous);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(previous);return previous;}
            AssetDatabase.CreateAsset(value,path);return value;
        }
        static Material Mat(string name,Color color,string shader="Unlit/Color",Texture texture=null)
        {
            var found=Shader.Find(shader); if(!found) throw new Exception("Missing shader: "+shader);
            var mat=new Material(found){name=name};if(mat.HasProperty("_Color"))mat.color=color;if(texture)mat.mainTexture=texture;
            return Save(mat,name+".mat");
        }
        static Transform Group(string name,Transform parent=null)
        {var g=new GameObject(name);if(parent)g.transform.SetParent(parent,false);return g.transform;}
        static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);
            g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }
        static TextMesh Text(string name,string value,Transform parent,Vector3 position,float size,Color color)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;
            var t=g.AddComponent<TextMesh>();t.font=font;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.text=value;t.color=color;
            g.GetComponent<MeshRenderer>().sharedMaterial=font.material;return t;
        }

        static Mesh BakeCharacter()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SandJam/Resources/Original/CharacterVisual.prefab");
            if(!prefab)throw new Exception("Original character copy is missing.");
            var instance=UnityEngine.Object.Instantiate(prefab);
            try
            {
                instance.transform.position=Vector3.zero;
                var skin=instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if(!skin || skin.bones.Length!=41 || skin.bones.Any(b=>!b))throw new Exception("Original rig is incomplete.");
                var mesh=new Mesh();skin.BakeMesh(mesh);
                var vertices=mesh.vertices; var matrix=skin.transform.localToWorldMatrix;
                for(int i=0;i<vertices.Length;i++) vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
                var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(v);
                var pivot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-pivot)/bounds.size.y;
                mesh.vertices=vertices;
                if(matrix.determinant<0)
                {
                    for(int sub=0;sub<mesh.subMeshCount;sub++)
                    {var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3){int x=indices[i];indices[i]=indices[i+2];indices[i+2]=x;}mesh.SetTriangles(indices,sub);}
                }
                mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.name="OriginalCharacter_NormalizedPose";
                return Save(mesh,"OriginalCharacterPose.asset");
            }
            finally {UnityEngine.Object.DestroyImmediate(instance);}
        }

        static Mesh Tiles(int[] rows,int[] cols,Color color,string name)
        {
            var vertices=new Vector3[rows.Length*4];var uv=new Vector2[vertices.Length];var tint=new Color32[vertices.Length];var triangles=new int[rows.Length*6];
            for(int i=0;i<rows.Length;i++)
            {
                int v=i*4,t=i*6;float x=cols[i]*Cell-3.15f,y=rows[i]*Cell+.4f;
                vertices[v]=new Vector3(x,y,0);vertices[v+1]=new Vector3(x,y+Cell,0);vertices[v+2]=new Vector3(x+Cell,y+Cell,0);vertices[v+3]=new Vector3(x+Cell,y,0);
                for(int k=0;k<4;k++){tint[v+k]=color;uv[v+k]=new Vector2(vertices[v+k].x,vertices[v+k].y)*.5f;}
                triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            var mesh=new Mesh{name=name,vertices=vertices,uv=uv,colors32=tint,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();
            return Save(mesh,name+".asset");
        }
        static MeshFilter MeshObject(string name,Transform parent,Mesh mesh,Material material)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);
            var filter=g.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            return filter;
        }

        [MenuItem("Sand Jam/3D Scene/Open scene")]
        public static void OpenScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))CreateScene();else EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Sand Jam/3D Scene/Regenerate scene from tutorial")]
        public static void CreateScene()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            font=Resources.Load<Font>("Inter");
            var data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Tutorial").text);SandGame.Validate(data);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=Group("Sand Jam Gameplay 3D");
            var controller=root.gameObject.AddComponent<SandJamSceneController>();
            root.gameObject.AddComponent<AudioSource>().playOnAwake=false;
            var environment=Group("Environment",root);
            var board=Group("Board - 112 rows x 84 columns",root);
            var queueRoot=Group("Characters - original model copies",root);
            var stash=Group("Stash - 5 positions",root);
            var lanes=Group("Lanes - 3 queues",root);
            controller.ProjectileRoot=Group("Projectiles - pooled at runtime",root);
            white=Mat("StageWhite",Hex("E4EDF0"));dark=Mat("BoardFrame",Hex("233E4E"));textBacking=Mat("CounterBacking",Hex("304B5C"));
            var laneMat=Mat("LaneBackground",Hex("ECF2F4"));
            var sand=Resources.Load<Texture2D>("Original/Sand");
            var boardMat=Mat("BoardSurface",Color.white,"SandJamTest/BoardSurface",sand);
            var palette=JsonUtility.FromJson<PaletteData>(Resources.Load<TextAsset>("Original/Palette").text);
            var characterMaterials=new Dictionary<int,Material>();
            foreach(var p in palette.colors)characterMaterials.Add(p.id,Mat("Character_"+p.id,new Color(p.r,p.g,p.b),"SandJamTest/OriginalSandPreview",sand));
            controller.MaterialColorIds=Colors;
            controller.ProjectileMaterials=Colors.Select(c=>Mat("Projectile_"+c,SandJamDemo.Palette(c))).ToArray();
            Cube("Board outer frame",environment,new Vector3(0,4.6f,.45f),new Vector3(6.65f,8.75f,.5f),white);
            Cube("Board dark inner edge",environment,new Vector3(0,4.6f,.19f),new Vector3(6.40f,8.50f,.12f),dark);
            controller.Regions=new SceneRegionView[data.parts.Length];
            for(int i=0;i<data.parts.Length;i++)
            {
                var p=data.parts[i];var tint=Color.Lerp(Hex("E1EDF2"),SandJamDemo.Palette(p.ColorType),p.isOpenedAtStart?.32f:.12f);
                var region=Group(p.name+" - color "+p.ColorType,board);
                var view=region.gameObject.AddComponent<SceneRegionView>();view.PartIndex=i;
                view.Geometry=MeshObject("Colored tiles",region,Tiles(p.rows,p.cols,tint,"Region_"+i),boardMat);
                var center=new Vector3(((float)p.cols.Average()+.5f)*Cell-3.15f,((float)p.rows.Average()+.5f)*Cell+.4f,-.03f);
                var target=Group("Shot target",region);target.position=center;view.Target=target;
                Cube("Amount backing",region,center+Vector3.back*.025f,new Vector3(.92f,.36f,.03f),textBacking);
                view.Counter=Text("Remaining amount",DisplayAmount.Units(p.amount,data.uiDivider).ToString(),region,center+Vector3.back*.055f,.044f,Color.white);
                controller.Regions[i]=view;
            }
            MeshObject("Obstacle tiles",board,Tiles(data.opr,data.opc,Hex("294657"),"Obstacles"),boardMat);
            controller.StashSlots=new Transform[5];
            for(int i=0;i<5;i++)
            {
                float x=-3f+i*1.5f;
                Cube("Slot "+(i+1)+" platform",stash,new Vector3(x,-1.04f,.50f),new Vector3(1.22f,1.37f,.15f),white);
                var point=Group("Slot "+(i+1)+" anchor",stash);point.position=new Vector3(x,-1.42f,0);controller.StashSlots[i]=point;
                Text("Slot index "+(i+1),(i+1).ToString(),stash,new Vector3(x,-1.88f,-.12f),.038f,Hex("8298A6"));
            }
            controller.LaneStarts=new Transform[3];
            for(int i=0;i<3;i++)
            {
                float x=-3+i*3;
                Cube("Lane "+(i+1)+" panel",lanes,new Vector3(x,-5.56f,.55f),new Vector3(2.42f,6.53f,.15f),laneMat);
                var point=Group("Lane "+(i+1)+" front",lanes);point.position=new Vector3(x,-3.42f,0);controller.LaneStarts[i]=point;
            }
            var characterMesh=BakeCharacter();
            var shadowMaterial=Mat("OriginalShadow",new Color(1,1,1,.16f),"Unlit/Transparent",Resources.Load<Texture2D>("Original/Shadow"));
            var actorList=new List<SceneActorView>();
            for(int lane=0;lane<3;lane++)for(int order=0;order<data.laneData[lane].ColorAmmoDatas.Length;order++)
            {
                var info=data.laneData[lane].ColorAmmoDatas[order];
                var actor=Group("Lane "+(lane+1)+" - Character "+(order+1)+" - color "+info.ColorType,queueRoot);
                actor.position=controller.LaneStarts[lane].position+Vector3.down*controller.QueueSpacing*order;actor.gameObject.layer=8;
                var view=actor.gameObject.AddComponent<SceneActorView>();view.SourceLane=lane;view.SourceOrder=order;view.ColorId=info.ColorType;
                view.Visual=MeshObject("Original character mesh",actor,characterMesh,characterMaterials[info.ColorType]).transform;
                view.Visual.localScale=Vector3.one*.95f;
                view.AmmoLabel=Text("Ammo",DisplayAmount.Units(info.AmmoCount,data.uiDivider).ToString(),actor,new Vector3(0,-.15f,-.40f),.041f,Hex("233E4E"));
                view.ClickCollider=actor.gameObject.AddComponent<BoxCollider>();view.ClickCollider.center=new Vector3(0,.43f,0);view.ClickCollider.size=new Vector3(.95f,1.1f,.75f);
                var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad);shadow.name="Original shadow";shadow.transform.SetParent(actor,false);shadow.transform.localPosition=new Vector3(0,.06f,.42f);shadow.transform.localScale=new Vector3(.75f,.12f,1);shadow.GetComponent<Renderer>().sharedMaterial=shadowMaterial;UnityEngine.Object.DestroyImmediate(shadow.GetComponent<Collider>());
                actorList.Add(view);
            }
            controller.Characters=actorList.ToArray();
            var projectile=GameObject.CreatePrimitive(PrimitiveType.Sphere);projectile.name="Sand projectile";projectile.transform.localScale=Vector3.one*.09f;
            UnityEngine.Object.DestroyImmediate(projectile.GetComponent<Collider>());projectile.GetComponent<Renderer>().sharedMaterial=controller.ProjectileMaterials[0];
            controller.ProjectilePrefab=PrefabUtility.SaveAsPrefabAsset(projectile,Folder+"/SandProjectile.prefab");UnityEngine.Object.DestroyImmediate(projectile);
            var camObject=Group("Main Camera",root);var camera=camObject.gameObject.AddComponent<Camera>();camObject.gameObject.tag="MainCamera";
            camObject.position=new Vector3(0,0,-30);camera.orthographic=true;camera.orthographicSize=10.6f;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Hex("F4F8FA");camObject.gameObject.AddComponent<AudioListener>();controller.GameCamera=camera;
            var light=Group("Key Light",environment).gameObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.transform.rotation=Quaternion.Euler(30,-30,0);
            controller.LevelJson=Resources.Load<TextAsset>("Tutorial");controller.InterfaceFont=font;
            controller.SelectSound=Resources.Load<AudioClip>("Original/Select");controller.CompleteSound=Resources.Load<AudioClip>("Original/RegionComplete");controller.VictorySound=Resources.Load<AudioClip>("Original/Victory");controller.PlayButtonTexture=Resources.Load<Texture2D>("Original/PlayButton");
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            VerifyScene(controller);
        }

        static void VerifyScene(SandJamSceneController controller)
        {
            if(controller.Characters.Length!=13 || controller.Regions.Length!=5 || controller.StashSlots.Length!=5 || controller.LaneStarts.Length!=3)throw new Exception("Scene hierarchy counts wrong.");
            if(controller.Characters.Any(a=>!a.Visual.GetComponent<MeshFilter>().sharedMesh || !a.ClickCollider || !a.AmmoLabel))throw new Exception("Character reference missing.");
            if(controller.Regions.Any(v=>v.Geometry.sharedMesh.vertexCount<4 || !v.Counter || !v.Target))throw new Exception("Region references missing.");
            var game=new SandGame(JsonUtility.FromJson<LevelData>(controller.LevelJson.text));game.SelectLane(0);game.Tick(20,s=>false);
            if(game.SpentAmmo!=0)throw new Exception("Movement gate ignored.");game.Tick(20,s=>true);if(game.SpentAmmo!=20)throw new Exception("Movement gate never released.");
            Directory.CreateDirectory("TestResults/Scene3D");
            File.WriteAllText("TestResults/Scene3D/editor-tests.txt","PASS: Scene serialized with 13 model actors, 5 regions, 5 slots, 3 lanes\nPASS: Mesh, collider, text and target references resolve\nPASS: Movement gate blocks and releases shooting\n");
        }
        [MenuItem("Sand Jam/3D Scene/Build Windows player")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            BuildDemo.Tests(); SandFlowChecks.Run();
            if(!File.Exists(ScenePath))CreateScene();else EditorSceneManager.OpenScene(ScenePath);
            var controller = UnityEngine.Object.FindObjectOfType<SandJamSceneController>();
            controller.ConfigureFiveSlots();
            var level = JsonUtility.FromJson<LevelData>(controller.LevelJson.text);
            foreach(var actor in controller.Characters) { actor.Divider=level.uiDivider; actor.AmmoLabel.text=DisplayAmount.Units(level.laneData[actor.SourceLane].ColorAmmoDatas[actor.SourceOrder].AmmoCount,level.uiDivider).ToString(); }
            foreach(var region in controller.Regions) { region.Divider=level.uiDivider; region.Counter.text=DisplayAmount.Units(level.parts[region.PartIndex].amount,level.uiDivider).ToString(); }
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            VerifyScene(controller);
            PlayerSettings.defaultScreenWidth=600;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
            Directory.CreateDirectory("BuildScene3D");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="BuildScene3D/SandJam-Scene3D.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            File.WriteAllText("TestResults/Scene3D/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nSize: "+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Scene build failed.");
        }
        public static void CreateAndBuild(){CreateScene();Build();}
        [Serializable]sealed class PaletteData{public PaletteEntry[] colors;}
        [Serializable]sealed class PaletteEntry{public int id;public float r,g,b;}
    }
}




