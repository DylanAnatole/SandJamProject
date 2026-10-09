using System;
using System.Collections.Generic;
using UnityEngine;

namespace SandJamTest.Scene3D
{
    // One reusable stream per occupied slot. No separate projectile per ammo tick.
    public sealed class ProjectileManager
    {
        readonly Transform root;
        readonly Material[] materials;
        readonly int[] colorIds;
        readonly Dictionary<int, Stream> streams = new Dictionary<int, Stream>();
        sealed class Stream { public LineRenderer Line, Shot; public SceneActorView Actor; public SceneRegionView Region; public Vector3 From, To; public float Remaining, Phase; public MaterialPropertyBlock Properties=new MaterialPropertyBlock(); }
        public ProjectileManager(Transform root, GameObject prefab, Material[] materials, int[] colorIds)
        {
            this.root=root;this.colorIds=colorIds;this.materials=new Material[materials.Length];
            var shader=Shader.Find("SandJamTest/SandStream");
            for(int i=0;i<materials.Length;i++)this.materials[i]=new Material(shader){color=materials[i].color};
        }
        public void Dispose(){foreach(var material in materials)UnityEngine.Object.Destroy(material);}
        public void Reset()
        {
            foreach(var stream in streams.Values) { stream.Remaining=0;stream.Line.enabled=false;stream.Shot.enabled=false; }
        }
        public void Spawn(SceneActorView actor, SceneRegionView region, int color, int slot=0)
        {
            Stream stream;
            if(!streams.TryGetValue(slot,out stream))
            {
                var obj=new GameObject("Sand stream slot "+(slot+1));obj.transform.SetParent(root,false);
                var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=3;
                line.startWidth=.028f;line.endWidth=.02f;line.numCapVertices=0;line.sortingOrder=2;
                line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                var shotObject=new GameObject("Sand shot slot "+(slot+1));shotObject.transform.SetParent(root,false);
                var shot=shotObject.AddComponent<LineRenderer>();shot.useWorldSpace=true;shot.positionCount=3;
                shot.startWidth=.035f;shot.endWidth=.018f;shot.numCapVertices=2;shot.sortingOrder=2;
                shot.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;shot.receiveShadows=false;
                stream=new Stream{Line=line,Shot=shot,Phase=slot*1.73f};streams.Add(slot,stream);
            }
            stream.Actor=actor;stream.Region=region;
            // Local falling sand remains clipped; the transfer beam travels from the actual shooter.
            stream.From=region.Target.position;
            stream.To=stream.From+region.Board.transform.TransformVector(Vector3.down*SandBoardTextureView.CellSize*5);
            // Padlocks have no sand mask; their stream is not clipped.
            stream.Properties.SetFloat("_ClipToRegion",region.FlowMask?1:0);
            if(region.FlowMask)stream.Properties.SetTexture("_RegionMask",region.FlowMask);
            stream.Properties.SetMatrix("_WorldToBoard",region.Board.transform.worldToLocalMatrix);
            stream.Properties.SetVector("_BoardSize",new Vector4(region.Board.Width*SandBoardTextureView.CellSize,region.Board.Height*SandBoardTextureView.CellSize,0,0));
            stream.Line.SetPropertyBlock(stream.Properties);stream.Remaining=.13f;
            stream.Line.sharedMaterial=materials[Array.IndexOf(colorIds,color)];stream.Line.enabled=true;
            stream.Shot.sharedMaterial=stream.Line.sharedMaterial;stream.Shot.enabled=true;
            Draw(stream);
        }
        static void Draw(Stream stream)
        {
            var from=stream.Actor.AimPoint;var to=stream.Region.Target.position;
            // The pour mouth sweeps along the region, so the falling segment follows it.
            var drop=stream.To-stream.From;stream.From=to;stream.To=to+drop;
            stream.Shot.SetPosition(0,from);stream.Shot.SetPosition(1,Vector3.Lerp(from,to,.5f));stream.Shot.SetPosition(2,to);
            stream.Line.SetPosition(0,stream.From);
            stream.Line.SetPosition(1,Vector3.Lerp(stream.From,stream.To,.5f));
            stream.Line.SetPosition(2,stream.To);
        }
        public void Advance(float delta)
        {
            foreach(var stream in streams.Values)
            {
                stream.Remaining-=delta;
                if(stream.Remaining<=0){stream.Line.enabled=false;stream.Shot.enabled=false;continue;}
                stream.Phase+=delta*30;Draw(stream);
            }
        }
    }
}
