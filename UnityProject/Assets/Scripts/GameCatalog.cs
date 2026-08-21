using UnityEngine;

namespace Nightfall.UnityMvp
{
    public enum HeroKind { Amelia, Sam, Zike }
    public enum EnemyKind { Vampire, Zombie, Bandit, BureauAgent, Possessed, Mutant, Drone }
    public enum BossKind { EarthDragon, Assassin, EliteAgent, BastionMech }
    public enum AttackKind { Light, Death, Lightning }

    public sealed class HeroDefinition
    {
        public HeroKind kind; public string id, displayName, subtitle; public float hp, speed, damage, attackDelay; public AttackKind attack;
    }
    public sealed class EnemyDefinition
    {
        public EnemyKind kind; public string spriteId; public float hp, speed, damage, range, cooldown, scale; public Color fallback;
    }
    public sealed class BossDefinition
    {
        public BossKind kind; public string name, spriteId; public float hp, speed, damage, scale; public Color fallback;
    }

    public static class GameCatalog
    {
        public static readonly HeroDefinition[] Heroes =
        {
            new HeroDefinition{kind=HeroKind.Amelia,id="hero_amelia",displayName="АМЕЛИЯ",subtitle="Свет • исцеление • выживание",hp=145,speed=5.4f,damage=24,attackDelay=.62f,attack=AttackKind.Light},
            new HeroDefinition{kind=HeroKind.Sam,id="hero_sam",displayName="СЭМ",subtitle="Смерть • вампиризм • посох",hp=105,speed=5.1f,damage=34,attackDelay=.78f,attack=AttackKind.Death},
            new HeroDefinition{kind=HeroKind.Zike,id="hero_zike",displayName="ЗИК",subtitle="Молния • катана • скорость",hp=82,speed=6.7f,damage=18,attackDelay=.38f,attack=AttackKind.Lightning}
        };

        public static readonly EnemyDefinition[] Enemies =
        {
            new EnemyDefinition{kind=EnemyKind.Vampire,spriteId="enemy_vampire",hp=32,speed=3.2f,damage=12,range=.7f,cooldown=.7f,scale=1,fallback=new Color(.52f,.12f,.22f)},
            new EnemyDefinition{kind=EnemyKind.Zombie,spriteId="enemy_zombie",hp=66,speed=2.05f,damage=9,range=.72f,cooldown=.8f,scale=1,fallback=new Color(.35f,.48f,.3f)},
            new EnemyDefinition{kind=EnemyKind.Bandit,spriteId="enemy_bandit",hp=42,speed=2.65f,damage=10,range=5.5f,cooldown=1.8f,scale=1,fallback=new Color(.72f,.38f,.13f)},
            new EnemyDefinition{kind=EnemyKind.BureauAgent,spriteId="enemy_agent",hp=78,speed=2.75f,damage=15,range=6.5f,cooldown=1.35f,scale=1.05f,fallback=new Color(.18f,.48f,.62f)},
            new EnemyDefinition{kind=EnemyKind.Possessed,spriteId="enemy_possessed",hp=54,speed=2.8f,damage=13,range=1.75f,cooldown=2.4f,scale=1,fallback=new Color(.54f,.17f,.65f)},
            new EnemyDefinition{kind=EnemyKind.Mutant,spriteId="enemy_mutant",hp=190,speed=1.65f,damage=25,range=.95f,cooldown=1.45f,scale=1.45f,fallback=new Color(.32f,.65f,.42f)},
            new EnemyDefinition{kind=EnemyKind.Drone,spriteId="enemy_drone",hp=30,speed=3.8f,damage=9,range=7,cooldown=1.55f,scale=.72f,fallback=new Color(.42f,.82f,.92f)}
        };

        public static readonly BossDefinition[] Bosses =
        {
            new BossDefinition{kind=BossKind.EarthDragon,name="ЗЕМЛЯНОЙ ДРАКОН",spriteId="boss_dragon",hp=1500,speed=1.8f,damage=28,scale=2.8f,fallback=new Color(.55f,.33f,.15f)},
            new BossDefinition{kind=BossKind.Assassin,name="УБИЙЦА",spriteId="boss_assassin",hp=900,speed=4.8f,damage=24,scale=1.1f,fallback=new Color(.08f,.08f,.1f)},
            new BossDefinition{kind=BossKind.EliteAgent,name="ЭЛИТНЫЙ АГЕНТ БЮРО",spriteId="boss_elite_agent",hp=1350,speed=2.5f,damage=22,scale=1.35f,fallback=new Color(.15f,.36f,.48f)},
            new BossDefinition{kind=BossKind.BastionMech,name="МЕХ «БАСТИОН»",spriteId="boss_mech",hp=1900,speed=1.75f,damage=30,scale=2.15f,fallback=new Color(.43f,.48f,.52f)}
        };

        public static HeroDefinition Hero(HeroKind kind) => Heroes[(int)kind];
        public static EnemyDefinition Enemy(EnemyKind kind) => Enemies[(int)kind];
        public static BossDefinition Boss(BossKind kind) => Bosses[(int)kind];
    }
}
