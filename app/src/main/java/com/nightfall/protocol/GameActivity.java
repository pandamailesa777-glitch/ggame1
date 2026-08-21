package com.nightfall.protocol;

import android.app.*;
import android.os.*;
import android.content.*;
import android.content.pm.ActivityInfo;
import android.graphics.*;
import android.graphics.drawable.*;
import android.view.*;
import java.util.*;

public final class GameActivity extends Activity {
    @Override public void onCreate(Bundle b) {
        super.onCreate(b); setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE);
        getWindow().setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        getWindow().getDecorView().setSystemUiVisibility(5894);
        setContentView(new GameView(this));
    }
    @Override public void onBackPressed() { GameView v=(GameView)findViewById(77); if(v!=null&&v.state!=0){v.state=0;v.resetInput();}else super.onBackPressed(); }
}

final class GameView extends SurfaceView implements SurfaceHolder.Callback, Runnable {
    static final int MENU=0, SELECT=1, PLAY=2, LEVEL=3, END=4;
    final Paint p=new Paint(3); final Random rng=new Random(); Thread loop; volatile boolean running;
    int state=MENU, hero=0, W,H, level=1, kills, upgradeCount; float px,py,hp,maxHp,xp,xpNeed,time,fireCd,skillCd,spawnCd, invuln;
    float moveSpeed, damage, attackRate, radius, magnet, regen, crit, pierce, special, projectileSize, facingX, facingY;
    float joyX,joyY,joyDX,joyDY; int joyPointer=-1; boolean bossAlive; int bossStage=-1; final int[] bossOrder=new int[3]; String endTitle="";
    Bitmap[] portraits=new Bitmap[3]; RectF[] cards={new RectF(),new RectF(),new RectF()}; int[] offered=new int[3];
    final SpriteFactory spriteFactory; final DirectionalSpriteAnimator heroVisual;
    final Enemy[] enemies=new Enemy[420]; final Shot[] shots=new Shot[180]; final Gem[] gems=new Gem[350];
    final ArrayList<Enemy> sortedEnemies=new ArrayList<>(420);
    final String[] heroNames={"АМЕЛИЯ","СЭМ","ЗИК"};
    final String[] heroRoles={"Свет • исцеление • выживание","Смерть • вампиризм • мощь","Молния • катана • скорость"};
    final String[] upNames={"Сила","Темп","Дальность","Скорость","Крепость","Регенерация","Магнит","Критический удар","Двойной импульс","Пробитие","Масштаб","Священный круг","Очищение","Вампиризм","Волна смерти","Цепная молния","Грозовой след","Небесный удар"};
    final String[] upDesc={"Урон +20%","Атаки на 15% чаще","Радиус атаки +18%","Скорость движения +12%","Макс. HP +25 и лечение","+0.8 HP в секунду","Сбор опыта +35%","Шанс крита +8%","Шанс второй атаки +18%","Снаряд проходит ещё цель","Размер атак +18%","Круг света чаще и сильнее","Круг очищает большую область","Похищение здоровья +3%","Попадание создаёт тёмную волну","Молния получает ещё цель","Движение оставляет разряды","Шанс мощной молнии +8%"};

    GameView(Context c){ super(c);setId(77);getHolder().addCallback(this);setFocusable(true);spriteFactory=new SpriteFactory(c);heroVisual=spriteFactory.create("hero_amelia");
        for(int i=0;i<enemies.length;i++)enemies[i]=new Enemy(); for(int i=0;i<shots.length;i++)shots[i]=new Shot(); for(int i=0;i<gems.length;i++)gems[i]=new Gem();
        portraits[0]=load(c,"amelia");portraits[1]=load(c,"sam");portraits[2]=load(c,"zike");
    }
    Bitmap load(Context c,String n){int id=getResources().getIdentifier(n,"drawable",c.getPackageName());return id==0?null:BitmapFactory.decodeResource(getResources(),id);}
    public void surfaceCreated(SurfaceHolder h){running=true;loop=new Thread(this,"NightfallLoop");loop.start();}
    public void surfaceDestroyed(SurfaceHolder h){running=false;try{loop.join(500);}catch(Exception ignored){}}
    public void surfaceChanged(SurfaceHolder h,int f,int w,int hh){W=w;H=hh;joyX=W*.13f;joyY=H*.78f;}
    public void run(){long last=System.nanoTime();while(running){long n=System.nanoTime();float dt=Math.min(.033f,(n-last)/1e9f);last=n;if(state==PLAY)update(dt);Canvas c=null;try{c=getHolder().lockCanvas();if(c!=null)drawAll(c);}finally{if(c!=null)getHolder().unlockCanvasAndPost(c);} long used=System.nanoTime()-n;if(used<15000000)try{Thread.sleep((15000000-used)/1000000);}catch(Exception ignored){}}}

