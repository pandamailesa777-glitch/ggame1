using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    // Mobile-safe ability VFX: pooled sprites and LineRenderers, no incompatible particle shaders.
    public sealed class AbilityVfxController : MonoBehaviour
    {
        private sealed class SpriteFx { public GameObject go; public SpriteRenderer renderer; public float age,duration,start,end,spin; public Color color; public bool ground; }
        private sealed class LineFx { public GameObject go; public LineRenderer line; public float age,duration; public Color color; }
        private static AbilityVfxController instance;
        private readonly List<SpriteFx> sprites=new List<SpriteFx>(64);
        private readonly List<LineFx> lines=new List<LineFx>(32);
        private Sprite sigil,slash,shield;
        private Camera camera;

        private static AbilityVfxController Get(Camera camera)
        {
            if(instance==null){var go=new GameObject("AbilityVfxController");instance=go.AddComponent<AbilityVfxController>();instance.Initialize(camera);}
            if(camera!=null)instance.camera=camera;return instance;
        }

        public static void SpawnSigil(Vector3 position,Color color,float radius,int style,Camera camera)
        {
            var v=Get(camera);v.EmitSprite(position,color,radius,.78f,true,style==0?85:style==1?-65:130,v.sigil);
            v.EmitSprite(position+Vector3.up*.04f,Color.Lerp(color,Color.white,.65f),radius*.62f,.58f,true,style==1?110:-100,v.sigil);
        }

        public static void SpawnShield(Vector3 position,Color color,float radius,Camera camera)
        {
            var v=Get(camera);v.EmitSprite(position+Vector3.up*.72f,color,radius,.95f,false,45,v.shield);
            v.EmitSprite(position+Vector3.up*.72f,Color.white,radius*.72f,.70f,false,-70,v.shield);
        }

        public static void SpawnWhip(Vector3 from,Vector3 to,Color color,Camera camera)=>Get(camera).EmitJagged(from,to,color,.18f,.34f,5,.20f);
        public static void SpawnBeam(Vector3 from,Vector3 to,Color color,Camera camera)
        {
            var v=Get(camera);v.EmitJagged(from,to,new Color(.12f,.01f,.02f,1),.30f,.42f,1,0);v.EmitJagged(from,to,color,.13f,.36f,1,0);
        }
        public static void SpawnLightning(Vector3 from,Vector3 to,Color color,Camera camera)
        {
            var v=Get(camera);v.EmitJagged(from,to,Color.white,.09f,.26f,9,.28f);v.EmitJagged(from,to,color,.20f,.34f,8,.34f);
        }
        public static void SpawnCrossSlash(Vector3 position,Color color,float radius,Camera camera)
        {
            var v=Get(camera);var a=v.EmitSprite(position+Vector3.up*.38f,color,radius,.38f,false,0,v.slash);a.go.transform.Rotate(0,0,42);
            var b=v.EmitSprite(position+Vector3.up*.38f,Color.white,radius*.88f,.32f,false,0,v.slash);b.go.transform.Rotate(0,0,-48);
        }

        public static void SpawnRuneBloom(Vector3 position,Color color,float radius,Camera camera)
        {
            var v=Get(camera);v.EmitSprite(position,color,radius*.72f,.72f,true,95,v.sigil);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Vector3 p=position+new Vector3(Mathf.Cos(a),.08f,Mathf.Sin(a))*radius*.62f;v.EmitSprite(p,Color.Lerp(color,Color.white,.35f),radius*.28f,.52f,true,i%2==0?150:-150,v.sigil);}
        }

        public static void SpawnBladeWheel(Vector3 position,Color color,float radius,Camera camera)
        {
            var v=Get(camera);for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Vector3 p=position+new Vector3(Mathf.Cos(a),.38f,Mathf.Sin(a))*radius*.42f;var fx=v.EmitSprite(p,i%2==0?color:Color.Lerp(color,Color.white,.55f),radius*.72f,.46f,false,i%2==0?210:-210,v.slash);fx.go.transform.Rotate(0,0,-a*Mathf.Rad2Deg);}
        }

        public static void SpawnImpactBurst(Vector3 position,Color color,float radius,Camera camera)
        {
            var v=Get(camera);v.EmitSprite(position+Vector3.up*.45f,color,radius,.42f,false,180,v.shield);
            for(int i=0;i<4;i++){var fx=v.EmitSprite(position+Vector3.up*.38f,i%2==0?Color.white:color,radius*(1-i*.08f),.34f,false,i%2==0?80:-80,v.slash);fx.go.transform.Rotate(0,0,i*45);}
        }

        public static void SpawnCastFlash(Vector3 position,Color color,float radius,Camera camera)
        {var v=Get(camera);v.EmitSprite(position+Vector3.up*.48f,Color.Lerp(color,Color.white,.55f),radius,.22f,false,110,v.shield);}

        public static void SpawnProjectileImpact(Vector3 position,Color color,int style,float radius,Camera camera)
        {var v=Get(camera);if(style==2){for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;v.EmitJagged(position,position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius,color,.055f,.18f,3,.08f);}}else{var fx=v.EmitSprite(position+Vector3.up*.28f,style==1?Color.Lerp(color,Color.black,.25f):Color.Lerp(color,Color.white,.55f),radius,.22f,false,style==1?-220:180,v.slash);fx.go.transform.Rotate(0,0,style==1?35:0);}}

        public static void SpawnHealingRise(Vector3 position,Color color,float radius,Camera camera)
        {var v=Get(camera);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Vector3 p=position+new Vector3(Mathf.Cos(a),.18f,Mathf.Sin(a))*radius*.34f;v.EmitJagged(p,p+Vector3.up*(.65f+radius*.16f),color,.045f,.62f,2,.03f);}}

        public static void SpawnArrowVolleyCue(Vector3 position,Vector3 forward,Color color,int count,Camera camera)
        {var v=Get(camera);Vector3 side=Vector3.Cross(Vector3.up,forward.normalized);int marks=Mathf.Clamp(count,3,7);for(int i=0;i<marks;i++){float t=marks==1?0:i/(float)(marks-1)-.5f;Vector3 p=position+side*t*1.15f+Vector3.up*.35f;v.EmitJagged(p,p+forward.normalized*.72f,Color.Lerp(color,Color.white,.45f),.045f,.2f,1,0);}}

        public static void SpawnStaffSweep(Vector3 position,Color color,float radius,Camera camera)
        {var v=Get(camera);var fx=v.EmitSprite(position+Vector3.up*.32f,color,radius*1.08f,.38f,false,420,v.slash);fx.go.transform.Rotate(0,0,-18);v.EmitSprite(position,color,radius,.44f,true,-180,v.sigil);}

        public static void SpawnDeathWave(Vector3 from,Vector3 to,Color color,Camera camera)
        {var v=Get(camera);Vector3 direction=(to-from).normalized,side=Vector3.Cross(Vector3.up,direction);for(int i=-1;i<=1;i++)v.EmitJagged(from+side*i*.13f,to+side*i*.34f,i==0?color:new Color(.16f,.01f,.04f,1),i==0?.12f:.07f,.38f,3,.08f);}

        public static void SpawnSoulDrain(Vector3 from,Vector3 to,Color color,float radius,Camera camera)
        {var v=Get(camera);for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Vector3 edge=from+new Vector3(Mathf.Cos(a),.12f,Mathf.Sin(a))*radius*.62f;v.EmitJagged(edge,to+Vector3.up*.48f,color,.055f,.52f,5,.16f);}}

        public static void SpawnSoulHarvest(Vector3 position,Color color,float radius,Camera camera)
        {var v=Get(camera);v.EmitSprite(position,color,radius,.68f,true,-210,v.sigil);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Vector3 edge=position+new Vector3(Mathf.Cos(a),.12f,Mathf.Sin(a))*radius*.72f;v.EmitJagged(edge,position+Vector3.up*.55f,i%2==0?color:new Color(.18f,0,.04f,1),.07f,.55f,4,.18f);}}

        public static void SpawnFuneralVolleyCue(Vector3 position,Vector3 forward,Color color,int count,Camera camera)
        {var v=Get(camera);Vector3 side=Vector3.Cross(Vector3.up,forward.normalized);for(int i=0;i<Mathf.Clamp(count,3,7);i++){float t=i/(float)(Mathf.Clamp(count,3,7)-1)-.5f;v.EmitJagged(position+side*t*.9f+Vector3.up*.4f,position+side*t*1.15f+forward.normalized*.62f+Vector3.up*.4f,color,.075f,.24f,2,.04f);}}

        public static void SpawnStormTrail(Vector3 position,Vector3 backward,Color color,float radius,Camera camera)
        {var v=Get(camera);Vector3 end=position+backward.normalized*radius;v.EmitJagged(position+Vector3.up*.12f,end,color,.12f,.52f,7,.22f);v.EmitJagged(position+Vector3.up*.18f,end*.15f+position*.85f,Color.white,.045f,.28f,4,.12f);}

        public static void SpawnDashPath(Vector3 from,Vector3 to,Color color,Camera camera)
        {var v=Get(camera);Vector3 side=Vector3.Cross(Vector3.up,(to-from).normalized);v.EmitJagged(from+Vector3.up*.32f,to+Vector3.up*.32f,color,.18f,.34f,8,.18f);v.EmitJagged(from+side*.13f+Vector3.up*.42f,to-side*.13f+Vector3.up*.42f,Color.white,.055f,.25f,6,.10f);v.EmitSprite(from+Vector3.up*.42f,new Color(color.r,color.g,color.b,.55f),1.15f,.30f,false,-90,v.slash);v.EmitSprite(to+Vector3.up*.42f,Color.Lerp(color,Color.white,.4f),1.3f,.34f,false,130,v.slash);}

        public static void SpawnKatanaSwing(Vector3 position,Vector3 direction,Color color,float width,Camera camera)
        {var v=Get(camera);float angle=Mathf.Atan2(direction.z,direction.x)*Mathf.Rad2Deg;var outer=v.EmitSprite(position+direction.normalized*.58f+Vector3.up*.42f,color,width,.28f,false,0,v.slash);outer.go.transform.Rotate(0,0,angle-32);outer.go.transform.localScale=new Vector3(width,width*.72f,1);var edge=v.EmitSprite(position+direction.normalized*.72f+Vector3.up*.46f,Color.white,width*.82f,.20f,false,0,v.slash);edge.go.transform.Rotate(0,0,angle-22);edge.go.transform.localScale=new Vector3(width*.88f,width*.52f,1);}

        public static void SpawnLightningFan(Vector3 position,int count,Color color,float radius,Camera camera)
        {var v=Get(camera);int rays=Mathf.Clamp(count,5,12);for(int i=0;i<rays;i++){float a=i*Mathf.PI*2/rays;Vector3 end=position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;v.EmitJagged(position,end,i%2==0?Color.white:color,.075f,.28f,5,.16f);}}

        private void Initialize(Camera target)
        {
            camera=target;sigil=CreateSigil();slash=CreateSlash();shield=CreateShield();
            for(int i=0;i<64;i++){var go=new GameObject("AbilitySprite_"+i);var r=go.AddComponent<SpriteRenderer>();r.sortingOrder=35;go.SetActive(false);sprites.Add(new SpriteFx{go=go,renderer=r});}
            var shader=Shader.Find("Sprites/Default");
            for(int i=0;i<32;i++){var go=new GameObject("AbilityLine_"+i);var l=go.AddComponent<LineRenderer>();l.useWorldSpace=true;l.numCapVertices=3;l.numCornerVertices=2;l.sortingOrder=38;if(shader!=null)l.material=new Material(shader);go.SetActive(false);lines.Add(new LineFx{go=go,line=l});}
        }

        private SpriteFx EmitSprite(Vector3 position,Color color,float radius,float duration,bool ground,float spin,Sprite image)
        {
            SpriteFx fx=null;foreach(var f in sprites)if(!f.go.activeSelf){fx=f;break;}if(fx==null)fx=sprites[0];
            fx.renderer.sprite=image;fx.renderer.color=color;fx.go.transform.position=position;fx.go.transform.rotation=ground?Quaternion.Euler(90,0,0):(camera!=null?camera.transform.rotation:Quaternion.identity);
            fx.go.transform.localScale=Vector3.one*.15f;fx.age=0;fx.duration=duration;fx.start=.15f;fx.end=radius;fx.spin=spin;fx.color=color;fx.ground=ground;fx.go.SetActive(true);return fx;
        }

        private void EmitJagged(Vector3 from,Vector3 to,Color color,float width,float duration,int segments,float jitter)
        {
            LineFx fx=null;foreach(var f in lines)if(!f.go.activeSelf){fx=f;break;}if(fx==null)fx=lines[0];
            int count=Mathf.Max(2,segments+1);fx.line.positionCount=count;Vector3 delta=to-from;Vector3 side=Vector3.Cross(Vector3.up,delta.normalized);
            for(int i=0;i<count;i++){float t=i/(float)(count-1);float noise=(i==0||i==count-1)?0:Random.Range(-jitter,jitter);fx.line.SetPosition(i,Vector3.Lerp(from,to,t)+Vector3.up*(.30f+Mathf.Sin(t*Mathf.PI)*.12f)+side*noise);}
            fx.line.startWidth=width;fx.line.endWidth=width*.42f;fx.line.startColor=color;fx.line.endColor=new Color(color.r,color.g,color.b,.35f);fx.color=color;fx.age=0;fx.duration=duration;fx.go.SetActive(true);
        }

        private void Update()
        {
            float dt=Time.deltaTime;
            foreach(var fx in sprites){if(!fx.go.activeSelf)continue;fx.age+=dt;float t=Mathf.Clamp01(fx.age/fx.duration),pop=Mathf.Sin(Mathf.Min(1,t*1.35f)*Mathf.PI*.5f);float scale=Mathf.Lerp(fx.start,fx.end,pop);fx.go.transform.localScale=Vector3.one*scale;fx.go.transform.Rotate(fx.ground?Vector3.forward:Vector3.forward,fx.spin*dt,Space.Self);float alpha=(1-t)*Mathf.Min(1,t*7);fx.renderer.color=new Color(fx.color.r,fx.color.g,fx.color.b,alpha);if(t>=1)fx.go.SetActive(false);}
            foreach(var fx in lines){if(!fx.go.activeSelf)continue;fx.age+=dt;float t=Mathf.Clamp01(fx.age/fx.duration),a=(1-t)*Mathf.Min(1,t*10);fx.line.startColor=new Color(fx.color.r,fx.color.g,fx.color.b,a);fx.line.endColor=new Color(fx.color.r,fx.color.g,fx.color.b,a*.28f);fx.line.widthMultiplier=1+Mathf.Sin(t*Mathf.PI)*.45f;if(t>=1)fx.go.SetActive(false);}
        }

        private static Sprite CreateSigil()
        {
            const int s=128;var t=new Texture2D(s,s,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var p=new Color[s*s];
            for(int y=0;y<s;y++)for(int x=0;x<s;x++){float nx=(x+.5f-s*.5f)/(s*.5f),ny=(y+.5f-s*.5f)/(s*.5f),r=Mathf.Sqrt(nx*nx+ny*ny),a=Mathf.Atan2(ny,nx);float rings=Mathf.Max(Band(r,.83f,.035f),Band(r,.55f,.025f));float spokes=Band(Mathf.Abs(Mathf.Sin(a*6)),0,.055f)*Mathf.Clamp01((r-.30f)*8)*Mathf.Clamp01((.78f-r)*8);float diamond=Band(Mathf.Abs(nx)+Mathf.Abs(ny),.36f,.025f);p[y*s+x]=new Color(1,1,1,Mathf.Max(rings,Mathf.Max(spokes,diamond)));}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),s);
        }
        private static Sprite CreateSlash(){const int s=128;var t=new Texture2D(s,s,TextureFormat.RGBA32,false);var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float nx=(x+.5f-s*.5f)/(s*.5f),ny=(y+.5f-s*.5f)/(s*.5f);float line=Mathf.Clamp01(1-Mathf.Abs(ny-nx*.12f)/.075f)*Mathf.Clamp01(1-Mathf.Abs(nx));p[y*s+x]=new Color(1,1,1,line);}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),s);}
        private static Sprite CreateShield(){const int s=128;var t=new Texture2D(s,s,TextureFormat.RGBA32,false);var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float nx=(x+.5f-s*.5f)/(s*.5f),ny=(y+.5f-s*.5f)/(s*.5f),r=Mathf.Sqrt(nx*nx+ny*ny);float rim=Band(r,.76f,.08f),fill=r<.72f?.12f*(1-r/.72f):0;p[y*s+x]=new Color(1,1,1,Mathf.Max(rim,fill));}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),s);}
        private static float Band(float value,float center,float width)=>Mathf.Clamp01(1-Mathf.Abs(value-center)/width);
    }
}
