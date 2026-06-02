using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class ExerciseCatalog
{
    private readonly ExerciseDefinition[] _exercises =
    {
        new("chest_machine_press", "chest", "胸推機", "器械", "固定軌道胸推，適合把注意力放在胸肌發力與穩定節奏。", "肩胛微收、手腕保持中立，推到底不要鎖死手肘。", true),
        new("chest_barbell_bench_press", "chest", "槓鈴臥推", "槓鈴", "經典水平推舉，能訓練胸肌、前三角與肱三頭。", "需要穩定臥推架與保護，肩痛時降低重量或改用器械。"),
        new("chest_dumbbell_bench_press", "chest", "啞鈴臥推", "啞鈴", "左右手獨立推舉，活動範圍較大，能補強兩側控制。", "下放時不要過度拉伸肩膀，啞鈴路徑保持可控。"),
        new("chest_incline_dumbbell_press", "chest", "上斜啞鈴推", "啞鈴", "偏重上胸與肩前三角，適合搭配水平推舉。", "椅背角度不要過高，避免變成肩推。"),
        new("chest_push_up", "chest", "伏地挺身", "徒手", "徒手水平推，方便暖身、收尾或作為無器械選項。", "身體保持一直線，腰不要塌，肩膀不適時縮短動作幅度。"),
        new("chest_pec_deck", "chest", "蝴蝶機夾胸", "器械", "以夾合動作集中刺激胸肌，適合中低重量控制。", "手肘微彎，不要用肩膀硬夾或快速回彈。"),
        new("chest_cable_fly", "chest", "滑輪夾胸", "滑輪", "滑輪提供持續張力，可從不同角度訓練胸肌。", "核心收緊，肩膀不要聳起，重量以可控制軌跡為準。"),
        new("chest_incline_machine_press", "chest", "上斜胸推機", "器械", "固定軌道上斜推舉，較容易穩定上胸訓練。", "調整座椅讓把手落在上胸附近，避免肩膀頂住。"),

        new("shoulders_machine_press", "shoulders", "肩推機", "器械", "固定軌道垂直推舉，適合穩定訓練肩部推力。", "手肘略在身體前側，不要聳肩硬推。", true),
        new("shoulders_dumbbell_press", "shoulders", "啞鈴肩推", "啞鈴", "左右手獨立肩推，能訓練肩部力量與穩定。", "核心收緊，避免下背代償，肩痛時降低深度。"),
        new("shoulders_barbell_press", "shoulders", "槓鈴肩推", "槓鈴", "垂直推舉主力動作，適合進階力量訓練。", "路徑要穩，避免過度後仰，必要時採坐姿或較輕重量。"),
        new("shoulders_dumbbell_lateral_raise", "shoulders", "啞鈴側平舉", "啞鈴", "針對中束三角肌，適合用中低重量累積張力。", "手肘微彎，舉到肩高即可，不要甩動。"),
        new("shoulders_cable_lateral_raise", "shoulders", "滑輪側平舉", "滑輪", "滑輪讓側平舉全程保持張力，適合控制感訓練。", "身體不要歪斜借力，起始重量保守。"),
        new("shoulders_reverse_pec_deck", "shoulders", "反向蝴蝶機", "器械", "訓練後三角與上背穩定，平衡推舉訓練。", "胸口貼穩靠墊，肩胛不要過度聳起。"),
        new("shoulders_face_pull", "shoulders", "面拉", "滑輪", "訓練後三角、外旋與肩胛控制，適合肩部保養。", "繩索拉向臉部兩側，保持手肘高但不要夾脖子。"),
        new("shoulders_bent_over_reverse_fly", "shoulders", "俯身反向飛鳥", "啞鈴", "以髖折俯身訓練後三角，器材需求低。", "背部保持中立，重量過重會變成甩動。"),

        new("back_seated_row", "back", "坐姿划船機", "器械", "水平拉動作，訓練背闊肌、中背與肩胛後收。", "先穩住身體再拉，避免用下背大幅後仰。", true),
        new("back_lat_pulldown", "back", "高位下拉", "器械", "垂直拉動作，適合建立背闊肌發力。", "把手拉向上胸，不要拉到頸後。"),
        new("back_pull_up", "back", "引體向上", "徒手", "自體重量垂直拉，是背部與握力的進階訓練。", "無法控制全程時改用輔助，不要聳肩硬拉。"),
        new("back_assisted_pull_up", "back", "輔助引體向上", "器械", "降低自體重量負擔，練習引體向上的路徑與節奏。", "輔助重量調到能穩定完成，不要快速彈起。"),
        new("back_one_arm_dumbbell_row", "back", "單臂啞鈴划船", "啞鈴", "單側水平拉，可補強左右背部控制差異。", "軀幹保持穩定，拉向髖側而不是聳肩。"),
        new("back_barbell_row", "back", "槓鈴划船", "槓鈴", "髖折姿勢下的水平拉，訓練背部與後鏈穩定。", "背部保持中立，腰不舒服時改用器械划船。"),
        new("back_t_bar_row", "back", "T 槓划船", "器械", "胸托或固定軌道版本可穩定做重訓練。", "不要用腰甩起重量，頂端停一下再下放。"),
        new("back_straight_arm_pulldown", "back", "直臂下拉", "滑輪", "以肩伸動作集中感受背闊肌。", "手肘微彎固定，避免變成三頭下壓。"),

        new("legs_leg_press", "legs", "腿推機", "器械", "固定軌道下肢推蹬，適合穩定訓練股四頭與臀腿。", "膝蓋方向對齊腳尖，不要把平台放太低造成骨盆後傾。", true),
        new("legs_barbell_squat", "legs", "槓鈴深蹲", "槓鈴", "下肢主力動作，訓練腿部、臀部與核心穩定。", "保持脊柱中立，重量進展保守，必要時請人保護。"),
        new("legs_goblet_squat", "legs", "高腳杯深蹲", "啞鈴", "抱持啞鈴深蹲，適合學習蹲姿與暖身。", "膝蓋跟腳尖同向，軀幹保持穩定。"),
        new("legs_leg_extension", "legs", "腿伸展機", "器械", "針對股四頭肌的孤立訓練，容易控制節奏。", "膝蓋有不適時降低重量或縮短活動範圍。"),
        new("legs_leg_curl", "legs", "腿彎舉機", "器械", "訓練腿後側肌群，平衡深蹲與腿推。", "髖部貼穩墊面，不要用身體彈起重量。"),
        new("legs_romanian_deadlift", "legs", "羅馬尼亞硬舉", "槓鈴", "以髖折訓練臀腿後側與背部穩定。", "背部保持中立，感受腿後側拉伸，不要追求過低深度。"),
        new("legs_lunge", "legs", "弓箭步", "徒手", "單腳主導動作，訓練腿部力量與平衡。", "步距保持穩定，膝蓋不要內夾。"),
        new("legs_standing_calf_raise", "legs", "站姿提踵", "器械", "訓練小腿後側，可作為腿日收尾。", "頂端停留，下降可控，不要快速彈跳。"),

        new("core_plank", "core", "棒式", "徒手", "基礎抗伸展核心訓練，適合穩定軀幹。", "身體保持一直線，腰痠代表需要縮短時間或降低難度。", true),
        new("core_crunch", "core", "捲腹", "徒手", "短幅度腹直肌訓練，器材需求低。", "不要拉脖子，吐氣捲起，動作幅度不必過大。"),
        new("core_leg_raise", "core", "仰臥抬腿", "徒手", "訓練下腹控制與髖屈協調。", "下背不要離地拱起，必要時彎膝降低難度。"),
        new("core_cable_crunch", "core", "滑輪捲腹", "滑輪", "可加重量的核心屈曲訓練，適合漸進負荷。", "髖部穩定，讓肋骨向骨盆收，不要只用手拉繩。"),
        new("core_russian_twist", "core", "俄羅斯轉體", "徒手", "訓練旋轉控制與腹斜肌，可徒手或抱重量。", "腰不舒服時減少幅度，避免快速甩動。"),
        new("core_dead_bug", "core", "死蟲", "徒手", "低衝擊核心穩定訓練，適合學習控制下背。", "下背貼穩地面，動作慢而可控。"),
        new("core_mountain_climber", "core", "登山者", "徒手", "結合核心與心肺節奏，適合短回合訓練。", "肩膀撐穩，腰不要塌，速度以姿勢穩定為先。"),
        new("core_captains_chair_knee_raise", "core", "羅馬椅抬膝", "器械", "支撐身體進行抬膝，訓練腹部與髖屈控制。", "背部貼穩靠墊，避免用擺盪完成。"),

        new("arms_machine_biceps_curl", "arms", "二頭彎舉機", "器械", "固定軌道訓練肱二頭肌，容易維持節奏。", "手肘貼穩墊面，不要用肩膀帶動。", true),
        new("arms_dumbbell_curl", "arms", "啞鈴二頭彎舉", "啞鈴", "自由重量彎舉，可訓練左右手控制。", "上臂固定，避免身體後仰借力。"),
        new("arms_barbell_curl", "arms", "槓鈴彎舉", "槓鈴", "雙手同步彎舉，適合穩定增加負荷。", "手腕保持中立，重量過重容易腰部代償。"),
        new("arms_hammer_curl", "arms", "槌式彎舉", "啞鈴", "中立握訓練肱肌與前臂，能補強手臂厚度。", "手肘不要前後晃，頂端不用硬甩。"),
        new("arms_triceps_pushdown", "arms", "三頭下壓", "滑輪", "滑輪訓練肱三頭，適合手臂推力補強。", "手肘靠近身體，肩膀不要聳起。"),
        new("arms_rope_triceps_pushdown", "arms", "繩索三頭下壓", "滑輪", "繩索版本活動更自然，可在底端外展收縮。", "保持手肘位置，不要用身體壓重量。"),
        new("arms_close_grip_push_up", "arms", "窄距伏地挺身", "徒手", "徒手訓練肱三頭與胸肌，適合無器械環境。", "手腕不適時改用握把，身體保持一直線。"),
        new("arms_seated_triceps_extension", "arms", "坐姿三頭伸展", "器械", "以上臂固定的伸展動作訓練肱三頭長頭。", "不要讓手肘外張過多，肩膀不舒服時降低深度。"),
    };

    public IReadOnlyList<ExerciseDefinition> GetAll()
    {
        return _exercises;
    }

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
                "基礎訓練",
                "通用",
                "使用安全、可控制的基礎動作完成本次訓練。",
                "選擇熟悉的動作，保持姿勢穩定並避免疼痛。");
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
