using System.Collections.Generic;
using System.Linq;
using DungeonFit.Core.Models;

namespace DungeonFit.Core.Content;

public sealed class ExerciseCatalog
{
    public const string Machine = "器械";
    public const string Barbell = "槓鈴";
    public const string Dumbbell = "啞鈴";
    public const string Cable = "滑輪";
    public const string Bodyweight = "徒手";

    private readonly ExerciseDefinition[] _exercises =
    {
        new("chest_machine_press", "chest", "坐姿胸推", Machine, "穩定的水平推動作，適合作為胸部主訓練。", "肩胛保持穩定，手肘不要過度外張。", true),
        new("chest_barbell_bench_press", "chest", "槓鈴臥推", Barbell, "經典胸部力量動作，適合追蹤重量 PR。", "使用安全架或保護者，避免彈胸。"),
        new("chest_dumbbell_bench_press", "chest", "啞鈴臥推", Dumbbell, "左右手獨立發力，活動範圍較自由。", "下降時控制肩膀，不要讓啞鈴拉過深。"),
        new("chest_incline_dumbbell_press", "chest", "上斜啞鈴推", Dumbbell, "偏重上胸與前三角的推動作。", "椅背角度不要過高，避免變成肩推。"),
        new("chest_push_up", "chest", "伏地挺身", Bodyweight, "徒手水平推，適合暖身、收尾或無器械訓練。", "身體保持一直線，腰不要塌。"),
        new("chest_pec_deck", "chest", "蝴蝶機夾胸", Machine, "穩定孤立胸肌收縮。", "手肘略彎，不要用肩膀硬夾。"),
        new("chest_cable_fly", "chest", "滑輪夾胸", Cable, "可調角度的胸部孤立動作。", "重量不要過重，保持胸肌控制。"),
        new("chest_incline_machine_press", "chest", "上斜機械胸推", Machine, "穩定刺激上胸的器械推。", "手把高度對齊上胸，避免聳肩。"),
        new("chest_knee_push_up", "chest", "跪姿伏地挺身", Bodyweight, "降低負重的徒手胸推，適合累積次數。", "身體從膝到肩保持直線。"),
        new("chest_decline_push_up", "chest", "下斜伏地挺身", Bodyweight, "腳抬高增加上胸與肩前側負荷。", "核心收緊，避免腰椎下陷。"),
        new("chest_diamond_push_up", "chest", "鑽石伏地挺身", Bodyweight, "窄距推動作，胸部與三頭都會參與。", "手腕不適時改用一般窄距。"),

        new("shoulders_machine_press", "shoulders", "肩推機", Machine, "穩定的垂直推動作，適合作為肩部主訓練。", "背部貼穩，避免聳肩代償。", true),
        new("shoulders_dumbbell_press", "shoulders", "啞鈴肩推", Dumbbell, "左右手獨立的垂直推。", "推起時不要過度拱腰。"),
        new("shoulders_barbell_press", "shoulders", "槓鈴肩推", Barbell, "可追蹤重量進步的肩部力量動作。", "核心收緊，避免用腰甩起。"),
        new("shoulders_dumbbell_lateral_raise", "shoulders", "啞鈴側平舉", Dumbbell, "訓練中束三角肌的經典動作。", "手肘微彎，重量以可控制為主。"),
        new("shoulders_cable_lateral_raise", "shoulders", "滑輪側平舉", Cable, "張力連續的側平舉變化。", "不要聳肩，動作放慢。"),
        new("shoulders_reverse_pec_deck", "shoulders", "反向蝴蝶機", Machine, "訓練後束與肩胛控制。", "胸口貼穩，避免甩動。"),
        new("shoulders_face_pull", "shoulders", "面拉", Cable, "後束、上背與肩胛穩定動作。", "拉向臉部兩側，避免下背代償。"),
        new("shoulders_bent_over_reverse_fly", "shoulders", "俯身反向飛鳥", Dumbbell, "啞鈴後束訓練。", "背部固定，手臂不要甩。"),
        new("shoulders_pike_push_up", "shoulders", "派克伏地挺身", Bodyweight, "徒手肩推變化，偏重前三角。", "頭頂朝地面下降，頸部保持自然。"),
        new("shoulders_wall_walk", "shoulders", "靠牆爬行", Bodyweight, "肩部穩定與核心控制訓練。", "只走到能穩定控制的位置。"),
        new("shoulders_plank_shoulder_tap", "shoulders", "棒式肩碰", Bodyweight, "肩部抗旋轉與支撐穩定。", "骨盆不要左右晃動。"),

        new("back_seated_row", "back", "坐姿划船", Machine, "穩定訓練背部水平拉。", "先收肩胛再拉，避免聳肩。", true),
        new("back_lat_pulldown", "back", "高位下拉", Machine, "訓練闊背肌的垂直拉。", "拉到上胸附近，不要用身體後仰甩。"),
        new("back_pull_up", "back", "引體向上", Bodyweight, "徒手垂直拉，適合追蹤次數 PR。", "肩膀不適時縮短幅度或改輔助。"),
        new("back_assisted_pull_up", "back", "輔助引體向上", Machine, "降低負重的垂直拉。", "保持全程控制，不要彈起。"),
        new("back_one_arm_dumbbell_row", "back", "單臂啞鈴划船", Dumbbell, "單側背部與肩胛控制。", "軀幹穩定，手肘朝身體後側拉。"),
        new("back_barbell_row", "back", "槓鈴划船", Barbell, "背部厚度與髖鉸鏈控制。", "腰背保持中立，不要圓背硬拉。"),
        new("back_t_bar_row", "back", "T 槓划船", Machine, "較穩定的重訓划船變化。", "胸口或核心固定，避免借力過多。"),
        new("back_straight_arm_pulldown", "back", "直臂下拉", Cable, "孤立闊背肌的下拉動作。", "手肘角度固定，避免變成三頭下壓。"),
        new("back_prone_cobra", "back", "俯臥眼鏡蛇", Bodyweight, "徒手上背伸展與肩胛後收。", "抬起幅度小而穩，不要硬折腰。"),
        new("back_superman_pull", "back", "超人式拉背", Bodyweight, "徒手背伸與肩胛控制。", "腹部貼地，避免快速甩動。"),
        new("back_reverse_snow_angel", "back", "俯臥雪天使", Bodyweight, "訓練肩胛活動與上背耐力。", "慢速移動，肩膀疼痛時降低幅度。"),

        new("legs_leg_press", "legs", "腿推機", Machine, "穩定的下肢主訓練。", "膝蓋方向對齊腳尖，不要鎖死。", true),
        new("legs_barbell_squat", "legs", "槓鈴深蹲", Barbell, "下肢力量與全身張力動作。", "維持軀幹穩定，深度以可控制為準。"),
        new("legs_goblet_squat", "legs", "高腳杯深蹲", Dumbbell, "容易上手的負重深蹲。", "膝蓋跟腳尖方向一致。"),
        new("legs_leg_extension", "legs", "腿伸展", Machine, "股四頭肌孤立動作。", "不要用慣性甩起重量。"),
        new("legs_leg_curl", "legs", "腿彎舉", Machine, "腿後側孤立訓練。", "骨盆穩定，動作放慢。"),
        new("legs_romanian_deadlift", "legs", "羅馬尼亞硬舉", Barbell, "訓練腿後側與臀部髖鉸鏈。", "背部中立，重量貼近身體。"),
        new("legs_lunge", "legs", "弓箭步", Bodyweight, "單腳穩定與腿部肌力訓練。", "前膝對齊腳尖，步距不要太短。"),
        new("legs_standing_calf_raise", "legs", "站姿提踵", Machine, "小腿訓練。", "頂端停一下，下降控制。"),
        new("legs_bodyweight_squat", "legs", "徒手深蹲", Bodyweight, "無器械下肢主動作。", "保持腳掌穩定，膝蓋不要內夾。"),
        new("legs_reverse_lunge", "legs", "後跨弓箭步", Bodyweight, "較容易控制膝蓋壓力的弓箭步。", "身體直立，後腳輕點地。"),
        new("legs_glute_bridge", "legs", "臀橋", Bodyweight, "臀部與腿後側啟動。", "頂端夾臀，不要用腰頂。"),

        new("core_plank", "core", "平板支撐", Bodyweight, "核心抗伸展基礎動作。", "肋骨收下，腰不要塌。", true),
        new("core_crunch", "core", "捲腹", Bodyweight, "腹直肌短幅度訓練。", "不要拉脖子，吐氣捲起。"),
        new("core_leg_raise", "core", "仰臥抬腿", Bodyweight, "下腹與髖屈肌控制。", "腰離地時縮短幅度。"),
        new("core_cable_crunch", "core", "滑輪捲腹", Cable, "可負重的核心屈曲訓練。", "用腹部捲曲，不要只拉手。"),
        new("core_russian_twist", "core", "俄羅斯轉體", Bodyweight, "核心旋轉控制。", "保持胸口轉動，腰不舒服時降低幅度。"),
        new("core_dead_bug", "core", "死蟲", Bodyweight, "低風險核心抗伸展訓練。", "下背保持貼近地面。"),
        new("core_mountain_climber", "core", "登山者", Bodyweight, "核心與心肺混合動作。", "肩膀對齊手腕，骨盆不要翹高。"),
        new("core_captains_chair_knee_raise", "core", "羅馬椅提膝", Machine, "支撐式抬膝核心訓練。", "避免甩腿，慢慢放下。"),
        new("core_side_plank", "core", "側棒式", Bodyweight, "核心抗側屈與肩部支撐。", "身體保持一直線。"),
        new("core_hollow_hold", "core", "空心支撐", Bodyweight, "核心張力與身體控制。", "腰貼不住地面時屈膝降低難度。"),
        new("core_bird_dog", "core", "鳥狗式", Bodyweight, "核心穩定與髖肩協調。", "伸展時骨盆不要旋轉。"),

        new("arms_machine_biceps_curl", "arms", "二頭彎舉機", Machine, "穩定的手臂彎舉。", "手肘貼穩，不要用身體晃。", true),
        new("arms_dumbbell_curl", "arms", "啞鈴彎舉", Dumbbell, "基本二頭訓練。", "下降控制，不要甩肩。"),
        new("arms_barbell_curl", "arms", "槓鈴彎舉", Barbell, "可追蹤重量的二頭主動作。", "核心收緊，避免後仰。"),
        new("arms_hammer_curl", "arms", "槌式彎舉", Dumbbell, "訓練肱橈肌與二頭輔助。", "手腕保持中立。"),
        new("arms_triceps_pushdown", "arms", "三頭下壓", Cable, "三頭肌孤立訓練。", "手肘固定在身側。"),
        new("arms_rope_triceps_pushdown", "arms", "繩索三頭下壓", Cable, "三頭下壓變化，底端可外展。", "不要聳肩或用身體壓。"),
        new("arms_close_grip_push_up", "arms", "窄距伏地挺身", Bodyweight, "徒手三頭與胸推訓練。", "手腕不適時放寬手距。"),
        new("arms_seated_triceps_extension", "arms", "坐姿三頭伸展", Machine, "三頭長頭訓練。", "手肘不要過度外開。"),
        new("arms_bench_dip", "arms", "椅上撐體", Bodyweight, "徒手三頭訓練。", "肩膀不適時停用，身體靠近椅子。"),
        new("arms_plank_up_down", "arms", "棒式上下撐", Bodyweight, "手臂推撐與核心穩定。", "骨盆保持穩定，左右手交替。"),
        new("arms_isometric_push_up_hold", "arms", "伏地挺身停留", Bodyweight, "用等長停留增加手臂與胸部張力。", "停在可控制角度，避免肩前側疼痛。"),
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
                "自訂訓練",
                Bodyweight,
                "用目前可完成的安全動作完成本房間。",
                "保持可控制的幅度與呼吸。");
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
