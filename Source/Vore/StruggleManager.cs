using HarmonyLib;
using RimVore2;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PRV2E
{
    // 新语法：使用 HarmonyPatch 特性的标准写法，明确指定目标类、目标方法
    [HarmonyPatch(typeof(StruggleManager), nameof(StruggleManager.Tick))]
    public static class Patch_StruggleManager_Tick
    {
        [HarmonyPostfix]
        private static void LearnMeleePostStruggle(VoreTrackerRecord ___record)
        {
            Pawn prey = ___record.Prey, predator = ___record.Predator;

            SkillRecord PreySkill = prey.skills.GetSkill(SkillDefOf.Melee), 
                PredatorSkill = predator.skills.GetSkill(SkillDefOf.Melee);

            PreySkill.xpSinceLastLevel += 40f * (RV2Mod.Settings.cheats.TraitStealIgnoresLearningFactor
            ? 1f
            : PreySkill.LearnRateFactor(true));

            PredatorSkill.xpSinceLastLevel += 30f * (RV2Mod.Settings.cheats.TraitStealIgnoresLearningFactor
            ? 1f
            : PredatorSkill.LearnRateFactor(true));

        }
    }
}