    void startRun(){state=PLAY;px=py=time=xp=0;level=1;kills=upgradeCount=0;xpNeed=20;bossAlive=false;bossStage=-1;spawnCd=fireCd=skillCd=0;invuln=0;
        bossOrder[0]=rng.nextBoolean()?0:1; bossOrder[1]=rng.nextInt(4); do{bossOrder[2]=2+rng.nextInt(2);}while(bossOrder[2]==bossOrder[1]);
        maxHp=hero==0?145:hero==1?100:82;hp=maxHp;moveSpeed=hero==2?245:hero==0?185:200;damage=hero==1?29:hero==2?19:22;attackRate=hero==2?2.1f:hero==1?1.15f:1.4f;radius=360;magnet=78;regen=hero==0?.7f:0;crit=hero==2?.08f:.04f;pierce=hero==1?1:0;special=1;projectileSize=1;facingX=0;facingY=1;spriteFactory.bind(heroVisual,hero==0?"hero_amelia":hero==1?"hero_sam":"hero_zike");
        for(Enemy e:enemies)e.on=false;for(Shot s:shots)s.on=false;for(Gem g:gems)g.on=false;
    }
    void update(float dt){time+=dt;invuln-=dt;hp=Math.min(maxHp,hp+regen*dt);if(joyDX*joyDX+joyDY*joyDY>.02f){facingX=joyDX;facingY=joyDY;}px+=joyDX*moveSpeed*dt;py+=joyDY*moveSpeed*dt;
        float limit=2800;px=Math.max(-limit,Math.min(limit,px));py=Math.max(-limit,Math.min(limit,py));
        int stage=time<180?0:time<360?1:time<540?2:3;spawnCd-=dt; if(spawnCd<=0){int batch=2+stage*2+(int)(time/150);for(int i=0;i<batch;i++)spawnEnemy(stage);spawnCd=Math.max(.16f,.55f-stage*.08f-time/2500f);}
        if(!bossAlive){if(time>=660&&bossStage<2){bossStage=2;spawnBoss(bossOrder[2]);}else if(time>=480&&bossStage<1){bossStage=1;spawnBoss(bossOrder[1]);}else if(time>=240&&bossStage<0){bossStage=0;spawnBoss(bossOrder[0]);}}
        Enemy nearest=null;float nd=Float.MAX_VALUE;
        for(Enemy e:enemies)if(e.on){float dx=px-e.x,dy=py-e.y,d2=dx*dx+dy*dy;if(!e.dying&&d2<nd){nd=d2;nearest=e;} updateEnemy(e,dt,dx,dy,d2);}
        fireCd-=dt;for(Enemy e:enemies)if(e.on&&e.type==13&&e.suppress>0&&dist2(px,py,e.x,e.y)<210*210)fireCd+=dt*.55f;if(fireCd<=0&&nearest!=null&&nd<radius*radius){attack(nearest);fireCd=1f/attackRate;}
        skillCd-=dt;if(skillCd<=0){if(hero==0)holyCircle();else if(hero==1)deathWave();else chainLightning();skillCd=(hero==0?7.5f:hero==1?6.5f:5.5f)/special;}
        for(Shot s:shots)if(s.on)updateShot(s,dt);for(Gem g:gems)if(g.on)updateGem(g,dt);
        heroVisual.setDirection(facingX,facingY);if(!heroVisual.oneShot)heroVisual.play(joyDX*joyDX+joyDY*joyDY>.02f?"move":"idle");heroVisual.update(dt);if(hp<=0){hp=0;state=END;endTitle="ПОРАЖЕНИЕ";resetInput();}
    }
    void spawnEnemy(int stage){Enemy e=freeEnemy();if(e==null)return;double a=rng.nextDouble()*Math.PI*2;float d=620+rng.nextFloat()*220;e.x=px+(float)Math.cos(a)*d;e.y=py+(float)Math.sin(a)*d;e.on=true;e.boss=false;e.cool=rng.nextFloat();
        int roll=rng.nextInt(100);e.type=stage==0?(roll<62?0:roll<88?4:1):stage==1?(roll<30?0:roll<52?4:roll<68?1:roll<82?2:5):stage==2?(roll<18?0:roll<36?4:roll<49?2:roll<63?3:roll<80?5:roll<92?6:7):(roll<15?0:roll<35?4:roll<47?2:roll<61?3:roll<75?5:roll<89?6:7);
        float scale=1+time/700;float baseHp=e.type==0?30:e.type==1?48:e.type==2?38:e.type==3?78:e.type==4?66:e.type==5?52:e.type==6?190:32;e.hp=e.maxHp=baseHp*scale;e.speed=e.type==0?82:e.type==1?58:e.type==2?50:e.type==3?62:e.type==4?34:e.type==5?56:e.type==6?32:105;e.damage=e.type==6?25:e.type==3?15:e.type==0?11:e.type==5?13:9;e.state=0;e.abilityCd=1+rng.nextFloat()*2;e.suppress=0;e.dying=false;e.deathTimer=0;spriteFactory.bind(e.visual,e.type==4?"enemy_zombie":null);
    }
    void spawnBoss(int type){Enemy e=freeEnemy();if(e==null)return;bossAlive=true;e.on=e.boss=true;e.dying=false;e.type=10+type;e.x=px+650;e.y=py;e.cool=2;e.state=0;e.abilityCd=5;e.suppress=0;e.speed=type==0?38:type==1?115:type==2?48:44;e.maxHp=e.hp=type==0?3500:type==1?2800:type==2?4800:5600;e.damage=type==0?24:type==1?20:type==2?18:26;spriteFactory.bind(e.visual,null);}
    Enemy freeEnemy(){for(Enemy e:enemies)if(!e.on)return e;return null;}
    void updateEnemy(Enemy e,float dt,float dx,float dy,float d2){if(e.dying){e.deathTimer-=dt;e.visual.update(dt);if(e.deathTimer<=0){e.on=false;e.dying=false;e.visual.reset();}return;}float d=(float)Math.sqrt(d2)+.01f;e.cool-=dt;e.abilityCd-=dt;e.visual.setDirection(dx,dy);if(!e.visual.oneShot)e.visual.play("move");e.visual.update(dt);if(e.type==13){updateMech(e,dt,dx,dy,d);return;}
        if(e.type==2||e.type==3||e.type==7||e.type==12){float ideal=e.type==12?360:e.type==7?340:300;if(d>ideal)e.x+=dx/d*e.speed*dt;if(d<ideal-70){e.x-=dx/d*e.speed*.5f*dt;e.y-=dy/d*e.speed*.5f*dt;}if(e.cool<=0){enemyBullet(e);e.cool=e.type==7?1.25f:e.type==12?.7f:1.7f;}}
        else {float speed=e.speed;if(e.type==11&&e.cool<=0){speed*=5;e.cool=2.7f;}if(e.type==10&&e.cool<=0){speed*=4;e.cool=3.6f;}e.x+=dx/d*speed*dt;e.y+=dy/d*speed*dt;}
        if(e.type==5&&d<145&&e.abilityCd<=0){areaHit(px,py,72,10,false);e.abilityCd=3.2f;}
        if(e.type==6&&d<70&&e.abilityCd<=0){areaHit(e.x,e.y,88,18,false);px+=dx/d*55;py+=dy/d*55;e.abilityCd=3.8f;}
        if(d<30+(e.boss?38:0)&&invuln<=0){hp-=e.damage;invuln=.45f;e.visual.playOneShot("attack","move");}
        if(e.boss&&e.type==10&&e.cool<.35f&&e.cool>0)areaHit(e.x,e.y,110,12,false);
        if(e.boss&&e.type==12&&e.cool>.55f&&e.cool<.6f)areaHit(px,py,70,14,false);
    }
    void updateMech(Enemy e,float dt,float dx,float dy,float d){boolean phase2=e.hp<e.maxHp*.5f;e.suppress=Math.max(0,e.suppress-dt);if(e.abilityCd<=0){e.suppress=4;e.abilityCd=phase2?8:11;}
        if(e.state==0){float ideal=260;if(d>ideal){e.x+=dx/d*e.speed*(phase2?1.35f:1)*dt;e.y+=dy/d*e.speed*(phase2?1.35f:1)*dt;}if(e.cool<=0){int pick=rng.nextInt(phase2?4:3);e.state=pick+1;e.telegraph=pick==1?1.15f:.75f;e.tx=px;e.ty=py;e.cool=phase2?1.8f:2.8f;}}
        else {e.telegraph-=dt;if(e.telegraph<=0){if(e.state==1){for(int i=-2;i<=2;i++)enemyBulletAim(e,px+i*28,py+i*12,360,13);}else if(e.state==2){areaHit(e.tx,e.ty,92,22,false);}else if(e.state==3){areaHit(e.x,e.y,130,28,false);if(d<130){px+=dx/d*75;py+=dy/d*75;}}else if(e.state==4){e.x+=dx/d*260;e.y+=dy/d*260;areaHit(e.x,e.y,68,24,false);}e.state=0;}}
        if(d<42&&invuln<=0){hp-=e.damage;invuln=.5f;}
    }
    void enemyBulletAim(Enemy e,float tx,float ty,float speed,float dmg){float dx=tx-e.x,dy=ty-e.y,d=(float)Math.sqrt(dx*dx+dy*dy);Shot s=freeShot();if(s==null)return;s.on=true;s.enemy=true;s.x=e.x;s.y=e.y;s.vx=dx/d*speed;s.vy=dy/d*speed;s.life=2.5f;s.damage=dmg;s.color=Color.rgb(255,155,55);s.size=7;}
    void enemyBullet(Enemy e){float dx=px-e.x,dy=py-e.y,d=(float)Math.sqrt(dx*dx+dy*dy);Shot s=freeShot();if(s==null)return;s.on=true;s.enemy=true;s.x=e.x;s.y=e.y;s.vx=dx/d*280;s.vy=dy/d*280;s.life=2.5f;s.damage=e.damage;s.color=e.type==3||e.type==12?Color.rgb(255,70,55):Color.rgb(255,190,70);s.size=7;}
    void attack(Enemy target){float dx=target.x-px,dy=target.y-py,d=(float)Math.sqrt(dx*dx+dy*dy);facingX=dx/d;facingY=dy/d;heroVisual.setDirection(facingX,facingY);heroVisual.playOneShot("attack",joyDX*joyDX+joyDY*joyDY>.02f?"move":"idle");if(hero==2){areaHit(target.x,target.y,46*projectileSize,damage,true);flashShot(px,py,target.x,target.y,Color.CYAN,12);}
        else {Shot s=freeShot();if(s!=null){s.on=true;s.enemy=false;s.x=px;s.y=py;s.vx=dx/d*(hero==0?390:460);s.vy=dy/d*(hero==0?390:460);s.life=1.5f;s.damage=damage;s.pierce=(int)pierce;s.color=hero==0?Color.rgb(255,238,150):Color.rgb(190,35,65);s.size=(hero==0?12:9)*projectileSize;}}
        if(rng.nextFloat()<.18f*upgradeCount/6f)fireCd*=.2f;
    }
    Shot freeShot(){for(Shot s:shots)if(!s.on)return s;return null;}
    void updateShot(Shot s,float dt){s.x+=s.vx*dt;s.y+=s.vy*dt;s.life-=dt;if(s.life<=0){s.on=false;return;}if(s.enemy){float dx=s.x-px,dy=s.y-py;if(dx*dx+dy*dy<500&&invuln<=0){hp-=s.damage;invuln=.35f;s.on=false;}return;}
        for(Enemy e:enemies)if(e.on){float dx=s.x-e.x,dy=s.y-e.y;if(dx*dx+dy*dy<(20+s.size)*(20+s.size)){hit(e,s.damage);if(hero==1&&special>1)areaHit(e.x,e.y,40,damage*.22f,true);if(s.pierce--<=0)s.on=false;return;}}
    }
    void hit(Enemy e,float amount){if(e.dying)return;boolean c=rng.nextFloat()<crit;float dealt=amount*(c?2:1);e.hp-=dealt;e.visual.playOneShot("hit","move");if(hero==1)hp=Math.min(maxHp,hp+dealt*(.01f+(special-1)*.015f));if(e.hp<=0)kill(e);}
    void kill(Enemy e){if(e.dying)return;e.dying=true;e.deathTimer=.35f;e.visual.playOneShot("death","idle");kills++;if(e.type==5&&rng.nextFloat()<.28f)areaHit(e.x,e.y,82,16,false);dropGem(e.x,e.y,e.boss?80:e.type==6?8:e.type==3?5:2);if(e.boss){bossAlive=false;if(bossStage==2){state=END;endTitle="ПОБЕДА";resetInput();}}}
    void dropGem(float x,float y,int val){Gem g=null;for(Gem q:gems)if(!q.on){g=q;break;}if(g==null)return;g.on=true;g.x=x;g.y=y;g.value=val;}
    void updateGem(Gem g,float dt){float dx=px-g.x,dy=py-g.y,d2=dx*dx+dy*dy;if(d2<magnet*magnet){float d=(float)Math.sqrt(d2)+1;g.x+=dx/d*420*dt;g.y+=dy/d*420*dt;}if(d2<625){g.on=false;xp+=g.value;if(xp>=xpNeed){xp-=xpNeed;level++;xpNeed=18+level*10;rollUpgrades();state=LEVEL;resetInput();}}}
    void holyCircle(){areaHit(px,py,115*projectileSize,damage*1.5f,true);hp=Math.min(maxHp,hp+5*special);}
    void deathWave(){areaHit(px,py,95*projectileSize,damage*.85f,true);}
    void chainLightning(){Enemy cur=null;for(Enemy e:enemies)if(e.on&&(cur==null||dist2(e.x,e.y,px,py)<dist2(cur.x,cur.y,px,py)))cur=e;float sx=px,sy=py;int n=2+(int)special;if(cur!=null)for(int i=0;i<n;i++){flashShot(sx,sy,cur.x,cur.y,Color.CYAN,8);hit(cur,damage*.9f);sx=cur.x;sy=cur.y;Enemy next=null;for(Enemy e:enemies)if(e.on&&e!=cur&&dist2(e.x,e.y,sx,sy)<180*180&&(next==null||dist2(e.x,e.y,sx,sy)<dist2(next.x,next.y,sx,sy)))next=e;cur=next;if(cur==null)break;}}
    void areaHit(float x,float y,float r,float dmg,boolean toEnemy){if(toEnemy){for(Enemy e:enemies)if(e.on&&dist2(e.x,e.y,x,y)<r*r)hit(e,dmg);}else if(dist2(px,py,x,y)<r*r&&invuln<=0){hp-=dmg;invuln=.4f;}effects.add(new Fx(x,y,r,toEnemy?Color.argb(150,100,220,255):Color.argb(160,255,70,50)));}
    final ArrayList<Fx> effects=new ArrayList<>();void flashShot(float x,float y,float x2,float y2,int c,float w){effects.add(new Fx(x,y,x2,y2,c,w));}
    static float dist2(float x,float y,float a,float b){x-=a;y-=b;return x*x+y*y;}

