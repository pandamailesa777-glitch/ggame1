using UnityEngine;

namespace Nightfall.UnityMvp
{
    public sealed class CharacterVisualProfile
    {
        public HeroKind hero; public string bodyMove,bodyIdle,weapon; public Vector2 weaponPivot; public float weaponScale,attackDuration,hitTime;
        public Vector2[] handAnchors; public float[] idleAngles; public int[] sorting;
    }

    public static class CharacterVisualLibrary
    {
        // Direction order: E, NE, N, NW, W, SW, S, SE. These are approved runtime assets only.
        private static readonly CharacterVisualProfile[] Profiles={
            new CharacterVisualProfile{hero=HeroKind.Amelia,bodyMove="Art/Generated/hero_amelia_canonical__move__8dir__7fps__loop",bodyIdle="Art/Generated/hero_amelia_canonical__idle__8dir__1fps__loop",weapon="Art/Weapons/weapon_amelia_whip_v1",weaponPivot=new Vector2(.225f,.39f),weaponScale=.32f,attackDuration=.42f,hitTime=.48f,
                handAnchors=A(new(.27f,.82f),new(.22f,.84f),new(-.13f,.83f),new(-.22f,.84f),new(-.27f,.82f),new(-.21f,.80f),new(.15f,.80f),new(.23f,.81f)),idleAngles=F(12,55,98,140,188,225,278,322),sorting=I(14,14,8,8,8,14,14,14)},
            new CharacterVisualProfile{hero=HeroKind.Sam,bodyMove="Art/Generated/hero_sam_canonical__move__8dir__7fps__loop",bodyIdle="Art/Generated/hero_sam_canonical__idle__8dir__1fps__loop",weapon="Art/Weapons/weapon_sam_staff_v1",weaponPivot=new Vector2(.5f,.50f),weaponScale=.62f,attackDuration=.40f,hitTime=.50f,
                handAnchors=A(new(.30f,.81f),new(.24f,.84f),new(-.15f,.83f),new(-.25f,.84f),new(-.31f,.81f),new(-.24f,.79f),new(.16f,.79f),new(.26f,.80f)),idleAngles=F(-8,35,82,130,172,215,265,310),sorting=I(14,14,8,8,8,14,14,14)},
            new CharacterVisualProfile{hero=HeroKind.Zike,bodyMove="Art/Generated/hero_zike_canonical__move__8dir__7fps__loop",bodyIdle="Art/Generated/hero_zike_canonical__idle__8dir__1fps__loop",weapon="Art/Weapons/weapon_zike_katana_v1",weaponPivot=new Vector2(.285f,.175f),weaponScale=.30f,attackDuration=.32f,hitTime=.42f,
                handAnchors=A(new(.35f,.91f),new(.28f,.95f),new(-.16f,.94f),new(-.28f,.95f),new(-.36f,.91f),new(-.27f,.88f),new(.19f,.88f),new(.31f,.89f)),idleAngles=F(-28,18,62,108,152,198,242,288),sorting=I(14,14,8,8,8,14,14,14)}
        };
        public static CharacterVisualProfile Get(HeroKind hero)=>Profiles[(int)hero];
        private static Vector2[] A(params Vector2[] v)=>v;private static float[] F(params float[] v)=>v;private static int[] I(params int[] v)=>v;
    }
}
