using HarmonyLib;
using RimVore2;
using System;
using System.Collections.Generic;
using Verse;

namespace PRV2E
{
    /// <summary>
    /// RimVore2 的「跳转目标(RV2_RMB_JumpGoal)」右键选项在添加每条记录的二级菜单前，
    /// 只检查了未经筛选的 jump 列表；筛选（排除当前路径、排除不同 voreType）后可能一条都不剩，
    /// 于是创建了空的 FloatMenu，触发 "Created FloatMenu with no options. Closing." 红字。
    /// 这里整体接管该方法，在添加二级菜单前校验筛选结果。
    /// </summary>
    [HarmonyPatch(typeof(FloatMenuOptionProvider_Vore), "GetStageJumpOption")]
    public static class Patch_FloatMenuOptionProvider_Vore_GetStageJumpOption
    {
        /// <summary>
        /// RimVore2 更新后若目标方法改名/改签名，跳过补丁而不是让整个 PatchAll 抛异常
        /// （PatchAll 抛异常会连带 PRV2E 的其它补丁一起失效）。
        /// </summary>
        public static bool Prepare()
        {
            if(AccessTools.Method(typeof(FloatMenuOptionProvider_Vore), "GetStageJumpOption", new[] { typeof(Pawn) }) != null)
            {
                return true;
            }
            Log.Warning("PRV2E: RimVore2.FloatMenuOptionProvider_Vore.GetStageJumpOption not found, the empty vore jump menu fix is disabled.");
            return false;
        }

        public static bool Prefix(Pawn pawn, ref FloatMenuOption __result)
        {
            try
            {
                __result = BuildStageJumpOption(pawn);
                return false;
            }
            catch(Exception e)
            {
                // 交给 RimVore2 原实现，避免因本补丁异常而丢失整个右键菜单项
                Log.Error($"PRV2E: failed to build vore stage jump menu, falling back to RimVore2 implementation: {e}");
                return true;
            }
        }

        private static FloatMenuOption BuildStageJumpOption(Pawn pawn)
        {
            VoreTracker tracker = pawn.PawnData()?.VoreTracker;
            if(tracker == null || !tracker.IsTrackingVore)
            {
                return null;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach(VoreTrackerRecord record in tracker.VoreTrackerRecords)
            {
                string jumpKey = record.CurrentVoreStage?.def?.jumpKey;
                if(jumpKey == null)  // 当前阶段没有跳转能力
                {
                    continue;
                }

                List<FloatMenuOption> jumpOptions = new List<FloatMenuOption>();
                foreach(VoreJump jump in JumpUtility.Jumps(record.Predator, record.Prey, jumpKey))
                {
                    if(jump.path == record.VorePath.def)  // 不把正在进行的路径作为目标
                    {
                        continue;
                    }
                    if(jump.path.voreType != record.VorePath.VoreType)  // 只允许同 voreType 的路径（防止口腔跳转到肛门等）
                    {
                        continue;
                    }
                    jumpOptions.Add(new FloatMenuOption(jump.path.voreGoal.label, () => jump.Jump(record)));
                }

                if(jumpOptions.Count == 0)  // 关键修复：筛选后没有可选项，就不提供这条二级菜单
                {
                    continue;
                }
                options.Add(new FloatMenuOption(record.DisplayLabel, () => Find.WindowStack.Add(new FloatMenu(jumpOptions))));
            }

            if(options.Count == 0)
            {
                return null;
            }
            return new FloatMenuOption("RV2_RMB_JumpGoal".Translate(), () => Find.WindowStack.Add(new FloatMenu(options)));
        }
    }
}
