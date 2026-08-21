using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class RuntimeSpriteFactory
    {
        private sealed class Definition
        {
            public string resource; public int columns, rows, entityRow; public float fps, ppu, scale=1, width=1; public int[] map, mirrors; public Color fallback;
        }
        private readonly Dictionary<string, Definition> definitions = new Dictionary<string, Definition>();
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly Camera camera;

        public RuntimeSpriteFactory(Camera camera)
        {
            this.camera = camera;
            // Equal apparent height; the shared 1.30 width multiplier is an intentional art-direction choice.
            Register("hero_amelia","Art/Generated/hero_amelia_canonical__move__8dir__7fps__loop",6,8,-1,7,72,1.48f,new Color(1,.9f,.65f),1.30f);
            Register("hero_sam","Art/Generated/hero_sam_canonical__move__8dir__7fps__loop",6,8,-1,7,72,1.50f,new Color(.55f,.08f,.18f),1.30f);
            Register("hero_zike","Art/Generated/hero_zike_canonical__move__8dir__7fps__loop",6,8,-1,7,72,1.74f,new Color(.2f,.75f,1),1.30f);
            Register("enemy_zombie", "Art/enemy_zombie_move_8dir", 6, 8, -1, 7, 72, 1, new Color(.35f,.48f,.3f));
            foreach (var e in GameCatalog.Enemies) if (!definitions.ContainsKey(e.spriteId)) RegisterFallback(e.spriteId,e.fallback,e.scale);
            foreach (var b in GameCatalog.Bosses) RegisterFallback(b.spriteId,b.fallback,b.scale);
        }

        private void Register(string id,string resource,int columns,int rows,int entityRow,float fps,float ppu,float scale,Color fallback,float width=1f)
        {
            definitions[id]=new Definition{resource=resource,columns=columns,rows=rows,entityRow=entityRow,fps=fps,ppu=ppu,scale=scale,width=width,fallback=fallback,
                // Old hero strip: E, NE, N, NW, W, SW, S, SE.
                map=entityRow>=0?new[]{2,2,3,4,5,0,1,0}:null,mirrors=null};
        }
        private void RegisterFallback(string id,Color color,float scale) => definitions[id]=new Definition{fallback=color,scale=scale,entityRow=-1};

        public DirectionalSpriteVisual Bind(GameObject target,string id)
        {
            var visual=target.GetComponent<DirectionalSpriteVisual>() ?? target.AddComponent<DirectionalSpriteVisual>();
            if (!definitions.TryGetValue(id,out var d)) { visual.Configure(GetFallback("missing",Color.magenta),1,1,1,32,camera); return visual; }
            Texture2D texture=null;
            if (!string.IsNullOrEmpty(d.resource))
            {
                if (!textures.TryGetValue(d.resource,out texture)) { texture=Resources.Load<Texture2D>(d.resource); textures[d.resource]=texture; }
            }
            if(texture==null&&d.entityRow<0)
            {
                string generated="Art/Generated/"+id+"_move_8dir";texture=Resources.Load<Texture2D>(generated);
                if(texture==null){generated="Art/Generated/"+id+"_idle_8dir";texture=Resources.Load<Texture2D>(generated);}
                if(texture!=null){d.columns=Mathf.Max(1,texture.width/(texture.height/8));d.rows=8;d.fps=d.columns>1?7:1;d.ppu=72;}
            }
            if (texture==null){visual.Configure(GetFallback(id,d.fallback),1,1,1,32,camera);Debug.LogWarning("SPRITE_FACTORY_FALLBACK "+id);}
            else if(d.entityRow>=0) visual.ConfigureMappedRow(texture,d.columns,d.rows,d.entityRow,d.map,d.mirrors,d.ppu,camera);
            else{visual.Configure(texture,d.columns,d.rows,d.fps,d.ppu,camera);Debug.Log("SPRITE_FACTORY_BOUND "+id+" "+texture.name+" "+d.columns+"x"+d.rows);}
            if(texture!=null&&d.entityRow<0)AddOptionalClips(visual,id,d.ppu);
            visual.SetScale(d.scale,d.width);
            return visual;
        }

        private void AddOptionalClips(DirectionalSpriteVisual visual,string id,float ppu)
        {
            AddOptionalClip(visual,id,"attack",14,ppu,false);AddOptionalClip(visual,id,"hit",12,ppu,false);AddOptionalClip(visual,id,"death",9,ppu,false);
            AddOptionalClip(visual,id,"cast",10,ppu,false);AddOptionalClip(visual,id,"dash",14,ppu,false);
        }
        private void AddOptionalClip(DirectionalSpriteVisual visual,string id,string clip,float fps,float ppu,bool loop)
        {
            string resource="Art/Generated/"+id+"_"+clip+"_8dir";if(!textures.TryGetValue(resource,out var texture)){texture=Resources.Load<Texture2D>(resource);textures[resource]=texture;}
            if(texture==null)return;int rows=8,cell=texture.height/rows,columns=Mathf.Max(1,texture.width/cell);visual.AddClip(clip,texture,columns,rows,fps,ppu,loop);
        }

        private Texture2D GetFallback(string id,Color color)
        {
            string key="fallback:"+id;
            if(textures.TryGetValue(key,out var texture))return texture;
            texture=new Texture2D(32,48,TextureFormat.RGBA32,false){name=key,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[32*48];
            for(int y=0;y<48;y++)for(int x=0;x<32;x++)
            {
                float nx=(x-15.5f)/15.5f,ny=(y-23.5f)/23.5f;
                bool body=nx*nx+ny*ny<.72f||y<13&&Mathf.Abs(nx)<.32f;
                pixels[y*32+x]=body?color:Color.clear;
            }
            texture.SetPixels(pixels);texture.Apply(false,true);textures[key]=texture;return texture;
        }
    }
}
