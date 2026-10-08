using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // Rounded cage and a separate sand volume: empty space is actual geometry, not a body cutout.
    public sealed class CharacterSandVessel : MonoBehaviour
    {
        public SceneActorView Actor;
        Transform sand;
        MeshRenderer fillRenderer;
        static Mesh frameMesh, sandMesh, legsMesh;
        static int users;
        bool initialized;
        public void Initialize(SceneActorView actor, Material material)
        {
            if(initialized)return;
            Actor=actor;users++;initialized=true;
            if(!frameMesh)BuildMeshes();
            var skin=actor.GetComponent<CharacterDepthStyle>().Skin;
            if(!legsMesh)legsMesh=Resources.Load<Mesh>("CharacterAnimation/CharacterLegs");
            if(!legsMesh)throw new System.InvalidOperationException("Generated CharacterLegs resource is missing");
            skin.sharedMesh=legsMesh;
            // Reference: the count is printed on the lower front face, large, not below the block.
            if(actor.AmmoLabel){actor.AmmoLabel.transform.localPosition+=new Vector3(0,.2f,0);actor.AmmoLabel.transform.localScale*=1.22f;}
            var frame=Part("Rounded vessel frame",frameMesh,material,actor.Visual);frameRenderer=frame;
            frameBlock=new MaterialPropertyBlock();frameBlock.SetFloat("_GrainStrength",0);frameBlock.SetFloat("_OutlineWidth",.007f);frame.SetPropertyBlock(frameBlock);
            fillRenderer=Part("Contained sand volume",sandMesh,material,actor.Visual);sand=fillRenderer.transform;
            // Face-aligned pixel sand (reference blocks show square ~10-per-face speckles, about +/-10% brightness).
            sandBlock=new MaterialPropertyBlock();sandBlock.SetFloat("_OutlineWidth",0);
            sandBlock.SetTexture("_MainTex",PixelSand());sandBlock.SetVector("_MainTex_ST",new Vector4(1,1,0,0));
            sandBlock.SetFloat("_GrainStrength",1);sandBlock.SetFloat("_GrainContrast",2.5f);
            sandBlock.SetFloat("_ShadeStrength",.95f);sandBlock.SetFloat("_Gloss",.22f);fillRenderer.SetPropertyBlock(sandBlock);
            var cover=actor.GetComponent<ReferenceQueueCover>();
            // The frame's visibility is owned by Apply(); only the sand block is masked under a secret cover.
            if(cover)cover.MaskedRenderers=cover.MaskedRenderers.Concat(new Renderer[]{fillRenderer}).ToArray();
            Apply(false,false);
        }
        // Original look: in the lane a character is a solid speckled sand block (black outline only
        // on the selectable front row); once docked in a waiting slot it becomes a glass box whose sand drains.
        MeshRenderer frameRenderer;
        MeshFilter sandFilter;
        TextMesh sleepIcon;
        // Sleeping half in a waiting slot: floating "zZ" above the wedge (original HalfGif).
        void UpdateHalf(bool docked)
        {
            if(!sandFilter)sandFilter=fillRenderer.GetComponent<MeshFilter>();
            var want=Actor.Shooter.Half?halfMesh:sandMesh;
            if(want && sandFilter.sharedMesh!=want)sandFilter.sharedMesh=want;
            bool sleeping=Actor.Shooter.Half && docked && Actor.AtRest;
            if(sleeping && !sleepIcon && Actor.AmmoLabel)
            {
                var obj=new GameObject("Sleeping zZ"){layer=Actor.gameObject.layer};
                obj.transform.SetParent(Actor.transform,false);
                sleepIcon=obj.AddComponent<TextMesh>();
                sleepIcon.text="z Z";sleepIcon.fontSize=64;sleepIcon.characterSize=.045f;sleepIcon.fontStyle=FontStyle.Bold;
                sleepIcon.anchor=TextAnchor.MiddleCenter;sleepIcon.alignment=TextAlignment.Center;
                sleepIcon.font=Actor.AmmoLabel.font;obj.GetComponent<MeshRenderer>().sharedMaterial=Actor.AmmoLabel.GetComponent<MeshRenderer>().sharedMaterial;
            }
            if(!sleepIcon)return;
            sleepIcon.gameObject.SetActive(sleeping);
            if(sleeping)
            {
                float t=Time.time;
                sleepIcon.transform.localPosition=new Vector3(.22f,1.1f+Mathf.Sin(t*2.2f)*.05f,-.7f);
                sleepIcon.color=new Color(.25f,.22f,.45f,.65f+.35f*Mathf.Sin(t*3.1f));
            }
        }
        MaterialPropertyBlock frameBlock, sandBlock;
        static readonly Color Outline=new Color(.035f,.03f,.06f);
        public void Apply(bool front,bool docked)
        {
            if(!sand || Actor.Shooter==null)return;
            float amount=Actor.DisplayedFill;
            // A full block stays solid even in a slot (reference "27"); it turns to glass once it starts pouring.
            bool glass=amount<.999f;
            if(docked)front=false;
            // The rig is pitched ~54 deg toward the camera; stand the block back up so the front face
            // (with the number) dominates and the top is a narrow strip, as in the reference renders.
            var upright=Quaternion.Euler(BlockPitch,0,0);
            sand.localRotation=upright;frameRenderer.transform.localRotation=upright;
            var color=fillRenderer.sharedMaterial.GetColor("_Color");
            // Measured on the reference renders: faces read ~1.15-1.25x brighter than FinalColor with a
            // faint white lift (red 190,24,24 renders as 218,32,60), and lighting is soft.
            var lit=color*1.25f+Color.white*.06f;lit.a=1;
            sandBlock.SetColor("_Color",lit);sandBlock.SetFloat("_ShadeStrength",.55f);
            frameRenderer.enabled=glass;
            if(glass)
            {
                // Sand inside the glass: shrink the solid block to the cage interior and drain it from the top.
                sand.localPosition=new Vector3(0,.285f,0);
                sand.localScale=new Vector3(GlassInner.x/BlockSize.x,GlassInner.y*Mathf.Max(.001f,amount)/BlockSize.y,GlassInner.z/BlockSize.z);
                // Pale glass edges, like the reference waiting-slot boxes.
                frameBlock.SetColor("_Color",Color.Lerp(color,Color.white,.72f));
                frameBlock.SetFloat("_OutlineWidth",.006f);frameBlock.SetColor("_OutlineColor",Color.Lerp(color,Outline,.55f));
                frameRenderer.SetPropertyBlock(frameBlock);
                sandBlock.SetFloat("_OutlineWidth",0);
            }
            else
            {
                // Full-size rounded block covering the cage footprint, matching the reference proportions.
                sand.localPosition=new Vector3(0,.235f,0);
                sand.localScale=Vector3.one;
                sandBlock.SetFloat("_OutlineWidth",front?.034f:0);
                sandBlock.SetColor("_OutlineColor",Outline);
            }
            fillRenderer.SetPropertyBlock(sandBlock);
            sand.gameObject.SetActive(amount>.001f);
            UpdateHalf(docked);
        }
        static MeshRenderer Part(string name,Mesh mesh,Material material,Transform parent)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        static Vector3[] Loop(float width,float depth,float radius,float y)
        {
            var points=new List<Vector3>();
            for(int corner=0;corner<4;corner++)for(int step=0;step<5;step++)
            {
                float angle=(corner*90+step*22.5f)*Mathf.Deg2Rad;
                float x=(corner==0 || corner==3)?width*.5f-radius:-width*.5f+radius;
                float z=corner<2?depth*.5f-radius:-depth*.5f+radius;
                points.Add(new Vector3(x+Mathf.Cos(angle)*radius,y,(z+Mathf.Sin(angle)*radius)*1.15f));
            }
            return points.ToArray();
        }
        static void Quad(List<Vector3> vertices,List<int> indices,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            int start=vertices.Count;vertices.AddRange(new[]{a,b,c,d});indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
        }
        static void Ring(List<Vector3> v,List<int> t,float y)
        {
            var outer=Loop(.74f,.57f,.095f,y);var inner=Loop(.65f,.48f,.055f,y);
            var up=Vector3.up*.035f;
            for(int i=0;i<outer.Length;i++)
            {
                int j=(i+1)%outer.Length;
                Quad(v,t,outer[i]+up,inner[i]+up,inner[j]+up,outer[j]+up);
                Quad(v,t,outer[j],inner[j],inner[i],outer[i]);
                Quad(v,t,outer[i],outer[i]+up,outer[j]+up,outer[j]);
                Quad(v,t,inner[j],inner[j]+up,inner[i]+up,inner[i]);
            }
        }
        static Mesh Finish(List<Vector3> v,List<int> t,string name)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);
            mesh.SetUVs(0,v.Select(p=>new Vector2(p.x*.6f+p.y*.4f,p.z*.6f+p.y*.4f)).ToList());mesh.RecalculateNormals();
            // Weld only normals, retaining separate face UVs for the grain texture.
            var sums=new Dictionary<Vector3,Vector3>();var normals=mesh.normals;
            for(int i=0;i<v.Count;i++){Vector3 sum;sums.TryGetValue(v[i],out sum);sums[v[i]]=sum+normals[i];}
            for(int i=0;i<v.Count;i++)normals[i]=sums[v[i]].normalized;
            mesh.normals=normals;mesh.RecalculateBounds();return mesh;
        }
        static void BuildMeshes()
        {
            var v=new List<Vector3>();var t=new List<int>();Ring(v,t,.25f);Ring(v,t,.80f);
            foreach(float x in new[]{-.33f,.33f})foreach(float z in new[]{-.245f,.245f})
            {
                var low=Loop(.032f,.032f,.01f,.27f);var high=Loop(.032f,.032f,.01f,.815f);var offset=new Vector3(x,0,z*1.15f);
                for(int i=0;i<low.Length;i++){int j=(i+1)%low.Length;Quad(v,t,low[i]+offset,high[i]+offset,high[j]+offset,low[j]+offset);}
            }
            frameMesh=Finish(v,t,"Rounded sand vessel frame");
            sandMesh=RoundedBlock(BlockSize,BevelRadius);
            halfMesh=Wedge(sandMesh);
        }

        // Solid block size in Visual space (pivot at the bottom centre) and the glass cage interior.
        // Slightly taller than wide, like the reference blocks.
        public static readonly Vector3 BlockSize=new Vector3(.735f,.72f,.646f), GlassInner=new Vector3(.65f,.51f,.552f);
        const float BevelRadius=.075f;
        public const float BlockPitch=30f;
        const float BlocksPerUnit=13.5f; // ~10 sand pixels across a face, as in the reference renders
        static Texture2D pixelSand;

        // Rounded cube: every edge and corner is bevelled; each face keeps its own planar UVs so the
        // pixel-sand texture stays square-aligned on that face (no smearing across the bevel).
        // Half cube (original HalfGif): a wedge whose top slopes down from the left edge to the right.
        static Mesh halfMesh;
        static Mesh Wedge(Mesh block)
        {
            var wedge=Object.Instantiate(block);wedge.name="Rounded half wedge";
            var v=wedge.vertices;float minX=-BlockSize.x*.5f;
            for(int i=0;i<v.Length;i++){float t=Mathf.Clamp01((v[i].x-minX)/BlockSize.x);v[i].y*=Mathf.Lerp(1f,.38f,t);}
            wedge.vertices=v;wedge.RecalculateNormals();wedge.RecalculateBounds();return wedge;
        }
        internal static Mesh RoundedBlock(Vector3 size,float radius)
        {
            var half=size*.5f;var inner=half-Vector3.one*radius;
            var center=new Vector3(0,half.y,0);
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
            Vector3[] axes={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            for(int f=0;f<6;f++)
            {
                var n=axes[f];
                var u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,n).normalized;
                var w=Vector3.Cross(n,u);
                float hu=Mathf.Abs(Vector3.Dot(half,Abs(u))),hw=Mathf.Abs(Vector3.Dot(half,Abs(w))),hn=Mathf.Abs(Vector3.Dot(half,Abs(n)));
                var su=Samples(hu,radius);var sw=Samples(hw,radius);
                int start=vertices.Count;
                // Per-face offset so neighbouring faces do not repeat the same speckle pattern.
                var offset=new Vector2(f*.37f,f*.61f);
                foreach(float b in sw)foreach(float a in su)
                {
                    var q=n*hn+u*a+w*b;
                    var c=new Vector3(Mathf.Clamp(q.x,-inner.x,inner.x),Mathf.Clamp(q.y,-inner.y,inner.y),Mathf.Clamp(q.z,-inner.z,inner.z));
                    var dir=q-c;var normal=dir.sqrMagnitude>1e-8f?dir.normalized:n;
                    vertices.Add(center+c+normal*radius);normals.Add(normal);
                    uvs.Add(new Vector2(a,b)*BlocksPerUnit/32f+offset);
                }
                int cols=su.Count;
                for(int y=0;y+1<sw.Count;y++)for(int x=0;x+1<cols;x++)
                {
                    int i0=start+y*cols+x,i1=i0+1,i2=i0+cols,i3=i2+1;
                    // Unity front faces: cross(b-a,c-a) points along the outward normal.
                    bool flip=Vector3.Dot(Vector3.Cross(vertices[i2]-vertices[i0],vertices[i1]-vertices[i0]),n)<0;
                    if(!flip)triangles.AddRange(new[]{i0,i2,i1,i1,i2,i3});else triangles.AddRange(new[]{i0,i1,i2,i1,i3,i2});
                }
            }
            var mesh=new Mesh{name="Rounded sand block"};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();return mesh;
        }
        static Vector3 Abs(Vector3 v){return new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));}
        // Coordinates across a face: dense through each bevel band, sparse on the flat middle.
        static List<float> Samples(float half,float radius)
        {
            var list=new List<float>();
            float[] band={0,.2f,.45f,.7f,.88f,1};
            foreach(float k in band)list.Add(-half+radius*k);
            for(int i=1;i<4;i++)list.Add(Mathf.Lerp(-half+radius,half-radius,i/4f));
            for(int i=band.Length-1;i>=0;i--)list.Add(half-radius*band[i]);
            return list;
        }

        // 32x32 point-filtered blocky noise; brightness steps of roughly +/-10% like the reference sand.
        static Texture2D PixelSand()
        {
            if(pixelSand)return pixelSand;
            pixelSand=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Pixel sand",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            var random=new System.Random(1234);var pixels=new Color32[32*32];
            for(int i=0;i<pixels.Length;i++)
            {
                int roll=random.Next(100);
                // Shader maps r through (r-.75)*2.5+.75: .72 -> darker grain, .80 -> base, .86 -> lighter grain.
                byte r=(byte)((roll<24?.72f:roll<78?.80f:.86f)*255);
                pixels[i]=new Color32(r,r,r,255);
            }
            pixelSand.SetPixels32(pixels);pixelSand.Apply(false,true);
            return pixelSand;
        }
        void OnDestroy(){if(initialized && --users==0){Destroy(frameMesh);Destroy(sandMesh);if(halfMesh)Destroy(halfMesh);if(pixelSand)Destroy(pixelSand);}}
    }
}
