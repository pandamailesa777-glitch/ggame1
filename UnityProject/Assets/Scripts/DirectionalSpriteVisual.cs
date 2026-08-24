using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DirectionalSpriteVisual : MonoBehaviour
    {
        private sealed class Clip { public Sprite[,] frames; public int frameCount; public float fps; public bool loop; }
        private readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
        private string activeClip="move",fallbackClip="move"; private bool activeLoop=true;
        private SpriteRenderer spriteRenderer;
        private Sprite[,] frames;
        private int directions;
        private int frameCount;
        private float fps;
        private float clock;
        private Vector2 facing = Vector2.down;
        private Camera targetCamera;
        private bool[] flipDirections;
        private Vector3 baseScale = Vector3.one;
        private float proceduralTimer;
        private string proceduralAction;
        private bool moving=true,proceduralLocomotion;
        // Packed rows: E, SE, S, SW, W, NW, N, NE.
        // Runtime angles: E, NE, N, NW, W, SW, S, SE.
        private static readonly int[] PackedRowsForRuntime={0,7,6,5,4,3,2,1};

        public void Configure(Texture2D sheet, int columns, int rows, float animationFps, float pixelsPerUnit, Camera camera)
        {
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();spriteRenderer.sortingOrder=10;spriteRenderer.color=Color.white;
            targetCamera = camera;
            directions = rows;
            frameCount = columns;
            fps = animationFps;
            frames = new Sprite[rows, columns];
            int width = sheet.width / columns;
            int height = sheet.height / rows;
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                int sourceRow=rows==8?PackedRowsForRuntime[row]:row;
                int textureRow = rows - 1 - sourceRow;
                frames[row, col] = Sprite.Create(sheet, new Rect(col * width, textureRow * height, width, height), new Vector2(.5f, .08f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            spriteRenderer.sprite = frames[0, 0];
            clips.Clear();clips["move"]=new Clip{frames=frames,frameCount=frameCount,fps=fps,loop=true};activeClip=fallbackClip="move";activeLoop=true;clock=0;
        }

        public void AddClip(string id,Texture2D sheet,int columns,int rows,float animationFps,float pixelsPerUnit,bool loop)
        {
            if(sheet==null||string.IsNullOrEmpty(id))return;var clipFrames=new Sprite[rows,columns];int width=sheet.width/columns,height=sheet.height/rows;
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){int sourceRow=rows==8?PackedRowsForRuntime[row]:row;clipFrames[row,col]=Sprite.Create(sheet,new Rect(col*width,(rows-1-sourceRow)*height,width,height),new Vector2(.5f,.08f),pixelsPerUnit,0,SpriteMeshType.FullRect);}
            clips[id]=new Clip{frames=clipFrames,frameCount=columns,fps=animationFps,loop=loop};
        }

        public void Play(string id,bool restart=false)
        {
            if(!clips.ContainsKey(id)){proceduralAction=id;proceduralTimer=id=="hit"?.18f:id=="dash"?.28f:.26f;return;}
            if(activeClip!=id||restart){activeClip=id;clock=0;}activeLoop=clips[id].loop;
        }

        public void SetScale(float scale, float widthMultiplier=1f)
        {
            baseScale = new Vector3(scale*widthMultiplier,scale,scale);
            transform.localScale = baseScale;
        }

        public void ConfigureFallback(Color color, Camera camera)
        {
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();spriteRenderer.sortingOrder=10;spriteRenderer.color=Color.white;
            targetCamera = camera;
            directions = 1; frameCount = 1; fps = 1;
            var texture = new Texture2D(32, 48, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                float nx = (x - 15.5f) / 15.5f;
                float ny = (y - 8f) / 40f;
                bool body = ny >= 0 && ny < .7f && Mathf.Abs(nx) < .36f + ny * .14f;
                bool head = (nx * nx + (ny - .78f) * (ny - .78f) * 2.1f) < .12f;
                texture.SetPixel(x, y, body || head ? color : clear);
            }
            texture.filterMode = FilterMode.Point; texture.Apply();
            frames = new Sprite[1, 1];
            frames[0, 0] = Sprite.Create(texture, new Rect(0, 0, 32, 48), new Vector2(.5f, .08f), 32, 0, SpriteMeshType.FullRect);
            spriteRenderer.sprite = frames[0, 0];
            clips.Clear();clips["move"]=new Clip{frames=frames,frameCount=1,fps=1,loop=true};activeClip=fallbackClip="move";
        }

        public void ConfigureMappedRow(Texture2D sheet, int columns, int rows, int entityRow, int[] directionMap, int[] mirroredDirections, float pixelsPerUnit, Camera camera)
        {
            spriteRenderer = gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();spriteRenderer.sortingOrder=10;spriteRenderer.color=Color.white;
            targetCamera = camera;
            directions = directionMap.Length;
            frameCount = 1;
            fps = 1;
            frames = new Sprite[directions, 1];
            flipDirections = new bool[directions];
            int width = sheet.width / columns;
            int height = sheet.height / rows;
            int textureRow = rows - 1 - entityRow;
            for (int direction = 0; direction < directions; direction++)
            {
                int column = directionMap[direction];
                frames[direction, 0] = Sprite.Create(sheet, new Rect(column * width, textureRow * height, width, height), new Vector2(.5f, .08f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            if (mirroredDirections != null)
                foreach (int direction in mirroredDirections)
                    if (direction >= 0 && direction < directions) flipDirections[direction] = true;
            spriteRenderer.sprite = frames[0, 0];
            clips.Clear();clips["move"]=new Clip{frames=frames,frameCount=1,fps=1,loop=true};activeClip=fallbackClip="move";
        }

        public void SetFacing(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude < .001f) return;
            facing = new Vector2(worldDirection.x, worldDirection.z).normalized;
        }
        public void SetMoving(bool value){moving=value;}
        public void SetProceduralLocomotion(bool value){proceduralLocomotion=value;}

        private void LateUpdate()
        {
            if (frames == null) return;
            if(spriteRenderer==null)spriteRenderer=gameObject.GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            clock += Time.deltaTime;
            if(clips.TryGetValue(activeClip,out var clip)){frames=clip.frames;frameCount=clip.frameCount;fps=clip.fps;if(!clip.loop&&clock*fps>=frameCount){activeClip=fallbackClip;clock=0;clip=clips[activeClip];frames=clip.frames;frameCount=clip.frameCount;fps=clip.fps;}}
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            int direction = Mathf.RoundToInt(angle / (360f / directions));
            direction = (direction % directions + directions) % directions;
            bool proceduralAttack=proceduralTimer>0&&(proceduralAction=="attack"||proceduralAction=="cast");
            // A procedural attack must never advance the movement strip. The previous
            // branch deliberately walked through it, so the supposedly fixed body still
            // appeared to spin/bob like a carousel while the weapon attacked.
            int frame = frameCount <= 1||proceduralAttack||(activeClip=="move"&&(!moving||proceduralLocomotion))
                ? 0
                : Mathf.FloorToInt(clock * fps) % frameCount;
            spriteRenderer.sprite = frames[direction, frame];
            spriteRenderer.flipX = flipDirections != null && flipDirections[direction];
            float tilt=0,scalePulse=1,walkStretch=1;
            // Single-frame directional art should still read as grounded locomotion.
            // A subtle alternating step avoids both static gliding and fake frame rotation.
            if(moving&&activeClip=="move"&&(frameCount<=1||proceduralLocomotion)){float step=Mathf.Sin(clock*10.5f);tilt=step*2.2f;walkStretch=1+Mathf.Abs(step)*.035f;}
            if(proceduralTimer>0){proceduralTimer=Mathf.Max(0,proceduralTimer-Time.deltaTime);float t=proceduralTimer/.28f;if(proceduralAction=="attack"||proceduralAction=="cast"){tilt=0;scalePulse=1;}else if(proceduralAction=="hit"){tilt=Mathf.Sin(t*Mathf.PI)*12;scalePulse=.9f;}else if(proceduralAction=="dash")scalePulse=1+Mathf.Sin(t*Mathf.PI)*.16f;}
            if (targetCamera != null) transform.rotation = targetCamera.transform.rotation*Quaternion.Euler(0,0,tilt);
            transform.localScale = new Vector3(baseScale.x/Mathf.Sqrt(walkStretch),baseScale.y*walkStretch,baseScale.z)*scalePulse;
        }
    }
}
