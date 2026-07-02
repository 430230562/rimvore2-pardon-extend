using RimVore2;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PRV2E
{

    public class StagePassCondition_Warmup_Quirk : StagePassCondition
    {
        public int duration = 20;
        public override bool IsPassed(VoreTrackerRecord record, out float progress)
        {
            // 空值保护
            if (record == null || record.Predator == null || record.CurrentVoreStage == null)
            {
                progress = 0;
                RV2Log.Error($"{nameof(StagePassCondition_Warmup_Quirk)}: 无效的捕食记录或捕食者数据");
                return false;
            }

            float totalSpeed = RV2Mod.Settings.cheats.VoreSpeedMultiplier * record.Predator.QuirkManager().ModifyValue("WarmupSpeed", 1f);
            if(record.Predator.QuirkManager().ModifyValue("WarmupSpeed", 1f) >= 20 || totalSpeed >= 20)
            {
                progress = 1;
                return true;
            }
            int adaptedDuration = Math.Max(1, (int)(duration / totalSpeed));
            int currentlyPassed = record.CurrentVoreStage.PassedRareTicks;
            progress = CalculateProgress(currentlyPassed, adaptedDuration, 0);
            bool isPassed = currentlyPassed >= adaptedDuration;

            if (RV2Log.ShouldLog(true, "OngoingVore"))
            {
                RV2Log.Message($"{record.LogLabel} - StagePassCondition_Warmup_Quirk progress: {progress} " +
                    $"({currentlyPassed}/{duration}({adaptedDuration})), speed {totalSpeed}, passed ? {isPassed}", true, "OngoingVore");
            }
            return isPassed;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (duration <= 0)
            {
                yield return "required field \"duration\" must be larger than 0";
            }
        }

        /// <summary>
        /// 数据序列化/反序列化（存档/读档）
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // 序列化时长
            Scribe_Values.Look(ref duration, "duration");
        }

        public override float AbstractDuration(StageWorker onCycle, StageWorker onStart)
        {
            return duration;
        }
    }

    public class StagePassCondition_Warmup_Hediffs : StagePassCondition_Warmup_Quirk
    {
        public List<HediffDef> hediffs = new List<HediffDef>();

        public override bool IsPassed(VoreTrackerRecord record, out float progress)
        {
            if (!RV2Mod.Settings.fineTuning.SkipWarmupWhenAlreadyDigesting)
            {
                return base.IsPassed(record, out progress);
            }
            else
            {
                HediffSet hediffSet = record.Predator.health.hediffSet;
                bool predatorHasWarmedUp = hediffs.Any(h => hediffSet.HasHediff(h));
                if (predatorHasWarmedUp)
                {
                    progress = 1;
                    return true;
                }

                return base.IsPassed(record, out progress);
            }
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

    public class StagePassCondition_Reform : StagePassCondition_PassValue
    {
        public VoreRole target = VoreRole.Prey;

        public float targetValue = float.MinValue;

        protected Pawn TargetPawn(VoreTrackerRecord record) => record.GetPawnByRole(target);

        public int targetAge = 0;

        private long targetAgeTicks => targetAge * GenDate.TicksPerYear;

        private long initialAgeTicks = -1;

        public override bool IsPassed(VoreTrackerRecord record, out float progress)
        {
            if(initialAgeTicks == -1)
            {
                initialAgeTicks = TargetPawn(record).ageTracker.AgeBiologicalTicks;
            }

            if (!record.PassValues.TryGetValue(passValueName, out float currentValue))
            {
                if (RV2Log.ShouldLog(true, "OngoingVore"))
                    RV2Log.Message($"{record.LogLabel} - PassCondition_Reform, but record does not have PassValue {passValueName} set! Passing to prevent being stuck", true, "OngoingVore");
                progress = -1;
                return true;
            }
            if (!record.InitialPassValues.TryGetValue(passValueName, out float initialValue))
            {
                if (RV2Log.ShouldLog(true, "OngoingVore"))
                    RV2Log.Message($"{record.LogLabel} - PassCondition_Reform, but record does not have InitialPassValue {passValueName} set! Passing to prevent being stuck", true, "OngoingVore");
                progress = -1;
                return true;
            }
            float passValueProgress = CalculateProgress(currentValue, targetValue, initialValue);
            float ageProgress = CalculateProgress(TargetPawn(record).ageTracker.AgeBiologicalTicks, targetAgeTicks, initialAgeTicks);
            progress = Math.Max(passValueProgress, ageProgress);

            bool isPassed = (progress == 1);
            if (RV2Log.ShouldLog(true, "OngoingVore"))
                RV2Log.Message($"{record.LogLabel} - PassCondition_Reform pass value progress: {passValueProgress} ({currentValue}/{targetValue}), age progress: {ageProgress} ({TargetPawn(record).ageTracker.AgeBiologicalTicks}/{targetAgeTicks}), passed ? {isPassed}", true, "OngoingVore");
            return isPassed;

        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref targetValue, "targetValue");
            Scribe_Values.Look(ref target, "target");
            Scribe_Values.Look(ref targetAge, "targetAge");
        }
    }
}