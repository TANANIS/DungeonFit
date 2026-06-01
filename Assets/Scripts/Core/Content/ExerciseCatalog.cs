using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class ExerciseCatalog
{
    private readonly ExerciseDefinition[] _exercises =
    {
        new("chest_machine_press", "chest", "胸推機", "器械", "穩定訓練胸大肌，適合多數玩家作為起始動作。", "肩膀不要聳起，手肘不要過度後拉。", true),
        new("chest_barbell_bench_press", "chest", "槓鈴臥推", "槓鈴", "經典胸部主力動作，能推進力量成長。", "需要穩定肩胛，重量不足把握時不要單獨挑戰。"),
        new("chest_dumbbell_bench_press", "chest", "啞鈴臥推", "啞鈴", "增加左右控制，訓練胸部與穩定肌群。", "下放時保持手腕直，不要讓啞鈴失控外翻。"),
        new("chest_incline_dumbbell_press", "chest", "上斜啞鈴推胸", "啞鈴", "偏重上胸與肩前束協同。", "椅背角度不要過高，避免變成肩推。"),
        new("chest_push_up", "chest", "伏地挺身", "徒手", "隨時可做的胸部與核心整合動作。", "身體保持一直線，腰不要塌。"),
        new("chest_pec_deck", "chest", "蝴蝶機夾胸", "器械", "強化胸部內收感與收縮控制。", "不要用慣性甩動，手臂角度保持穩定。"),
        new("chest_cable_fly", "chest", "纜繩夾胸", "纜繩", "用穩定張力訓練胸部收縮路徑。", "軀幹不要前後晃動代償。"),
        new("chest_incline_machine_press", "chest", "上斜胸推機", "器械", "以器械穩定刺激上胸。", "肩膀若有夾擠感，降低重量或角度。"),

        new("shoulders_machine_press", "shoulders", "肩推機", "器械", "穩定訓練肩部推舉，適合作為肩地城推薦。", "不要鎖死手肘，避免聳肩硬推。", true),
        new("shoulders_dumbbell_press", "shoulders", "啞鈴肩推", "啞鈴", "訓練肩部力量與左右穩定。", "核心收緊，避免腰椎過度後仰。"),
        new("shoulders_barbell_press", "shoulders", "槓鈴肩推", "槓鈴", "強化垂直推舉能力。", "動作路徑需穩定，肩不適時改器械或啞鈴。"),
        new("shoulders_dumbbell_lateral_raise", "shoulders", "啞鈴側平舉", "啞鈴", "針對中束，增加肩部寬度感。", "重量不要過重，避免甩手與聳肩。"),
        new("shoulders_cable_lateral_raise", "shoulders", "纜繩側平舉", "纜繩", "以持續張力訓練肩中束。", "手腕放鬆，動作慢且可控。"),
        new("shoulders_reverse_pec_deck", "shoulders", "反向飛鳥機", "器械", "訓練後三角與上背穩定。", "胸口貼穩，避免用腰背甩動。"),
        new("shoulders_face_pull", "shoulders", "面拉", "纜繩", "強化後肩、斜方肌與肩胛控制。", "拉向臉部時不要聳肩，肘保持高位。"),
        new("shoulders_bent_over_reverse_fly", "shoulders", "俯身反向飛鳥", "啞鈴", "器材少時可補後肩訓練。", "背部保持中立，不要用下背借力。"),

        new("back_seated_row", "back", "坐姿划船機", "器械", "穩定訓練背闊肌與中背，是背地城推薦。", "拉時先收肩胛，不要只用手臂。", true),
        new("back_lat_pulldown", "back", "高位下拉", "器械", "模擬引體向上路徑，訓練背闊肌。", "不要把槓拉到脖子後方。"),
        new("back_pull_up", "back", "引體向上", "徒手", "高強度背部與手臂整合動作。", "無法控制全程時可用輔助機。"),
        new("back_assisted_pull_up", "back", "輔助引體向上", "器械", "降低難度，練習垂直拉。", "身體不要晃，專注肩胛下壓。"),
        new("back_one_arm_dumbbell_row", "back", "單臂啞鈴划船", "啞鈴", "強化單側背部與軀幹穩定。", "髖與背保持穩定，不要旋轉借力。"),
        new("back_barbell_row", "back", "槓鈴划船", "槓鈴", "背部厚度與髖鉸鏈控制訓練。", "下背不穩或疼痛時先改器械。"),
        new("back_t_bar_row", "back", "T 槓划船", "器械", "以較穩定角度訓練中背。", "胸墊貼穩，避免用身體彈起。"),
        new("back_straight_arm_pulldown", "back", "直臂下拉", "纜繩", "強調背闊肌發力與肩關節控制。", "手肘微彎固定，不要變成三頭下壓。"),

        new("legs_leg_press", "legs", "腿推機", "器械", "穩定訓練股四頭、臀腿，是腿地城推薦。", "膝蓋方向跟腳尖一致，不要鎖死膝蓋。", true),
        new("legs_barbell_squat", "legs", "深蹲", "槓鈴", "下肢主力動作，訓練腿部與核心穩定。", "動作不穩時先降低重量或改徒手。"),
        new("legs_goblet_squat", "legs", "高腳杯深蹲", "啞鈴", "入門友善的深蹲變化。", "胸口保持上提，膝蓋不要內夾。"),
        new("legs_leg_extension", "legs", "腿伸展機", "器械", "針對股四頭肌的孤立訓練。", "膝蓋不適時減重並縮小活動範圍。"),
        new("legs_leg_curl", "legs", "腿後勾機", "器械", "強化腿後側與膝關節穩定。", "不要用臀部抬起代償。"),
        new("legs_romanian_deadlift", "legs", "羅馬尼亞硬舉", "槓鈴", "訓練腿後側、臀部與髖鉸鏈。", "背保持中立，重量不要拉到下背。"),
        new("legs_lunge", "legs", "弓箭步", "徒手", "單腳穩定與臀腿協調訓練。", "前腳膝蓋跟腳尖同向。"),
        new("legs_standing_calf_raise", "legs", "站姿提踵", "器械", "訓練小腿肌群。", "下放與上提都要完整控制。"),

        new("core_plank", "core", "平板撐", "徒手", "穩定核心抗伸展能力，是核心地城推薦。", "腰不要塌，肩膀不要聳。", true),
        new("core_crunch", "core", "捲腹", "徒手", "基礎腹直肌訓練。", "不要拉脖子，用腹部帶動上身。"),
        new("core_leg_raise", "core", "仰臥抬腿", "徒手", "強化下腹與髖屈控制。", "腰離地不穩時縮短活動範圍。"),
        new("core_cable_crunch", "core", "纜繩卷腹", "纜繩", "可漸進加重的腹部動作。", "不要用手臂硬拉，保持腹部捲曲。"),
        new("core_russian_twist", "core", "俄羅斯轉體", "徒手", "訓練旋轉控制與腹斜肌。", "轉動要可控，不要快速甩腰。"),
        new("core_dead_bug", "core", "死蟲", "徒手", "低衝擊核心控制訓練。", "腰背貼穩，動作慢。"),
        new("core_mountain_climber", "core", "登山者", "徒手", "核心與心肺整合動作。", "肩膀穩定，骨盆不要大幅晃動。"),
        new("core_captains_chair_knee_raise", "core", "羅馬椅抬膝", "器械", "以器械支撐訓練腹部與髖屈。", "不要用擺盪帶動腿部。"),

        new("arms_machine_biceps_curl", "arms", "二頭彎舉機", "器械", "穩定訓練二頭肌，是手臂地城推薦。", "手肘貼穩，不要聳肩借力。", true),
        new("arms_dumbbell_curl", "arms", "啞鈴二頭彎舉", "啞鈴", "基礎二頭肌訓練。", "手腕保持中立，避免身體後仰。"),
        new("arms_barbell_curl", "arms", "槓鈴彎舉", "槓鈴", "適合漸進加重的二頭動作。", "不要用腰甩重量。"),
        new("arms_hammer_curl", "arms", "錘式彎舉", "啞鈴", "訓練肱肌與前臂參與。", "手肘位置固定。"),
        new("arms_triceps_pushdown", "arms", "三頭下壓", "纜繩", "穩定訓練三頭肌。", "肘夾身體兩側，不要肩膀前後晃。"),
        new("arms_rope_triceps_pushdown", "arms", "繩索三頭下壓", "纜繩", "提供自然手腕角度的三頭訓練。", "下壓末端展開即可，不要過度甩動。"),
        new("arms_close_grip_push_up", "arms", "窄握伏地挺身", "徒手", "徒手訓練三頭與胸部輔助。", "手腕不適時改用握把或器械。"),
        new("arms_seated_triceps_extension", "arms", "坐姿三頭伸展機", "器械", "固定路徑訓練三頭伸展。", "肘關節不適時降低重量。"),
    };

    public IReadOnlyList<ExerciseDefinition> GetForDungeon(string dungeonTypeId)
    {
        return _exercises
            .Where(exercise => exercise.DungeonTypeId == dungeonTypeId)
            .ToArray();
    }

    public ExerciseDefinition GetDefaultForDungeon(string dungeonTypeId)
    {
        var matches = GetForDungeon(dungeonTypeId);
        return matches.FirstOrDefault(exercise => exercise.IsRecommended) ??
            matches.FirstOrDefault() ??
            new ExerciseDefinition(
                $"{dungeonTypeId}_default_exercise",
                dungeonTypeId,
                "訓練動作",
                "一般",
                "依照本次地城安排完成訓練。",
                "保持可控節奏，身體不適時停止。");
    }

    public ExerciseDefinition GetById(string dungeonTypeId, string? exerciseId)
    {
        if (!string.IsNullOrWhiteSpace(exerciseId))
        {
            var exercise = _exercises.FirstOrDefault(candidate =>
                candidate.DungeonTypeId == dungeonTypeId && candidate.Id == exerciseId);
            if (exercise is not null)
            {
                return exercise;
            }
        }

        return GetDefaultForDungeon(dungeonTypeId);
    }
}
