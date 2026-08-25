using System;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class CharacterVisualController : MonoBehaviour
    {
        private DirectionalSpriteVisual body; private SpriteRenderer weapon,glow; private LineRenderer whip; private CharacterVisualProfile profile; private Camera camera;
        private Vector2 facing=Vector2.down;private float attackClock;private bool hitSent,visible=true;private Action hitEvent;

        public void Configure(HeroKind hero,DirectionalSpriteVisual bodyVisual)
        {
            profile=CharacterVisualLibrary.Get(hero);body=bodyVisual;camera=Camera.main;
            if(weapon==null){weapon=Make("DirectionalWeapon",14);glow=Make("DirectionalWeaponGlow",13);whip=MakeWhip();}
            var texture=Resources.Load<Texture2D>(profile.weapon);var sprite=texture==null?null:Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),profile.weaponPivot,192);
            weapon.sprite=glow.sprite=sprite;glow.color=hero==HeroKind.Amelia?new Color(1,.72f,.18f,.22f):hero==HeroKind.Zike?new Color(.08f,.72f,1,.22f):Color.clear;
            attackClock=0;hitEvent=null;SetVisible(true);ApplyPose(0);
        }
        private SpriteRenderer Make(string name,int order){var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sortingOrder=order;return r;}
        private LineRenderer MakeWhip()
        {
            var go=new GameObject("AmeliaWhip");go.transform.SetParent(transform,false);
            var r=go.AddComponent<LineRenderer>();r.useWorldSpace=true;r.alignment=LineAlignment.View;r.textureMode=LineTextureMode.Stretch;
            r.positionCount=13;r.startWidth=.045f;r.endWidth=.018f;r.numCapVertices=2;r.numCornerVertices=2;
            r.material=new Material(Shader.Find("Sprites/Default"));r.startColor=new Color(1f,.72f,.18f,1);r.endColor=new Color(1f,.92f,.42f,.9f);r.sortingOrder=15;r.enabled=false;return r;
        }
        public void SetMovement(Vector3 direction,bool moving)
        {
            // Keep the attack direction locked for the whole one-shot clip. Updating it
            // from movement every frame makes the renderer jump between atlas rows and
            // reads as the character orbiting around the gameplay transform.
            if(attackClock<=0&&direction.sqrMagnitude>.001f)
            {
                facing=new Vector2(direction.x,direction.z).normalized;
                body.SetFacing(direction);
            }
            body.SetMoving(moving);
        }
        public void PlayAttack(Vector3 direction,Action onHit)
        {
            if(direction.sqrMagnitude>.001f){facing=new Vector2(direction.x,direction.z).normalized;body.SetFacing(direction);}attackClock=profile.attackDuration;hitSent=false;hitEvent=onHit;body.TriggerAttackBob();
            // Amelia keeps her current locomotion pose. Her generated body attack frame
            // subtly changes proportions and reads as stretching; only the whip/VFX move.
            if(profile.hero==HeroKind.Amelia)body.SuppressBodyAnimation(true);
            else body.Play("attack",true);
        }
        public void PlayCast(){body.Play("cast",true);}
        public Vector3 AttackOrigin()
        {
            if(profile==null)return transform.position;
            Vector2 anchor=ScaledHandAnchor(profile.handAnchors[Direction()]);
            if(camera==null)camera=Camera.main;
            return camera!=null
                ? transform.position+camera.transform.right*anchor.x+camera.transform.up*anchor.y
                : transform.position+new Vector3(anchor.x,anchor.y,0);
        }
        private Vector2 ScaledHandAnchor(Vector2 anchor)
        {
            Vector3 scale=transform.lossyScale;
            // Use the vertical character scale for both axes. The body has an intentional
            // extra width multiplier; applying it to the hand anchor detached weapons.
            return new Vector2(anchor.x*Mathf.Abs(scale.y),anchor.y*Mathf.Abs(scale.y));
        }
        public void SetVisible(bool value){visible=value;if(body!=null)body.enabled=value;if(weapon!=null){bool spriteWeapon=value&&profile!=null&&profile.hero!=HeroKind.Amelia;weapon.enabled=spriteWeapon;glow.enabled=spriteWeapon&&profile.hero!=HeroKind.Sam;}if(whip!=null)whip.enabled=value&&profile!=null&&profile.hero==HeroKind.Amelia&&attackClock>0;}
        private int Direction(){float a=Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg;int d=Mathf.RoundToInt(a/45);return(d%8+8)%8;}
        private void LateUpdate()
        {
            if(profile==null||weapon==null)return;float phase=attackClock>0?1-attackClock/profile.attackDuration:0;
            if(attackClock>0){attackClock=Mathf.Max(0,attackClock-Time.deltaTime);if(!hitSent&&phase>=profile.hitTime){hitSent=true;var callback=hitEvent;hitEvent=null;callback?.Invoke();}if(attackClock<=0&&profile.hero==HeroKind.Amelia)body.SuppressBodyAnimation(false);}
            ApplyPose(phase);
        }
        private void ApplyPose(float phase)
        {
            int d=Direction();Vector2 anchor=ScaledHandAnchor(profile.handAnchors[d]);float angle=profile.idleAngles[d];float scale=profile.weaponScale;
            if(attackClock>0)
            {
                // A short hand-centred swing.  The previous 84-degree arc, positional
                // orbit and scale pulse made the detached weapon circle the hitbox.
                float windup=profile.hero==HeroKind.Sam?18:profile.hero==HeroKind.Zike?14:10;
                float strike=profile.hero==HeroKind.Sam?-28:profile.hero==HeroKind.Zike?-34:-18;
                float offset;
                if(phase<.25f)offset=Mathf.Lerp(0,windup,phase/.25f);
                else if(phase<.60f)offset=Mathf.Lerp(windup,strike,(phase-.25f)/.35f);
                else offset=Mathf.Lerp(strike,0,(phase-.60f)/.40f);
                angle+=offset;
                anchor+=new Vector2(Mathf.Sin(phase*Mathf.PI)*.018f,Mathf.Sin(phase*Mathf.PI)*.008f);
            }
            Vector3 world=transform.position;if(camera==null)camera=Camera.main;if(camera!=null){world+=camera.transform.right*anchor.x+camera.transform.up*anchor.y;weapon.transform.rotation=glow.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,angle);}else world+=new Vector3(anchor.x,anchor.y,0);
            weapon.transform.position=glow.transform.position=world;weapon.transform.localScale=Vector3.one*scale;glow.transform.localScale=Vector3.one*(scale*1.06f);weapon.sortingOrder=profile.sorting[d];glow.sortingOrder=profile.sorting[d]-1;weapon.enabled=visible;glow.enabled=visible&&profile.hero!=HeroKind.Sam;
            if(profile.hero==HeroKind.Amelia)
            {
                weapon.enabled=glow.enabled=false;
                bool active=visible&&attackClock>0;whip.enabled=active;
                if(active)ApplyWhip(world,angle,phase);
            }
        }
        private void ApplyWhip(Vector3 hand,float angle,float phase)
        {
            Vector3 right=camera!=null?camera.transform.right:Vector3.right,up=camera!=null?camera.transform.up:Vector3.up;
            float radians=angle*Mathf.Deg2Rad;Vector3 forward=right*Mathf.Cos(radians)+up*Mathf.Sin(radians);
            Vector3 side=-right*Mathf.Sin(radians)+up*Mathf.Cos(radians);
            float strike=Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-.16f)/.44f));
            float recover=Mathf.SmoothStep(0,1,Mathf.Clamp01((phase-.72f)/.28f));
            float length=Mathf.Lerp(.42f,1.55f,strike)*(1-.38f*recover);
            float curl=Mathf.Lerp(.46f,.10f,strike)+.22f*recover;
            for(int i=0;i<whip.positionCount;i++)
            {
                float t=i/(float)(whip.positionCount-1);
                float bend=Mathf.Sin(t*Mathf.PI)*curl;
                float ripple=Mathf.Sin(t*Mathf.PI*2-phase*10)*.055f*(1-strike);
                whip.SetPosition(i,hand+forward*(length*t)+side*(bend+ripple));
            }
        }
    }
}
