using System;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    public enum LootKind { Treat }

    [Serializable]
    public sealed class PetDefinition
    {
        public string id,displayName,ownerCharacterId,description,spriteResource,portraitResource;
        public float attackDamageMultiplier=.20f,attackCooldown=1.15f,abilityEchoChance=.12f;
        public float followDistance=1.25f,moveSpeed=7.5f,catchupDistance=8f,targetingRange=9f;
    }

    public static class PetCatalog
    {
        public const int MaxLevel=5;
        public const float DamageBonusPerLevel=.25f;
        private static readonly PetDefinition[] Definitions={
            new PetDefinition{id="amelia_cat",displayName="ЛЮМЕН",ownerCharacterId="hero_amelia",description="Белый кот-фамильяр Амелии. Атакует врагов и повторяет её заклинания.",spriteResource="Art/Pets/AmeliaCat/pet_amelia_cat",portraitResource="Art/Pets/AmeliaCat/pet_amelia_cat_portrait"},
            new PetDefinition{id="sam_wolf_cub",displayName="ФЕНРИ",ownerCharacterId="hero_sam",description="Маленький волчонок Сэма. Держится рядом и усиливает натиск хозяина.",spriteResource="Art/Pets/SamWolfCub/pet_sam_wolf_cub",portraitResource="Art/Pets/SamWolfCub/pet_sam_wolf_cub_portrait"},
            new PetDefinition{id="zephyrka",displayName="ЗЕФИРКА",ownerCharacterId="hero_zike",description="Белая пушистая самоедка Зика. Быстро атакует и проводит эхо молний.",spriteResource="Art/Pets/Zephyrka/pet_zephyrka",portraitResource="Art/Pets/Zephyrka/pet_zephyrka_portrait",attackCooldown=1.0f,moveSpeed=8.2f}
        };

        public static PetDefinition ForOwner(string ownerId)
        {
            foreach(var definition in Definitions)if(definition.ownerCharacterId==ownerId)return definition;
            return Definitions[0];
        }
    }

    public static class LootDropTable
    {
        public const float TreatChanceBeforeSummon=.15f,TreatChanceAfterSummon=.055f;
        public static bool Roll(LootKind kind,bool enabled,bool petUnlocked=false)
        {
            if(!enabled)return false;
            return kind==LootKind.Treat&&UnityEngine.Random.value<(petUnlocked?TreatChanceAfterSummon:TreatChanceBeforeSummon);
        }
    }

    public sealed class PetController : MonoBehaviour
    {
        public PetDefinition Definition{get;private set;}
        public Transform Owner{get;private set;}
        public float AttackClock{get;set;}
        private SpriteRenderer spriteRenderer;
        private readonly Sprite[] directionalSprites=new Sprite[8];
        private float attackPose,runPhase;
        private Vector3 velocity,lastOwnerPosition,lastTravelDirection=Vector3.back;

        public void Configure(PetDefinition definition,Transform owner,Sprite sprite,Camera camera)
        {
            Definition=definition;Owner=owner;spriteRenderer=GetComponent<SpriteRenderer>();if(spriteRenderer==null)spriteRenderer=gameObject.AddComponent<SpriteRenderer>();
            string[] suffixes={"_east","_north_east","_north","_north_west","_west","_south_west","_south","_south_east"};
            for(int i=0;i<directionalSprites.Length;i++){Texture2D texture=Resources.Load<Texture2D>(definition.spriteResource+suffixes[i]);directionalSprites[i]=texture!=null?Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.16f),48,0,SpriteMeshType.FullRect):sprite;}
            spriteRenderer.sprite=directionalSprites[6];spriteRenderer.sortingOrder=18;spriteRenderer.color=Color.white;
            transform.rotation=camera.transform.rotation;transform.localScale=Vector3.one*1.05f;lastOwnerPosition=owner.position;AttackClock=.4f;
        }

        public void TickMovement(float dt)
        {
            if(Owner==null||Definition==null)return;
            Vector3 ownerMotion=Owner.position-lastOwnerPosition;ownerMotion.y=0;lastOwnerPosition=Owner.position;if(ownerMotion.sqrMagnitude>.0002f)lastTravelDirection=ownerMotion.normalized;
            Vector3 side=new Vector3(lastTravelDirection.z,0,-lastTravelDirection.x);Vector3 desired=Owner.position-lastTravelDirection*Definition.followDistance+side*.55f;Vector3 delta=desired-transform.position;delta.y=0;
            if(delta.magnitude>Definition.catchupDistance){transform.position=new Vector3(desired.x,.025f,desired.z);velocity=Vector3.zero;}
            else{Vector3 wanted=delta.magnitude>.18f?delta.normalized*Definition.moveSpeed:Vector3.zero;velocity=Vector3.MoveTowards(velocity,wanted,Definition.moveSpeed*5*dt);transform.position+=velocity*dt;transform.position=new Vector3(transform.position.x,.025f,transform.position.z);}
            bool running=velocity.sqrMagnitude>.12f;if(running){runPhase+=dt*11;SetFacing(velocity);}
            attackPose=Mathf.MoveTowards(attackPose,0,dt*5);float attackSquash=Mathf.Sin(attackPose*Mathf.PI),stride=running?Mathf.Sin(runPhase)*.055f:Mathf.Sin(Time.time*3.2f)*.018f;transform.localScale=new Vector3(1.08f+attackSquash*.18f+stride,1.08f-attackSquash*.12f-stride,1.08f);
        }
        private void SetFacing(Vector3 direction){float angle=Mathf.Atan2(direction.z,direction.x)*Mathf.Rad2Deg;if(angle<0)angle+=360;int index=Mathf.RoundToInt(angle/45f)%8;if(spriteRenderer!=null)spriteRenderer.sprite=directionalSprites[index];}
        public void PlayAttack(){attackPose=1;}
    }

    public sealed class FamiliarEcho
    {
        private bool resolving;
        public bool TryBegin(PetDefinition definition,bool eligible)
        {
            if(resolving||definition==null||!eligible||UnityEngine.Random.value>=definition.abilityEchoChance)return false;
            resolving=true;return true;
        }
        public void End(){resolving=false;}
    }
}
