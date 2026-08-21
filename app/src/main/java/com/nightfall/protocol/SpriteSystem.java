package com.nightfall.protocol;

import android.content.Context;
import android.graphics.*;
import org.json.*;
import java.io.*;
import java.util.*;

final class SpriteAnimationDefinition {
    String id, sheet, layout;
    int columns=1, rows=1, entityRow=0, frames=1, directions=1;
    float fps=1;
    boolean loop=true;
    int[] directionMap, mirrorDirections;

    boolean isMirrored(int direction){if(mirrorDirections==null)return false;for(int d:mirrorDirections)if(d==direction)return true;return false;}
}

final class SpriteEntityDefinition {
    String id, entityType;
    float scale=1, pivotX=.5f, pivotY=.85f, sortOffset=0;
    int fallbackColor=Color.MAGENTA;
    final HashMap<String,SpriteAnimationDefinition> animations=new HashMap<>();
    SpriteAnimationDefinition animation(String name){SpriteAnimationDefinition a=animations.get(name);if(a==null)a=animations.get("idle");return a;}
}

final class DirectionalSpriteAnimator {
    SpriteEntityDefinition definition; SpriteAnimationDefinition animation;
    String state="idle", queuedState="idle"; float time, directionX=0, directionY=1;
    boolean oneShot; Runnable completion;
    final Rect source=new Rect(); final RectF destination=new RectF();

    void bind(SpriteEntityDefinition d){definition=d;reset();}
    void reset(){state="idle";queuedState="idle";time=0;oneShot=false;completion=null;directionX=0;directionY=1;animation=definition==null?null:definition.animation("idle");}
    void play(String next){if(definition==null)return;if(!oneShot&&next.equals(state))return;state=next;animation=definition.animation(next);time=0;oneShot=false;completion=null;}
    void playOneShot(String next,String after){if(definition==null)return;state=next;queuedState=after;animation=definition.animation(next);time=0;oneShot=true;completion=null;}
    void playOneShot(String next,String after,Runnable callback){playOneShot(next,after);completion=callback;}
    void setDirection(float x,float y){if(x*x+y*y<.0001f)return;directionX=x;directionY=y;}
    void update(float dt){if(animation==null)return;time+=dt;float length=Math.max(1,animation.frames)/Math.max(.01f,animation.fps);if(oneShot&&time>=length){Runnable done=completion;oneShot=false;play(queuedState);if(done!=null)done.run();}else if(animation.loop&&time>=length)time%=length;}
    int frame(){if(animation==null)return 0;int f=(int)(time*animation.fps);return animation.loop?f%Math.max(1,animation.frames):Math.min(animation.frames-1,f);}
    int direction(){int count=animation==null?1:animation.directions;if(count<=1)return 0;double angle=Math.atan2(directionY,directionX);int d=(int)Math.round(angle/(Math.PI*2/count));return (d+count)%count;}
}

final class SpriteFactory {
    private final Context context;
    private final HashMap<String,SpriteEntityDefinition> definitions=new HashMap<>();
    private final HashMap<String,Bitmap> bitmaps=new HashMap<>();

    SpriteFactory(Context c){context=c.getApplicationContext();loadRegistry("sprite_factory/entities.json");loadRegistry("sprite_factory/generated_entities.json");}
    SpriteEntityDefinition get(String id){return definitions.get(id);}
    void bind(DirectionalSpriteAnimator animator,String id){animator.bind(get(id));}
    DirectionalSpriteAnimator create(String id){DirectionalSpriteAnimator a=new DirectionalSpriteAnimator();bind(a,id);return a;}

