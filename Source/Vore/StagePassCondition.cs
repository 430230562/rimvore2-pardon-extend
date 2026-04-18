using RimVore2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PRV2E
{
    public class StagePassCondition_Age : TargetedStagePassCondition
    {
        public override float AbstractDuration(StageWorker onCycle, StageWorker onStart)
        {
            return 100f;
        }

    }
    //希望有用
    public class StagePassCondition_QuirkWarmup : StagePassCondition_Timed
    {
        public int duration;
        public List<HediffDef> hediffs = new List<HediffDef>();

        public override bool IsPassed(VoreTrackerRecord record, out float progress)
        {
            if (!RV2Mod.Settings.fineTuning.SkipWarmupWhenAlreadyDigesting)
            {
                int adaptedDuration = Math.Max(1, (int)(duration / RV2Mod.Settings.cheats.VoreSpeedMultiplier * record.Predator.QuirkManager().ModifyValue("warmupSpeed", 1)));    // prevent divide by 0 exception with math.max
                int currentlyPassed = record.CurrentVoreStage.PassedRareTicks;
                progress = currentlyPassed / adaptedDuration;
                progress = CalculateProgress(currentlyPassed, adaptedDuration, 0);
                bool isPassed = record.CurrentVoreStage.PassedRareTicks >= adaptedDuration;
                if (RV2Log.ShouldLog(true, "OngoingVore"))
                    RV2Log.Message($"{record.LogLabel} - PassCondition_Timed progress: {progress} ({currentlyPassed}/{duration}({adaptedDuration})), passed ? {isPassed}", true, "OngoingVore");
                return isPassed;
            }

            HediffSet hediffSet = record.Predator.health.hediffSet;
            bool predatorHasWarmedUp = hediffs.Any(h => hediffSet.HasHediff(h));
            if (predatorHasWarmedUp)
            {
                progress = 1;
                return true;
            }

            return base.IsPassed(record, out progress);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (hediffs.NullOrEmpty())
            {
                yield return $"Required list {nameof(hediffs)} is not provided";
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref hediffs, "hediffs");
        }
    }
}