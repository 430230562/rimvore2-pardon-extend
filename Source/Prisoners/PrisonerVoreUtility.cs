using System;
using System.Collections.Generic;
using System.Linq;
using RimVore2;
using RimWorld;
using Verse;
using Verse.AI;

namespace PRV2E
{
    /// <summary>
    /// 囚犯面板里“由殖民者吞下囚犯”的两种互动模式：
    /// 体内牢房（PRV2E_InBodyCell）用 Store 目标长期关押；
    /// 转化原料（PRV2E_ConvertMaterial）用 Convert 开头的目标把囚犯转化为产物。
    /// </summary>
    public static class PrisonerVoreUtility
    {
        private const float minimumPathWeight = 0.01f;

        // 转化流程中出现过的阶段目标，用于过滤被其它模组破坏成残缺阶段的路径
        private static readonly HashSet<string> conversionPartGoals = new HashSet<string>()
        {
            "WarmUp", "Dissolve", "Mush", "Lubrify", "Lactify", "Spermify", "Sericify"
        };

        private class TransferProfile
        {
            public bool ForceVore;
            public Func<VoreGoalDef, bool> GoalFilter;
            public Func<VorePathDef, bool> PathFilter;
        }

        private static TransferProfile inBodyCellProfile;
        private static TransferProfile conversionMaterialProfile;

        public static bool IsInBodyCell(this Pawn prisoner)
        {
            return IsMode(prisoner, PRV2E_DefOf.PRV2E_InBodyCell);
        }

        public static bool IsConversionMaterial(this Pawn prisoner)
        {
            return IsMode(prisoner, PRV2E_DefOf.PRV2E_ConvertMaterial);
        }

        /// <summary>
        /// 该囚犯是否被标记为“由殖民者吞入体内”的任一模式。
        /// </summary>
        public static bool IsPrisonerVoreTransfer(this Pawn prisoner)
        {
            return prisoner.IsInBodyCell() || prisoner.IsConversionMaterial();
        }

        private static bool IsMode(Pawn prisoner, PrisonerInteractionModeDef mode)
        {
            return mode != null && prisoner?.guest?.ExclusiveInteractionMode == mode;
        }

        public static void ProcessMap(Map map)
        {
            if(map == null)
            {
                return;
            }
            List<Pawn> prisoners = map.mapPawns.PrisonersOfColonySpawned;
            if(prisoners.NullOrEmpty())
            {
                return;
            }
            foreach(Pawn prisoner in prisoners)
            {
                if(prisoner.Dead || !prisoner.Spawned)
                {
                    continue;
                }
                TransferProfile profile = ProfileFor(prisoner);
                if(profile == null)
                {
                    continue;
                }
                if(HasPendingTransfer(prisoner, map))
                {
                    continue;
                }
                if(!TryFindPredator(prisoner, profile, out Pawn predator, out VorePathDef path))
                {
                    continue;
                }
                StartTransfer(predator, prisoner, path, profile.ForceVore);
            }
        }

        private static TransferProfile ProfileFor(Pawn prisoner)
        {
            if(prisoner.IsInBodyCell())
            {
                if(inBodyCellProfile == null)
                {
                    inBodyCellProfile = new TransferProfile()
                    {
                        ForceVore = false,
                        GoalFilter = goal => goal == VoreGoalDefOf.Store,
                        PathFilter = IsUsableStoragePath
                    };
                }
                return inBodyCellProfile;
            }
            if(prisoner.IsConversionMaterial())
            {
                if(conversionMaterialProfile == null)
                {
                    conversionMaterialProfile = new TransferProfile()
                    {
                        ForceVore = true,
                        GoalFilter = goal => goal.defName.StartsWith("Convert", StringComparison.Ordinal),
                        PathFilter = IsUsableConversionPath
                    };
                }
                return conversionMaterialProfile;
            }
            return null;
        }

