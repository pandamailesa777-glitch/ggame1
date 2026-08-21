using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nightfall.UnityMvp
{
    public sealed class NightfallGame : MonoBehaviour
    {
        // TEMP QA PATCH: keep enabled until the first boss encounter has been verified.
        private const bool DebugSpawnBossImmediately = true;
        private enum State { Menu, HeroSelect, Playing, Upgrade, Paused, Dead, Victory }
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
        private sealed class Upgrade { public string name,description; public Action apply; public HeroKind? owner; public int ability=-1; }

        private readonly List<Enemy> enemies=new List<Enemy>(220);
        private readonly List<Projectile> projectiles=new List<Projectile>(140);
        private readonly List<Orb> orbs=new List<Orb>(180);
        private readonly List<Upgrade> upgrades=new List<Upgrade>(20);
        private readonly Upgrade[] offered=new Upgrade[3];
        private readonly List<Vector3> obstaclePositions=new List<Vector3>(48);private readonly List<float> obstacleRadii=new List<float>(48);
        private readonly int[] abilityRanks=new int[3]; private readonly float[] abilityTimers=new float[3];
        private Camera worldCamera; private RuntimeSpriteFactory spriteFactory; private Sprite solidSprite;
        private GameObject player; private DirectionalSpriteVisual playerVisual; private CharacterVisualController characterVisual; private HeroDefinition hero;
        private State state=State.Menu; private Vector2 moveInput,joystickOrigin; private int joystickFinger=-1;
        private float hp,maxHp,damage,attackDelay,moveSpeed,attackRange=10,critChance=.05f,magnet=2.8f,regen;
        private int pierce=1,projectileCount=1,level=1,xp,xpNeed=10,kills,bossIndex; private float runTime,spawnClock,attackClock,uniqueClock;
        private readonly BossKind[] selectedBosses=new BossKind[3]; private Enemy currentBoss; private bool bossSpawnedForStage;
        private float suppressionMultiplier=1,invulnerableTimer,zikeVanishTimer,abilityFlashTimer;private string abilityFlash=""; private GUIStyle titleStyle,buttonStyle,hudStyle,centerStyle,cardStyle,captionStyle;
        private readonly Texture2D[] heroPortraits=new Texture2D[3];
        private Texture2D uiMenuBackground,uiPanelFrame,uiButtonPlate,uiCardFrame;
        private Font uiFont;
        private readonly Dictionary<string,Material> worldMaterials=new Dictionary<string,Material>();

        private void Awake()
        {
            solidSprite=CreateSolidSprite();heroPortraits[0]=Resources.Load<Texture2D>("Art/Portraits/hero_amelia_card");heroPortraits[1]=Resources.Load<Texture2D>("Art/Portraits/hero_sam_card");heroPortraits[2]=Resources.Load<Texture2D>("Art/Portraits/hero_zike_card");
            uiMenuBackground=Resources.Load<Texture2D>("Art/UI/ui_menu_background_v1");uiPanelFrame=Resources.Load<Texture2D>("Art/UI/ui_panel_frame_v1");uiButtonPlate=Resources.Load<Texture2D>("Art/UI/ui_button_plate_v1");uiCardFrame=Resources.Load<Texture2D>("Art/UI/ui_card_frame_v1");
            uiFont=Resources.Load<Font>("Fonts/RussoOne-Regular");
            BuildCamera(); spriteFactory=new RuntimeSpriteFactory(worldCamera); BuildWorld(); BuildPlayer(); BuildPools(); BuildUpgrades();if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="-nightfallQuickStart")){SetHero(HeroKind.Amelia);StartRun();}
        }

        private void BuildCamera()
        {
            var go=new GameObject("WorldCamera"); worldCamera=go.AddComponent<Camera>(); worldCamera.orthographic=true; worldCamera.orthographicSize=8.2f;
            worldCamera.clearFlags=CameraClearFlags.SolidColor; worldCamera.backgroundColor=new Color(.035f,.055f,.075f);
            worldCamera.transform.position=new Vector3(0,12,-12); worldCamera.transform.rotation=Quaternion.Euler(43,0,0);
        }

        private void BuildWorld()
        {
            var texture=Resources.Load<Texture2D>("Art/forest_clearing_v1");
            var ground=new GameObject("ForestClearing");var sr=ground.AddComponent<SpriteRenderer>();sr.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),30);sr.sortingOrder=-100;ground.transform.position=new Vector3(0,-.08f,0);ground.transform.rotation=Quaternion.Euler(90,0,0);ground.transform.localScale=Vector3.one*3f;
            var fogTexture=Resources.Load<Texture2D>("Art/Environment/map_edge_fog_ring_v2");if(fogTexture!=null)BuildMapEdgeFog(fogTexture);
            var obstacleRoot=new GameObject("ProceduralObstacles").transform;
            for(int i=0;i<44;i++)
            {
                float a=i*2.399963f,r=7.5f+(i%9)*2.2f;Vector3 pos=new Vector3(Mathf.Cos(a)*r,.55f,Mathf.Sin(a)*r*.52f);if(Mathf.Abs(pos.x)<3.2f&&Mathf.Abs(pos.z)<7)pos.x+=Mathf.Sign(pos.x==0?1:pos.x)*4.5f;
                int type=i%12;float radius=type==7||type==8||type==9||type==11?1.0f:type==5?1.15f:.72f;obstaclePositions.Add(new Vector3(pos.x,0,pos.z));obstacleRadii.Add(radius);CreateObstacle(obstacleRoot,pos,type,i);
            }
        }

        private void BuildMapEdgeFog(Texture2D texture)
        {
            var go=new GameObject("ContinuousMapEdgeFog");var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),30,0,SpriteMeshType.FullRect);renderer.sortingOrder=24;renderer.color=new Color(.76f,.84f,.92f,.88f);
            go.transform.position=new Vector3(0,.04f,0);go.transform.rotation=Quaternion.Euler(90,0,0);go.transform.localScale=Vector3.one*1.45f;
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
            var root=new GameObject($"Obstacle_{index:00}_Type_{type+1:00}").transform;root.SetParent(parent,false);root.position=new Vector3(position.x,0,position.z);root.rotation=Quaternion.Euler(0,(index*73)%360,0);
            var bark=WorldMaterial("bark",new Color(.23f,.13f,.075f));var barkLight=WorldMaterial("barkLight",new Color(.36f,.22f,.12f));var leaf=WorldMaterial("leaf",new Color(.10f,.28f,.14f));var leafLight=WorldMaterial("leafLight",new Color(.18f,.42f,.20f));var dead=WorldMaterial("dead",new Color(.31f,.27f,.20f));var stone=WorldMaterial("stone",new Color(.32f,.36f,.35f));var stoneDark=WorldMaterial("stoneDark",new Color(.20f,.24f,.24f));
            switch(type)
            {
                case 0: ObstaclePart(root,"OakTrunk",PrimitiveType.Cylinder,new Vector3(0,.75f,0),new Vector3(.28f,.75f,.28f),bark);ObstaclePart(root,"OakCrown",PrimitiveType.Sphere,new Vector3(0,1.9f,0),new Vector3(1.25f,.9f,1.1f),leaf);break;
                case 1: ObstaclePart(root,"PineTrunk",PrimitiveType.Cylinder,new Vector3(0,.8f,0),new Vector3(.2f,.8f,.2f),bark);ObstaclePart(root,"PineLow",PrimitiveType.Cylinder,new Vector3(0,1.35f,0),new Vector3(.95f,.75f,.95f),leaf);ObstaclePart(root,"PineTop",PrimitiveType.Cylinder,new Vector3(0,2.15f,0),new Vector3(.62f,.8f,.62f),leafLight);break;
                case 2: ObstaclePart(root,"DeadTrunk",PrimitiveType.Cylinder,new Vector3(0,.9f,0),new Vector3(.24f,.9f,.24f),dead,new Vector3(0,0,8));ObstaclePart(root,"DeadBranch",PrimitiveType.Cylinder,new Vector3(.38f,1.42f,0),new Vector3(.10f,.55f,.10f),dead,new Vector3(0,0,-48));break;
                case 3: ObstaclePart(root,"TwinTrunkA",PrimitiveType.Cylinder,new Vector3(-.22f,.65f,0),new Vector3(.22f,.65f,.22f),bark);ObstaclePart(root,"TwinTrunkB",PrimitiveType.Cylinder,new Vector3(.25f,.85f,.05f),new Vector3(.25f,.85f,.25f),barkLight);ObstaclePart(root,"TwinCrown",PrimitiveType.Sphere,new Vector3(0,1.8f,0),new Vector3(1.05f,.75f,.85f),leaf);break;
                case 4: for(int j=0;j<3;j++)ObstaclePart(root,"BushBall"+j,PrimitiveType.Sphere,new Vector3((j-1)*.42f,.38f,j%2*.18f),new Vector3(.68f,.48f,.62f),j==1?leafLight:leaf);break;
                case 5: for(int j=0;j<5;j++)ObstaclePart(root,"WideBush"+j,PrimitiveType.Sphere,new Vector3((j-2)*.42f,.34f,(j%2)*.22f),new Vector3(.62f,.42f,.55f),j%2==0?leaf:leafLight);break;
                case 6: ObstaclePart(root,"ThornCore",PrimitiveType.Sphere,new Vector3(0,.45f,0),new Vector3(.78f,.55f,.72f),leaf);for(int j=0;j<4;j++)ObstaclePart(root,"Thorn"+j,PrimitiveType.Cylinder,new Vector3(Mathf.Cos(j*1.57f)*.55f,.55f,Mathf.Sin(j*1.57f)*.55f),new Vector3(.045f,.42f,.045f),dead,new Vector3(35,j*90,35));break;
                case 7: ObstaclePart(root,"BrokenWall",PrimitiveType.Cube,new Vector3(0,.48f,0),new Vector3(2.1f,.96f,.38f),stone);ObstaclePart(root,"MissingTop",PrimitiveType.Cube,new Vector3(.62f,1.12f,0),new Vector3(.65f,.42f,.38f),stoneDark);break;
                case 8: ObstaclePart(root,"RuinL_A",PrimitiveType.Cube,new Vector3(-.45f,.55f,0),new Vector3(1.4f,1.1f,.36f),stone);ObstaclePart(root,"RuinL_B",PrimitiveType.Cube,new Vector3(.42f,.38f,.52f),new Vector3(.36f,.76f,1.4f),stoneDark);break;
                case 9: ObstaclePart(root,"FallenPillar",PrimitiveType.Cylinder,new Vector3(0,.28f,0),new Vector3(.34f,1.15f,.34f),stone,new Vector3(0,0,90));ObstaclePart(root,"PillarCap",PrimitiveType.Cube,new Vector3(1.05f,.28f,0),new Vector3(.28f,.58f,.58f),stoneDark);break;
                case 10: for(int j=0;j<5;j++)ObstaclePart(root,"Rubble"+j,j%2==0?PrimitiveType.Cube:PrimitiveType.Sphere,new Vector3((j-2)*.28f,.16f+(j%2)*.08f,(j%3-1)*.23f),new Vector3(.42f,.30f,.38f),j%2==0?stone:stoneDark,new Vector3(j*13,j*31,j*7));break;
                default: ObstaclePart(root,"ArchLeft",PrimitiveType.Cube,new Vector3(-.62f,.72f,0),new Vector3(.38f,1.44f,.42f),stone);ObstaclePart(root,"ArchRight",PrimitiveType.Cube,new Vector3(.62f,.72f,0),new Vector3(.38f,1.44f,.42f),stone);ObstaclePart(root,"ArchTop",PrimitiveType.Cube,new Vector3(0,1.45f,0),new Vector3(1.62f,.34f,.42f),stoneDark);break;
            }
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
        }

        private GameObject CreateColoredSprite(string name,Color color,float scale,bool active)
        {var go=new GameObject(name);var r=go.AddComponent<SpriteRenderer>();r.sprite=solidSprite;r.color=color;go.transform.localScale=Vector3.one*scale;go.SetActive(active);return go;}

        private void Update()
        {
            ReadInput();if(Input.GetKeyDown(KeyCode.Escape)){if(state==State.Playing)state=State.Paused;else if(state==State.Paused)state=State.Playing;} if(state!=State.Playing)return; float dt=Time.deltaTime; runTime+=dt; suppressionMultiplier=Mathf.MoveTowards(suppressionMultiplier,1,dt*.45f);
            Vector3 movement=new Vector3(moveInput.x,0,moveInput.y);Vector3 next=player.transform.position+movement*moveSpeed*dt;player.transform.position=ResolveObstacles(next);characterVisual.SetMovement(movement,movement.sqrMagnitude>.01f);
            worldCamera.transform.position=player.transform.position+new Vector3(0,12,-12); if(regen>0)hp=Mathf.Min(maxHp,hp+regen*dt);
            spawnClock-=dt;if(spawnClock<=0){SpawnWave();spawnClock=Mathf.Max(.10f,.55f-runTime*.0005f);}
            UpdateBossTimeline(); attackClock-=dt;if(attackClock<=0){AutoAttack();attackClock=attackDelay/suppressionMultiplier;}
            uniqueClock-=dt;if(uniqueClock<=0){UseUniqueAbility();uniqueClock=hero.kind==HeroKind.Amelia?7.5f:hero.kind==HeroKind.Sam?6.2f:5.2f;}
            invulnerableTimer=Mathf.Max(0,invulnerableTimer-dt);abilityFlashTimer=Mathf.Max(0,abilityFlashTimer-dt);UpdateHeroAbilities(dt);
            UpdateEnemies(dt);UpdateProjectiles(dt);UpdateOrbs(dt);
        }

        private void ReadInput()
        {
            moveInput=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
            for(int i=0;i<Input.touchCount;i++){Touch t=Input.GetTouch(i);if(t.phase==TouchPhase.Began&&t.position.x<Screen.width*.45f&&joystickFinger<0){joystickFinger=t.fingerId;joystickOrigin=t.position;}if(t.fingerId!=joystickFinger)continue;if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled)joystickFinger=-1;else moveInput=Vector2.ClampMagnitude((t.position-joystickOrigin)/100f,1);}
            if(moveInput.sqrMagnitude>1)moveInput.Normalize();
        }
        private Vector3 ResolveObstacles(Vector3 position)
        {
            position.x=Mathf.Clamp(position.x,-26.5f,26.5f);position.z=Mathf.Clamp(position.z,-14.5f,14.5f);
            for(int i=0;i<obstaclePositions.Count;i++){Vector3 delta=position-obstaclePositions[i];delta.y=0;float min=obstacleRadii[i]+.38f;if(delta.sqrMagnitude>=min*min)continue;if(delta.sqrMagnitude<.0001f)delta=Vector3.right;position=obstaclePositions[i]+delta.normalized*min;position.y=.03f;}return position;
        }

        private void SpawnWave()
        {
            int count=1+(runTime>360?2:runTime>120?1:0);for(int i=0;i<count;i++)SpawnEnemy(PickEnemyKind(),Random.Range(10f,14f));
        }
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
            Vector3 direction=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float tx=Mathf.Abs(direction.x)>.001f?((direction.x>0?25.8f:-25.8f)-player.transform.position.x)/direction.x:float.PositiveInfinity;float tz=Mathf.Abs(direction.z)>.001f?((direction.z>0?13.8f:-13.8f)-player.transform.position.z)/direction.z:float.PositiveInfinity;float edgeDistance=Mathf.Min(tx>0?tx:float.PositiveInfinity,tz>0?tz:float.PositiveInfinity);if(float.IsInfinity(edgeDistance))edgeDistance=radius;
            e.go.transform.position=player.transform.position+direction*edgeDistance+Vector3.up*.03f;e.def=d;e.boss=null;e.maxHp=e.hp=d.hp*(1+runTime/780f);e.attackClock=Random.value*d.cooldown;e.abilityClock=1+Random.value*2;e.phaseTwo=false;e.active=true;e.generation++;
            e.visual=spriteFactory.Bind(e.go,d.spriteId);e.visual.SetScale(d.scale);e.go.SetActive(true);return e;
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
            e.go.transform.position=player.transform.position+new Vector3(0,.03f,11);e.visual=spriteFactory.Bind(e.go,b.spriteId);e.visual.SetScale(b.scale);e.go.SetActive(true);currentBoss=e;
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
            characterVisual.PlayAttack(baseDir,()=>{Color attackColor=hero.attack==AttackKind.Light?new Color(1,.82f,.32f):hero.attack==AttackKind.Death?new Color(.85f,.05f,.2f):new Color(.15f,.8f,1);CombatVfxPool.SpawnAttack(player.transform.position,baseDir,attackColor,(int)hero.kind,worldCamera);for(int i=0;i<projectileCount;i++){float spread=(i-(projectileCount-1)*.5f)*8;Vector3 dir=Quaternion.Euler(0,spread,0)*baseDir;Color c=hero.attack==AttackKind.Light?new Color(1,.75f,.2f):hero.attack==AttackKind.Death?new Color(.75f,.04f,.18f):Color.cyan;SpawnProjectile(player.transform.position+Vector3.up*.55f,dir,damage,10,false,c);}});
        }
        private Enemy NearestEnemy(){Enemy result=null;float best=attackRange*attackRange;foreach(var e in enemies){if(!e.active)continue;float d=(e.go.transform.position-player.transform.position).sqrMagnitude;if(d<best){best=d;result=e;}}return result;}

        private void UseUniqueAbility()
        {
            Color color=AbilityColor(hero.kind);AbilityBurst(player.transform.position,color,(int)hero.kind);
            if(hero.kind==HeroKind.Amelia){float dealt=DamageRadius(player.transform.position,2.7f,damage*1.3f);hp=Mathf.Min(maxHp,hp+8+dealt*.015f);Pulse(player.transform.position,color,2.7f);}
            else if(hero.kind==HeroKind.Sam){float dealt=DamageRadius(player.transform.position,2.35f,damage*1.55f);hp=Mathf.Min(maxHp,hp+dealt*.07f);Pulse(player.transform.position,color,2.35f);}
            else{Enemy from=NearestEnemy();for(int i=0;i<5&&from!=null;i++){Hit(from,damage*.8f);Enemy next=NearestTo(from.go.transform.position,from);from=next;}Pulse(player.transform.position,color,3);}
        }

        private void UpdateHeroAbilities(float dt)
        {
            for(int i=0;i<3;i++){if(abilityRanks[i]<=0)continue;abilityTimers[i]-=dt;if(abilityTimers[i]<=0)CastHeroAbility(i);}
            if(zikeVanishTimer>0){zikeVanishTimer-=dt;if(zikeVanishTimer<=0){characterVisual.SetVisible(true);DamageRadius(player.transform.position,2.1f+abilityRanks[1]*.18f,damage*(1.25f+abilityRanks[1]*.12f));AbilityVfxController.SpawnCrossSlash(player.transform.position,Color.cyan,2.4f,worldCamera);Pulse(player.transform.position,Color.cyan,2.2f);}}
        }

        private void CastHeroAbility(int slot)
        {
            bool needsTarget=(hero.kind==HeroKind.Amelia&&slot==1)||(hero.kind==HeroKind.Sam&&slot==1)||(hero.kind==HeroKind.Zike&&slot==0);
            if(needsTarget&&NearestEnemy()==null){abilityTimers[slot]=.25f;return;}
            if(hero.kind==HeroKind.Zike&&slot==2&&moveInput.sqrMagnitude<=.05f){abilityTimers[slot]=.2f;return;}
            int rank=abilityRanks[slot];bool evolved=rank>=6;abilityFlash=AbilityName(hero.kind,slot)+(evolved?" • ЭВОЛЮЦИЯ":"")+"\n"+AbilityDescription(hero.kind,slot);abilityFlashTimer=2.1f;characterVisual.PlayCast();AbilityBurst(player.transform.position,AbilityColor(hero.kind),(int)hero.kind);
            if(hero.kind==HeroKind.Amelia)
            {
                if(slot==0){float radius=2.2f+rank*.28f;AbilityVfxController.SpawnSigil(player.transform.position,new Color(1,.76f,.18f),radius,0,worldCamera);float dealt=DamageRadius(player.transform.position,radius,damage*(.65f+rank*.16f));hp=Mathf.Min(maxHp,hp+4+dealt*(.012f+rank*.003f));Pulse(player.transform.position,new Color(1,.82f,.3f),radius);if(evolved)RadialShots(player.transform.position,8,damage*.45f,new Color(1,.92f,.55f));abilityTimers[slot]=Mathf.Max(4.2f,8-rank*.45f);}
                else if(slot==1){int lashes=2+rank/2;for(int i=0;i<lashes;i++){Enemy target=NearestEnemy();if(target!=null){Vector3 hitPos=target.go.transform.position;AbilityVfxController.SpawnWhip(player.transform.position,hitPos,i%2==0?new Color(1,.72f,.12f):Color.white,worldCamera);Hit(target,damage*(.8f+rank*.2f));Pulse(hitPos,new Color(1,.78f,.3f),1.15f);}}if(evolved)DamageRadius(player.transform.position,3.8f,damage*1.15f);abilityTimers[slot]=Mathf.Max(2.5f,5.5f-rank*.35f);}
                else{hp=Mathf.Min(maxHp,hp+8+rank*4);invulnerableTimer=.25f+rank*.12f;AbilityVfxController.SpawnShield(player.transform.position,new Color(1,.88f,.38f),1.8f+rank*.12f,worldCamera);Pulse(player.transform.position,new Color(.95f,.95f,.65f),2+rank*.2f);if(evolved)DamageRadius(player.transform.position,3.5f,damage*1.6f);abilityTimers[slot]=Mathf.Max(7,12-rank*.55f);}
            }
            else if(hero.kind==HeroKind.Sam)
            {
                if(slot==0){float radius=1.65f+rank*.28f;AbilityVfxController.SpawnSigil(player.transform.position,new Color(.72f,.015f,.10f),radius,1,worldCamera);float dealt=DamageRadius(player.transform.position,radius,damage*(.75f+rank*.18f));hp=Mathf.Min(maxHp,hp+dealt*(.035f+rank*.008f));Pulse(player.transform.position,new Color(.65f,.03f,.16f),radius);if(evolved)RadialShots(player.transform.position,10,damage*.42f,new Color(.8f,.04f,.18f));abilityTimers[slot]=Mathf.Max(2.8f,6.2f-rank*.4f);}
                else if(slot==1){Enemy t=NearestEnemy();if(t!=null){Vector3 targetPos=t.go.transform.position,dir=(targetPos-player.transform.position).normalized;AbilityVfxController.SpawnBeam(player.transform.position,targetPos,new Color(.9f,.02f,.16f),worldCamera);for(int i=0;i<1+rank/2;i++)SpawnProjectile(player.transform.position+Vector3.up*.55f,Quaternion.Euler(0,(i-rank/4f)*9,0)*dir,damage*(1+rank*.18f),9,false,new Color(.7f,.02f,.2f));}if(evolved)DamageRadius(player.transform.position,2.6f,damage*.8f);abilityTimers[slot]=Mathf.Max(2.4f,5-rank*.3f);}
                else{RadialShots(player.transform.position,4+rank*2,damage*(.38f+rank*.06f),new Color(.45f,.01f,.12f));hp=Mathf.Min(maxHp,hp+rank*2);if(evolved){critChance+=.005f;Pulse(player.transform.position,Color.black,3.5f);}abilityTimers[slot]=Mathf.Max(4,8-rank*.4f);}
            }
            else
            {
                if(slot==0){Enemy from=NearestEnemy();Vector3 previous=player.transform.position;int jumps=2+rank;for(int i=0;i<jumps&&from!=null;i++){Vector3 hitPos=from.go.transform.position;AbilityVfxController.SpawnLightning(previous,hitPos,Color.cyan,worldCamera);Hit(from,damage*(.55f+rank*.1f));Pulse(hitPos,Color.cyan,1.0f);previous=hitPos;from=NearestTo(hitPos,from);}if(evolved)RadialShots(player.transform.position,8,damage*.5f,Color.cyan);abilityTimers[slot]=Mathf.Max(2.2f,5-rank*.35f);}
                else if(slot==1){invulnerableTimer=1.05f;zikeVanishTimer=1;AbilityVfxController.SpawnCrossSlash(player.transform.position,Color.cyan,2.0f,worldCamera);characterVisual.SetVisible(false);DamageRadius(player.transform.position,1.8f+rank*.15f,damage*(1+rank*.14f));player.transform.position+=new Vector3(moveInput.x,0,moveInput.y).normalized*(1.5f+rank*.22f);if(evolved)RadialShots(player.transform.position,12,damage*.45f,Color.cyan);abilityTimers[slot]=Mathf.Max(5,10-rank*.55f);}
                else{if(moveInput.sqrMagnitude>.05f){DamageRadius(player.transform.position,1.25f+rank*.12f,damage*(.35f+rank*.07f));AbilityVfxController.SpawnSigil(player.transform.position,new Color(.05f,.55f,1),1.45f,2,worldCamera);}if(evolved){Enemy t=NearestEnemy();if(t!=null){Hit(t,damage*2.2f);AbilityVfxController.SpawnLightning(player.transform.position,t.go.transform.position,Color.white,worldCamera);Pulse(t.go.transform.position,Color.white,1.5f);}}abilityTimers[slot]=Mathf.Max(.65f,1.8f-rank*.14f);}
            }
        }
        private static string AbilityName(HeroKind kind,int slot)
        {
            if(kind==HeroKind.Amelia)return new[]{"Священный круг","Кнут света","Светилище"}[slot];
            if(kind==HeroKind.Sam)return new[]{"Круговой удар посохом","Импульс смерти","Кровавая орбита"}[slot];
            return new[]{"Цепная молния","Молниеносный шаг","Грозовой след"}[slot];
        }
        private static string AbilityDescription(HeroKind kind,int slot)
        {
            if(kind==HeroKind.Amelia)return new[]{"Круг света наносит урон и лечит Амелию.","Световой кнут поражает несколько ближайших целей.","Лечение и короткая неуязвимость."}[slot];
            if(kind==HeroKind.Sam)return new[]{"Удар вокруг Сэма наносит урон и похищает здоровье.","Посох выпускает мощные пробивающие заряды.","Веер тёмных зарядов лечит Сэма."}[slot];
            return new[]{"Молния перескакивает между ближайшими врагами.","Зик исчезает, неуязвим и наносит два разреза.","Движение создаёт электрические импульсы."}[slot];
        }
        private static Color AbilityColor(HeroKind kind)=>kind==HeroKind.Amelia?new Color(1,.82f,.3f):kind==HeroKind.Sam?new Color(.78f,.03f,.17f):new Color(.15f,.82f,1);
        private static string HeroAbilitiesSummary(HeroKind kind)
        {
            return AbilityName(kind,0)+" — "+AbilityDescription(kind,0)+"\n"+AbilityName(kind,1)+" — "+AbilityDescription(kind,1)+"\n"+AbilityName(kind,2)+" — "+AbilityDescription(kind,2);
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
            if(!boss&&e.def.kind==EnemyKind.Possessed&&Random.value<.22f){DamageRadius(pos,1.45f,damage*.55f);Pulse(pos,new Color(.7f,.12f,.8f),1.45f);}
            if(boss){currentBoss=null;bossIndex++;bossSpawnedForStage=false;if(bossIndex>=3)state=State.Victory;}
        }
        private void SpawnOrb(Vector3 pos,int value){foreach(var o in orbs){if(o.active)continue;o.active=true;o.value=value;o.go.transform.position=pos+Vector3.up*.12f;o.go.SetActive(true);return;}}
        private void UpdateOrbs(float dt)
        {foreach(var o in orbs){if(!o.active)continue;o.phase+=dt*3.2f;float pulse=.48f+Mathf.Sin(o.phase)*.06f+(o.value>1?.10f:0);o.go.transform.localScale=Vector3.one*pulse;o.go.transform.rotation=worldCamera.transform.rotation*Quaternion.Euler(0,0,Time.time*38+o.phase*18);Vector3 delta=player.transform.position-o.go.transform.position;float d=delta.magnitude;if(d<magnet)o.go.transform.position+=delta.normalized*Mathf.Lerp(3,12,1-d/magnet)*dt;if(d<.45f){o.active=false;o.go.SetActive(false);AddXp(o.value);}}}
        private void AddXp(int value){xp+=value;if(xp<xpNeed)return;xp-=xpNeed;xpNeed=Mathf.CeilToInt(xpNeed*1.28f+2);level++;RollUpgrades();state=State.Upgrade;}
        private void DamagePlayer(float amount){if(invulnerableTimer>0)return;hp-=amount;if(hp<=0){hp=0;state=State.Dead;}}

        private void RadialShots(Vector3 pos,int count,float amount,Color color){bool hostile=(pos-player.transform.position).sqrMagnitude>.1f;for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count;SpawnProjectile(pos,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),amount,hostile?5.5f:7,hostile,color);}}
        private void Pulse(Vector3 pos,Color color,float scale){CombatVfxPool.SpawnRing(pos,color,scale,worldCamera);}
        private void AbilityBurst(Vector3 pos,Color color,int style){AbilityVfxController.SpawnSigil(pos,color,2.15f,style,worldCamera);CombatVfxPool.SpawnRing(pos,color,2.5f,worldCamera,.78f);for(int i=0;i<8;i++){float a=i*Mathf.PI*2/8;CombatVfxPool.SpawnAttack(pos,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),color,style,worldCamera);}}

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
            AddAbility(HeroKind.Amelia,0,"СВЯЩЕННЫЙ КРУГ","Световой круг наносит урон и лечит Амелию");
            AddAbility(HeroKind.Amelia,1,"КНУТ СВЕТА","Поражает несколько ближайших целей световым кнутом");
            AddAbility(HeroKind.Amelia,2,"СВЕТИЛИЩЕ","Лечение и короткая божественная защита");
            AddAbility(HeroKind.Sam,0,"КРУГОВОЙ УДАР ПОСОХОМ","Удар вокруг Сэма с похищением здоровья");
            AddAbility(HeroKind.Sam,1,"ИМПУЛЬС СМЕРТИ","Мощные пробивающие заряды через посох");
            AddAbility(HeroKind.Sam,2,"КРОВАВАЯ ОРБИТА","Веер тёмных зарядов и восстановление здоровья");
            AddAbility(HeroKind.Zike,0,"ЦЕПНАЯ МОЛНИЯ","Молния перескакивает между противниками");
            AddAbility(HeroKind.Zike,1,"МОЛНИЕНОСНЫЙ ШАГ","Зик исчезает, неуязвим и наносит два разреза");
            AddAbility(HeroKind.Zike,2,"ГРОЗОВОЙ СЛЕД","Движение оставляет электрические импульсы");
        }
        private void AddAbility(HeroKind owner,int slot,string name,string description)
        {upgrades.Add(new Upgrade{name=name,description=description,owner=owner,ability=slot,apply=()=>{abilityRanks[slot]=Mathf.Min(6,abilityRanks[slot]+1);abilityTimers[slot]=.65f;}});}
        private void RollUpgrades()
        {
            var eligible=new List<Upgrade>();foreach(var u in upgrades)if((!u.owner.HasValue||u.owner.Value==hero.kind)&&(u.ability<0||abilityRanks[u.ability]<6))eligible.Add(u);
            if(level==2){for(int slot=0;slot<3;slot++){foreach(var u in eligible)if(u.owner==hero.kind&&u.ability==slot){offered[slot]=u;break;}}return;}
            for(int i=0;i<3;i++){Upgrade pick;do pick=eligible[Random.Range(0,eligible.Count)];while(i>0&&Array.IndexOf(offered,pick,0,i)>=0);offered[i]=pick;}
        }

        private void StartRun()
        {
            maxHp=hero.hp;hp=maxHp;damage=hero.damage;attackDelay=hero.attackDelay;moveSpeed=hero.speed;attackRange=10;critChance=.05f;magnet=2.8f;regen=0;projectileCount=1;pierce=1;
            level=1;xp=0;xpNeed=10;kills=0;runTime=0;spawnClock=0;attackClock=.4f;uniqueClock=4;bossIndex=0;bossSpawnedForStage=false;currentBoss=null;suppressionMultiplier=1;invulnerableTimer=0;zikeVanishTimer=0;characterVisual.SetVisible(true);
            for(int i=0;i<3;i++){abilityRanks[i]=0;abilityTimers[i]=0;}
            var pool=new[]{BossKind.EarthDragon,BossKind.Assassin,BossKind.EliteAgent,BossKind.BastionMech};for(int i=0;i<pool.Length;i++){int j=Random.Range(i,pool.Length);(pool[i],pool[j])=(pool[j],pool[i]);}for(int i=0;i<3;i++)selectedBosses[i]=pool[i];
            foreach(var e in enemies){e.active=false;e.go.SetActive(false);}foreach(var p in projectiles){p.active=false;p.go.SetActive(false);}foreach(var o in orbs){o.active=false;o.go.SetActive(false);}
            player.transform.position=Vector3.zero;player.SetActive(true);state=State.Playing;
            if(DebugSpawnBossImmediately){SpawnBoss(selectedBosses[0]);bossSpawnedForStage=true;}
        }

        private void InitStyles()
        {
            if(titleStyle!=null)return;titleStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=46,alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Normal,normal={textColor=Color.white}};
            buttonStyle=new GUIStyle(GUI.skin.button){font=uiFont,fontSize=23,fontStyle=FontStyle.Normal,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};hudStyle=new GUIStyle(GUI.skin.label){font=uiFont,fontSize=20,fontStyle=FontStyle.Normal,normal={textColor=Color.white}};
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
        private void OnGUI()
        {
            InitStyles();float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);float w=Screen.width/scale,h=Screen.height/scale;
            if(state==State.Menu){DrawMenuBackground(w,h);DrawPanel(new Rect(w*.28f,h*.16f,w*.44f,h*.67f));GUI.Label(new Rect(0,h*.26f,w,80),"NIGHTFALL PROTOCOL",titleStyle);GUI.Label(new Rect(0,h*.39f,w,55),"МАГИЯ ПОД НАДЗОРОМ",centerStyle);if(GUI.Button(new Rect(w*.385f,h*.56f,w*.23f,100),"ИГРАТЬ",buttonStyle))state=State.HeroSelect;GUI.Label(new Rect(0,h*.72f,w,32),"URBAN FANTASY SURVIVAL",new GUIStyle(centerStyle){fontSize=16,normal={textColor=new Color(.45f,.9f,1)}});return;}
            if(state==State.HeroSelect){DrawMenuBackground(w,h);GUI.Label(new Rect(0,22,w,65),"ВЫБЕРИТЕ ОПЕРАТИВНИКА",titleStyle);GUI.Label(new Rect(0,82,w,34),"Три боевых протокола. Один шанс пережить ночь.",new GUIStyle(centerStyle){fontSize=17});for(int i=0;i<3;i++){var d=GameCatalog.Hero((HeroKind)i);float x=w*(.09f+i*.303f),cw=w*.245f;Rect card=new Rect(x,118,cw,845);DrawCard(card);float portraitSize=Mathf.Min(230,cw-176);Rect portrait=new Rect(x+(cw-portraitSize)*.5f,218,portraitSize,portraitSize);Color portraitShade=GUI.color;GUI.color=new Color(.015f,.025f,.035f,.96f);GUI.DrawTexture(portrait,Texture2D.whiteTexture);GUI.color=Color.white;if(heroPortraits[i]!=null)GUI.DrawTexture(portrait,heroPortraits[i],ScaleMode.ScaleToFit,true);GUI.color=portraitShade;GUI.Label(new Rect(x+44,472,cw-88,44),d.displayName,new GUIStyle(centerStyle){fontSize=25});Color accent=i==0?new Color(1,.76f,.28f):i==1?new Color(.95f,.18f,.32f):new Color(.20f,.82f,1);Color old=GUI.color;GUI.color=accent;GUI.DrawTexture(new Rect(x+92,526,cw-184,3),Texture2D.whiteTexture);GUI.color=old;GUI.Label(new Rect(x+52,548,cw-104,190),d.subtitle+"\n"+d.hp.ToString("0")+" HP\n\n"+HeroAbilitiesSummary(d.kind),captionStyle);if(GUI.Button(new Rect(x+72,787,cw-144,66),"В БОЙ",buttonStyle)){SetHero(d.kind);StartRun();}}return;}
            DrawPanel(new Rect(12,8,550,150),.70f);GUI.Label(new Rect(40,26,480,36),$"{hero.displayName}   УР. {level}",hudStyle);GUI.Label(new Rect(40,70,480,32),$"HP {Mathf.CeilToInt(hp)} / {Mathf.CeilToInt(maxHp)}",hudStyle);GUI.HorizontalScrollbar(new Rect(40,112,455,18),0,hp,0,maxHp);
            GUI.Label(new Rect(w*.43f,20,260,50),$"{(int)(runTime/60):00}:{(int)(runTime%60):00}",centerStyle);GUI.HorizontalScrollbar(new Rect(w*.36f,86,w*.28f,18),0,xp,0,xpNeed);GUI.Label(new Rect(w-355,25,260,40),$"УНИЧТОЖЕНО: {kills}",hudStyle);if(state==State.Playing&&GUI.Button(new Rect(w-86,18,62,55),"Ⅱ",buttonStyle))state=State.Paused;
            DrawPanel(new Rect(w-485,h-310,470,295),.68f);for(int i=0;i<3;i++){string rank=abilityRanks[i]<=0?"не изучено":abilityRanks[i]>=6?"ЭВОЛЮЦИЯ":"★ "+abilityRanks[i];string cooldown=abilityRanks[i]>0&&abilityTimers[i]>0?"  "+abilityTimers[i].ToString("0.0")+"с":"  ГОТОВО";GUI.Label(new Rect(w-450,h-282+i*84,405,34),AbilityName(hero.kind,i)+"  ["+rank+"]"+(abilityRanks[i]>0?cooldown:""),new GUIStyle(hudStyle){fontSize=18});GUI.Label(new Rect(w-450,h-250+i*84,405,46),AbilityDescription(hero.kind,i),new GUIStyle(hudStyle){fontSize=15,fontStyle=FontStyle.Normal,wordWrap=true});}if(abilityFlashTimer>0){DrawPanel(new Rect(w*.29f,h*.16f,w*.42f,125),.74f);GUI.Label(new Rect(w*.30f,h*.18f,w*.40f,85),abilityFlash,new GUIStyle(centerStyle){wordWrap=true});}
            if(currentBoss!=null){GUI.Label(new Rect(w*.30f,122,w*.40f,38),currentBoss.boss.name,centerStyle);GUI.HorizontalScrollbar(new Rect(w*.31f,164,w*.38f,20),0,currentBoss.hp,0,currentBoss.maxHp);}
            if(joystickFinger>=0)GUI.Box(new Rect(joystickOrigin.x/scale-72,h-joystickOrigin.y/scale-72,144,144),"");
            if(state==State.Upgrade){Color old=GUI.color;GUI.color=new Color(0,0,0,.48f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;DrawPanel(new Rect(w*.14f,h*.12f,w*.72f,h*.76f),.90f);GUI.Label(new Rect(0,h*.16f,w,70),"НОВЫЙ УРОВЕНЬ",titleStyle);for(int i=0;i<3;i++){float x=w*(.19f+i*.21f);var u=offered[i];int next=u.ability>=0?abilityRanks[u.ability]+1:0;string stars=u.ability<0?"":next>=6?"\n★★★★★ → ЭВОЛЮЦИЯ":"\n"+new string('★',next)+new string('☆',5-next);Rect card=new Rect(x,h*.30f,w*.19f,430);DrawCard(card);if(GUI.Button(card,"",GUIStyle.none)){u.apply();state=State.Playing;}GUI.Label(new Rect(card.x+30,card.y+80,card.width-60,260),u.name+stars+"\n\n"+u.description,cardStyle);}}
            else if(state==State.Paused){Color old=GUI.color;GUI.color=new Color(0,0,0,.58f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;DrawPanel(new Rect(w*.33f,h*.16f,w*.34f,h*.68f),.92f);GUI.Label(new Rect(0,h*.22f,w,70),"ПАУЗА",titleStyle);if(GUI.Button(new Rect(w*.40f,h*.38f,w*.20f,75),"ПРОДОЛЖИТЬ",buttonStyle))state=State.Playing;if(GUI.Button(new Rect(w*.40f,h*.49f,w*.20f,75),"НАЧАТЬ ЗАНОВО",buttonStyle))StartRun();if(GUI.Button(new Rect(w*.40f,h*.60f,w*.20f,75),"ВЫБОР ГЕРОЯ",buttonStyle)){player.SetActive(false);state=State.HeroSelect;}}
            else if(state==State.Dead||state==State.Victory){string text=state==State.Victory?"ЗАБЕГ ЗАВЕРШЁН":"ОПЕРАТИВНИК ПОГИБ";Color old=GUI.color;GUI.color=new Color(0,0,0,.58f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=old;DrawPanel(new Rect(w*.28f,h*.25f,w*.44f,h*.48f),.92f);GUI.Label(new Rect(0,h*.33f,w,80),text,titleStyle);GUI.Label(new Rect(0,h*.44f,w,45),$"Уничтожено целей: {kills}",centerStyle);if(GUI.Button(new Rect(w*.4f,h*.56f,w*.2f,90),"ЕЩЁ РАЗ",buttonStyle))state=State.HeroSelect;}
        }

        private static Sprite CreateSolidSprite(){var t=new Texture2D(1,1,TextureFormat.RGBA32,false);t.SetPixel(0,0,Color.white);t.Apply();return Sprite.Create(t,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);}
        private static Sprite CreateProjectileSprite(){const int s=32;var t=new Texture2D(s,s,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float dx=Mathf.Abs(x+.5f-s*.5f),dy=Mathf.Abs(y+.5f-s*.5f),d=Mathf.Sqrt(dx*dx+dy*dy);bool ray=(dx<2&&dy<14)||(dy<2&&dx<14);Color c=Color.clear;if(d<5)c=new Color(1,1,1,1);else if(d<8)c=new Color(.62f,.62f,.62f,1);else if(d<10)c=new Color(.05f,.05f,.07f,1);else if(ray)c=new Color(.82f,.82f,.82f,1);p[y*s+x]=c;}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),32);}
        private static Sprite CreateExperienceSprite(){const int s=32;var t=new Texture2D(s,s,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};var p=new Color[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float dx=Mathf.Abs(x+.5f-s*.5f),dy=Mathf.Abs(y+.5f-s*.5f),diamond=dx+dy;Color c=Color.clear;if(diamond<7)c=new Color(1,1,1,1);else if(diamond<11)c=new Color(.62f,.62f,.62f,1);else if(diamond<14)c=new Color(.035f,.045f,.06f,1);else if(diamond<16)c=new Color(.35f,.35f,.35f,.55f);p[y*s+x]=c;}t.SetPixels(p);t.Apply(false,true);return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),32);}
    }

    public sealed class PulseFx:MonoBehaviour
    {
        private float target,time;public void Begin(float scale){target=scale;time=.45f;}
        private void Update(){time-=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(target,.1f,time/.45f);var r=GetComponent<SpriteRenderer>();if(r!=null)r.color=new Color(r.color.r,r.color.g,r.color.b,Mathf.Clamp01(time/.45f));if(time<=0)Destroy(gameObject);}
    }

}
