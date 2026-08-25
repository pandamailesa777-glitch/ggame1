using System.Collections.Generic;
using UnityEngine;

namespace Nightfall.UnityMvp
{
    internal enum GameLanguage { Russian, English, Chinese }

    internal static class GameLocalization
    {
        private const string PrefKey="BureauBreakers.Language";
        private static readonly Dictionary<string,string[]> Lines=new Dictionary<string,string[]>
        {
            ["ИГРАТЬ"]=new[]{"PLAY","开始游戏"}, ["ВЫБЕРИТЕ ОПЕРАТИВНИКА"]=new[]{"CHOOSE AN OPERATIVE","选择特工"},
            ["ЗВУК"]=new[]{"VOLUME","音量"},
            ["Три боевых протокола. Один шанс пережить ночь."]=new[]{"Three combat protocols. One chance to survive the night.","三套战斗方案，一次活过长夜的机会。"},
            ["В БОЙ"]=new[]{"DEPLOY","出战"}, ["ПАУЗА"]=new[]{"PAUSE","暂停"}, ["ПРОДОЛЖИТЬ"]=new[]{"CONTINUE","继续"},
            ["НАЧАТЬ ЗАНОВО"]=new[]{"RESTART RUN","重新开始"}, ["ВЫБОР ГЕРОЯ"]=new[]{"CHOOSE HERO","选择英雄"},
            ["НОВЫЙ УРОВЕНЬ"]=new[]{"LEVEL UP","升级"}, ["ВЫБРАТЬ"]=new[]{"SELECT","选择"}, ["ЭВОЛЮЦИЯ"]=new[]{"EVOLUTION","进化"},
            ["ЗАБЕГ ЗАВЕРШЁН"]=new[]{"RUN COMPLETE","行动完成"}, ["ПОБЕДА"]=new[]{"VICTORY","胜利"}, ["ОПЕРАТИВНИК ПОГИБ"]=new[]{"OPERATIVE DOWN","特工阵亡"}, ["ЕЩЁ РАЗ"]=new[]{"TRY AGAIN","再试一次"}, ["ГЛАВНОЕ МЕНЮ"]=new[]{"MAIN MENU","主菜单"},
            ["ФАМИЛЬЯР ОТКЛИКНУЛСЯ"]=new[]{"A FAMILIAR ANSWERED","魔宠回应了召唤"}, ["ПРИНЯТЬ ФАМИЛЬЯРА"]=new[]{"ACCEPT FAMILIAR","接纳魔宠"},
            ["ГЕРОЙ"]=new[]{"HERO","英雄"}, ["ОПЫТ"]=new[]{"XP","经验"}, ["БОСС"]=new[]{"BOSS","首领"}, ["АРСЕНАЛ"]=new[]{"ARSENAL","技能"}, ["ГОТОВО"]=new[]{"READY","就绪"},
            ["Активные способности ещё не выбраны"]=new[]{"No active abilities selected yet","尚未选择主动技能"},
            ["АМЕЛИЯ"]=new[]{"AMELIA","阿梅莉娅"}, ["СЭМ"]=new[]{"SAM","萨姆"}, ["ЗИК"]=new[]{"ZIKE","齐克"},
            ["Свет • исцеление • выживание"]=new[]{"Light • healing • survival","圣光 • 治疗 • 生存"}, ["Смерть • вампиризм • посох"]=new[]{"Death • lifesteal • staff","死亡 • 吸血 • 法杖"}, ["Молния • катана • скорость"]=new[]{"Lightning • katana • speed","闪电 • 武士刀 • 速度"},
            ["Священный круг"]=new[]{"Sacred Circle","神圣之环"}, ["Кнут света"]=new[]{"Light Whip","圣光之鞭"}, ["Святилище"]=new[]{"Sanctuary","圣所"}, ["Солнечные стрелы"]=new[]{"Solar Arrows","太阳箭雨"}, ["Завет хранителя"]=new[]{"Guardian's Covenant","守护者盟约"},
            ["Круговой удар посохом"]=new[]{"Staff Sweep","法杖横扫"}, ["Импульс смерти"]=new[]{"Death Pulse","死亡脉冲"}, ["Кровавая орбита"]=new[]{"Blood Orbit","血色轨道"}, ["Жатва душ"]=new[]{"Soul Harvest","灵魂收割"}, ["Погребальный залп"]=new[]{"Funeral Volley","葬礼齐射"},
            ["Цепная молния"]=new[]{"Chain Lightning","连锁闪电"}, ["Молниеносный шаг"]=new[]{"Lightning Step","雷霆步"}, ["Грозовой след"]=new[]{"Storm Trail","风暴轨迹"}, ["Громовой приговор"]=new[]{"Thunder Judgment","雷霆审判"}, ["Штормовой веер"]=new[]{"Storm Fan","风暴扇击"},
            ["Круг света наносит урон и лечит Амелию."]=new[]{"A circle of light damages enemies and heals Amelia.","圣光之环伤害敌人并治疗阿梅莉娅。"}, ["Световой кнут поражает несколько ближайших целей."]=new[]{"A whip of light strikes several nearby targets.","圣光之鞭攻击多个附近目标。"}, ["Лечение и короткая неуязвимость."]=new[]{"Healing and brief invulnerability.","治疗并获得短暂无敌。"}, ["Выпускает веер священных лучей."]=new[]{"Fires a fan of sacred rays.","发射扇形神圣光束。"}, ["Защитный завет обжигает врагов и лечит Амелию."]=new[]{"A protective covenant burns enemies and heals Amelia.","守护盟约灼烧敌人并治疗阿梅莉娅。"},
            ["Удар вокруг Сэма наносит урон и похищает здоровье."]=new[]{"A sweeping strike damages foes and steals health.","横扫攻击造成伤害并吸取生命。"}, ["Посох выпускает мощные пробивающие заряды."]=new[]{"The staff fires powerful piercing bolts.","法杖发射强力穿透弹。"}, ["Веер тёмных зарядов лечит Сэма."]=new[]{"A fan of dark bolts heals Sam.","扇形暗影弹治疗萨姆。"}, ["Жатва вокруг Сэма вытягивает здоровье врагов."]=new[]{"A soul harvest drains nearby enemies.","灵魂收割吸取附近敌人的生命。"}, ["Плотный веер погребальных зарядов."]=new[]{"A dense fan of funeral bolts.","密集发射葬礼弹幕。"},
            ["Молния перескакивает между ближайшими врагами."]=new[]{"Lightning jumps between nearby enemies.","闪电在附近敌人之间跳跃。"}, ["Зик исчезает, становится неуязвимым и наносит два разреза."]=new[]{"Zike vanishes, becomes invulnerable and strikes twice.","齐克消失、进入无敌并连续斩击两次。"}, ["Движение создаёт электрические импульсы."]=new[]{"Movement creates electric pulses.","移动时产生电击脉冲。"}, ["Молния взрывает выбранную цель."]=new[]{"Lightning detonates the chosen target.","闪电引爆选中的目标。"}, ["Круговой залп молний даёт короткую защиту."]=new[]{"A radial lightning volley grants brief protection.","环形闪电齐射提供短暂保护。"},
            ["МОЩЬ"]=new[]{"POWER","力量"}, ["ТЕМП"]=new[]{"HASTE","急速"}, ["ДАЛЬНОСТЬ"]=new[]{"RANGE","射程"}, ["СКОРОСТЬ"]=new[]{"SPEED","速度"}, ["ЖИВУЧЕСТЬ"]=new[]{"VITALITY","活力"}, ["РЕГЕНЕРАЦИЯ"]=new[]{"REGENERATION","再生"}, ["МАГНИТ"]=new[]{"MAGNET","磁力"}, ["КРИТИЧЕСКИЙ УДАР"]=new[]{"CRITICAL STRIKE","暴击"}, ["ПРОБИВАНИЕ"]=new[]{"PIERCING","穿透"}, ["ДОПОЛНИТЕЛЬНЫЙ ЗАРЯД"]=new[]{"EXTRA PROJECTILE","额外投射物"},
            ["Урон +20%"]=new[]{"Damage +20%","伤害 +20%"}, ["Скорость атаки +15%"]=new[]{"Attack speed +15%","攻击速度 +15%"}, ["Дальность +18%"]=new[]{"Range +18%","射程 +18%"}, ["Движение +12%"]=new[]{"Movement +12%","移动速度 +12%"}, ["Макс. HP +30"]=new[]{"Max HP +30","最大生命 +30"}, ["Радиус сбора +35%"]=new[]{"Pickup radius +35%","拾取范围 +35%"}, ["Шанс крита +8%"]=new[]{"Critical chance +8%","暴击率 +8%"}, ["Снаряд пробивает ещё одну цель"]=new[]{"Projectile pierces one more target","投射物额外穿透一个目标"}, ["Ещё один снаряд"]=new[]{"One additional projectile","额外发射一个投射物"},
            ["+0.6 HP/с"]=new[]{"+0.6 HP/s","每秒恢复 0.6 HP"}, ["Световой круг наносит урон и лечит Амелию"]=new[]{"A circle of light damages enemies and heals Amelia","圣光之环伤害敌人并治疗阿梅莉娅"}, ["Поражает несколько ближайших целей световым кнутом"]=new[]{"Strikes several nearby targets with a whip of light","用圣光之鞭攻击多个附近目标"}, ["Лечение и кратковременная божественная защита"]=new[]{"Healing and brief divine protection","治疗并获得短暂神圣保护"}, ["Веер золотых священных лучей"]=new[]{"A fan of golden sacred rays","扇形金色神圣光束"}, ["Защитный круг обжигает врагов и лечит Амелию"]=new[]{"A protective circle burns enemies and heals Amelia","守护之环灼烧敌人并治疗阿梅莉娅"},
            ["Удар вокруг Сэма с похищением здоровья"]=new[]{"A sweeping strike around Sam that steals health","萨姆横扫周围并吸取生命"}, ["Посох выпускает мощные пробивающие заряды"]=new[]{"The staff fires powerful piercing bolts","法杖发射强力穿透弹"}, ["Веер тёмных зарядов и восстановление здоровья"]=new[]{"A fan of dark bolts that restores health","扇形暗影弹并恢复生命"}, ["Круговая жатва наносит урон и похищает здоровье"]=new[]{"A radial harvest damages enemies and steals health","环形收割造成伤害并吸取生命"}, ["Плотный веер тёмно-красных зарядов"]=new[]{"A dense fan of dark-red bolts","密集的暗红色弹幕"},
            ["Молния перескакивает между противниками"]=new[]{"Lightning jumps between enemies","闪电在敌人之间跳跃"}, ["Зик исчезает, становится неуязвимым и наносит два разреза"]=new[]{"Zike vanishes, becomes invulnerable and strikes twice","齐克消失、进入无敌并连续斩击两次"}, ["Движение оставляет электрические импульсы"]=new[]{"Movement leaves electric pulses","移动时留下电击脉冲"}, ["Молния поражает цель и взрывается вокруг неё"]=new[]{"Lightning strikes a target and detonates around it","闪电击中目标并在其周围爆炸"}, ["Круговой залп молний даёт короткую защиту"]=new[]{"A radial lightning volley grants brief protection","环形闪电齐射提供短暂保护"},
            ["СВЯЩЕННЫЙ КРУГ"]=new[]{"SACRED CIRCLE","神圣之环"}, ["КНУТ СВЕТА"]=new[]{"LIGHT WHIP","圣光之鞭"}, ["СВЯТИЛИЩЕ"]=new[]{"SANCTUARY","圣所"}, ["СОЛНЕЧНЫЕ СТРЕЛЫ"]=new[]{"SOLAR ARROWS","太阳箭雨"}, ["ЗАВЕТ ХРАНИТЕЛЯ"]=new[]{"GUARDIAN'S COVENANT","守护者盟约"},
            ["КРУГОВОЙ УДАР ПОСОХОМ"]=new[]{"STAFF SWEEP","法杖横扫"}, ["ИМПУЛЬС СМЕРТИ"]=new[]{"DEATH PULSE","死亡脉冲"}, ["КРОВАВАЯ ОРБИТА"]=new[]{"BLOOD ORBIT","血色轨道"}, ["ЖАТВА ДУШ"]=new[]{"SOUL HARVEST","灵魂收割"}, ["ПОГРЕБАЛЬНЫЙ ЗАЛП"]=new[]{"FUNERAL VOLLEY","葬礼齐射"},
            ["ЦЕПНАЯ МОЛНИЯ"]=new[]{"CHAIN LIGHTNING","连锁闪电"}, ["МОЛНИЕНОСНЫЙ ШАГ"]=new[]{"LIGHTNING STEP","雷霆步"}, ["ГРОЗОВОЙ СЛЕД"]=new[]{"STORM TRAIL","风暴轨迹"}, ["ГРОМОВОЙ ПРИГОВОР"]=new[]{"THUNDER JUDGMENT","雷霆审判"}, ["ШТОРМОВОЙ ВЕЕР"]=new[]{"STORM FAN","风暴扇击"},
            ["ЛЮМЕН"]=new[]{"LUMEN","流明"}, ["ФЕНРИ"]=new[]{"FENRI","芬里"}, ["ЗЕФИРКА"]=new[]{"ZEPHYRKA","泽菲尔卡"},
            ["Белый кот-фамильяр Амелии. Атакует врагов и повторяет её заклинания."]=new[]{"Amelia's white cat familiar. Attacks enemies and echoes her spells.","阿梅莉娅的白猫魔宠，会攻击敌人并复现她的法术。"}, ["Маленький волчонок Сэма. Держится рядом и усиливает натиск хозяина."]=new[]{"Sam's wolf cub. Stays close and strengthens its master's assault.","萨姆的小狼崽，会紧随主人并强化攻势。"}, ["Белая пушистая самоедка Зика. Быстро атакует и проводит эхо молний."]=new[]{"Zike's fluffy white Samoyed. Attacks quickly and echoes lightning.","齐克的白色萨摩耶犬，攻击迅速并复现闪电。"},
            ["АВТОАТАКА   20% урона героя\nЭХО НАВЫКА   12%\nРОЛЬ   наземный спутник"]=new[]{"AUTOATTACK   20% hero damage\nABILITY ECHO   12%\nROLE   ground companion","自动攻击   英雄伤害的 20%\n技能回响   12%\n定位   地面伙伴"},
            ["ЗЕМЛЯНОЙ ДРАКОН"]=new[]{"EARTH DRAGON","大地巨龙"}, ["УБИЙЦА"]=new[]{"ASSASSIN","刺客"}, ["ЭЛИТНЫЙ АГЕНТ БЮРО"]=new[]{"ELITE BUREAU AGENT","精英局特工"}, ["МЕХ «БАСТИОН»"]=new[]{"BASTION MECH","堡垒机甲"}
        };

        internal static GameLanguage Current { get; private set; }=(GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey,0),0,2);
        internal static string Tr(string source){if(Current==GameLanguage.Russian||string.IsNullOrEmpty(source))return source;return Lines.TryGetValue(source,out var values)?values[(int)Current-1]:source;}
        internal static string Code=>Current==GameLanguage.Russian?"RU":Current==GameLanguage.English?"EN":"中文";
        internal static void Cycle(){Current=(GameLanguage)(((int)Current+1)%3);PlayerPrefs.SetInt(PrefKey,(int)Current);PlayerPrefs.Save();}
    }
}
