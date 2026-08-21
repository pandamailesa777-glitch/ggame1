using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class CombatVfxPool : MonoBehaviour
    {
        private sealed class Item { public GameObject go; public SpriteRenderer renderer; public float age,duration,start,end; public Color color; public bool attack; public Vector3 baseScale; }
        private static CombatVfxPool instance; private readonly List<Item> items=new List<Item>(72); private readonly List<ParticleSystem> particleSystems=new List<ParticleSystem>(18); private Sprite ring,slash; private Camera camera;

        public static void SpawnRing(Vector3 position,Color color,float radius,Camera camera,float duration=.42f)
        {
            if(instance==null){var go=new GameObject("CombatVfxPool");instance=go.AddComponent<CombatVfxPool>();instance.Initialize(camera);}
            instance.Emit(position,color,radius,duration);
        }
        public static void SpawnAttack(Vector3 position,Vector3 direction,Color color,int style,Camera camera)
        {
            if(instance==null){var go=new GameObject("CombatVfxPool");instance=go.AddComponent<CombatVfxPool>();instance.Initialize(camera);}
            instance.EmitAttack(position,direction,color,style);
        }
        public static void SpawnParticles(Vector3 position,Color color,int style,Camera camera)
        {
            if(instance==null){var go=new GameObject("CombatVfxPool");instance=go.AddComponent<CombatVfxPool>();instance.Initialize(camera);}
            instance.EmitParticles(position,color,style);
        }
        private void Initialize(Camera target){camera=target;ring=CreateRing();slash=CreateSlash();for(int i=0;i<72;i++){var go=new GameObject("Vfx_"+i);var r=go.AddComponent<SpriteRenderer>();r.sprite=ring;r.sortingOrder=20;go.SetActive(false);items.Add(new Item{go=go,renderer=r});}}
        private Item Take(){foreach(var candidate in items)if(!candidate.go.activeSelf)return candidate;return items[0];}
        private void Emit(Vector3 position,Color color,float radius,float duration)
        {
            Item item=Take();item.attack=false;item.renderer.sprite=ring;
            item.go.transform.position=position+Vector3.up*.08f;item.go.transform.rotation=camera!=null?camera.transform.rotation:Quaternion.identity;item.age=0;item.duration=Mathf.Max(duration,.52f);item.start=.15f;item.end=radius;item.color=color;item.renderer.color=color;item.go.SetActive(true);
        }
        private void EmitAttack(Vector3 position,Vector3 direction,Color color,int style)
        {
            Item item=Take();item.attack=true;item.renderer.sprite=slash;item.age=0;item.duration=style==2?.18f:.24f;item.color=color;
            item.go.transform.position=position+Vector3.up*.42f;float angle=Mathf.Atan2(direction.z,direction.x)*Mathf.Rad2Deg;item.go.transform.rotation=(camera!=null?camera.transform.rotation:Quaternion.identity)*Quaternion.Euler(0,0,angle-25);
            item.baseScale=style==0?new Vector3(1.15f,.72f,1):style==1?new Vector3(1.45f,.42f,1):new Vector3(1.65f,.62f,1);item.renderer.color=color;item.go.SetActive(true);
        }
        private void EmitParticles(Vector3 position,Color color,int style)
        {
            ParticleSystem ps=null;foreach(var candidate in particleSystems)if(!candidate.isPlaying){ps=candidate;break;}if(ps==null&&particleSystems.Count<18){var go=new GameObject("AbilityParticles_"+particleSystems.Count);ps=go.AddComponent<ParticleSystem>();var renderer=go.GetComponent<ParticleSystemRenderer>();var shader=Shader.Find("Particles/Standard Unlit")??Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply")??Shader.Find("Sprites/Default");if(shader!=null)renderer.material=new Material(shader);renderer.sortingOrder=25;particleSystems.Add(ps);}if(ps==null)ps=particleSystems[0];
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.transform.position=position+Vector3.up*.35f;var main=ps.main;main.loop=false;main.duration=.45f;main.startLifetime=style==2?.34f:.52f;main.startSpeed=style==0?2.1f:style==1?1.45f:3.4f;main.startSize=style==2?.12f:.18f;main.startColor=color;main.maxParticles=48;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=ps.emission;emission.enabled=true;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)(style==2?34:24))});var shape=ps.shape;shape.enabled=true;shape.shapeType=style==1?ParticleSystemShapeType.Circle:ParticleSystemShapeType.Sphere;shape.radius=style==0?.55f:style==1?.8f:.35f;var velocity=ps.velocityOverLifetime;velocity.enabled=style==2;velocity.orbitalY=style==2?4f:0;var colorLife=ps.colorOverLifetime;colorLife.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(color,0),new GradientColorKey(Color.white,.55f),new GradientColorKey(color,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.75f,.55f),new GradientAlphaKey(0,1)});colorLife.color=gradient;ps.Play();
        }
        private void Update(){foreach(var item in items){if(!item.go.activeSelf)continue;item.age+=Time.deltaTime;float t=Mathf.Clamp01(item.age/item.duration);if(item.attack){float pop=Mathf.Sin(t*Mathf.PI);item.go.transform.localScale=item.baseScale*(.72f+pop*.48f);item.renderer.color=new Color(item.color.r,item.color.g,item.color.b,(1-t)*.95f);}else{float s=Mathf.Lerp(item.start,item.end,1-Mathf.Pow(1-t,3));item.go.transform.localScale=Vector3.one*s;item.renderer.color=new Color(item.color.r,item.color.g,item.color.b,(1-t)*.82f);}if(t>=1)item.go.SetActive(false);}}
        private static Sprite CreateRing(){const int size=96;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=(x+0.5f-size*.5f)/(size*.5f),dy=(y+0.5f-size*.5f)/(size*.5f),d=Mathf.Sqrt(dx*dx+dy*dy);float rim=Mathf.Clamp01(1-Mathf.Abs(d-.75f)/.17f);float core=d<.68f?.12f*(1-d/.68f):0;pixels[y*size+x]=new Color(1,1,1,Mathf.Max(rim,core));}texture.SetPixels(pixels);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size,0,SpriteMeshType.FullRect);}
        private static Sprite CreateSlash(){const int size=96;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float nx=(x+.5f-size*.5f)/(size*.5f),ny=(y+.5f-size*.5f)/(size*.5f);float r=Mathf.Sqrt(nx*nx+ny*ny),a=Mathf.Atan2(ny,nx)*Mathf.Rad2Deg;float arc=Mathf.Clamp01(1-Mathf.Abs(r-.68f)/.13f);float sector=Mathf.SmoothStep(0,1,Mathf.Clamp01((72-Mathf.Abs(a))/18));pixels[y*size+x]=new Color(1,1,1,arc*sector);}texture.SetPixels(pixels);texture.Apply(false,true);return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size,0,SpriteMeshType.FullRect);}
    }
}
