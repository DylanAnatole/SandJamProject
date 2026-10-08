using UnityEngine;
using Unity.Collections;

namespace SandJamTest.Scene3D
{
    // One shared texture and sprite for the complete board (all five regions).
    public sealed class SandBoardTextureView : MonoBehaviour
    {
        public const float CellSize=.075f;
        public static Color ObstacleTint = new Color(.16f,.27f,.34f);
        public Texture2D Texture { get; private set; }
        public SpriteRenderer Renderer { get; private set; }
        public int UploadCount { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        NativeArray<Color32> background, pixels;
        Sprite sprite;
        Material material;
        bool dirty;
        int lastUploadFrame=-1;

        public void Initialize(LevelData level, Transform coordinates)
        {
            Width=level.columnCount; Height=level.rowCount;
            background=new NativeArray<Color32>(Width*Height,Allocator.Persistent); pixels=new NativeArray<Color32>(background.Length,Allocator.Persistent);
            Texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false) {name="Sand grid - one pixel per cell",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,anisoLevel=0};
            sprite=Sprite.Create(Texture,new Rect(0,0,Width,Height),Vector2.zero,1f/CellSize,0,SpriteMeshType.FullRect);
            sprite.name="Sand board single quad";
            Renderer=gameObject.AddComponent<SpriteRenderer>(); Renderer.sprite=sprite;
            material=new Material(Shader.Find("SandJamTest/PixelSandSprite")); material.SetFloat("_BoardSurface",1); Renderer.sharedMaterial=material;
            transform.SetParent(coordinates,false); transform.localPosition=new Vector3(-3.15f,.4f,-.02f);
            // Include obstacles in the same texture so the board needs only one renderer.
            var wall=(Color32)ObstacleTint;
            for(int i=0;i<level.opr.Length;i++) SetBase(level.opc[i],level.opr[i],wall);
        }
        int Index(int x,int y) { return y*Width+x; }
        public Color32 ReadPixel(int x,int y) { return pixels[Index(x,y)]; }
        public void SetBase(int x,int y,Color32 color)
        {
            int i=Index(x,y); background[i]=color; Set(i,color);
        }
        public void SetGrain(int x,int y,Color32 color) { Set(Index(x,y),color); }
        public void ClearGrain(int x,int y) { int i=Index(x,y); Set(i,background[i]); }
        void Set(int i,Color32 color)
        {
            var old=pixels[i];
            if(old.r==color.r && old.g==color.g && old.b==color.b && old.a==color.a) return;
            pixels[i]=color; dirty=true;
        }
        void LateUpdate()
        {
            if(!dirty || lastUploadFrame==Time.frameCount) return;
            Texture.SetPixelData(pixels,0); Texture.Apply(false,false);
            dirty=false; lastUploadFrame=Time.frameCount; UploadCount++;
        }
        void OnDestroy()
        {
            if(background.IsCreated)background.Dispose();if(pixels.IsCreated)pixels.Dispose();
            if(sprite) Destroy(sprite); if(Texture) Destroy(Texture); if(material) Destroy(material);
        }
    }
}