    void rollUpgrades(){boolean[] used=new boolean[upNames.length];for(int i=0;i<3;i++){int u;do{u=rng.nextInt(upNames.length);}while(used[u]||!validUpgrade(u));used[u]=true;offered[i]=u;}}
    boolean validUpgrade(int u){if(u>=11&&u<=13)return hero==0;if(u==14)return hero==1;if(u>=15)return hero==2;return true;}
    void applyUpgrade(int u){upgradeCount++;switch(u){case 0:damage*=1.2f;break;case 1:attackRate*=1.15f;break;case 2:radius*=1.18f;break;case 3:moveSpeed*=1.12f;break;case 4:maxHp+=25;hp+=25;break;case 5:regen+=.8f;break;case 6:magnet*=1.35f;break;case 7:crit+=.08f;break;case 8:attackRate*=1.18f;break;case 9:pierce++;break;case 10:projectileSize*=1.18f;break;case 11:special*=1.2f;break;case 12:projectileSize*=1.25f;break;case 13:special+=.4f;break;case 14:special+=.5f;break;case 15:special+=1;break;case 16:attackRate*=1.25f;break;case 17:crit+=.1f;break;}state=PLAY;}

    void drawAll(Canvas c){W=c.getWidth();H=c.getHeight();p.setTypeface(Typeface.create("sans",Typeface.NORMAL));if(state==MENU)drawMenu(c);else if(state==SELECT)drawSelect(c);else{drawWorld(c);drawHud(c);if(state==LEVEL)drawLevel(c);if(state==END)drawEnd(c);}}
    void bg(Canvas c,int color){c.drawColor(color);}
    void drawMenu(Canvas c){bg(c,Color.rgb(5,11,18));p.setShader(new LinearGradient(0,0,W,H,Color.rgb(12,35,45),Color.rgb(30,8,25),Shader.TileMode.CLAMP));c.drawRect(0,0,W,H,p);p.setShader(null);text(c,"NIGHTFALL",W/2,H*.32f,H*.13f,Color.WHITE,true);text(c,"ПРОТОКОЛ СУМЕРЕК",W/2,H*.43f,H*.047f,Color.rgb(112,221,244),true);button(c,W*.37f,H*.61f,W*.63f,H*.78f,"ИГРАТЬ");text(c,"Городское фэнтези • survivors-like",W/2,H*.9f,H*.027f,Color.LTGRAY,true);}
    void drawSelect(Canvas c){bg(c,Color.rgb(7,14,21));text(c,"ВЫБЕРИТЕ ОПЕРАТИВНИКА",W/2,H*.105f,H*.052f,Color.WHITE,true);float cw=W*.25f,gap=W*.025f,start=(W-(cw*3+gap*2))/2;for(int i=0;i<3;i++){float l=start+i*(cw+gap);cards[i].set(l,H*.16f,l+cw,H*.86f);p.setColor(i==hero?Color.rgb(35,85,100):Color.rgb(18,25,34));c.drawRoundRect(cards[i],18,18,p);if(portraits[i]!=null)c.drawBitmap(portraits[i],null,new RectF(l+8,H*.17f,l+cw-8,H*.59f),p);p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(i==hero?5:2);p.setColor(i==hero?Color.CYAN:Color.DKGRAY);c.drawRoundRect(cards[i],18,18,p);p.setStyle(Paint.Style.FILL);text(c,heroNames[i],l+cw/2,H*.67f,H*.044f,Color.WHITE,true);text(c,heroRoles[i],l+cw/2,H*.73f,H*.021f,Color.LTGRAY,true);text(c,i==0?"145 HP":i==1?"100 HP • посох":"82 HP • катана",l+cw/2,H*.79f,H*.024f,i==hero?Color.CYAN:Color.GRAY,true);}button(c,W*.41f,H*.88f,W*.59f,H*.98f,"В БОЙ");}
    void drawWorld(Canvas c){bg(c,Color.rgb(22,29,34));float ox=W*.5f,oy=H*.52f; // faux perspective city grid
        p.setStrokeWidth(2);p.setColor(Color.rgb(46,56,60));for(int i=-12;i<=12;i++){float x=ox+i*95-(px%95);c.drawLine(x,0,x,H,p);}for(int i=-8;i<=8;i++){float y=oy+i*58-(py%58)*.55f;c.drawLine(0,y,W,y,p);}p.setColor(Color.rgb(55,68,70));for(int i=-4;i<5;i+=2)c.drawRect(ox+i*230-(px%460)-90,oy-360-(py%320)*.55f,ox+i*230-(px%460)+65,oy-110-(py%320)*.55f,p);
        sortedEnemies.clear();for(Enemy e:enemies)if(e.on)sortedEnemies.add(e);Collections.sort(sortedEnemies,(a,b)->Float.compare(a.y+(a.visual.definition==null?0:a.visual.definition.sortOffset),b.y+(b.visual.definition==null?0:b.visual.definition.sortOffset)));for(Gem g:gems)if(g.on)drawGem(c,g);for(Enemy e:sortedEnemies)drawEnemy(c,e);for(Shot s:shots)if(s.on)drawShot(c,s);drawHero(c);
        for(int i=effects.size()-1;i>=0;i--){Fx f=effects.get(i);f.life-=.035f;if(f.life<=0){effects.remove(i);continue;}drawFx(c,f);}
        if(state==PLAY){p.setColor(Color.argb(55,255,255,255));c.drawCircle(joyX,joyY,H*.105f,p);p.setColor(Color.argb(120,112,221,244));c.drawCircle(joyX+joyDX*H*.055f,joyY+joyDY*H*.055f,H*.045f,p);}
    }
    float sx(float x){return W*.5f+(x-px);}float sy(float y){return H*.52f+(y-py)*.55f;}
    void drawHero(Canvas c){float x=W*.5f,y=H*.52f;p.setColor(Color.argb(90,0,0,0));c.drawOval(x-34,y+17,x+34,y+33,p);float bob="move".equals(heroVisual.state)?(float)Math.sin(time*12)*4:0;spriteFactory.draw(c,p,heroVisual,x,y+32+bob,152,1);if(invuln>0){p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(4);p.setColor(Color.WHITE);c.drawCircle(x,y,42,p);p.setStyle(Paint.Style.FILL);}}
    void drawEnemy(Canvas c,Enemy e){float x=sx(e.x),y=sy(e.y);if(x<-150||x>W+150||y<-150||y>H+150)return;if(e.type==4&&e.visual.definition!=null){p.setColor(Color.argb(90,0,0,0));c.drawOval(x-20,y+15,x+20,y+27,p);spriteFactory.draw(c,p,e.visual,x,y+24,82,e.dying?Math.max(0,e.deathTimer/.35f):1);return;}float z=e.boss?(e.type==10?2.2f:e.type==13?1.9f:1.55f):e.type==6?1.45f:e.type==7?.72f:1;
        if(e.type==13&&e.suppress>0){p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(5);p.setColor(Color.argb(100,70,190,230));c.drawOval(x-210,y-115,x+210,y+115,p);p.setStyle(Paint.Style.FILL);}if(e.type==13&&e.state>0){p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(5);p.setColor(Color.argb(210,255,80,45));if(e.state==1||e.state==4)c.drawLine(x,y,W*.5f,H*.52f,p);else if(e.state==2)c.drawOval(sx(e.tx)-92,sy(e.ty)-51,sx(e.tx)+92,sy(e.ty)+51,p);else c.drawOval(x-130,y-72,x+130,y+72,p);p.setStyle(Paint.Style.FILL);}
        p.setColor(Color.argb(100,0,0,0));c.drawOval(x-20*z,y+15*z,x+20*z,y+27*z,p);int col=e.type==0?Color.rgb(115,35,55):e.type==1?Color.rgb(170,105,45):e.type==2?Color.rgb(195,135,55):e.type==3?Color.rgb(45,100,115):e.type==4?Color.rgb(76,102,65):e.type==5?Color.rgb(105,45,125):e.type==6?Color.rgb(92,125,76):e.type==7?Color.rgb(65,150,175):e.type==10?Color.rgb(125,90,45):e.type==11?Color.rgb(20,20,25):e.type==13?Color.rgb(75,86,91):Color.rgb(32,75,88);p.setColor(col);c.drawCircle(x,y,19*z,p);
        if(e.type==5){p.setColor(Color.MAGENTA);c.drawCircle(x-7,y-5,3,p);c.drawCircle(x+7,y-5,3,p);}if(e.type==7){p.setStrokeWidth(3);p.setColor(Color.CYAN);c.drawLine(x-24,y,x+24,y,p);c.drawCircle(x,y-12,5,p);}if(e.type==3||e.type==12||e.type==13){p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(4);p.setColor(Color.CYAN);c.drawCircle(x,y,14*z,p);p.setStyle(Paint.Style.FILL);}if(e.type==10){p.setStrokeWidth(10);p.setColor(Color.rgb(155,115,60));c.drawLine(x-45,y,x+52,y-28,p);}if(e.type==13){p.setColor(Color.rgb(40,48,52));c.drawRect(x-28,y-25,x+28,y+28,p);p.setColor(e.hp<e.maxHp*.5f?Color.rgb(255,90,45):Color.rgb(90,220,240));c.drawCircle(x,y-12,6,p);if(e.hp<e.maxHp*.5f){p.setColor(Color.LTGRAY);c.drawCircle(x+26,y-32,5,p);c.drawCircle(x+34,y-46,3,p);}}
        if(e.boss){p.setColor(Color.rgb(30,5,5));c.drawRect(x-50,y-58,x+50,y-50,p);p.setColor(Color.RED);c.drawRect(x-50,y-58,x-50+100*e.hp/e.maxHp,y-50,p);}}
    void drawShot(Canvas c,Shot s){float x=sx(s.x),y=sy(s.y);p.setColor(s.color);c.drawCircle(x,y,s.size,p);}
    void drawGem(Canvas c,Gem g){float x=sx(g.x),y=sy(g.y);p.setColor(Color.rgb(70,245,215));Path q=new Path();q.moveTo(x,y-8);q.lineTo(x+7,y);q.lineTo(x,y+8);q.lineTo(x-7,y);q.close();c.drawPath(q,p);}
    void drawFx(Canvas c,Fx f){p.setColor(f.color);p.setAlpha((int)(220*f.life));if(f.line){p.setStrokeWidth(f.w);c.drawLine(sx(f.x),sy(f.y),sx(f.x2),sy(f.y2),p);}else{p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(8);c.drawOval(sx(f.x)-f.r,sy(f.y)-f.r*.55f,sx(f.x)+f.r,sy(f.y)+f.r*.55f,p);p.setStyle(Paint.Style.FILL);}p.setAlpha(255);}
    void drawHud(Canvas c){p.setColor(Color.argb(180,5,10,15));c.drawRoundRect(18,15,W*.34f,H*.15f,14,14,p);text(c,heroNames[hero]+"  УР. "+level,34,H*.058f,H*.027f,Color.WHITE,false);bar(c,34,H*.075f,W*.27f,H*.024f,hp/maxHp,Color.rgb(220,55,65));text(c,(int)hp+" / "+(int)maxHp,40,H*.099f,H*.018f,Color.WHITE,false);bar(c,34,H*.116f,W*.27f,H*.015f,xp/xpNeed,Color.rgb(70,230,210));text(c,String.format(Locale.US,"%02d:%02d",(int)time/60,(int)time%60),W*.5f,H*.07f,H*.045f,Color.WHITE,true);text(c,"Уничтожено: "+kills,W*.98f,H*.055f,H*.025f,Color.LTGRAY,false,true);if(bossAlive){Enemy b=null;for(Enemy e:enemies)if(e.on&&e.boss)b=e;if(b!=null){String n=b.type==10?"ЗЕМЛЯНОЙ ДРАКОН":b.type==11?"УБИЙЦА":b.type==12?"ЭЛИТНЫЙ АГЕНТ БЮРО":"МЕХ-КОСТЮМ «БАСТИОН»";text(c,n,W/2,H*.13f,H*.026f,Color.WHITE,true);bar(c,W*.25f,H*.145f,W*.5f,H*.018f,b.hp/b.maxHp,Color.rgb(220,45,45));if(b.type==13&&b.suppress>0)text(c,"ПОЛЕ ПОДАВЛЕНИЯ: АТАКИ ЗАМЕДЛЕНЫ",W/2,H*.19f,H*.021f,Color.CYAN,true);}}}
    void drawLevel(Canvas c){p.setColor(Color.argb(220,3,8,15));c.drawRect(0,0,W,H,p);text(c,"НОВЫЙ УРОВЕНЬ",W/2,H*.16f,H*.06f,Color.WHITE,true);float cw=W*.25f,gap=W*.025f,start=(W-(cw*3+gap*2))/2;for(int i=0;i<3;i++){float l=start+i*(cw+gap);cards[i].set(l,H*.25f,l+cw,H*.75f);p.setColor(Color.rgb(21,39,50));c.drawRoundRect(cards[i],20,20,p);p.setColor(i==0?Color.rgb(250,220,100):i==1?Color.rgb(210,70,90):Color.CYAN);c.drawCircle(l+cw/2,H*.38f,H*.065f,p);text(c,upNames[offered[i]],l+cw/2,H*.54f,H*.035f,Color.WHITE,true);text(c,upDesc[offered[i]],l+cw/2,H*.63f,H*.025f,Color.LTGRAY,true);}}
    void drawEnd(Canvas c){p.setColor(Color.argb(225,3,7,12));c.drawRect(0,0,W,H,p);text(c,endTitle,W/2,H*.32f,H*.1f,endTitle.equals("ПОБЕДА")?Color.CYAN:Color.rgb(240,70,70),true);text(c,"Время  "+String.format(Locale.US,"%02d:%02d",(int)time/60,(int)time%60)+"   •   Уничтожено  "+kills+"   •   Уровень  "+level,W/2,H*.48f,H*.032f,Color.WHITE,true);button(c,W*.36f,H*.62f,W*.64f,H*.78f,"К ВЫБОРУ ГЕРОЯ");}
    void button(Canvas c,float l,float t,float r,float b,String s){p.setColor(Color.rgb(28,91,109));c.drawRoundRect(l,t,r,b,18,18,p);p.setStyle(Paint.Style.STROKE);p.setStrokeWidth(3);p.setColor(Color.CYAN);c.drawRoundRect(l,t,r,b,18,18,p);p.setStyle(Paint.Style.FILL);text(c,s,(l+r)/2,(t+b)/2+H*.013f,H*.034f,Color.WHITE,true);}
    void bar(Canvas c,float x,float y,float w,float h,float v,int col){p.setColor(Color.rgb(22,28,32));c.drawRoundRect(x,y,x+w,y+h,h/2,h/2,p);p.setColor(col);c.drawRoundRect(x,y,x+w*Math.max(0,Math.min(1,v)),y+h,h/2,h/2,p);}
    void text(Canvas c,String s,float x,float y,float size,int col,boolean center){text(c,s,x,y,size,col,center,false);}void text(Canvas c,String s,float x,float y,float size,int col,boolean center,boolean right){p.setTextSize(size);p.setColor(col);p.setTypeface(Typeface.create("sans",Typeface.BOLD));p.setTextAlign(center?Paint.Align.CENTER:right?Paint.Align.RIGHT:Paint.Align.LEFT);c.drawText(s,x,y,p);}
    void tri(Canvas c,float a,float b,float d,float e,float f,float g){Path q=new Path();q.moveTo(a,b);q.lineTo(d,e);q.lineTo(f,g);q.close();c.drawPath(q,p);}

