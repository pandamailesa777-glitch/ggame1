using System;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class CharacterVisualController : MonoBehaviour
    {
        private DirectionalSpriteVisual body; private SpriteRenderer weapon,glow; private CharacterVisualProfile profile; private Camera camera;
        private Vector2 facing=Vector2.down;private float attackClock;private bool hitSent,visible=true;private Action hitEvent;

        public void Configure(HeroKind hero,DirectionalSpriteVisual bodyVisual)
        {
            profile=CharacterVisualLibrary.Get(hero);body=bodyVisual;camera=Camera.main;
            if(weapon==null){weapon=Make("DirectionalWeapon",14);glow=Make("DirectionalWeaponGlow",13);}
            var texture=Resources.Load<Texture2D>(profile.weapon);var sprite=texture==null?null:Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),profile.weaponPivot,192);
            weapon.sprite=glow.sprite=sprite;glow.color=hero==HeroKind.Amelia?new Color(1,.72f,.18f,.22f):hero==HeroKind.Zike?new Color(.08f,.72f,1,.22f):Color.clear;
            attackClock=0;hitEvent=null;SetVisible(true);ApplyPose(0);
        }
        private SpriteRenderer Make(string name,int order){var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sortingOrder=order;return r;}
        public void SetMovement(Vector3 direction,bool moving){if(direction.sqrMagnitude>.001f){facing=new Vector2(direction.x,direction.z).normalized;body.SetFacing(direction);}body.SetMoving(moving);}
        public void PlayAttack(Vector3 direction,Action onHit)
        {
            if(direction.sqrMagnitude>.001f){facing=new Vector2(direction.x,direction.z).normalized;body.SetFacing(direction);}attackClock=profile.attackDuration;hitSent=false;hitEvent=onHit;body.Play("attack",true);
        }
        public void PlayCast(){body.Play("cast",true);}
        public void SetVisible(bool value){visible=value;if(body!=null)body.enabled=value;if(weapon!=null){weapon.enabled=value;glow.enabled=value&&profile!=null&&profile.hero!=HeroKind.Sam;}}
        private int Direction(){float a=Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg;int d=Mathf.RoundToInt(a/45);return(d%8+8)%8;}
        private void LateUpdate()
        {
            if(profile==null||weapon==null)return;float phase=attackClock>0?1-attackClock/profile.attackDuration:0;
            if(attackClock>0){attackClock=Mathf.Max(0,attackClock-Time.deltaTime);if(!hitSent&&phase>=profile.hitTime){hitSent=true;var callback=hitEvent;hitEvent=null;callback?.Invoke();}}
            ApplyPose(phase);
        }
        private void ApplyPose(float phase)
        {
            int d=Direction();Vector2 anchor=profile.handAnchors[d];float angle=profile.idleAngles[d];float scale=profile.weaponScale;
            if(attackClock>0)
            {
                // Four authored phases. Direction selects the base pose; no RotateTowards/free orbiting.
                float offset;if(phase<.25f)offset=Mathf.Lerp(0,profile.hero==HeroKind.Sam?32:22,phase/.25f);else if(phase<.55f)offset=Mathf.Lerp(profile.hero==HeroKind.Sam?32:22,profile.hero==HeroKind.Amelia?-38:-62,(phase-.25f)/.30f);else if(phase<.78f)offset=Mathf.Lerp(profile.hero==HeroKind.Amelia?-38:-62,-18,(phase-.55f)/.23f);else offset=Mathf.Lerp(-18,0,(phase-.78f)/.22f);
                angle+=offset;anchor+=new Vector2(Mathf.Sin(phase*Mathf.PI)*.055f,Mathf.Sin(phase*Mathf.PI)*.018f);scale*=1+Mathf.Sin(phase*Mathf.PI)*.06f;
            }
            Vector3 world=transform.position;if(camera==null)camera=Camera.main;if(camera!=null){world+=camera.transform.right*anchor.x+camera.transform.up*anchor.y;weapon.transform.rotation=glow.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,angle);}else world+=new Vector3(anchor.x,anchor.y,0);
            weapon.transform.position=glow.transform.position=world;weapon.transform.localScale=Vector3.one*scale;glow.transform.localScale=Vector3.one*(scale*1.06f);weapon.sortingOrder=profile.sorting[d];glow.sortingOrder=profile.sorting[d]-1;weapon.enabled=visible;glow.enabled=visible&&profile.hero!=HeroKind.Sam;
        }
    }
}