    private void loadRegistry(String asset){try(InputStream in=context.getAssets().open(asset)){ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] buf=new byte[4096];for(int n;(n=in.read(buf))>0;)out.write(buf,0,n);JSONObject root=new JSONObject(out.toString("UTF-8"));JSONArray entities=root.getJSONArray("entities");for(int i=0;i<entities.length();i++)parseEntity(entities.getJSONObject(i));}catch(IOException ignored){}catch(Exception e){throw new IllegalStateException("Invalid sprite registry: "+asset,e);}}
    private void parseEntity(JSONObject j)throws JSONException{SpriteEntityDefinition d=new SpriteEntityDefinition();d.id=j.getString("id");d.entityType=j.optString("entityType","enemy");d.scale=(float)j.optDouble("scale",1);d.pivotX=(float)j.optDouble("pivotX",.5);d.pivotY=(float)j.optDouble("pivotY",.85);d.sortOffset=(float)j.optDouble("sortOffset",0);d.fallbackColor=Color.parseColor(j.optString("fallbackColor","#ff00ff"));JSONArray list=j.getJSONArray("animations");for(int i=0;i<list.length();i++){JSONObject q=list.getJSONObject(i);SpriteAnimationDefinition a=new SpriteAnimationDefinition();a.id=q.getString("id");a.sheet=q.optString("sheet","");a.layout=q.optString("layout","direction_rows");a.columns=q.optInt("columns",1);a.rows=q.optInt("rows",1);a.entityRow=q.optInt("entityRow",0);a.frames=q.optInt("frames",1);a.directions=q.optInt("directions",1);a.fps=(float)q.optDouble("fps",1);a.loop=q.optBoolean("loop",true);a.directionMap=intArray(q.optJSONArray("directionMap"));a.mirrorDirections=intArray(q.optJSONArray("mirrorDirections"));d.animations.put(a.id,a);}definitions.put(d.id,d);}
    private int[] intArray(JSONArray a)throws JSONException{if(a==null)return null;int[] r=new int[a.length()];for(int i=0;i<r.length;i++)r[i]=a.getInt(i);return r;}
    private Bitmap bitmap(String name){if(name==null||name.length()==0)return null;Bitmap b=bitmaps.get(name);if(b!=null)return b;int id=context.getResources().getIdentifier(name,"drawable",context.getPackageName());if(id==0)return null;b=BitmapFactory.decodeResource(context.getResources(),id);bitmaps.put(name,b);return b;}

    void draw(Canvas c,Paint p,DirectionalSpriteAnimator v,float x,float baseline,float baseWidth,float alpha){if(v==null||v.definition==null)return;SpriteEntityDefinition d=v.definition;SpriteAnimationDefinition a=v.animation;Bitmap sheet=a==null?null:bitmap(a.sheet);int direction=v.direction(),frame=v.frame();float w=baseWidth*d.scale,h=w*1.38f;if(sheet==null){drawFallback(c,p,v,x,baseline,w,alpha);return;}int col,row;if("mapped_columns".equals(a.layout)){col=a.directionMap==null?direction:a.directionMap[Math.min(direction,a.directionMap.length-1)];row=a.entityRow;}else{col=frame;row=Math.min(direction,a.rows-1);}int cellW=sheet.getWidth()/Math.max(1,a.columns),cellH=sheet.getHeight()/Math.max(1,a.rows);v.source.set(col*cellW,row*cellH,Math.min(sheet.getWidth(),(col+1)*cellW),Math.min(sheet.getHeight(),(row+1)*cellH));float left=x-w*d.pivotX,top=baseline-h*d.pivotY;v.destination.set(left,top,left+w,top+h);int old=p.getAlpha();p.setAlpha((int)(255*Math.max(0,Math.min(1,alpha))));if(a.isMirrored(direction)){c.save();c.scale(-1,1,x,baseline);c.drawBitmap(sheet,v.source,v.destination,p);c.restore();}else c.drawBitmap(sheet,v.source,v.destination,p);p.setAlpha(old);}
    private void drawFallback(Canvas c,Paint p,DirectionalSpriteAnimator v,float x,float y,float w,float alpha){int old=p.getAlpha();p.setAlpha((int)(255*alpha));int color=v.definition.fallbackColor;if("hit".equals(v.state))color=Color.WHITE;p.setColor(color);float pulse="attack".equals(v.state)?1.18f:1;c.drawOval(x-w*.22f*pulse,y-w*.8f*pulse,x+w*.22f*pulse,y,p);p.setStrokeWidth(Math.max(3,w*.05f));float dx=v.directionX,dy=v.directionY,mag=(float)Math.sqrt(dx*dx+dy*dy);if(mag>0){dx/=mag;dy/=mag;c.drawLine(x,y-w*.45f,x+dx*w*.32f,y-w*.45f+dy*w*.18f,p);}p.setAlpha(old);}
}
