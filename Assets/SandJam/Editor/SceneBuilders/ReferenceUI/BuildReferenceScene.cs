using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SandJamTest.Scene3D;

namespace SandJamTest.Editor
{
    public static class BuildReferenceScene
    {
        const string Folder="Assets/SandJam/Generated/ReferenceUI";
        const string Scene="Assets/SandJam/Scenes/SandJamReferenceUI.unity";
        static Font font; static Material spriteMaterial;
        static Sprite rounded, pad;
        static Color Hex(string value){Color c;ColorUtility.TryParseHtmlString("#"+value,out c);return c;}
        static Vector3 P(float x,float y,float z=0){return new Vector3((x-241.5f)*.01f,(537.5f-y)*.01f,z);}
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;if(parent)t.SetParent(parent,false);return t;}
        static T Save<T>(T asset,string name) where T:UnityEngine.Object
        {
            string path=Folder+"/"+name;var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old){EditorUtility.CopySerialized(asset,old);UnityEngine.Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);return old;}
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static Sprite Asset(string name)
        {
            string path="Assets/SandJam/Resources/ReferenceUI/"+name+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Bilinear;importer.spritePixelsPerUnit=100;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static Sprite RoundSprite(string file="RoundedPanel",float radius=24)
        {
            string path=Folder+"/"+file+".png";var t=new Texture2D(128,128,TextureFormat.RGBA32,false);
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            {
                float dx=Mathf.Max(Mathf.Abs(x-63.5f)-(63.5f-radius),0),dy=Mathf.Max(Mathf.Abs(y-63.5f)-(63.5f-radius),0);
                float a=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy));float shade=Mathf.Lerp(.78f,1,y/127f);
                t.SetPixel(x,y,new Color(shade,shade,shade,a));
            }
            t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=100;importer.spriteBorder=new Vector4(radius+1,radius+1,radius+1,radius+1);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static SpriteRenderer Image(string name,Sprite sprite,Transform parent,float x,float y,float w,float h,Color tint,float z=0,bool sliced=false)
        {
            var t=Group(name,parent);t.position=P(x,y,z);var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=tint;r.sharedMaterial=spriteMaterial;
            if(sliced){r.drawMode=SpriteDrawMode.Sliced;r.size=new Vector2(w*.01f,h*.01f);}else t.localScale=new Vector3(w*.01f/sprite.bounds.size.x,h*.01f/sprite.bounds.size.y,1);
            return r;
        }
        static TextMesh Text(string name,string value,Transform parent,float x,float y,float pixels,Color color,float z=-1,bool outline=true)
        {
            var t=Group(name,parent);t.position=P(x,y,z);var text=t.gameObject.AddComponent<TextMesh>();text.font=font;text.fontSize=64;text.characterSize=pixels*.01f/7f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.text=value;text.color=color;t.gameObject.GetComponent<MeshRenderer>().sharedMaterial=font.material;
            if(outline)
            {
                foreach(var d in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)})
                {var edge=Group("Text outline",t);edge.localPosition=new Vector3(d.x*.012f,d.y*.012f,.012f);var e=edge.gameObject.AddComponent<TextMesh>();e.font=font;e.fontSize=64;e.characterSize=text.characterSize;e.anchor=text.anchor;e.alignment=text.alignment;e.text=value;e.color=Hex("252434");edge.gameObject.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
            }
            if(outline) t.gameObject.AddComponent<ReferenceTextOutline>();
            return text;
        }
        static Material Mat(string name,Color color,string shader="SandJamTest/OriginalSandPreview")
        {var m=new Material(Shader.Find(shader));if(m.HasProperty("_Color"))m.color=color;if(m.HasProperty("_MainTex"))m.mainTexture=Resources.Load<Texture2D>("Original/Sand");return Save(m,name+".mat");}
        static MeshFilter Tiles(PartData p,Transform parent)
        {
            var mesh=new Mesh();var v=new Vector3[p.rows.Length*4];var tris=new int[p.rows.Length*6];
            for(int i=0;i<p.rows.Length;i++){float x=p.cols[i]*.075f-3.15f,y=p.rows[i]*.075f+.4f;int k=i*4,j=i*6;v[k]=new Vector3(x,y,0);v[k+1]=new Vector3(x,y+.075f,0);v[k+2]=new Vector3(x+.075f,y+.075f,0);v[k+3]=new Vector3(x+.075f,y,0);tris[j]=k;tris[j+1]=k+1;tris[j+2]=k+2;tris[j+3]=k;tris[j+4]=k+2;tris[j+5]=k+3;}
            mesh.vertices=v;mesh.triangles=tris;mesh.RecalculateBounds();mesh=Save(mesh,p.name+".asset");var t=Group("Editor grid reference",parent);var filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().enabled=false;return filter;
        }
        static ReferenceUiPlaceholder Placeholder(Transform root,string name,string action,int price)
        {var p=Group(name,root).gameObject.AddComponent<ReferenceUiPlaceholder>();p.ActionId=action;p.DisplayPrice=price;return p;}
        [MenuItem("Sand Jam/Reference UI/Create screenshot layout")]
        public static void Create()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();font=Resources.Load<Font>("Inter");rounded=RoundSprite();pad=RoundSprite("SmallCorner",10);spriteMaterial=Save(new Material(Shader.Find("SandJamTest/PixelSandSprite")),"UiSprite.mat");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var root=Group("Sand Jam - Screenshot Reference");
            var controller=root.gameObject.AddComponent<SandJamSceneController>();controller.HidePrototypeHud=true;controller.ViewAspect=483f/1075;controller.QueueSpacing=1.04f;root.gameObject.AddComponent<AudioSource>().playOnAwake=false;
            var reference=root.gameObject.AddComponent<ReferenceScreen>();reference.Controller=controller;
            var environment=Group("Backdrop and frame",root);var hud=Group("HUD - visual controls only",root);
            Image("Frame shadow",rounded,environment,242,374,438,553,Hex("2E2F38"),.8f,true);
            Image("Raised board frame",rounded,environment,239,363,426,544,Hex("777487"),.7f,true);
            Image("Inset rim",rounded,environment,239,364,382,501,Hex("B6B3CC"),.6f,true);
            Image("Tray bevel",rounded,environment,241,623,474,46,Hex("565565"),.25f,true);
            Image("Tray top",rounded,environment,241,610,450,29,Hex("646275"),.24f,true);
            var left=Image("Left lane rail",rounded,environment,-2,973,164,442,Hex("E9E9F0"),.6f,true);left.transform.rotation=Quaternion.Euler(0,0,-9);
            var leftInner=Image("Left lane bank",rounded,environment,-15,966,158,444,Hex("656575"),.5f,true);leftInner.transform.rotation=Quaternion.Euler(0,0,-9);
            var right=Image("Right booster rail",rounded,environment,479,971,147,438,Hex("F7F7FA"),.6f,true);right.transform.rotation=Quaternion.Euler(0,0,9);
            var rightInner=Image("Right booster bank",rounded,environment,491,968,150,435,Hex("777382"),.5f,true);rightInner.transform.rotation=Quaternion.Euler(0,0,9);
            // Exactly five serialized waiting positions.
            var stash=Group("Stash - 5 positions",root);controller.StashSlots=new Transform[5];
            for(int i=0;i<5;i++){float x=95+i*69;Image("Waiting pad "+(i+1),pad,stash,x,687,63,43,Color.white,.3f,true);var anchor=Group("Reference slot "+(i+1),stash);anchor.position=P(x,711,-.1f);controller.StashSlots[i]=anchor;}
            var settings=Placeholder(hud,"Settings - UI only","settings",0);Image("Settings icon",Asset("icon-settings-v2"),settings.transform,40,70,46,46,Color.white,-1);
            Image("Level badge",Asset("bg-level-normal"),hud,239,71,114,34,Color.white,-1);Text("Level 111","Level 111",hud,239,71,22,Color.white,-1.1f);
            var wallet=Placeholder(hud,"Coin wallet - UI only","currency",0);Image("Coin bar",Asset("bg-level-normal"),wallet.transform,401,71,123,33,Color.white,-1);Image("Add coins icon",Asset("button-plus_0"),wallet.transform,352,71,32,32,Color.white,-1.1f);Image("Coin",Asset("icon-coin"),wallet.transform,450,71,40,40,Color.white,-1.15f);Text("Preview coin balance","210",wallet.transform,410,71,21,Color.white,-1.2f);
            var controls=new List<ReferenceUiPlaceholder>{settings,wallet};
            for(int i=0;i<3;i++)
            {
                float y=811+i*78;int price=i==0?150:i==1?500:1;var p=Placeholder(hud,"Booster "+(i+1)+" - UI only",i==0?"rocket":i==1?"swap":"select",price);controls.Add(p);
                Image("Purple booster button",Asset("button-booster"),p.transform,448,y,74,57,Color.white,-1);
                Image("Booster icon",Asset("icon-booster-"+(3-i)),p.transform,448,y-19,49,49,Color.white,-1.1f);
                if(i<2){Image("Price coin",Asset("icon-coin_0"),p.transform,434,y+12,15,15,Color.white,-1.2f);Text("Price",price.ToString(),p.transform,461,y+12,17,Color.white,-1.3f);}
                else {Image("Quantity badge",Asset("bg-booster-quantity"),p.transform,415,y+13,25,25,Color.white,-1.2f);Text("Quantity","1",p.transform,415,y+13,20,Color.white,-1.3f);}
            }
            reference.Placeholders=controls.ToArray();reference.Status=Text("Game result","",hud,241,731,18,Color.white,-1.5f,false);
            var data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("ReferenceUI/WildlifeOriginal").text);
            // Preview ordering follows the screenshot; original JSON is retained unchanged separately.
            var list=data.laneData[0].ColorAmmoDatas.ToList();var red=list[2];list.RemoveAt(2);list.Insert(0,red);data.laneData[0].ColorAmmoDatas=list.ToArray();
            list=data.laneData[1].ColorAmmoDatas.ToList();var white=list[3];list.RemoveAt(3);list.Insert(0,white);var orange=list[3];list[3]=list[2];list[2]=orange;data.laneData[1].ColorAmmoDatas=list.ToArray();
            File.WriteAllText(Folder+"/ReferencePlayable.json",JsonUtility.ToJson(data,true));AssetDatabase.ImportAsset(Folder+"/ReferencePlayable.json");controller.LevelJson=AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"/ReferencePlayable.json");SandGame.Validate(data);
            var board=Group("Deer board - grid texture",root);board.position=new Vector3(-.035f,-.745f-.4f*(3.76f/6.3f),0);board.localScale=Vector3.one*(3.76f/6.3f);
            controller.Regions=new SceneRegionView[data.parts.Length];
            for(int i=0;i<data.parts.Length;i++)
            {
                var part=data.parts[i];var t=Group(part.name,board);var view=t.gameObject.AddComponent<SceneRegionView>();view.PartIndex=i;view.Divider=data.uiDivider;view.EmptyTint=Hex("BCBDD3");view.OpenTintStrength=.9f;view.Geometry=Tiles(part,t);
                view.Target=Group("Pour target",t);float x=((float)part.cols.Average()+.5f)*.075f-3.15f,y=((float)part.rows.Average()+.5f)*.075f+.4f;
                var label=Text("Remaining amount",part.isOpenedAtStart?DisplayAmount.Units(part.amount,data.uiDivider).ToString():"",t,0,0,20,Color.white,-.08f,true);label.transform.localPosition=new Vector3(x,y,-.09f);label.characterSize=.062f;foreach(var edge in label.GetComponentsInChildren<TextMesh>())edge.characterSize=.062f;label.gameObject.SetActive(part.isOpenedAtStart);view.Counter=label;controller.Regions[i]=view;
            }
            // Serialized editor preview; the runtime grid texture replaces it on Play.
            var previewTexture=new Texture2D(84,112,TextureFormat.RGBA32,false);
            var previewPixels=Enumerable.Repeat((Color32)Hex("BCBDD3"),84*112).ToArray();
            foreach(var part in data.parts) if(part.isOpenedAtStart)
                for(int i=0;i<part.rows.Length;i++)previewPixels[part.rows[i]*84+part.cols[i]]=Color.Lerp(Hex("BCBDD3"),SandJamDemo.Palette(part.ColorType),.9f);
            for(int i=0;i<data.opr.Length;i++)previewPixels[data.opr[i]*84+data.opc[i]]=Hex("8280AD");
            previewTexture.SetPixels32(previewPixels);previewTexture.Apply();string previewPath=Folder+"/DeerBoardPreview.png";File.WriteAllBytes(previewPath,previewTexture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(previewTexture);AssetDatabase.ImportAsset(previewPath);
            var previewImporter=(TextureImporter)AssetImporter.GetAtPath(previewPath);previewImporter.textureType=TextureImporterType.Sprite;previewImporter.mipmapEnabled=false;previewImporter.filterMode=FilterMode.Point;previewImporter.spritePixelsPerUnit=100;previewImporter.textureCompression=TextureImporterCompression.Uncompressed;previewImporter.SaveAndReimport();
            var preview=Group("Editor board preview",board);preview.localPosition=new Vector3(0,4.6f,-.02f);preview.localScale=Vector3.one*7.5f;
            reference.EditorPreview=preview.gameObject.AddComponent<SpriteRenderer>();reference.EditorPreview.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(previewPath);reference.EditorPreview.sharedMaterial=spriteMaterial;
            var actors=Group("Characters - copied original mesh",root);controller.LaneStarts=new Transform[3];var actorViews=new List<SceneActorView>();
                        var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SandJam/Resources/ReferenceUI/RoundedCube.asset");
            var mesh=UnityEngine.Object.Instantiate(source);var vertices=mesh.vertices;var bounds=mesh.bounds;
            for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((vertices[i].x-bounds.center.x)/bounds.size.x,(vertices[i].y-bounds.min.y)/bounds.size.y,(vertices[i].z-bounds.center.z)/bounds.size.z);
            mesh.vertices=vertices;mesh.RecalculateBounds();mesh=Save(mesh,"OriginalRoundedCubeNormalized.asset");
            var ids=data.parts.Select(p=>p.ColorType).Distinct().OrderBy(c=>c).ToArray();controller.MaterialColorIds=ids;controller.ProjectileMaterials=ids.Select(id=>Mat("Grain_"+id,SandJamDemo.Palette(id),"Unlit/Color")).ToArray();
            for(int lane=0;lane<3;lane++)
            {
                var start=Group("Lane "+(lane+1),root);start.position=P(158+lane*82,835,-.1f);controller.LaneStarts[lane]=start;
                for(int order=0;order<data.laneData[lane].ColorAmmoDatas.Length;order++)
                {
                    var info=data.laneData[lane].ColorAmmoDatas[order];var actor=Group("Character "+lane+"-"+order,actors);actor.position=start.position+Vector3.down*order*controller.QueueSpacing;actor.gameObject.layer=8;
                    var view=actor.gameObject.AddComponent<SceneActorView>();view.SourceLane=lane;view.SourceOrder=order;view.ColorId=info.ColorType;view.Divider=data.uiDivider;
                    var pose=Group("Top-facing pose",actor);pose.localRotation=Quaternion.Euler(10,-7,0);
                    var model=Group("Original character",pose);model.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;model.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat("Character_"+info.ColorType,SandJamDemo.Palette(info.ColorType));                    model.localScale=new Vector3(.64f,.73f,.36f);view.Visual=model;
                    var outline=Group("Black bevel silhouette",model);outline.localPosition=new Vector3(0,-.015f,.14f);outline.localScale=new Vector3(1.075f,1.035f,1.02f);outline.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;outline.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat("CharacterOutline",Hex("15131D"),"Unlit/Color");
                    if((lane==0 && (order==1 || order==2)) || (lane==1 && order==2))
                    {
                        for(int diagonal=0;diagonal<2;diagonal++)
                        {
                            var strap=Group("Rope preview - cosmetic",actor);var line=strap.gameObject.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;float side=diagonal==0?1:-1;
                            line.SetPositions(new[]{new Vector3(-.28f*side,.66f,-.40f),new Vector3(.05f*side,.10f,-.42f),new Vector3(.30f*side,.02f,-.40f)});
                            line.startWidth=line.endWidth=.026f;line.numCapVertices=4;line.sharedMaterial=Mat("RopePreview",Hex("DDBF78"),"Unlit/Color");
                        }
                    }
                    var label=Text("Ammo",DisplayAmount.Units(info.AmmoCount,data.uiDivider).ToString(),actor,0,0,21,Color.white,-.7f,true);label.transform.localPosition=new Vector3(0,.06f,-.75f);view.AmmoLabel=label;
                    view.ClickCollider=actor.gameObject.AddComponent<BoxCollider>();view.ClickCollider.center=new Vector3(0,.30f,0);view.ClickCollider.size=new Vector3(.73f,.84f,1.5f);
                    if(lane==2 && order>0 && order<3)
                    {
                        var coverRoot=Group("Mystery preview - cosmetic",actor);var cover=Image("Mystery block",pad,coverRoot,0,0,68,84,Hex("555860"),-.9f,true);cover.transform.localPosition=new Vector3(0,.36f,-.9f);
                        var question=Text("Question","? ?\n ?",coverRoot,0,0,24,Color.white,-1,false);question.transform.localPosition=new Vector3(0,.34f,-1);var q=actor.gameObject.AddComponent<ReferenceQueueCover>();q.Actor=view;q.Controller=controller;q.Cover=coverRoot.gameObject;
                    }
                    actorViews.Add(view);
                }
            }
            controller.Characters=actorViews.ToArray();controller.ProjectileRoot=Group("Pooled sand projectiles",root);controller.ProjectilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SandJam/Generated/Scene3D/SandProjectile.prefab");
            controller.InterfaceFont=font;controller.SelectSound=Resources.Load<AudioClip>("Original/Select");controller.CompleteSound=Resources.Load<AudioClip>("Original/RegionComplete");controller.VictorySound=Resources.Load<AudioClip>("Original/Victory");controller.PlayButtonTexture=Resources.Load<Texture2D>("Original/PlayButton");
            var camera=Group("Main Camera",root).gameObject.AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,0,-30);camera.orthographic=true;camera.orthographicSize=5.375f;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Hex("3A3D47");camera.gameObject.AddComponent<AudioListener>();controller.GameCamera=camera;
            EditorSceneManager.SaveScene(scene,Scene);AssetDatabase.SaveAssets();
        }
        public static void CreateAndBuild()
        {
            Create();PlayerSettings.defaultScreenWidth=483;PlayerSettings.defaultScreenHeight=1075;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;Directory.CreateDirectory("BuildReferenceUI");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName="BuildReferenceUI/SandJam-ReferenceUI.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Reference build failed");
        }
    }
}



