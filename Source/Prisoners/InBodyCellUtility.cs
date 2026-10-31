using System.Collections.Generic;
using System.Linq;
using RimVore2;
using RimWorld;
using Verse;
using Verse.AI;

namespace PRV2E
{
    /// <summary>
    /// 体内牢房：当囚犯的互动模式为 PRV2E_InBodyCell 时，
    /// 自动匹配一名能够执行 Store（手动储存）目标的殖民者，把囚犯吞入体内长期关押。
    /// </summary>
    public static class InBodyCellUtility
    {
        public static bool IsInBodyCell(this Pawn prisoner)
        {
            return prisoner?.guest?.ExclusiveInteractionMode == PRV2E_DefOf.PRV2E_InBodyCell;
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
                if(!prisoner.IsInBodyCell() || prisoner.Dead || !prisoner.Spawned)
                {
                    continue;
                }
                if(HasPendingTransfer(prisoner, map))
                {
                    continue;
                }
                if(!TryFindPredator(prisoner, out Pawn predator, out VorePathDef path))
                {
                    continue;
                }
                StartTransfer(predator, prisoner, path);
            }
        }

        private static bool TryFindPredator(Pawn prisoner, out Pawn predator, out VorePathDef path)
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
                VoreInteraction interaction = VoreInteractionManager.Retrieve(
                    new VoreInteractionRequest(candidate, prisoner, VoreRole.Predator, isForAuto: true));
                if(!interaction.IsValid)
                {
                    continue;
                }
                List<VorePathDef> storePaths = interaction.ValidPathsFor(VoreGoalDefOf.Store).ToList();
                if(storePaths.NullOrEmpty())
                {
                    continue;
                }
                VorePathDef preferredPath = interaction.PreferredPath;
                predator = candidate;
                path = storePaths.Contains(preferredPath) ? preferredPath : storePaths.RandomElement();
                return true;
            }
            return false;
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

        private static void StartTransfer(Pawn predator, Pawn prisoner, VorePathDef path)
        {
            if(predator?.jobs == null)
            {
                return;
            }
            VoreJob job = VoreJobMaker.MakeJob(VoreJobDefOf.RV2_VoreInitAsPredator, predator, prisoner);
            job.targetA = prisoner;
            job.VorePath = path;
            job.IsForced = false;
            predator.jobs.TryTakeOrderedJob(job);
        }
    }
}