        private static bool TryFindPredator(Pawn prisoner, TransferProfile profile, out Pawn predator, out VorePathDef path)
        {
            predator = null;
            path = null;
            Map map = prisoner.Map;
            if(map == null)
            {
                return false;
            }

            List<Pawn> candidates = map.mapPawns.FreeColonistsSpawned
                .Where(candidate => IsValidCandidate(candidate, prisoner))
                .OrderBy(candidate => candidate.Position.DistanceToSquared(prisoner.Position))
                .ToList();

            foreach(Pawn candidate in candidates)
            {
                // 这是玩家通过囚犯面板下的明确指令，不受“自动吞噬”开关限制
                VoreInteraction interaction = VoreInteractionManager.Retrieve(
                    new VoreInteractionRequest(candidate, prisoner, VoreRole.Predator));
                if(!interaction.IsValid)
                {
                    continue;
                }
                List<VorePathDef> usablePaths = interaction.ValidPaths
                    .Where(candidatePath => profile.GoalFilter(candidatePath.voreGoal))
                    .Where(profile.PathFilter)
                    .ToList();
                if(usablePaths.NullOrEmpty())
                {
                    continue;
                }
                predator = candidate;
                path = PickPath(candidate, interaction, usablePaths);
                return true;
            }
            return false;
        }

        private static VorePathDef PickPath(Pawn predator, VoreInteraction interaction, List<VorePathDef> usablePaths)
        {
            VorePathDef preferredPath = interaction.PreferredPath;
            if(preferredPath != null && usablePaths.Contains(preferredPath))
            {
                return preferredPath;
            }
            return RandomUtility.RandomElementByWeight(usablePaths, path => Math.Max(minimumPathWeight, path.GetPreference(predator, VoreRole.Predator)));
        }

        /// <summary>
        /// 过滤掉 stage 不完整的储存路径。其它模组破坏 VoreStageDef 时，路径会只剩入口阶段，
        /// 此时 RV2 会认为已经走到路径终点，在入口阶段的时间条件满足后立刻把猎物吐出。
        /// </summary>
        private static bool IsUsableStoragePath(VorePathDef path)
        {
            if(!HasMultipleStages(path))
            {
                return false;
            }
            return path.stages.Any(stage => stage?.partGoal?.defName == "Store");
        }

        private static bool IsUsableConversionPath(VorePathDef path)
        {
            if(!HasMultipleStages(path))
            {
                return false;
            }
            return path.stages.Any(stage => stage?.partGoal != null && conversionPartGoals.Contains(stage.partGoal.defName));
        }

        private static bool HasMultipleStages(VorePathDef path)
        {
            return path?.stages != null && path.stages.Count >= 2;
        }

        private static bool IsValidCandidate(Pawn candidate, Pawn prisoner)
        {
            if(candidate == null || candidate == prisoner || candidate.jobs == null)
            {
                return false;
            }
            if(candidate.Dead || !candidate.Spawned || candidate.Downed || candidate.Drafted)
            {
                return false;
            }
            if(!candidate.IsColonistPlayerControlled || candidate.InMentalState)
            {
                return false;
            }
            if(IsBusyWithVore(candidate))
            {
                return false;
            }
            if(!candidate.CanReach(prisoner, PathEndMode.Touch, Danger.Some))
            {
                return false;
            }
            return candidate.CanReserve(prisoner);
        }

        private static bool IsBusyWithVore(Pawn pawn)
        {
            if(pawn?.jobs == null)
            {
                return false;
            }
            if(pawn.jobs.curJob is VoreJob)
            {
                return true;
            }
            JobQueue jobQueue = pawn.jobs.jobQueue;
            if(jobQueue == null)
            {
                return false;
            }
            foreach(QueuedJob queuedJob in jobQueue)
            {
                if(queuedJob.job is VoreJob)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasPendingTransfer(Pawn prisoner, Map map)
        {
            foreach(Pawn colonist in map.mapPawns.FreeColonistsSpawned)
            {
                if(colonist?.jobs == null)
                {
                    continue;
                }
                if(IsTransferJob(colonist.jobs.curJob, prisoner))
                {
                    return true;
                }
                JobQueue jobQueue = colonist.jobs.jobQueue;
                if(jobQueue == null)
                {
                    continue;
                }
                foreach(QueuedJob queuedJob in jobQueue)
                {
                    if(IsTransferJob(queuedJob.job, prisoner))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsTransferJob(Job job, Pawn prisoner)
        {
            return job is VoreJob
                && job.def == VoreJobDefOf.RV2_VoreInitAsPredator
                && (prisoner == null || job.targetA.Pawn == prisoner);
        }

        private static void StartTransfer(Pawn predator, Pawn prisoner, VorePathDef path, bool forceVore)
        {
            if(predator?.jobs == null)
            {
                return;
            }
            VoreJob job = VoreJobMaker.MakeJob(VoreJobDefOf.RV2_VoreInitAsPredator, predator, prisoner);
            job.targetA = prisoner;
            job.VorePath = path;
            job.IsForced = forceVore;
            predator.jobs.TryTakeOrderedJob(job);
        }
    }
}
