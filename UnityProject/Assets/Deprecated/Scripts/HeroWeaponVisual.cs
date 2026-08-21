using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class HeroWeaponVisual : MonoBehaviour
    {
        private SpriteRenderer weapon,glow;
        private HeroKind hero;
        private Vector2 facing=Vector2.down;
        private float attackTime;
        private bool desiredVisible=true;
        private Camera viewCamera;
        private const float AttackDuration=.34f;

        // Screen-space hand anchors for runtime directions E, NE, N, NW, W, SW, S, SE.
        // They are deliberately authored per direction: one generic offset caused floating weapons.
        private static readonly Vector2[] AmeliaHands={new(.26f,.58f),new(.20f,.61f),new(-.12f,.60f),new(-.20f,.61f),new(-.27f,.58f),new(-.20f,.57f),new(.15f,.57f),new(.23f,.58f)};
        private static readonly Vector2[] SamHands={new(.27f,.57f),new(.21f,.59f),new(-.13f,.58f),new(-.22f,.59f),new(-.28f,.57f),new(-.21f,.55f),new(.14f,.55f),new(.23f,.56f)};
        private static readonly Vector2[] ZikeHands={new(.25f,.55f),new(.20f,.58f),new(-.12f,.57f),new(-.20f,.58f),new(-.26f,.55f),new(-.20f,.53f),new(.14f,.53f),new(.22f,.54f)};

        public void Configure(HeroKind kind,Sprite unused)
        {
            hero=kind;viewCamera=Camera.main;
            if(weapon==null){weapon=MakeSprite("CanonicalWeapon",13);glow=MakeSprite("WeaponGlow",12);}
            string resource=kind==HeroKind.Amelia?"Art/Weapons/weapon_amelia_whip_v1":kind==HeroKind.Sam?"Art/Weapons/weapon_sam_staff_v1":"Art/Weapons/weapon_zike_katana_v1";
            var texture=Resources.Load<Texture2D>(resource);
            weapon.sprite=texture==null?null:Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),300);
            glow.sprite=weapon.sprite;glow.color=kind==HeroKind.Amelia?new Color(1,.68f,.12f,.25f):kind==HeroKind.Zike?new Color(.1f,.75f,1,.25f):Color.clear;
            attackTime=0;SetVisible(true);
        }

        private SpriteRenderer MakeSprite(string name,int order){var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SpriteRenderer>();r.sortingOrder=order;return r;}
        public void SetFacing(Vector3 direction){if(direction.sqrMagnitude>.001f)facing=new Vector2(direction.x,direction.z).normalized;}
        public void PlayAttack(){attackTime=AttackDuration;}
        public void SetVisible(bool visible){desiredVisible=visible;if(weapon!=null){weapon.enabled=visible;glow.enabled=visible&&hero!=HeroKind.Sam;}}

        private int DirectionIndex()
        {
            float angle=Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg;
            int index=Mathf.RoundToInt(angle/45f);return(index%8+8)%8;
        }

        private void LateUpdate()
        {
            if(weapon==null||weapon.sprite==null)return;if(viewCamera==null)viewCamera=Camera.main;
            attackTime=Mathf.Max(0,attackTime-Time.deltaTime);float t=attackTime>0?1-attackTime/AttackDuration:0;float strike=Mathf.SmoothStep(0,1,t);
            int direction=DirectionIndex();Vector2 anchor=(hero==HeroKind.Amelia?AmeliaHands:hero==HeroKind.Sam?SamHands:ZikeHands)[direction];
            float facingAngle=Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg;
            float angle=facingAngle-90;float baseSize=hero==HeroKind.Amelia?.72f:hero==HeroKind.Sam?.82f:.70f;Vector2 attackOffset=Vector2.zero;

            if(hero==HeroKind.Sam)
            {
                float swing=attackTime<=0?8:t<.25f?Mathf.Lerp(8,38,t/.25f):t<.68f?Mathf.Lerp(38,-54,(t-.25f)/.43f):Mathf.Lerp(-54,8,(t-.68f)/.32f);
                angle+=swing;attackOffset.x=Mathf.Sin(strike*Mathf.PI)*.07f;
            }
            else if(hero==HeroKind.Zike)
            {
                angle-=35;float swing=attackTime<=0?0:t<.22f?Mathf.Lerp(0,28,t/.22f):t<.58f?Mathf.Lerp(28,-78,(t-.22f)/.36f):Mathf.Lerp(-78,0,(t-.58f)/.42f);
                angle+=swing;baseSize*=1+Mathf.Sin(strike*Mathf.PI)*.08f;attackOffset.x=Mathf.Sin(strike*Mathf.PI)*.08f;
            }
            else
            {
                angle+=90+(attackTime>0?Mathf.Lerp(30,-24,strike):12);
                float extension=attackTime>0?Mathf.Lerp(.45f,1.02f,Mathf.Sin(Mathf.Clamp01(t/.7f)*Mathf.PI*.5f)):.48f;
                weapon.transform.localScale=new Vector3(baseSize*extension,baseSize,1);glow.transform.localScale=weapon.transform.localScale*1.08f;
            }

            if(hero!=HeroKind.Amelia){weapon.transform.localScale=Vector3.one*baseSize;glow.transform.localScale=weapon.transform.localScale*1.08f;}
            Vector2 hand=anchor+attackOffset;Vector3 world=transform.position;
            if(viewCamera!=null){world+=viewCamera.transform.right*hand.x+viewCamera.transform.up*hand.y;Quaternion rotation=viewCamera.transform.rotation*Quaternion.Euler(0,0,angle);weapon.transform.rotation=glow.transform.rotation=rotation;}
            else{world+=new Vector3(hand.x,hand.y,-.02f);weapon.transform.rotation=glow.transform.rotation=Quaternion.Euler(0,0,angle);}
            weapon.transform.position=glow.transform.position=world;
            bool behind=direction>=1&&direction<=3;weapon.sortingOrder=behind?9:13;glow.sortingOrder=behind?8:12;
            weapon.enabled=desiredVisible;glow.enabled=desiredVisible&&hero!=HeroKind.Sam;
        }
    }
}
