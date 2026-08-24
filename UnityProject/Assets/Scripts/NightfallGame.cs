using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nightfall.UnityMvp
{
    public sealed partial class NightfallGame : MonoBehaviour
    {
        private enum State { Menu, HeroSelect, Playing, Upgrade, PetUnlock, Paused, Dead, Victory }
        private sealed class Enemy
        {
            public GameObject go; public DirectionalSpriteVisual visual; public EnemyDefinition def; public BossDefinition boss;
            public float hp,maxHp,attackClock,abilityClock; public bool active,phaseTwo; public int generation;
            public bool IsBoss => boss != null;
        }
        private sealed class Projectile
        {
            public GameObject go; public Vector3 velocity; public float life,damage,radius,phase; public int hitsRemaining; public bool active,hostile; public Color color; public Enemy lastHit;
        }
        private sealed class Orb { public GameObject go; public bool active; public int value; public float phase; }
        private sealed class LootPickup { public GameObject go; public bool active; public LootKind kind; public float phase; }
        private sealed class Upgrade { public string name,description,icon; public Action apply; public HeroKind? owner; public int ability=-1; }

        private readonly List<Enemy> enemies=new List<Enemy>(220);
        private readonly List<Projectile> projectiles=new List<Projectile>(140);
        private readonly List<Orb> orbs=new List<Orb>(180);
        private readonly List<LootPickup> lootPickups=new List<LootPickup>(32);
        private readonly List<Upgrade> upgrades=new List<Upgrade>(20);
        private readonly Upgrade[] offered=new Upgrade[3];
        private readonly List<Vector3> obstaclePositions=new List<Vector3>(48);private readonly List<float> obstacleRadii=new List<float>(48);
        private readonly int[] abilityRanks=new int[5]; private readonly float[] abilityTimers=new float[5];
        private readonly Dictionary<string,int> passiveRanks=new Dictionary<string,int>();
        private Camera worldCamera; private RuntimeSpriteFactory spriteFactory; private GameAudioController audioController; private Sprite solidSprite;
        private GameObject player; private DirectionalSpriteVisual playerVisual; private CharacterVisualController characterVisual; private HeroDefinition hero;
        private State state=State.Menu; private Vector2 moveInput,joystickOrigin; private int joystickFinger=-1;
        private float hp,maxHp,damage,attackDelay,moveSpeed,attackRange=10,critChance=.05f,magnet=2.8f,regen;
        private int pierce=1,projectileCount=1,level=1,xp,xpNeed=10,kills,bossIndex; private float runTime,spawnClock,attackClock,uniqueClock;
        private readonly BossKind[] selectedBosses=new BossKind[3]; private Enemy currentBoss; private bool bossSpawnedForStage;
        private float suppressionMultiplier=1,invulnerableTimer,zikeVanishTimer,abilityFlashTimer;private string abilityFlash="";private Color abilityFlashColor;private int abilityFlashSlot=-1; private GUIStyle titleStyle,buttonStyle,hudStyle,centerStyle,cardStyle,captionStyle;
        private readonly Texture2D[] heroPortraits=new Texture2D[3];
        private Texture2D uiMenuBackground,uiPanelFrame,uiButtonPlate,uiCardFrame;
        private Font uiFont;
        private readonly Dictionary<string,Material> worldMaterials=new Dictionary<string,Material>();
        private readonly Dictionary<string,Sprite> obstacleSprites=new Dictionary<string,Sprite>();
        private readonly Dictionary<string,Texture2D> uiIcons=new Dictionary<string,Texture2D>();
        private SpriteRenderer groundRenderer,mapFogRenderer; private readonly Sprite[] mapGroundSprites=new Sprite[2]; private Transform obstacleRoot; private int mapVariant=-1; private bool qaInvulnerable;
        private int treatsCollected;private bool petUnlocked,petUnlockPending;private PetDefinition petDefinition;private PetController petController;private GameObject petObject;private readonly FamiliarEcho familiarEcho=new FamiliarEcho();private Texture2D petPortrait;

        private void Awake()
        {
            solidSprite=CreateSolidSprite();heroPortraits[0]=Resources.Load<Texture2D>("Art/Portraits/hero_amelia_card_v2");heroPortraits[1]=Resources.Load<Texture2D>("Art/Portraits/hero_sam_card_v2");heroPortraits[2]=Resources.Load<Texture2D>("Art/Portraits/hero_zike_card_v2");
            uiMenuBackground=Resources.Load<Texture2D>("Art/UI/ui_menu_background_v1");uiPanelFrame=Resources.Load<Texture2D>("Art/UI/ui_panel_frame_v1");uiButtonPlate=Resources.Load<Texture2D>("Art/UI/ui_button_plate_v1");uiCardFrame=Resources.Load<Texture2D>("Art/UI/ui_card_frame_v1");
            uiFont=Resources.Load<Font>("Fonts/RussoOne-Regular");
            BuildCamera();audioController=gameObject.AddComponent<GameAudioController>();spriteFactory=new RuntimeSpriteFactory(worldCamera); BuildWorld(); BuildPlayer(); BuildPools(); BuildUpgrades();BuildModernUi();if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-nightfallQuickStart")){SetHero(HeroKind.Amelia);StartRun();}
        }

        private void BuildCamera()
        {
            var go=new GameObject("WorldCamera"); worldCamera=go.AddComponent<Camera>(); worldCamera.orthographic=true; worldCamera.orthographicSize=8.2f;
            worldCamera.clearFlags=CameraClearFlags.SolidColor; worldCamera.backgroundColor=new Color(.035f,.055f,.075f);
            worldCamera.transform.position=new Vector3(0,12,-12); worldCamera.transform.rotation=Quaternion.Euler(43,0,0);
        }

        private void BuildWorld()
        {
            var texture=Resources.Load<Texture2D>("Art/forest_clearing_v1");texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Point;
            var ground=new GameObject("MapGround");groundRenderer=ground.AddComponent<SpriteRenderer>();groundRenderer.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),30,0,SpriteMeshType.FullRect);groundRenderer.drawMode=SpriteDrawMode.Tiled;groundRenderer.size=new Vector2(78,46);groundRenderer.sortingOrder=-100;ground.transform.position=new Vector3(0,-.08f,0);ground.transform.rotation=Quaternion.Euler(90,0,0);
            string[] generatedMaps={"Art/Maps/forest_arena_v1","Art/Maps/moon_arena_v1"};for(int i=0;i<2;i++){var generated=Resources.Load<Texture2D>(generatedMaps[i]);if(generated!=null){generated.filterMode=FilterMode.Point;generated.wrapMode=TextureWrapMode.Clamp;mapGroundSprites[i]=Sprite.Create(generated,new Rect(0,0,generated.width,generated.height),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);}}
            var fogTexture=Resources.Load<Texture2D>("Art/Environment/map_edge_fog_ring_v2");if(fogTexture!=null)BuildMapEdgeFog(fogTexture);
            obstacleRoot=new GameObject("ProceduralObstacles").transform;
            ApplyRandomMap();
        }

        private void ApplyRandomMap()
        {
            mapVariant=Random.Range(0,2);obstaclePositions.Clear();obstacleRadii.Clear();
            for(int i=obstacleRoot.childCount-1;i>=0;i--)Destroy(obstacleRoot.GetChild(i).gameObject);
            // The second arena is a colder, moonlit clearing with a rotated ground
            // and its own prop layout. Both maps deliberately share collision rules.
            if(mapGroundSprites[mapVariant]!=null){groundRenderer.drawMode=SpriteDrawMode.Simple;groundRenderer.sprite=mapGroundSprites[mapVariant];groundRenderer.color=Color.white;}else groundRenderer.color=mapVariant==0?Color.white:new Color(.58f,.72f,.86f);
            groundRenderer.transform.rotation=Quaternion.Euler(90,0,0);
            if(mapFogRenderer!=null)mapFogRenderer.color=mapVariant==0?new Color(.76f,.84f,.92f,.88f):new Color(.38f,.66f,.92f,.94f);
            for(int i=0;i<68;i++)
            {
                int type=i%12;float radius=type==7||type==8||type==9||type==11?1.0f:type==5?1.15f:.72f;
                Vector3 pos=FindObstaclePosition(i,radius);
                obstaclePositions.Add(new Vector3(pos.x,0,pos.z));obstacleRadii.Add(radius);CreateObstacle(obstacleRoot,pos,type,i);
            }
        }

        private Vector3 FindObstaclePosition(int index,float radius)
        {
            const float goldenAngle=2.399963f,minVisualGap=.72f;
            for(int attempt=0;attempt<40;attempt++)
            {
                int mapSalt=mapVariant*97;int sample=index+mapSalt+attempt*68;float angle=sample*goldenAngle;
                float distance=8.5f+((index*7+mapSalt+attempt*3)%14)*2.15f+attempt*.10f;
                Vector3 candidate=new Vector3(Mathf.Cos(angle)*distance,.02f,Mathf.Sin(angle)*distance*.58f);
                if(Mathf.Abs(candidate.x)<3.2f&&Mathf.Abs(candidate.z)<7)
                    candidate.x+=Mathf.Sign(candidate.x==0?1:candidate.x)*4.5f;
                bool clear=true;
                for(int j=0;j<obstaclePositions.Count;j++)
                {
                    float required=radius+obstacleRadii[j]+minVisualGap;
                    Vector3 delta=candidate-obstaclePositions[j];delta.y=0;
                    if(delta.sqrMagnitude<required*required){clear=false;break;}
                }
                if(clear)return candidate;
            }
            // Deterministic outer spiral fallback: it is preferable to move a prop
            // farther from the action than to allow two large silhouettes to overlap.
            float fallbackAngle=index*goldenAngle,fallbackDistance=18f+index*.65f;
            return new Vector3(Mathf.Cos(fallbackAngle)*fallbackDistance,.02f,Mathf.Sin(fallbackAngle)*fallbackDistance*.72f);
        }

        private void BuildMapEdgeFog(Texture2D texture)
        {
            var go=new GameObject("ContinuousMapEdgeFog");mapFogRenderer=go.AddComponent<SpriteRenderer>();mapFogRenderer.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),30,0,SpriteMeshType.FullRect);mapFogRenderer.sortingOrder=24;mapFogRenderer.color=new Color(.76f,.84f,.92f,.88f);
            go.transform.position=new Vector3(0,.04f,0);go.transform.rotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(2.05f,1.75f,1);
        }

        private Material WorldMaterial(string id,Color color)
        {
            if(worldMaterials.TryGetValue(id,out var material))return material;var shader=Shader.Find("Standard")??Shader.Find("Sprites/Default");material=new Material(shader){color=color};worldMaterials[id]=material;return material;
        }
        private GameObject ObstaclePart(Transform root,string name,PrimitiveType primitive,Vector3 localPosition,Vector3 scale,Material material,Vector3 euler=default)
        {
            var go=GameObject.CreatePrimitive(primitive);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=localPosition;go.transform.localScale=scale;go.transform.localRotation=Quaternion.Euler(euler);go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go;
        }
        private void CreateObstacle(Transform parent,Vector3 position,int type,int index)
        {
            string[] resources={"obstacle_oak","obstacle_pine","obstacle_dead_tree","obstacle_oak","obstacle_bush","obstacle_bush","obstacle_thorn_bush","obstacle_broken_wall","obstacle_broken_wall","obstacle_fallen_column","obstacle_rubble","obstacle_ruined_arch"};
            float[] scales={1f,1f,1f,.86f,.92f,1.22f,1f,1f,.86f,1f,1f,1f};
            string id=resources[Mathf.Clamp(type,0,resources.Length-1)];
            if(!obstacleSprites.TryGetValue(id,out var sprite))
            {
                var texture=Resources.Load<Texture2D>("Art/Obstacles/"+id);
                if(texture!=null)
                {
                    texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
                    sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.08f),64,0,SpriteMeshType.FullRect);
                }
                obstacleSprites[id]=sprite;
            }
            var root=new GameObject($"Obstacle_{index:00}_Type_{type+1:00}").transform;root.SetParent(parent,false);root.position=new Vector3(position.x,.02f,position.z);root.rotation=worldCamera.transform.rotation;
            var renderer=root.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=7;
            float mirror=index%2==0?1:-1;float size=scales[Mathf.Clamp(type,0,scales.Length-1)];root.localScale=new Vector3(size*mirror,size,size);
        }

        private void BuildPlayer(){player=new GameObject("Player");player.transform.position=new Vector3(0,.03f,0);characterVisual=player.AddComponent<CharacterVisualController>();SetHero(HeroKind.Amelia);player.SetActive(false);}
        private void SetHero(HeroKind kind)
        {
            hero=GameCatalog.Hero(kind);playerVisual=spriteFactory.Bind(player,hero.id);characterVisual.Configure(kind,playerVisual);
        }

        private void BuildPools()
        {
            for(int i=0;i<210;i++){var go=new GameObject("EnemyPool_"+i);go.SetActive(false);enemies.Add(new Enemy{go=go});}
            var projectileSprite=CreateProjectileSprite();for(int i=0;i<130;i++){var go=CreateColoredSprite("Projectile_"+i,Color.white,.72f,false);go.GetComponent<SpriteRenderer>().sprite=projectileSprite;go.GetComponent<SpriteRenderer>().sortingOrder=18;projectiles.Add(new Projectile{go=go});}
            var experienceSprite=CreateExperienceSprite();for(int i=0;i<170;i++){var go=CreateColoredSprite("Experience_"+i,new Color(.12f,1,.88f),.48f,false);go.GetComponent<SpriteRenderer>().sprite=experienceSprite;go.GetComponent<SpriteRenderer>().sortingOrder=16;orbs.Add(new Orb{go=go,phase=Random.value*Mathf.PI*2});}
            var treatSprite=CreateTreatSprite();for(int i=0;i<24;i++){var go=CreateColoredSprite("Treat_"+i,new Color(1,.58f,.18f),.58f,false);go.GetComponent<SpriteRenderer>().sprite=treatSprite;go.GetComponent<SpriteRenderer>().sortingOrder=17;lootPickups.Add(new LootPickup{go=go,kind=LootKind.Treat,phase=Random.value*Mathf.PI*2});}
        }

        private GameObject CreateColoredSprite(string name,Color color,float scale,bool active)
        {var go=new GameObject(name);var r=go.AddComponent<SpriteRenderer>();r.sprite=solidSprite;r.color=color;go.transform.localScale=Vector3.one*scale;go.SetActive(active);return go;}

        private void Update()
        {
            SyncModernUi();
            if(state==State.Upgrade||state==State.PetUnlock)return;ReadInput();if(Input.GetKeyDown(KeyCode.Escape)){if(state==State.Playing)state=State.Paused;else if(state==State.Paused)state=State.Playing;} if(state!=State.Playing)return; float dt=Time.deltaTime; runTime+=dt; suppressionMultiplier=Mathf.MoveTowards(suppressionMultiplier,1,dt*.45f);
            Vector3 movement=new Vector3(moveInput.x,0,moveInput.y);Vector3 next=player.transform.position+movement*moveSpeed*dt;player.transform.position=ResolveObstacles(next);characterVisual.SetMovement(movement,movement.sqrMagnitude>.01f);
            worldCamera.transform.position=player.transform.position+new Vector3(0,12,-12); if(regen>0)hp=Mathf.Min(maxHp,hp+regen*dt);
            spawnClock-=dt;if(spawnClock<=0){SpawnWave();spawnClock=SpawnInterval();}
            UpdateBossTimeline(); attackClock-=dt;if(attackClock<=0){AutoAttack();attackClock=attackDelay/suppressionMultiplier;}
            uniqueClock-=dt;if(uniqueClock<=0){UseUniqueAbility();uniqueClock=hero.kind==HeroKind.Amelia?7.5f:hero.kind==HeroKind.Sam?6.2f:5.2f;}
            invulnerableTimer=Mathf.Max(0,invulnerableTimer-dt);abilityFlashTimer=Mathf.Max(0,abilityFlashTimer-dt);UpdateHeroAbilities(dt);
            UpdateEnemies(dt);UpdateProjectiles(dt);UpdateOrbs(dt);UpdateLoot(dt);UpdatePet(dt);
        }

        private void ReadInput()
        {
            moveInput=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
            for(int i=0;i<Input.touchCount;i++){Touch t=Input.GetTouch(i);if(t.phase==TouchPhase.Began&&t.position.x<Screen.width*.45f&&joystickFinger<0){joystickFinger=t.fingerId;joystickOrigin=t.position;}if(t.fingerId!=joystickFinger)continue;if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)joystickFinger=-1;else moveInput=Vector2.ClampMagnitude((t.position-joystickOrigin)/100f,1);}
            if(moveInput.sqrMagnitude>1)moveInput.Normalize();
        }
        private Vector3 ResolveObstacles(Vector3 position)
        {
            position.x=Mathf.Clamp(position.x,-38f,38f);position.z=Mathf.Clamp(position.z,-22f,22f);
            for(int i=0;i<obstaclePositions.Count;i++){Vector3 delta=position-obstaclePositions[i];delta.y=0;float min=obstacleRadii[i]+.38f;if(delta.sqrMagnitude>=min*min)continue;if(delta.sqrMagnitude<.0001f)delta=Vector3.right;position=obstaclePositions[i]+delta.normalized*min;position.y=.03f;}return position;
        }

        private void SpawnWave()
        {
            int available=EnemyCap()-ActiveRegularEnemyCount();if(available<=0)return;
            int count=runTime>=420?2:1;count=Mathf.Min(count,available);
            for(int i=0;i<count;i++)SpawnEnemy(PickEnemyKind(),Random.Range(10f,14f));
        }
        private float SpawnInterval()
        {
            if(runTime<60)return .90f;
            if(runTime<180)return .72f;
            if(runTime<360)return .58f;
            if(runTime<600)return .46f;
            return .38f;
        }
        private int EnemyCap()
        {
            if(runTime<60)return 18;
            if(runTime<180)return 28;
            if(runTime<360)return 40;
            if(runTime<600)return 55;
            return 70;
        }
        private int ActiveRegularEnemyCount(){int count=0;foreach(var e in enemies)if(e.active&&!e.IsBoss)count++;return count;}
        private EnemyKind PickEnemyKind()
        {
            float r=Random.value;
            if(runTime<25)return r<.58f?EnemyKind.Vampire:EnemyKind.Zombie;
            if(runTime<90)return r<.36f?EnemyKind.Vampire:r<.66f?EnemyKind.Zombie:r<.86f?EnemyKind.Bandit:EnemyKind.Possessed;
            if(runTime<210)return r<.25f?EnemyKind.Vampire:r<.48f?EnemyKind.Zombie:r<.68f?EnemyKind.Bandit:r<.88f?EnemyKind.Possessed:EnemyKind.BureauAgent;
            if(runTime<420)return r<.23f?EnemyKind.Zombie:r<.42f?EnemyKind.Bandit:r<.62f?EnemyKind.BureauAgent:r<.8f?EnemyKind.Possessed:r<.92f?EnemyKind.Drone:EnemyKind.Mutant;
            return (EnemyKind)Random.Range(0,7);
        }

        private Enemy SpawnEnemy(EnemyKind kind,float radius)
        {
            Enemy e=GetEnemy();if(e==null)return null;var d=GameCatalog.Enemy(kind);float a=Random.value*Mathf.PI*2;
            Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float tx=Mathf.Abs(direction.x)>.001f?((direction.x>0?37.2f:-37.2f)-player.transform.position.x)/direction.x:float.PositiveInfinity;float tz=Mathf.Abs(direction.z)>.001f?((direction.z>0?21.2f:-21.2f)-player.transform.position.z)/direction.z:float.PositiveInfinity;float edgeDistance=Mathf.Min(tx>0?tx:float.PositiveInfinity,tz>0?tz:float.PositiveInfinity);if(float.IsInfinity(edgeDistance))edgeDistance=radius;
            e.go.transform.position=player.transform.position+direction*edgeDistance+Vector3.up*.03f;e.def=d;e.boss=null;e.maxHp=e.hp=d.hp*(1+runTime/780f);e.attackClock=Random.value*d.cooldown;e.abilityClock=1+Random.value*2;e.phaseTwo=false;e.active=true;e.generation++;
            e.visual=spriteFactory.Bind(e.go,d.spriteId);e.visual.SetScale(d.scale);e.visual.SetProceduralLocomotion(true);e.go.SetActive(true);return e;
        }
        private Enemy GetEnemy(){foreach(var e in enemies)if(!e.active)return e;return null;}

        private void UpdateBossTimeline()
        {
            int targetStage=runTime>=690?2:runTime>=450?1:runTime>=240?0:-1;
            if(targetStage>=0&&bossIndex==targetStage&&!bossSpawnedForStage&&currentBoss==null){SpawnBoss(selectedBosses[bossIndex]);bossSpawnedForStage=true;}
        }
        private void SpawnBoss(BossKind kind)
        {
            Enemy e=GetEnemy();if(e==null)return;var b=GameCatalog.Boss(kind);e.def=null;e.boss=b;e.maxHp=e.hp=b.hp*(1+bossIndex*.22f);e.attackClock=1;e.abilityClock=2.5f;e.phaseTwo=false;e.active=true;
            e.go.transform.position=player.transform.position+new Vector3(0,.03f,11);e.visual=spriteFactory.Bind(e.go,b.spriteId);e.visual.SetScale(b.scale);e.visual.SetProceduralLocomotion(true);e.go.SetActive(true);currentBoss=e;
        }

        private void UpdateEnemies(float dt)
        {
            Vector3 p=player.transform.position;
            foreach(var e in enemies)
            {
                if(!e.active)continue;Vector3 delta=p-e.go.transform.position;delta.y=0;float distance=delta.magnitude;Vector3 dir=distance>.01f?delta/distance:Vector3.zero;e.visual.SetFacing(dir);e.visual.SetMoving(true);
                if(e.IsBoss)UpdateBoss(e,dir,distance,dt);else UpdateRegular(e,dir,distance,dt);
            }
        }

        private void UpdateRegular(Enemy e,Vector3 dir,float distance,float dt)
        {
            var d=e.def;e.attackClock-=dt;e.abilityClock-=dt;
            bool ranged=d.kind==EnemyKind.Bandit||d.kind==EnemyKind.BureauAgent||d.kind==EnemyKind.Drone;
            float hover=d.kind==EnemyKind.Drone?.55f:0;e.go.transform.position=new Vector3(e.go.transform.position.x,hover,e.go.transform.position.z);
            if(!ranged||distance>d.range)e.go.transform.position+=dir*d.speed*dt;else if(distance<d.range*.65f)e.go.transform.position-=dir*d.speed*.55f*dt;
            if(ranged&&distance<=d.range&&e.attackClock<=0){e.visual.Play("attack",true);SpawnProjectile(e.go.transform.position+Vector3.up*.4f,dir,d.damage,6.5f,true,d.kind==EnemyKind.BureauAgent?Color.cyan:Color.red);e.attackClock=d.cooldown;}
            if(!ranged&&distance<d.range&&e.attackClock<=0){e.visual.Play("attack",true);DamagePlayer(d.damage);e.attackClock=d.cooldown;if(d.kind==EnemyKind.Mutant)player.transform.position+=dir*1.2f;}
            if(d.kind==EnemyKind.Possessed&&distance<1.8f&&e.abilityClock<=0){DamagePlayer(d.damage*.75f);Pulse(e.go.transform.position,new Color(.65f,.15f,.8f),1.8f);e.abilityClock=2.6f;}
        }

        private void UpdateBoss(Enemy e,Vector3 dir,float distance,float dt)
        {
            var b=e.boss;e.attackClock-=dt;e.abilityClock-=dt;if(!e.phaseTwo&&e.hp<e.maxHp*.5f){e.phaseTwo=true;Pulse(e.go.transform.position,Color.red,2.5f);}
            float phase=e.phaseTwo?1.35f:1;e.go.transform.position+=dir*b.speed*phase*dt;
            if(distance<1.2f&&e.attackClock<=0){DamagePlayer(b.damage);Pulse(e.go.transform.position,b.fallback,1.8f);e.attackClock=1.7f/phase;}
            if(e.abilityClock>0)return;e.abilityClock=Random.Range(2.2f,3.8f)/phase;
            switch(b.kind)
            {
                case BossKind.EarthDragon:RadialShots(e.go.transform.position,10,b.damage*.55f,new Color(.65f,.42f,.2f));break;
                case BossKind.Assassin:e.go.transform.position=player.transform.position-dir*1.4f;DamagePlayer(b.damage*.65f);break;
                case BossKind.EliteAgent:for(int i=-2;i<=2;i++)SpawnProjectile(e.go.transform.position,Quaternion.Euler(0,i*7,0)*dir,b.damage*.45f,9,true,Color.yellow);break;
                case BossKind.BastionMech:
                    if(Random.value<.5f)RadialShots(e.go.transform.position,12,b.damage*.5f,Color.red);
                    else{suppressionMultiplier=.58f;Pulse(player.transform.position,new Color(.15f,.6f,1),3);}
                    break;
            }
        }

        private void AutoAttack()
        {
            Enemy target=NearestEnemy();if(target==null)return;Vector3 baseDir=(target.go.transform.position-player.transform.position).normalized;
            characterVisual.PlayAttack(baseDir,()=>{Color attackColor=hero.attack==AttackKind.Light?new Color(1,.82f,.32f):hero.attack==AttackKind.Death?new Color(.85f,.05f,.2f):new Color(.15f,.8f,1);CombatVfxPool.SpawnAttack(player.transform.position,baseDir,attackColor,(int)hero.kind,worldCamera);for(int i=0;i<projectileCount;i++){float spread=(i-(projectileCount-1)*.5f)*8;Vector3 dir=Quaternion.Euler(0,spread,0)*baseDir;Color c=hero.attack==AttackKind.Light?new Color(1,.75f,.2f):hero.attack==AttackKind.Death?new Color(.75f,.04f,.18f):Color.cyan;SpawnProjectile(characterVisual.AttackOrigin(),dir,damage,10,false,c);}});
        }
        private Enemy NearestEnemy(){Enemy result=null;float best=attackRange*attackRange;foreach(var e in enemies){if(!e.active)continue;float d=(e.go.transform.position-player.transform.position).sqrMagnitude;if(d<best){best=d;result=e;}}return result;}

        private void UseUniqueAbility()
        {
            Color color=AbilityColor(hero.kind);audioController.PlayAbility(hero.kind,0);AbilityBurst(player.transform.position,color,(int)hero.kind);
            if(hero.kind==HeroKind.Amelia){float dealt=DamageRadius(player.transform.position,2.7f,damage*1.3f);hp=Mathf.Min(maxHp,hp+8+dealt*.015f);Pulse(player.transform.position,color,2.7f);}
            else if(hero.kind==HeroKind.Sam){float dealt=DamageRadius(player.transform.position,2.35f,damage*1.55f);hp=Mathf.Min(maxHp,hp+dealt*.07f);Pulse(player.transform.position,color,2.35f);}
            else{Enemy from=NearestEnemy();for(int i=0;i<5&&from!=null;i++){Hit(from,damage*.8f);Enemy next=NearestTo(from.go.transform.position,from);from=next;}Pulse(player.transform.position,color,3);}
        }

        private void UpdateHeroAbilities(float dt)
        {
            for(int i=0;i<5;i++){if(abilityRanks[i]<=0)continue;abilityTimers[i]-=dt;if(abilityTimers[i]<=0)CastHeroAbility(i);}
            if(zikeVanishTimer>0){zikeVanishTimer-=dt;if(zikeVanishTimer<=0){characterVisual.SetVisible(true);DamageRadius(player.transform.position,2.1f+abilityRanks[1]*.18f,damage*(1.25f+abilityRanks[1]*.12f));AbilityVfxController.SpawnCrossSlash(player.transform.position,Color.cyan,2.4f,worldCamera);Pulse(player.transform.position,Color.cyan,2.2f);}}
        }

        private void CastHeroAbility(int slot)
        {
            bool needsTarget=(hero.kind==HeroKind.Amelia&&slot==1)||(hero.kind==HeroKind.Sam&&slot==1)||(hero.kind==HeroKind.Zike&&slot==0);
            if(needsTarget&&NearestEnemy()==null){abilityTimers[slot]=.25f;return;}
            if(hero.kind==HeroKind.Zike&&slot==2&&moveInput.sqrMagnitude<=.05f){abilityTimers[slot]=.2f;return;}
            int rank=abilityRanks[slot];bool evolved=rank>=6;abilityFlash=AbilityName(hero.kind,slot)+(evolved?" • ЭВОЛЮЦИЯ":"");abilityFlashTimer=.85f;abilityFlashColor=AbilityColor(hero.kind);abilityFlashSlot=slot;audioController.PlayAbility(hero.kind,slot);characterVisual.PlayCast();AbilityBurst(player.transform.position,abilityFlashColor,(int)hero.kind);
            if(hero.kind==HeroKind.Amelia)
            {
                if(slot==0){float radius=2.2f+rank*.28f;AbilityVfxController.SpawnSigil(player.transform.position,new Color(1,.76f,.18f),radius,0,worldCamera);float dealt=DamageRadius(player.transform.position,radius,damage*(.65f+rank*.16f));hp=Mathf.Min(maxHp,hp+4+dealt*(.012f+rank*.003f));Pulse(player.transform.position,new Color(1,.82f,.3f),radius);if(evolved)RadialShots(player.transform.position,8,damage*.45f,new Color(1,.92f,.55f));abilityTimers[slot]=Mathf.Max(4.2f,8-rank*.45f);}
                else if(slot==1){int lashes=2+rank/2;for(int i=0;i<lashes;i++){Enemy target=NearestEnemy();if(target!=null){Vector3 hitPos=target.go.transform.position;AbilityVfxController.SpawnWhip(player.transform.position,hitPos,i%2==0?new Color(1,.72f,.12f):Color.white,worldCamera);Hit(target,damage*(.8f+rank*.2f));Pulse(hitPos,new Color(1,.78f,.3f),1.15f);}}if(evolved)DamageRadius(player.transform.position,3.8f,damage*1.15f);abilityTimers[slot]=Mathf.Max(2.5f,5.5f-rank*.35f);}
                else if(slot==2){hp=Mathf.Min(maxHp,hp+8+rank*4);invulnerableTimer=.25f+rank*.12f;AbilityVfxController.SpawnShield(player.transform.position,new Color(1,.88f,.38f),1.8f+rank*.12f,worldCamera);Pulse(player.transform.position,new Color(.95f,.95f,.65f),2+rank*.2f);if(evolved)DamageRadius(player.transform.position,3.5f,damage*1.6f);abilityTimers[slot]=Mathf.Max(7,12-rank*.55f);}
                else if(slot==3){int rays=5+rank;AbilityVfxController.SpawnRuneBloom(player.transform.position,new Color(1,.92f,.42f),2.2f+rank*.12f,worldCamera);RadialShots(player.transform.position,rays,damage*(.48f+rank*.08f),new Color(1,.92f,.58f));if(evolved)hp=Mathf.Min(maxHp,hp+18);abilityTimers[slot]=Mathf.Max(3.4f,7.2f-rank*.42f);}
                else{float radius=2.4f+rank*.25f;float dealt=DamageRadius(player.transform.position,radius,damage*(.45f+rank*.12f));hp=Mathf.Min(maxHp,hp+dealt*.02f);invulnerableTimer=Mathf.Max(invulnerableTimer,.12f+rank*.05f);AbilityVfxController.SpawnShield(player.transform.position,new Color(1,.72f,.22f),radius,worldCamera);abilityTimers[slot]=Mathf.Max(5.5f,10-rank*.45f);}
            }
            else if(hero.kind==HeroKind.Sam)
            {
                if(slot==0){float radius=1.65f+rank*.28f;AbilityVfxController.SpawnSigil(player.transform.position,new Color(.72f,.015f,.10f),radius,1,worldCamera);float dealt=DamageRadius(player.transform.position,radius,damage*(.75f+rank*.18f));hp=Mathf.Min(maxHp,hp+dealt*(.035f+rank*.008f));Pulse(player.transform.position,new Color(.65f,.03f,.16f),radius);if(evolved)RadialShots(player.transform.position,10,damage*.42f,new Color(.8f,.04f,.18f));abilityTimers[slot]=Mathf.Max(2.8f,6.2f-rank*.4f);}
                else if(slot==1){Enemy t=NearestEnemy();if(t!=null){Vector3 targetPos=t.go.transform.position,dir=(targetPos-player.transform.position).normalized;AbilityVfxController.SpawnBeam(player.transform.position,targetPos,new Color(.9f,.02f,.16f),worldCamera);for(int i=0;i<1+rank/2;i++)SpawnProjectile(player.transform.position+Vector3.up*.55f,Quaternion.Euler(0,(i-rank/4f)*9,0)*dir,damage*(1+rank*.18f),9,false,new Color(.7f,.02f,.2f));}if(evolved)DamageRadius(player.transform.position,2.6f,damage*.8f);abilityTimers[slot]=Mathf.Max(2.4f,5-rank*.3f);}
                else if(slot==2){RadialShots(player.transform.position,4+rank*2,damage*(.38f+rank*.06f),new Color(.45f,.01f,.12f));hp=Mathf.Min(maxHp,hp+rank*2);if(evolved){critChance+=.005f;Pulse(player.transform.position,Color.black,3.5f);}abilityTimers[slot]=Mathf.Max(4,8-rank*.4f);}
                else if(slot==3){float radius=2f+rank*.22f;AbilityVfxController.SpawnBladeWheel(player.transform.position,new Color(.72f,.01f,.10f),radius,worldCamera);float dealt=DamageRadius(player.transform.position,radius,damage*(.62f+rank*.13f));hp=Mathf.Min(maxHp,hp+dealt*(.025f+rank*.004f));Pulse(player.transform.position,new Color(.55f,0,.08f),radius);abilityTimers[slot]=Mathf.Max(3.2f,6.8f-rank*.38f);}
                else{Enemy t=NearestEnemy();if(t!=null){Vector3 dir=(t.go.transform.position-player.transform.position).normalized;for(int i=-1-rank/3;i<=1+rank/3;i++)SpawnProjectile(player.transform.position+Vector3.up*.55f,Quaternion.Euler(0,i*11,0)*dir,damage*(.7f+rank*.12f),8.5f,false,new Color(.22f,0,.05f));}if(evolved)RadialShots(player.transform.position,12,damage*.38f,Color.red);abilityTimers[slot]=Mathf.Max(3.6f,7.5f-rank*.4f);}
            }
            else
            {
                if(slot==0){Enemy from=NearestEnemy();Vector3 previous=player.transform.position;int jumps=2+rank;for(int i=0;i<jumps&&from!=null;i++){Vector3 hitPos=from.go.transform.position;AbilityVfxController.SpawnLightning(previous,hitPos,Color.cyan,worldCamera);Hit(from,damage*(.55f+rank*.1f));Pulse(hitPos,Color.cyan,1.0f);previous=hitPos;from=NearestTo(hitPos,from);}if(evolved)RadialShots(player.transform.position,8,damage*.5f,Color.cyan);abilityTimers[slot]=Mathf.Max(2.2f,5-rank*.35f);}
                else if(slot==1){invulnerableTimer=1.05f;zikeVanishTimer=1;AbilityVfxController.SpawnCrossSlash(player.transform.position,Color.cyan,2.0f,worldCamera);characterVisual.SetVisible(false);DamageRadius(player.transform.position,1.8f+rank*.15f,damage*(1+rank*.14f));player.transform.position+=new Vector3(moveInput.x,0,moveInput.y).normalized*(1.5f+rank*.22f);if(evolved)RadialShots(player.transform.position,12,damage*.45f,Color.cyan);abilityTimers[slot]=Mathf.Max(5,10-rank*.55f);}
                else if(slot==2){if(moveInput.sqrMagnitude>.05f){DamageRadius(player.transform.position,1.25f+rank*.12f,damage*(.35f+rank*.07f));AbilityVfxController.SpawnSigil(player.transform.position,new Color(.05f,.55f,1),1.45f,2,worldCamera);}if(evolved){Enemy t=NearestEnemy();if(t!=null){Hit(t,damage*2.2f);AbilityVfxController.SpawnLightning(player.transform.position,t.go.transform.position,Color.white,worldCamera);Pulse(t.go.transform.position,Color.white,1.5f);}}abilityTimers[slot]=Mathf.Max(.65f,1.8f-rank*.14f);}
                else if(slot==3){Enemy t=NearestEnemy();if(t!=null){Vector3 hit=t.go.transform.position;AbilityVfxController.SpawnLightning(player.transform.position,hit,Color.white,worldCamera);AbilityVfxController.SpawnImpactBurst(hit,new Color(.2f,.78f,1),1.45f+rank*.08f,worldCamera);Hit(t,damage*(1.1f+rank*.2f));DamageRadius(hit,1.25f+rank*.12f,damage*(.3f+rank*.06f));}abilityTimers[slot]=Mathf.Max(2.6f,5.8f-rank*.34f);}
                else{int count=5+rank;AbilityVfxController.SpawnBladeWheel(player.transform.position,new Color(.22f,.72f,1),2.15f+rank*.1f,worldCamera);RadialShots(player.transform.position,count,damage*(.42f+rank*.07f),new Color(.22f,.72f,1));invulnerableTimer=Mathf.Max(invulnerableTimer,.15f+rank*.04f);if(evolved)DamageRadius(player.transform.position,3.2f,damage*1.3f);abilityTimers[slot]=Mathf.Max(4.2f,8.5f-rank*.42f);}
            }
            TryFamiliarEcho(slot);
        }
        private static string AbilityName(HeroKind kind,int slot)
        {
            if(kind==HeroKind.Amelia)return new[]{"Священный круг","Кнут света","Светилище","Солнечные стрелы","Завет хранителя"}[slot];
            if(kind==HeroKind.Sam)return new[]{"Круговой удар посохом","Импульс смерти","Кровавая орбита","Жатва душ","Погребальный залп"}[slot];
            return new[]{"Цепная молния","Молниеносный шаг","Грозовой след","Громовой приговор","Штормовой веер"}[slot];
        }
        private static string AbilityDescription(HeroKind kind,int slot)
        {
            if(kind==HeroKind.Amelia)return new[]{"Круг света наносит урон и лечит Амелию.","Световой кнут поражает несколько ближайших целей.","Лечение и короткая неуязвимость.","Выпускает веер священных лучей.","Защитный завет обжигает врагов и лечит Амелию."}[slot];
            if(kind==HeroKind.Sam)return new[]{"Удар вокруг Сэма наносит урон и похищает здоровье.","Посох выпускает мощные пробивающие заряды.","Веер тёмных зарядов лечит Сэма.","Жатва вокруг Сэма вытягивает здоровье врагов.","Плотный веер погребальных зарядов."}[slot];
            return new[]{"Молния перескакивает между ближайшими врагами.","Зик исчезает, неуязвим и наносит два разреза.","Движение создаёт электрические импульсы.","Молния взрывает выбранную цель.","Круговой залп молний даёт короткую защиту."}[slot];
        }
        private static Color AbilityColor(HeroKind kind)=>kind==HeroKind.Amelia?new Color(1,.82f,.3f):kind==HeroKind.Sam?new Color(.78f,.03f,.17f):new Color(.15f,.82f,1);
        private static string HeroAbilitiesSummary(HeroKind kind)
        {
            return AbilityName(kind,0)+" — "+AbilityDescription(kind,0)+"\n"+AbilityName(kind,1)+" — "+AbilityDescription(kind,1)+"\n"+AbilityName(kind,2)+" — "+AbilityDescription(kind,2)+"\n"+AbilityName(kind,3)+" — "+AbilityDescription(kind,3)+"\n"+AbilityName(kind,4)+" — "+AbilityDescription(kind,4);
        }
        private static string HeroAbilitiesCardSummary(HeroKind kind)
        {
            if(kind==HeroKind.Amelia)return "Священный круг — урон и лечение\nКнут света — несколько целей\nСветилище — лечение и защита\nСолнечные стрелы — веер лучей\nЗавет хранителя — щит и лечение";
            if(kind==HeroKind.Sam)return "Удар посохом — круговой урон\nИмпульс смерти — пробивающий заряд\nКровавая орбита — веер и лечение\nЖатва душ — вампиризм вокруг\nПогребальный залп — плотный веер";
            return "Цепная молния — скачет по целям\nМолниеносный шаг — рывок и защита\nГрозовой след — импульсы в движении\nГромовой приговор — взрыв цели\nШтормовой веер — залп и защита";
        }
        private Enemy NearestTo(Vector3 point,Enemy excluded){Enemy result=null;float best=16;foreach(var e in enemies){if(!e.active||e==excluded)continue;float d=(e.go.transform.position-point).sqrMagnitude;if(d<best){best=d;result=e;}}return result;}

        private void SpawnProjectile(Vector3 pos,Vector3 dir,float amount,float speed,bool hostile,Color color)
        {foreach(var p in projectiles){if(p.active)continue;p.active=true;p.hostile=hostile;p.damage=amount;p.life=2.2f;p.radius=.46f;p.phase=Random.value*Mathf.PI*2;p.hitsRemaining=hostile?1:pierce;p.lastHit=null;p.velocity=dir.normalized*speed;p.go.transform.position=pos;p.go.transform.rotation=worldCamera.transform.rotation;p.go.GetComponent<SpriteRenderer>().color=color;p.go.SetActive(true);return;}}
        private void UpdateProjectiles(float dt)
        {
            foreach(var p in projectiles)
            {
                if(!p.active)continue;p.phase+=dt*10;float projectileScale=.68f+Mathf.Sin(p.phase)*.10f;p.go.transform.localScale=Vector3.one*projectileScale;p.go.transform.rotation=worldCamera.transform.rotation*Quaternion.Euler(0,0,p.phase*22);Vector3 from=p.go.transform.position;Vector3 to=from+p.velocity*dt;p.go.transform.position=to;p.life-=dt;bool consumed=p.life<=0;
                if(!consumed&&p.hostile&&SegmentHitsXZ(from,to,player.transform.position,p.radius+.45f)){DamagePlayer(p.damage);consumed=true;}
                if(!consumed&&!p.hostile)foreach(var e in enemies){if(!e.active||e==p.lastHit)continue;float hitRadius=p.radius+(e.IsBoss?.85f:.48f);if(!SegmentHitsXZ(from,to,e.go.transform.position,hitRadius))continue;float dealt=p.damage*(Random.value<critChance?2:1);Hit(e,dealt);p.lastHit=e;p.hitsRemaining--;consumed=p.hitsRemaining<=0;if(consumed)break;}
                if(consumed){p.active=false;p.go.SetActive(false);}
            }
        }
        private static bool SegmentHitsXZ(Vector3 from,Vector3 to,Vector3 point,float radius)
        {
            Vector2 a=new Vector2(from.x,from.z),b=new Vector2(to.x,to.z),p=new Vector2(point.x,point.z),ab=b-a;
            float lengthSq=ab.sqrMagnitude,t=lengthSq>.000001f?Mathf.Clamp01(Vector2.Dot(p-a,ab)/lengthSq):0;
            return (p-(a+ab*t)).sqrMagnitude<=radius*radius;
        }
        private void Hit(Enemy e,float amount){e.hp-=amount;e.visual.Play("hit",true);if(hero.kind==HeroKind.Sam)hp=Mathf.Min(maxHp,hp+amount*.035f);if(e.hp<=0)Kill(e);}
        private float DamageRadius(Vector3 pos,float radius,float amount){float total=0;foreach(var e in enemies){if(!e.active||(e.go.transform.position-pos).sqrMagnitude>radius*radius)continue;Hit(e,amount);total+=amount;}return total;}

        private void Kill(Enemy e)
        {
            bool boss=e.IsBoss;Vector3 pos=e.go.transform.position;e.active=false;e.go.SetActive(false);kills++;SpawnOrb(pos,boss?12:e.def.kind==EnemyKind.Mutant?3:1);
            if(!boss&&LootDropTable.Roll(LootKind.Treat,!petUnlocked&&!petUnlockPending))SpawnLoot(pos,LootKind.Treat);
            if(!boss&&e.def.kind==EnemyKind.Possessed&&Random.value<.22f){DamageRadius(pos,1.45f,damage*.55f);Pulse(pos,new Color(.7f,.12f,.8f),1.45f);}
            if(boss){currentBoss=null;bossIndex++;bossSpawnedForStage=false;if(bossIndex>=3)state=State.Victory;}
        }
        private void SpawnOrb(Vector3 pos,int value){foreach(var o in orbs){if(o.active)continue;o.active=true;o.value=value;o.go.transform.position=pos+Vector3.up*.12f;o.go.SetActive(true);return;}}
        private void UpdateOrbs(float dt)
        {foreach(var o in orbs){if(!o.active)continue;o.phase+=dt*3.2f;float pulse=.48f+Mathf.Sin(o.phase)*.06f+(o.value>1?.10f:0);o.go.transform.localScale=Vector3.one*pulse;o.go.transform.rotation=worldCamera.transform.rotation*Quaternion.Euler(0,0,Time.time*38+o.phase*18);Vector3 delta=player.transform.position-o.go.transform.position;float d=delta.magnitude;if(d<magnet)o.go.transform.position+=delta.normalized*Mathf.Lerp(3,12,1-d/magnet)*dt;if(d<.45f){o.active=false;o.go.SetActive(false);AddXp(o.value);}}}
        private void SpawnLoot(Vector3 pos,LootKind kind){foreach(var loot in lootPickups){if(loot.active)continue;loot.active=true;loot.kind=kind;loot.go.transform.position=pos+Vector3.up*.15f;loot.go.SetActive(true);return;}}
        private void UpdateLoot(float dt)
        {
            foreach(var loot in lootPickups){if(!loot.active)continue;loot.phase+=dt*4;loot.go.transform.localScale=Vector3.one*(.56f+Mathf.Sin(loot.phase)*.07f);loot.go.transform.rotation=worldCamera.transform.rotation*Quaternion.Euler(0,0,Mathf.Sin(loot.phase)*12);Vector3 delta=player.transform.position-loot.go.transform.position;float distance=delta.magnitude;if(distance<1.35f)loot.go.transform.position+=delta.normalized*7*dt;if(distance>=.48f)continue;loot.active=false;loot.go.SetActive(false);if(loot.kind==LootKind.Treat){treatsCollected=Mathf.Min(3,treatsCollected+1);if(treatsCollected>=3){petUnlockPending=true;foreach(var other in lootPickups){other.active=false;other.go.SetActive(false);}state=State.PetUnlock;}}}
        }
        private void UnlockPet()
        {
            if(petUnlocked&&petController!=null&&petController.GetComponent<SpriteRenderer>()!=null){state=State.Playing;return;}
            petUnlockPending=false;petUnlocked=true;petDefinition=PetCatalog.ForOwner(hero.id);petPortrait=Resources.Load<Texture2D>(petDefinition.portraitResource);
            if(petObject!=null)Destroy(petObject);
            petObject=new GameObject("Pet_"+petDefinition.id);petObject.transform.position=player.transform.position+new Vector3(-1.2f,.05f,-.2f);petController=petObject.AddComponent<PetController>();
            Texture2D texture=Resources.Load<Texture2D>(petDefinition.spriteResource);if(texture==null)Debug.LogError("PET_SPRITE_MISSING "+petDefinition.spriteResource);Sprite sprite=texture!=null?Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.18f),48,0,SpriteMeshType.FullRect):CreateTreatSprite();
            petController.Configure(petDefinition,player.transform,sprite,worldCamera);state=State.Playing;
        }
        private void QaUnlockPet()
        {
            if(petObject!=null)Destroy(petObject);petObject=null;petController=null;petUnlocked=false;petDefinition=PetCatalog.ForOwner(hero.id);petPortrait=Resources.Load<Texture2D>(petDefinition.portraitResource);petUnlockPending=true;state=State.PetUnlock;
        }
        private void UpdatePet(float dt)
        {
            if(!petUnlocked||petController==null)return;petController.TickMovement(dt);petController.AttackClock-=dt;if(petController.AttackClock>0)return;
            Enemy target=NearestToPet(petController.transform.position,petDefinition.targetingRange);if(target==null)return;Vector3 from=petController.transform.position+Vector3.up*.25f,dir=(target.go.transform.position-from).normalized;
            petController.PlayAttack();SpawnProjectile(from,dir,damage*petDefinition.attackDamageMultiplier,8.5f,false,AbilityColor(hero.kind));CombatVfxPool.SpawnAttack(from,dir,AbilityColor(hero.kind),3,worldCamera);petController.AttackClock=petDefinition.attackCooldown;
        }
        private Enemy NearestToPet(Vector3 point,float range){Enemy result=null;float best=range*range;foreach(var e in enemies){if(!e.active)continue;float d=(e.go.transform.position-point).sqrMagnitude;if(d<best){best=d;result=e;}}return result;}
        private void TryFamiliarEcho(int slot)
        {
            if(!petUnlocked||petController==null||!familiarEcho.TryBegin(petDefinition,true))return;
            Vector3 origin=petController.transform.position+Vector3.up*.25f;Enemy target=NearestToPet(origin,petDefinition.targetingRange);if(target!=null){Color color=AbilityColor(hero.kind);Vector3 hit=target.go.transform.position;AbilityVfxController.SpawnLightning(origin,hit,new Color(color.r,color.g,color.b,.9f),worldCamera);AbilityVfxController.SpawnImpactBurst(hit,color,1.1f+slot*.06f,worldCamera);Hit(target,damage*(.45f+abilityRanks[slot]*.08f));}
            familiarEcho.End();
        }
        private void AddXp(int value){xp+=value;if(xp<xpNeed)return;xp-=xpNeed;xpNeed=Mathf.CeilToInt(xpNeed*1.28f+2);level++;RollUpgrades();state=State.Upgrade;}
        private void DamagePlayer(float amount){if(qaInvulnerable||invulnerableTimer>0)return;hp-=amount;if(hp<=0){hp=0;state=State.Dead;}}

        private void RadialShots(Vector3 pos,int count,float amount,Color color){bool hostile=(pos-player.transform.position).sqrMagnitude>.1f;for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;SpawnProjectile(pos,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),amount,hostile?5.5f:7,hostile,color);}}
        private void Pulse(Vector3 pos,Color color,float scale){CombatVfxPool.SpawnRing(pos,color,scale,worldCamera);}
        private void AbilityBurst(Vector3 pos,Color color,int style){if(style==0)AbilityVfxController.SpawnRuneBloom(pos,color,2.15f,worldCamera);else if(style==1)AbilityVfxController.SpawnBladeWheel(pos,color,2.15f,worldCamera);else AbilityVfxController.SpawnImpactBurst(pos,color,2.15f,worldCamera);CombatVfxPool.SpawnRing(pos,color,2.5f,worldCamera,.78f);for(int i=0;i<8;i++){float a=i*Mathf.PI*2/8;CombatVfxPool.SpawnAttack(pos,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),color,style,worldCamera);}}

        private void BuildUpgrades()
        {
            BuildFilteredUpgrades();
            return;
            upgrades.Add(new Upgrade{name="МОЩЬ",description="Урон +20%",apply=()=>damage*=1.2f});
            upgrades.Add(new Upgrade{name="ТЕМП",description="Скорость атак +15%",apply=()=>attackDelay*=.85f});
            upgrades.Add(new Upgrade{name="ДАЛЬНОСТЬ",description="Дальность +18%",apply=()=>attackRange*=1.18f});
            upgrades.Add(new Upgrade{name="СКОРОСТЬ",description="Движение +12%",apply=()=>moveSpeed*=1.12f});
            upgrades.Add(new Upgrade{name="ЖИВУЧЕСТЬ",description="Макс. HP +30",apply=()=>{maxHp+=30;hp+=30;}});
            upgrades.Add(new Upgrade{name="РЕГЕНЕРАЦИЯ",description="+0.6 HP/с",apply=()=>regen+=.6f});
            upgrades.Add(new Upgrade{name="МАГНИТ",description="Сбор опыта +35%",apply=()=>magnet*=1.35f});
            upgrades.Add(new Upgrade{name="КРИТИЧЕСКИЙ УДАР",description="Шанс крита +8%",apply=()=>critChance+=.08f});
            upgrades.Add(new Upgrade{name="ПРОБИВАНИЕ",description="+1 потенциальная цель",apply=()=>pierce++});
            upgrades.Add(new Upgrade{name="ДВОЙНОЙ ЗАРЯД",description="Дополнительный снаряд",apply=()=>projectileCount=Mathf.Min(4,projectileCount+1)});
            upgrades.Add(new Upgrade{name="КРУПНЫЙ ЗАРЯД",description="Урон и радиус +10%",apply=()=>damage*=1.1f});
            upgrades.Add(new Upgrade{name="ЗАКАЛКА",description="Исцелить 40 HP",apply=()=>hp=Mathf.Min(maxHp,hp+40)});
            upgrades.Add(new Upgrade{name="СВЯТОЙ КРУГ",description="Круг Амелии сильнее",apply=()=>damage*=hero.kind==HeroKind.Amelia?1.16f:1.08f});
            upgrades.Add(new Upgrade{name="ПОХИЩЕНИЕ ЖИЗНИ",description="Сэм лечится эффективнее",apply=()=>{if(hero.kind==HeroKind.Sam)regen+=1;else maxHp+=15;}});
            upgrades.Add(new Upgrade{name="ЦЕПНАЯ МОЛНИЯ",description="Зик: больше цепного урона",apply=()=>{if(hero.kind==HeroKind.Zike)damage*=1.22f;else critChance+=.04f;}});
            upgrades.Add(new Upgrade{name="ПЛОТНЫЙ ОГОНЬ",description="Урон +12%, темп +6%",apply=()=>{damage*=1.12f;attackDelay*=.94f;}});
            upgrades.Add(new Upgrade{name="ОЧИЩЕНИЕ",description="HP +20 и лечение",apply=()=>{maxHp+=20;hp=maxHp;}});
            upgrades.Add(new Upgrade{name="ПЕРЕГРУЗКА",description="Крит +5%, урон +8%",apply=()=>{critChance+=.05f;damage*=1.08f;}});
        }
        private void BuildFilteredUpgrades()
        {
            upgrades.Clear();
            upgrades.Add(new Upgrade{name="МОЩЬ",description="Урон +20%",apply=()=>damage*=1.2f});
            upgrades.Add(new Upgrade{name="ТЕМП",description="Скорость атак +15%",apply=()=>attackDelay*=.85f});
            upgrades.Add(new Upgrade{name="ДАЛЬНОСТЬ",description="Дальность +18%",apply=()=>attackRange*=1.18f});
            upgrades.Add(new Upgrade{name="СКОРОСТЬ",description="Движение +12%",apply=()=>moveSpeed*=1.12f});
            upgrades.Add(new Upgrade{name="ЖИВУЧЕСТЬ",description="Макс. HP +30",apply=()=>{maxHp+=30;hp+=30;}});
            upgrades.Add(new Upgrade{name="РЕГЕНЕРАЦИЯ",description="+0.6 HP/с",apply=()=>regen+=.6f});
            upgrades.Add(new Upgrade{name="МАГНИТ",description="Радиус сбора +35%",apply=()=>magnet*=1.35f});
            upgrades.Add(new Upgrade{name="КРИТИЧЕСКИЙ УДАР",description="Шанс крита +8%",apply=()=>critChance+=.08f});
            upgrades.Add(new Upgrade{name="ПРОБИВАНИЕ",description="Снаряд пробивает ещё одну цель",apply=()=>pierce++});
            upgrades.Add(new Upgrade{name="ДОПОЛНИТЕЛЬНЫЙ ЗАРЯД",description="Ещё один снаряд",apply=()=>projectileCount=Mathf.Min(4,projectileCount+1)});
            string[] passiveIcons={"upgrade_power","upgrade_attack_speed","upgrade_range","upgrade_move_speed","upgrade_vitality","upgrade_regeneration","upgrade_magnet","upgrade_critical_strike","upgrade_piercing","upgrade_extra_projectile"};
            for(int i=0;i<passiveIcons.Length;i++)upgrades[i].icon=passiveIcons[i];
            AddAbility(HeroKind.Amelia,0,"СВЯЩЕННЫЙ КРУГ","Световой круг наносит урон и лечит Амелию");
            AddAbility(HeroKind.Amelia,1,"КНУТ СВЕТА","Поражает несколько ближайших целей световым кнутом");
            AddAbility(HeroKind.Amelia,2,"СВЕТИЛИЩЕ","Лечение и короткая божественная защита");
            AddAbility(HeroKind.Amelia,3,"СОЛНЕЧНЫЕ СТРЕЛЫ","Веер золотых священных лучей");
            AddAbility(HeroKind.Amelia,4,"ЗАВЕТ ХРАНИТЕЛЯ","Защитный круг обжигает врагов и лечит Амелию");
            AddAbility(HeroKind.Sam,0,"КРУГОВОЙ УДАР ПОСОХОМ","Удар вокруг Сэма с похищением здоровья");
            AddAbility(HeroKind.Sam,1,"ИМПУЛЬС СМЕРТИ","Мощные пробивающие заряды через посох");
            AddAbility(HeroKind.Sam,2,"КРОВАВАЯ ОРБИТА","Веер тёмных зарядов и восстановление здоровья");
            AddAbility(HeroKind.Sam,3,"ЖАТВА ДУШ","Круговая жатва наносит урон и похищает здоровье");
            AddAbility(HeroKind.Sam,4,"ПОГРЕБАЛЬНЫЙ ЗАЛП","Плотный веер тёмно-красных зарядов");
            AddAbility(HeroKind.Zike,0,"ЦЕПНАЯ МОЛНИЯ","Молния перескакивает между противниками");
            AddAbility(HeroKind.Zike,1,"МОЛНИЕНОСНЫЙ ШАГ","Зик исчезает, неуязвим и наносит два разреза");
            AddAbility(HeroKind.Zike,2,"ГРОЗОВОЙ СЛЕД","Движение оставляет электрические импульсы");
            AddAbility(HeroKind.Zike,3,"ГРОМОВОЙ ПРИГОВОР","Молния поражает цель и взрывается вокруг неё");
            AddAbility(HeroKind.Zike,4,"ШТОРМОВОЙ ВЕЕР","Круговой залп молний даёт короткую защиту");
        }
        private void AddAbility(HeroKind owner,int slot,string name,string description)
        {upgrades.Add(new Upgrade{name=name,description=description,icon=AbilityIconId(owner,slot),owner=owner,ability=slot,apply=()=>{abilityRanks[slot]=Mathf.Min(6,abilityRanks[slot]+1);abilityTimers[slot]=.65f;}});}
        private void RollUpgrades()
        {
            var eligible=new List<Upgrade>();foreach(var u in upgrades)if((!u.owner.HasValue||u.owner.Value==hero.kind)&&(u.ability<0||abilityRanks[u.ability]<6))eligible.Add(u);
            if(level==2){for(int slot=0;slot<3;slot++){foreach(var u in eligible)if(u.owner==hero.kind&&u.ability==slot){offered[slot]=u;break;}}return;}
            for(int i=0;i<3;i++)offered[i]=PickWeightedUpgrade(eligible,i);
        }
        private Upgrade PickWeightedUpgrade(List<Upgrade> eligible,int filled)
        {
            float total=0;foreach(var candidate in eligible){if(Array.IndexOf(offered,candidate,0,filled)>=0)continue;total+=candidate.owner==hero.kind&&candidate.ability>=0&&abilityRanks[candidate.ability]==0?3f:1f;}
            float roll=Random.value*total;foreach(var candidate in eligible){if(Array.IndexOf(offered,candidate,0,filled)>=0)continue;roll-=candidate.owner==hero.kind&&candidate.ability>=0&&abilityRanks[candidate.ability]==0?3f:1f;if(roll<=0)return candidate;}
            foreach(var candidate in eligible)if(Array.IndexOf(offered,candidate,0,filled)<0)return candidate;return eligible[0];
        }

        private void StartRun()
        {
            maxHp=hero.hp;hp=maxHp;damage=hero.damage;attackDelay=hero.attackDelay;moveSpeed=hero.speed;attackRange=10;critChance=.05f;magnet=2.8f;regen=0;projectileCount=1;pierce=1;
            level=1;xp=0;xpNeed=10;kills=0;runTime=0;spawnClock=.65f;attackClock=.4f;uniqueClock=4;bossIndex=0;bossSpawnedForStage=false;currentBoss=null;suppressionMultiplier=1;invulnerableTimer=0;zikeVanishTimer=0;characterVisual.SetVisible(true);
            for(int i=0;i<5;i++){abilityRanks[i]=0;abilityTimers[i]=0;}passiveRanks.Clear();treatsCollected=0;petUnlocked=false;petUnlockPending=false;petDefinition=PetCatalog.ForOwner(hero.id);petPortrait=Resources.Load<Texture2D>(petDefinition.portraitResource);if(petObject!=null)Destroy(petObject);petObject=null;petController=null;
            var pool=new[]{BossKind.EarthDragon,BossKind.Assassin,BossKind.EliteAgent,BossKind.BastionMech};for(int i=0;i<pool.Length;i++){int j=Random.Range(i,pool.Length);(pool[i],pool[j])=(pool[j],pool[i]);}for(int i=0;i<3;i++)selectedBosses[i]=pool[i];
            foreach(var e in enemies){e.active=false;e.go.SetActive(false);}foreach(var p in projectiles){p.active=false;p.go.SetActive(false);}foreach(var o in orbs){o.active=false;o.go.SetActive(false);}foreach(var loot in lootPickups){loot.active=false;loot.go.SetActive(false);}
            ApplyRandomMap();player.transform.position=Vector3.zero;player.SetActive(true);state=State.Playing;
        }

        private void QaLevelUp(){xp=0;xpNeed=Mathf.CeilToInt(xpNeed*1.28f+2);level++;RollUpgrades();state=State.Upgrade;}

        private static Rect UpgradeCardRect(int index,float w,float h)=>new Rect(w*(.19f+index*.21f),h*.30f,w*.19f,Mathf.Min(430,h*.52f));
        private void SelectUpgrade(int index)
        {
            if(index<0||index>=offered.Length||offered[index]==null)return;var upgrade=offered[index];upgrade.apply();
            if(upgrade.ability<0){if(!passiveRanks.ContainsKey(upgrade.name))passiveRanks[upgrade.name]=0;passiveRanks[upgrade.name]++;}state=State.Playing;
        }
        private void ReadUpgradeTouch()
        {
            float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f),w=Screen.width/scale,h=Screen.height/scale;
            for(int i=0;i<Input.touchCount;i++){Touch touch=Input.GetTouch(i);if(touch.phase!=TouchPhase.Ended)continue;Vector2 guiPoint=new Vector2(touch.position.x/scale,(Screen.height-touch.position.y)/scale);for(int card=0;card<3;card++)if(UpgradeCardRect(card,w,h).Contains(guiPoint)){SelectUpgrade(card);return;}}
        }
        private static Rect GuiSafeArea(float scale)
        {
            Rect safe=Screen.safeArea;return new Rect(safe.xMin/scale,(Screen.height-safe.yMax)/scale,safe.width/scale,safe.height/scale);
        }
        private static Rect PauseButtonRect(float scale){Rect safe=GuiSafeArea(scale);return new Rect(safe.xMax-170,safe.yMin+18,148,58);}
        private bool ReadPauseTouch()
        {
            float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);Rect button=PauseButtonRect(scale);
            for(int i=0;i<Input.touchCount;i++){Touch touch=Input.GetTouch(i);if(touch.phase!=TouchPhase.Ended)continue;Vector2 point=new Vector2(touch.position.x/scale,(Screen.height-touch.position.y)/scale);if(button.Contains(point)){state=State.Paused;return true;}}return false;
        }

        private void InitStyles()
        {
            if(titleStyle!=null)return;titleStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=46,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal,clipping=TextClipping.Clip,normal={textColor=Color.white}};
            buttonStyle=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=23,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Clip,normal={textColor=Color.white}};hudStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=20,fontStyle=FontStyle.Normal,clipping=TextClipping.Clip,normal={textColor=Color.white}};
            centerStyle=new GUIStyle(hudStyle){alignment=TextAnchor.MiddleCenter,fontSize=27};cardStyle=new GUIStyle(GUI.skin.label){font=uiFont,alignment=TextAnchor.MiddleCenter,wordWrap=true,fontSize=19,fontStyle=FontStyle.Normal,normal={textColor=Color.white}};
            captionStyle=new GUIStyle(hudStyle){alignment=TextAnchor.UpperCenter,wordWrap=true,fontSize=16,fontStyle=FontStyle.Normal,normal={textColor=new Color(.88f,.91f,.94f)}};
            if(uiButtonPlate!=null){buttonStyle.normal.background=uiButtonPlate;buttonStyle.hover.background=uiButtonPlate;buttonStyle.active.background=uiButtonPlate;buttonStyle.focused.background=uiButtonPlate;}
            buttonStyle.padding=new RectOffset(38,38,12,12);buttonStyle.border=new RectOffset(120,120,40,40);
        }

        private void DrawMenuBackground(float w,float h)
        {
            if(uiMenuBackground!=null)GUI.DrawTexture(new Rect(0,0,w,h),uiMenuBackground,ScaleMode.ScaleAndCrop,true);
            Color old=GUI.color;GUI.color=new Color(.015f,.025f,.04f,.34f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;
        }

        private void DrawPanel(Rect rect,float darkness=.80f)
        {
            Color old=GUI.color;GUI.color=new Color(.018f,.026f,.034f,darkness);GUI.DrawTexture(new Rect(rect.x+28,rect.y+25,rect.width-56,rect.height-50),Texture2D.whiteTexture);
            GUI.color=Color.white;if(uiPanelFrame!=null)GUI.DrawTexture(rect,uiPanelFrame,ScaleMode.StretchToFill,true);else GUI.Box(rect,"");GUI.color=old;
        }

        private void DrawCard(Rect rect)
        {
            if(uiCardFrame!=null)GUI.DrawTexture(rect,uiCardFrame,ScaleMode.StretchToFill,true);else GUI.Box(rect,"");
        }
        private void DrawFittedLabel(Rect rect,string text,GUIStyle source,int minimumSize=10)
        {
            var style=new GUIStyle(source){clipping=TextClipping.Clip};int original=Mathf.Max(minimumSize,style.fontSize);
            for(int size=original;size>=minimumSize;size--){style.fontSize=size;if(style.CalcHeight(new GUIContent(text),rect.width)<=rect.height){GUI.Label(rect,text,style);return;}}
            style.fontSize=minimumSize;GUI.Label(rect,text,style);
        }
        private Texture2D UiIcon(string id)
        {
            if(string.IsNullOrEmpty(id))return null;
            if(!uiIcons.TryGetValue(id,out var texture)){texture=Resources.Load<Texture2D>("Art/UI/Icons/"+id);uiIcons[id]=texture;}
            return texture;
        }
        private static string AbilityIconId(HeroKind kind,int slot)
        {
            string[][] ids={
                new[]{"amelia_sacred_circle","amelia_light_whip","amelia_sanctuary","amelia_solar_arrows","amelia_guardian_covenant"},
                new[]{"sam_staff_sweep","sam_death_pulse","sam_blood_orbit","sam_soul_harvest","sam_funeral_volley"},
                new[]{"zike_chain_lightning","zike_lightning_step","zike_storm_trail","zike_thunder_judgment","zike_storm_fan"}};
            return ids[(int)kind][slot];
        }
        private void DrawLoadout(float w,float h)
        {
            int learned=0;for(int i=0;i<5;i++)if(abilityRanks[i]>0)learned++;
            float panelHeight=Mathf.Clamp(58+learned*38,110,255);float top=h-panelHeight-15;
            DrawPanel(new Rect(w-505,top,490,panelHeight),.68f);
            GUI.Label(new Rect(w-472,top+20,430,30),"АРСЕНАЛ",new GUIStyle(hudStyle){fontSize=17});
            float y=top+52;
            for(int i=0;i<5;i++)
            {
                if(abilityRanks[i]<=0)continue;string rank=abilityRanks[i]>=6?"ЭВОЛЮЦИЯ":"★ "+abilityRanks[i];
                string cooldown=abilityTimers[i]>0?abilityTimers[i].ToString("0.0")+"с":"ГОТОВО";
                var icon=UiIcon(AbilityIconId(hero.kind,i));if(icon!=null)GUI.DrawTexture(new Rect(w-472,y,30,30),icon,ScaleMode.ScaleToFit,true);
                DrawFittedLabel(new Rect(w-436,y,404,32),AbilityName(hero.kind,i)+"  ["+rank+"]  "+cooldown,new GUIStyle(hudStyle){fontSize=15},11);y+=38;
            }
            if(learned==0)GUI.Label(new Rect(w-472,y,440,28),"Активные способности ещё не выбраны",new GUIStyle(hudStyle){fontSize=14});
        }
        private void DrawPetUnlockModal(float w,float h)
        {
            Color old=GUI.color;GUI.color=new Color(0,0,0,.72f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;
            Rect modal=new Rect(w*.27f,h*.17f,w*.46f,h*.66f);DrawPanel(modal,.96f);Color accent=AbilityColor(hero.kind);GUI.color=accent;GUI.DrawTexture(new Rect(modal.x+72,modal.y+92,modal.width-144,3),Texture2D.whiteTexture);GUI.color=old;
            DrawFittedLabel(new Rect(modal.x+45,modal.y+25,modal.width-90,55),"ФАМИЛЬЯР ОТКЛИКНУЛСЯ",titleStyle,23);
            Rect portraitFrame=new Rect(modal.x+62,modal.y+125,245,300);DrawCard(portraitFrame);if(petPortrait!=null)GUI.DrawTexture(new Rect(portraitFrame.x+32,portraitFrame.y+38,181,181),petPortrait,ScaleMode.ScaleToFit,true);DrawFittedLabel(new Rect(portraitFrame.x+25,portraitFrame.y+226,portraitFrame.width-50,48),petDefinition.displayName,new GUIStyle(centerStyle){fontSize=25},15);
            Rect info=new Rect(modal.x+340,modal.y+128,modal.width-405,292);GUI.color=new Color(.01f,.025f,.035f,.80f);GUI.DrawTexture(info,Texture2D.whiteTexture);GUI.color=old;DrawFittedLabel(new Rect(info.x+25,info.y+24,info.width-50,105),petDefinition.description,new GUIStyle(captionStyle){fontSize=17,alignment=TextAnchor.UpperLeft},11);DrawFittedLabel(new Rect(info.x+25,info.y+150,info.width-50,105),"АВТОАТАКА   20% урона героя\nЭХО НАВЫКА   12%\nРОЛЬ   наземный спутник",new GUIStyle(hudStyle){fontSize=16,alignment=TextAnchor.UpperLeft,wordWrap=true},11);
            Rect accept=new Rect(modal.center.x-170,modal.yMax-105,340,66);if(GUI.Button(accept,"ПРИНЯТЬ ФАМИЛЬЯРА",buttonStyle))UnlockPet();
        }
        private void LegacyOnGUI()
        {
            InitStyles();float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;Rect safe=GuiSafeArea(scale);
            if(state==State.Menu){DrawMenuBackground(w,h);DrawPanel(new Rect(w*.28f,h*.16f,w*.44f,h*.67f));GUI.Label(new Rect(0,h*.26f,w,80),"NIGHTFALL PROTOCOL",titleStyle);GUI.Label(new Rect(0,h*.39f,w,55),"МАГИЯ ПОД НАДЗОРОМ",centerStyle);if(GUI.Button(new Rect(w*.385f,h*.56f,w*.23f,100),"ИГРАТЬ",buttonStyle))state=State.HeroSelect;var legacySubtitleStyle=new GUIStyle(centerStyle){fontSize=16};legacySubtitleStyle.normal.textColor=new Color(.45f,.9f,1);GUI.Label(new Rect(0,h*.72f,w,32),"URBAN FANTASY SURVIVAL",legacySubtitleStyle);return;}
            if(state==State.HeroSelect)
            {
                DrawMenuBackground(w,h);DrawFittedLabel(new Rect(0,22,w,65),"ВЫБЕРИТЕ ОПЕРАТИВНИКА",titleStyle,28);DrawFittedLabel(new Rect(0,82,w,34),"Три боевых протокола. Один шанс пережить ночь.",new GUIStyle(centerStyle){fontSize=17},12);
                for(int i=0;i<3;i++)
                {
                    var d=GameCatalog.Hero((HeroKind)i);float x=w*(.09f+i*.303f),cw=w*.245f;Rect card=new Rect(x,118,cw,Mathf.Min(755,h-235));DrawCard(card);
                    float portraitSize=Mathf.Min(230,cw-176);Rect portrait=new Rect(card.center.x-portraitSize*.5f,card.y+100,portraitSize,portraitSize);Color portraitShade=GUI.color;GUI.color=Color.white;if(heroPortraits[i]!=null)GUI.DrawTexture(portrait,heroPortraits[i],ScaleMode.ScaleToFit,true);GUI.color=portraitShade;
                    DrawFittedLabel(new Rect(card.x+44,card.y+350,cw-88,44),d.displayName,new GUIStyle(centerStyle){fontSize=25},16);Color accent=i==0?new Color(1,.76f,.28f):i==1?new Color(.95f,.18f,.32f):new Color(.20f,.82f,1);Color old=GUI.color;GUI.color=accent;GUI.DrawTexture(new Rect(card.x+92,card.y+408,cw-184,3),Texture2D.whiteTexture);GUI.color=old;
                    Rect description=new Rect(card.x+54,card.y+426,cw-108,card.height-565);Color descriptionShade=GUI.color;GUI.color=new Color(.015f,.025f,.035f,.72f);GUI.DrawTexture(description,Texture2D.whiteTexture);GUI.color=descriptionShade;DrawFittedLabel(new Rect(description.x+12,description.y+8,description.width-24,description.height-16),d.subtitle+"\n"+d.hp.ToString("0")+" HP\n\n"+HeroAbilitiesCardSummary(d.kind),new GUIStyle(captionStyle){fontSize=15},10);
                    Rect buttonBlock=new Rect(card.x+38,card.yMax+12,cw-76,78);DrawPanel(buttonBlock,.94f);Rect fightButton=new Rect(buttonBlock.x+18,buttonBlock.y+10,buttonBlock.width-36,58);var fightStyle=new GUIStyle(buttonStyle){fontSize=24,normal={textColor=Color.white}};if(GUI.Button(fightButton,"В БОЙ  →",fightStyle)){SetHero(d.kind);StartRun();}
                }
                return;
            }
            Rect heroHud=new Rect(safe.xMin+12,safe.yMin+8,550,150);DrawPanel(heroHud,.70f);DrawFittedLabel(new Rect(heroHud.x+28,heroHud.y+18,330,36),$"{hero.displayName}   УР. {level}",hudStyle,14);DrawFittedLabel(new Rect(heroHud.x+28,heroHud.y+62,330,32),$"HP {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}",hudStyle,14);GUI.HorizontalScrollbar(new Rect(heroHud.x+28,heroHud.y+104,325,18),0,hp,0,maxHp);
            Rect petHud=new Rect(heroHud.x+374,heroHud.y+18,145,108);Color petShade=GUI.color;GUI.color=new Color(.01f,.025f,.035f,.78f);GUI.DrawTexture(petHud,Texture2D.whiteTexture);GUI.color=petShade;if(petUnlocked){if(petPortrait!=null)GUI.DrawTexture(new Rect(petHud.x+42,petHud.y+7,60,60),petPortrait,ScaleMode.ScaleToFit,true);DrawFittedLabel(new Rect(petHud.x+8,petHud.y+71,petHud.width-16,29),petDefinition.displayName,new GUIStyle(hudStyle){fontSize=14,alignment=TextAnchor.MiddleCenter},10);}else DrawFittedLabel(new Rect(petHud.x+10,petHud.y+18,petHud.width-20,70),"ВКУСНЯШКИ\n"+treatsCollected+" / 3",new GUIStyle(hudStyle){fontSize=14,alignment=TextAnchor.MiddleCenter},10);
            DrawFittedLabel(new Rect(w*.43f,safe.yMin+20,260,50),$"{(int)(runTime/60):00}:{(int)(runTime%60):00}",centerStyle,18);GUI.HorizontalScrollbar(new Rect(w*.36f,safe.yMin+86,w*.28f,18),0,xp,0,xpNeed);DrawFittedLabel(new Rect(safe.xMax-470,safe.yMin+29,280,40),$"УНИЧТОЖЕНО: {kills}",hudStyle,14);if(state==State.Playing&&GUI.Button(PauseButtonRect(scale),"ПАУЗА",buttonStyle))state=State.Paused;
            if(state==State.Playing){if(GUI.Button(new Rect(28,170,250,48),qaInvulnerable?"QA GOD: ON":"QA GOD: OFF",buttonStyle))qaInvulnerable=!qaInvulnerable;if(GUI.Button(new Rect(290,170,210,48),"QA +1 LEVEL",buttonStyle))QaLevelUp();if(GUI.Button(new Rect(512,170,210,48),petUnlocked?"QA PET: ON":"QA PET",buttonStyle))QaUnlockPet();}
            DrawLoadout(w,h);if(abilityFlashTimer>0){Rect notice=new Rect(w*.41f,h*.13f,w*.18f,62);DrawPanel(notice,.72f);var icon=abilityFlashSlot>=0?UiIcon(AbilityIconId(hero.kind,abilityFlashSlot)):null;if(icon!=null)GUI.DrawTexture(new Rect(notice.x+9,notice.y+7,48,48),icon,ScaleMode.ScaleToFit,true);DrawFittedLabel(new Rect(notice.x+63,notice.y+6,notice.width-70,50),abilityFlash,new GUIStyle(centerStyle){fontSize=15,alignment=TextAnchor.MiddleLeft,wordWrap=true},10);}
            if(currentBoss!=null){DrawFittedLabel(new Rect(w*.30f,122,w*.40f,38),currentBoss.boss.name,centerStyle,14);GUI.HorizontalScrollbar(new Rect(w*.31f,164,w*.38f,20),0,currentBoss.hp,0,currentBoss.maxHp);}
            if(joystickFinger>=0)GUI.Box(new Rect(joystickOrigin.x/scale-72,h-joystickOrigin.y/scale-72,144,144),"");
            if(state==State.Upgrade){Color old=GUI.color;GUI.color=new Color(0,0,0,.48f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;DrawPanel(new Rect(w*.14f,h*.12f,w*.72f,h*.76f),.90f);DrawFittedLabel(new Rect(0,h*.16f,w,70),"НОВЫЙ УРОВЕНЬ",titleStyle,28);for(int i=0;i<3;i++){var u=offered[i];int next=u.ability>=0?abilityRanks[u.ability]+1:0;string stars=u.ability<0?"":next>=6?"\n★★★★★ → ЭВОЛЮЦИЯ":"\n"+new string('★',next)+new string('☆',5-next);Rect card=UpgradeCardRect(i,w,h);DrawCard(card);if(GUI.Button(card,"",GUIStyle.none))SelectUpgrade(i);var upgradeIcon=UiIcon(u.icon);if(upgradeIcon!=null)GUI.DrawTexture(new Rect(card.center.x-40,card.y+62,80,80),upgradeIcon,ScaleMode.ScaleToFit,true);DrawFittedLabel(new Rect(card.x+30,card.y+150,card.width-60,card.height-172),u.name+stars+"\n\n"+u.description,cardStyle,10);}}
            else if(state==State.PetUnlock)DrawPetUnlockModal(w,h);
            else if(state==State.Paused){Color old=GUI.color;GUI.color=new Color(0,0,0,.58f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;DrawPanel(new Rect(w*.31f,h*.08f,w*.38f,h*.86f),.92f);GUI.Label(new Rect(0,h*.11f,w,70),"ПАУЗА",titleStyle);if(GUI.Button(new Rect(w*.40f,h*.22f,w*.20f,62),"ПРОДОЛЖИТЬ",buttonStyle))state=State.Playing;if(GUI.Button(new Rect(w*.40f,h*.31f,w*.20f,55),qaInvulnerable?"QA GOD: ON":"QA GOD: OFF",buttonStyle))qaInvulnerable=!qaInvulnerable;if(GUI.Button(new Rect(w*.40f,h*.39f,w*.20f,55),"QA +1 LEVEL",buttonStyle))QaLevelUp();if(GUI.Button(new Rect(w*.40f,h*.47f,w*.20f,55),petUnlocked?"QA PET: ON":"QA PET",buttonStyle))QaUnlockPet();if(GUI.Button(new Rect(w*.40f,h*.57f,w*.20f,62),"НАЧАТЬ ЗАНОВО",buttonStyle))StartRun();if(GUI.Button(new Rect(w*.40f,h*.68f,w*.20f,62),"ВЫБОР ГЕРОЯ",buttonStyle)){player.SetActive(false);state=State.HeroSelect;}}
            else if(state==State.Dead||state==State.Victory){string text=state==State.Victory?"ЗАБЕГ ЗАВЕРШЁН":"ОПЕРАТИВНИК ПОГИБ";Color old=GUI.color;GUI.color=new Color(0,0,0,.58f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;DrawPanel(new Rect(w*.28f,h*.25f,w*.44f,h*.48f),.92f);GUI.Label(new Rect(0,h*.33f,w,80),text,titleStyle);GUI.Label(new Rect(0,h*.44f,w,45),$"Уничтожено целей: {kills}",centerStyle);if(GUI.Button(new Rect(w*.4f,h*.56f,w*.2f,90),"ЕЩЁ РАЗ",buttonStyle))state=State.HeroSelect;}
        }

        private static Sprite CreateSolidSprite(){var t=new Texture2D(1,1,TextureFormat.RGBA32,false);t.SetPixel(0,0,Color.white);t.Apply();return Sprite.Create(t,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);}
        private static Sprite CreateProjectileSprite(){const int s=32;var t=new Texture2D(s,s,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float dx=Mathf.Abs(x+.5f-s*.5f),dy=Mathf.Abs(y+.5f-s*.5f),d=Mathf.Sqrt(dx*dx+dy*dy);bool ray=(dx<2&&dy<14)||(dy<2&&dx<14);Color c=Color.clear;if(d<5)c=new Color(1,1,1,1);else if(d<8)c=new Color(.62f,.62f,.62f,1);else if(d<10)c=new Color(.05f,.05f,.07f,1);else if(ray)c=new Color(.82f,.82f,.82f,1);p[y*s+x]=c;}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),32);}
        private static Sprite CreateExperienceSprite(){const int s=32;var t=new Texture2D(s,s,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float dx=Mathf.Abs(x+.5f-s*.5f),dy=Mathf.Abs(y+.5f-s*.5f),diamond=dx+dy;Color c=Color.clear;if(diamond<7)c=new Color(1,1,1,1);else if(diamond<11)c=new Color(.62f,.62f,.62f,1);else if(diamond<14)c=new Color(.035f,.045f,.06f,1);else if(diamond<16)c=new Color(.35f,.35f,.35f,.55f);p[y*s+x]=c;}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),32);}
        private static Sprite CreateTreatSprite(){const int s=32;var t=new Texture2D(s,s,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float dx=x-15.5f,dy=y-15.5f;bool body=(Mathf.Abs(dx)<9&&Mathf.Abs(dy)<5),left=((dx+10)*(dx+10)+dy*dy<30),right=((dx-10)*(dx-10)+dy*dy<30);Color c=Color.clear;if(body||left||right)c=new Color(.16f,.08f,.035f,1);if((Mathf.Abs(dx)<7&&Mathf.Abs(dy)<3)||((dx+10)*(dx+10)+dy*dy<15)||((dx-10)*(dx-10)+dy*dy<15))c=new Color(1,.58f,.14f,1);p[y*s+x]=c;}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),32);}
    }

    public sealed class PulseFx:MonoBehaviour
    {
        private float target,time;public void Begin(float scale){target=scale;time=.45f;}
        private void Update(){time-=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(target,.1f,time/.45f);var r=GetComponent<SpriteRenderer>();if(r!=null)r.color=new Color(r.color.r,r.color.g,r.color.b,Mathf.Clamp01(time/.45f));if(time<=0)Destroy(gameObject);}
    }

}