    @Override public boolean onTouchEvent(MotionEvent e){int action=e.getActionMasked(),idx=e.getActionIndex();float x=e.getX(idx),y=e.getY(idx);if(action==MotionEvent.ACTION_DOWN){if(state==MENU){state=SELECT;return true;}if(state==SELECT){for(int i=0;i<3;i++)if(cards[i].contains(x,y)){hero=i;return true;}if(y>H*.86f){startRun();return true;}}if(state==LEVEL){for(int i=0;i<3;i++)if(cards[i].contains(x,y)){applyUpgrade(offered[i]);return true;}}if(state==END){state=SELECT;return true;}if(state==PLAY&&x<W*.32f&&y>H*.48f){joyPointer=e.getPointerId(idx);updateJoy(x,y);return true;}}
        if(action==MotionEvent.ACTION_MOVE&&state==PLAY&&joyPointer>=0){int pi=e.findPointerIndex(joyPointer);if(pi>=0)updateJoy(e.getX(pi),e.getY(pi));return true;}if((action==MotionEvent.ACTION_UP||action==MotionEvent.ACTION_POINTER_UP||action==MotionEvent.ACTION_CANCEL)&&e.getPointerId(idx)==joyPointer){resetInput();return true;}return true;}
    void updateJoy(float x,float y){float dx=x-joyX,dy=y-joyY,d=(float)Math.sqrt(dx*dx+dy*dy),m=H*.095f;if(d>m){dx*=m/d;dy*=m/d;}joyDX=dx/m;joyDY=dy/m/.55f;float q=(float)Math.sqrt(joyDX*joyDX+joyDY*joyDY);if(q>1){joyDX/=q;joyDY/=q;}}
    void resetInput(){joyPointer=-1;joyDX=joyDY=0;}
    static final class Enemy{boolean on,boss,dying;int type,state;float x,y,hp,maxHp,speed,damage,cool,abilityCd,suppress,telegraph,tx,ty,deathTimer;final DirectionalSpriteAnimator visual=new DirectionalSpriteAnimator();}
    static final class Shot{boolean on,enemy;float x,y,vx,vy,life,damage,size;int pierce,color;}
    static final class Gem{boolean on;float x,y;int value;}
    static final class Fx{float x,y,x2,y2,r,w,life=1;int color;boolean line;Fx(float a,float b,float rr,int c){x=a;y=b;r=rr;color=c;}Fx(float a,float b,float c,float d,int col,float ww){x=a;y=b;x2=c;y2=d;color=col;w=ww;line=true;}}
}
