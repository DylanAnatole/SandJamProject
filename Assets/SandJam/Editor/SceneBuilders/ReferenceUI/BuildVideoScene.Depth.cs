using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SandJamTest.Scene3D;

namespace SandJamTest.Editor
{
    public static partial class BuildVideoScene
    {
        static Sprite contactShadow;
        static Vector3[] Contour(float w,float h,float radius,float z,float dy=0)
        {
            const int steps=8;var points=new Vector3[4*(steps+1)];radius=Mathf.Min(radius,Mathf.Min(w,h)*.49f);
            for(int corner=0;corner<4;corner++)for(int j=0;j<=steps;j++)
            {
                float angle=(corner*90+j*90f/steps)*Mathf.Deg2Rad;
                float x=(corner==0 || corner==3)?w*.5f-radius:-w*.5f+radius;
                float y=corner<2?h*.5f-radius:-h*.5f+radius;
                points[corner*(steps+1)+j]=new Vector3(x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius+dy,z);
            }
            return points;
        }
        static Mesh Loft(string name,List<Vector3[]> loops,bool caps,bool close=false)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();int count=loops[0].Length;
            foreach(var loop in loops)vertices.AddRange(loop);
            for(int layer=0;layer<(close?loops.Count:loops.Count-1);layer++)
            {
                int next=(layer+1)%loops.Count;
                for(int i=0;i<count;i++)
                {
                    int a=layer*count+i,b=next*count+i,c=next*count+(i+1)%count,d=layer*count+(i+1)%count;
                    triangles.AddRange(new[]{a,b,c,a,c,d});
                }
            }
            if(caps)
            {
                for(int end=0;end<2;end++)
                {
                    int source=end==0?0:(loops.Count-1)*count;int center=vertices.Count;
                    vertices.Add(new Vector3(0,0,vertices[source].z));int offset=vertices.Count;
                    // Separate cap normals keep the top face flat instead of a fan of lit triangles.
                    for(int i=0;i<count;i++)vertices.Add(vertices[source+i]);
                    for(int i=0;i<count;i++)triangles.AddRange(end==0?new[]{center,offset+i,offset+(i+1)%count}:new[]{center,offset+(i+1)%count,offset+i});
                }
            }
            var mesh=new Mesh();mesh.name=name;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].x,vertices[i].y);mesh.uv=uv;
            mesh.RecalculateNormals();mesh.RecalculateBounds();return Save(mesh,name+".asset");
        }
        static Mesh BevelBox(string name,float w,float h,float depth,float radius,float bevel)
        {
            return Loft(name,new List<Vector3[]>{Contour(w-2*bevel,h-2*bevel,radius-bevel,depth*.5f),Contour(w,h,radius,depth*.5f-bevel),Contour(w,h,radius,-depth*.5f+bevel),Contour(w-2*bevel,h-2*bevel,radius-bevel,-depth*.5f)},true);
        }
        static Material DepthMaterial(string name,Color color,float grain=0,float outline=0,float gloss=.12f)
        {
            var material=new Material(Shader.Find("SandJamTest/SandToonDepth"));material.SetColor("_Color",color);
            material.SetTexture("_MainTex",Resources.Load<Texture2D>("Original/Sand"));material.SetFloat("_GrainStrength",grain);
            material.SetFloat("_OutlineWidth",outline);material.SetFloat("_Gloss",gloss);return Save(material,name+".mat");
        }
        static Transform DepthMesh(string name,Transform parent,Mesh mesh,Material material,Vector3 position,Vector3 angles)
        {
            var t=Group(name,parent);t.position=position;t.localRotation=Quaternion.Euler(angles);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
        }
        static void PrepareDepth()
        {
            const int size=96;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x+ .5f-size*.5f)/(size*.5f),dy=(y+.5f-size*.5f)/(size*.5f),r=dx*dx+dy*dy;
                texture.SetPixel(x,y,new Color(.16f,.12f,.24f,Mathf.Pow(Mathf.Clamp01(1-r),3)*.32f));
            }
            texture.Apply();string path=Folder+"/ContactShadow.png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
            contactShadow=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Shadow(Transform parent,string name,float x,float y,float w,float h,float z)
        {Image(name,contactShadow,parent,x,y,w,h,Color.white,z);}
        static void SetOverlayLayer(Transform root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.gameObject.layer!=9)t.gameObject.layer=5;
        }
        static void ApplyDepth(Transform root,VideoScreen screen,SandJamSceneController controller)
        {
            var environment=screen.Gameplay.transform.Find("Backdrop and frame");
            foreach(Transform old in environment)old.gameObject.SetActive(false);
            var plum=DepthMaterial("StagePlum",Hex("67528D"),0,0,.12f);
            var frame=Loft("BevelledBoardFrame",new List<Vector3[]>{
                Contour(4.26f,5.32f,.34f,.24f),Contour(4.26f,5.32f,.34f,.04f),Contour(4.16f,5.22f,.29f,-.06f),
                Contour(3.77f,4.95f,.21f,-.06f,.07f),Contour(3.69f,4.87f,.17f,.04f,.07f),Contour(3.69f,4.87f,.17f,.24f,.07f)},false,true);
            DepthMesh("Solid board frame",environment,frame,plum,P(239,374,.2f),Vector3.zero);
            DepthMesh("Extruded plinth",environment,BevelBox("Plinth",4.72f,.30f,.40f,.10f,.045f),plum,P(241,623,-.08f),new Vector3(-32,0,0));
            Shadow(environment,"Plinth contact shadow",241,651,490,36,.35f);
            var white=DepthMaterial("Porcelain",Hex("FFFDFE"),0,0,.18f);
            var bank=DepthMaterial("RailTop",Hex("736487"),0,0,.04f);
            var outer=BevelBox("Rail",1.62f,4.5f,.15f,.32f,.025f);var inner=BevelBox("RailInset",1.52f,4.5f,.08f,.28f,.015f);
            DepthMesh("Left solid rail",environment,outer,white,P(-2,978,.36f),new Vector3(0,0,-9));
            DepthMesh("Left rail top",environment,inner,bank,P(-14,975,.23f),new Vector3(0,0,-9));
            DepthMesh("Right solid rail",environment,outer,white,P(483,978,.36f),new Vector3(0,0,9));
            DepthMesh("Right rail top",environment,inner,bank,P(497,975,.23f),new Vector3(0,0,9));
            var pads=screen.Gameplay.transform.Find("Stash - 5 positions");var padMesh=BevelBox("WaitingPad",.63f,.43f,.055f,.09f,.012f);
            for(int i=0;i<5;i++)
            {
                pads.Find("Waiting pad "+(i+1)).gameObject.SetActive(false);float x=95+i*69;
                Shadow(pads,"Pad shadow "+i,x,698,75,24,.3f);
                DepthMesh("Solid waiting pad "+i,pads,padMesh,white,P(x,687,.1f),new Vector3(18,0,0));
            }
            foreach(var actor in controller.Characters)
            {
                var shade=Image("Foot contact shadow",contactShadow,actor.transform,0,0,88,30,new Color(1,1,1,.8f),.2f);
                shade.transform.localPosition=new Vector3(.04f,.06f,.36f);
                var style=actor.gameObject.AddComponent<CharacterDepthStyle>();style.Actor=actor;style.Controller=controller;style.Skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            }
            // Real perspective for the board, geometry and moving actors; flat HUD has its own camera.
            controller.GameCamera.transform.position=new Vector3(0,0,-18);
            controller.GameCamera.orthographic=false;controller.GameCamera.fieldOfView=2*Mathf.Atan(5.375f/18f)*Mathf.Rad2Deg;
            controller.GameCamera.cullingMask=~(1<<5);controller.GameCamera.depth=0;controller.GameCamera.allowHDR=false;controller.GameCamera.allowMSAA=true;
            var overlay=Group("UI Camera - orthographic overlay",root).gameObject.AddComponent<Camera>();
            overlay.transform.position=new Vector3(0,0,-30);overlay.orthographic=true;overlay.orthographicSize=5.375f;overlay.clearFlags=CameraClearFlags.Depth;overlay.depth=1;overlay.cullingMask=1<<5;overlay.nearClipPlane=.1f;overlay.farClipPlane=100;overlay.allowHDR=false;
            overlay.gameObject.AddComponent<OverlayCameraSync>().WorldCamera=controller.GameCamera;screen.UiCamera=overlay;
            SetOverlayLayer(screen.Home.transform);SetOverlayLayer(screen.Loading.transform);SetOverlayLayer(screen.Celebration.transform);SetOverlayLayer(screen.Result.transform);
            SetOverlayLayer(screen.Gameplay.transform.Find("HUD - visual controls only"));
        }
        static GameObject MysteryCube(Transform actor)
        {
            var root=Group("Mystery cube - cosmetic",actor);root.localPosition=new Vector3(0,.29f,-.05f);root.localRotation=Quaternion.Euler(-38,0,0);
            var cube=DepthMesh("Solid question cube",root,BevelBox("MysteryCube",.65f,.62f,.58f,.055f,.025f),DepthMaterial("MysteryGray",Hex("646873"),0,.008f,.07f),Vector3.zero,Vector3.zero);cube.localPosition=Vector3.zero;
            var front=Text("Front question","?",root,0,0,32,Color.white,-.4f,false);front.transform.localPosition=new Vector3(0,-.02f,-.31f);
            var top=Text("Top questions","? ?\n ?",root,0,0,23,Color.white,0,false);top.transform.localPosition=new Vector3(0,.32f,0);top.transform.localRotation=Quaternion.Euler(90,0,0);
            return root.gameObject;
        }
    }
}
