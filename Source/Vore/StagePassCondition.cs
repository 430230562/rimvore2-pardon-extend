using RimVore2;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        protected Pawn TargetPawn(VoreTrackerRecord record) => record.GetPawnByRole(target);

        public int targetAge = 0;
        //或许需要一点缓冲
        private long targetAgeTicks => targetAge * GenDate.TicksPerYear + 240;

        private long initialAgeTicks = -1;

        private float targetValue
        {
            get
            {
                var field = typeof(StagePassCondition_PassValue)
                    .GetField("targetValue",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                return (float)field.GetValue(this);
            }
        }

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
            float ageProgress = CalculateProgress(initialAgeTicks - TargetPawn(record).ageTracker.AgeBiologicalTicks, initialAgeTicks - targetAgeTicks, 0);
            progress = Math.Max(passValueProgress, ageProgress);

            bool isPassed = progress == 1;
            if (RV2Log.ShouldLog(true, "OngoingVore"))
                RV2Log.Message($"{record.LogLabel} - PassCondition_Reform pass value progress: {passValueProgress} ({currentValue}/{targetValue}), age progress: {ageProgress} ({TargetPawn(record).ageTracker.AgeBiologicalTicks}/{targetAgeTicks}), passed ? {isPassed}", true, "OngoingVore");
            return isPassed;

        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref target, "target");
            Scribe_Values.Look(ref targetAge, "targetAge");
        }
    }

    /// <summary>
    /// 与 RollAction_Brainwash 配套的阶段通过条件：
    /// - 招募模式(enslave = false)：目标抵抗值(resistance)归零时通过
    /// - 奴役模式(enslave = true)：目标意志值(will)归零时通过
    /// 目标已死亡、没有 guest 数据或已经以对应身份加入时直接通过，避免阶段卡死。
    /// </summary>
    public class StagePassCondition_Brainwash : StagePassCondition
    {
        public VoreRole target = VoreRole.Prey;
        public bool enslave = false;
        // 目标已经以对应身份加入时直接通过
        public bool passWhenAlreadyJoined = true;
        // 仅用于估算阶段时长，不代表目标真实的抵抗值 / 意志值
        public float assumedInitialValue = 10f;
        // 与 RollAction_Brainwash 中 rollStrength * 0.05f 的系数保持一致
        private const float influencePerAction = 0.05f;

        // 阶段条件是定义级的共享实例，用 record 的哈希区分不同的吞噬记录，用于计算进度
        private readonly Dictionary<int, float> initialValues = new Dictionary<int, float>();

        public StagePassCondition_Brainwash()
        {
            // 抵抗值 / 意志值都是递减到 0
            decreasing = true;
        }

        protected Pawn TargetPawn(VoreTrackerRecord record) => record.GetPawnByRole(target);

        public override bool IsPassed(VoreTrackerRecord record, out float progress)
        {
            progress = -1f;
            Pawn pawn = TargetPawn(record);
            // 目标已死亡或不存在：直接通过，避免卡死
            if (pawn == null || pawn.Dead)
            {
                return true;
            }
            Pawn_GuestTracker guest = pawn.guest;
            if (guest == null)
            {
                // 没有 guest 数据的 pawn（例如动物）无法被洗脑，直接通过避免卡死
                if (RV2Log.ShouldLog(true, "OngoingVore"))
                    RV2Log.Message($"{record.LogLabel} - PassCondition_Brainwash, target {pawn.LabelShort} has no guest data, passing to prevent being stuck", true, "OngoingVore");
                return true;
            }
            if (passWhenAlreadyJoined && HasJoined(guest))
            {
                progress = 1f;
                return true;
            }

            float currentValue = enslave ? guest.will : guest.resistance;
            int initialValueKey = record.GetHashCode();
            if (!initialValues.TryGetValue(initialValueKey, out float initialValue))
            {
                initialValue = currentValue;
                initialValues[initialValueKey] = initialValue;
            }
            progress = CalculateProgress(currentValue, 0f, initialValue);
            bool isPassed = currentValue <= 0f;

            if (isPassed)
            {
                initialValues.Remove(initialValueKey);
            }

            if (RV2Log.ShouldLog(true, "OngoingVore"))
                RV2Log.Message($"{record.LogLabel} - PassCondition_Brainwash {(enslave ? "will" : "resistance")} progress: {progress} ({currentValue}/{initialValue}), passed ? {isPassed}", true, "OngoingVore");
            return isPassed;
        }

        private bool HasJoined(Pawn_GuestTracker guest)
        {
            JoinStatus desiredStatus = enslave ? JoinStatus.JoinAsSlave : JoinStatus.JoinAsColonist;
            return guest.joinStatus == desiredStatus;
        }

        public override float AbstractDuration(StageWorker onCycle, StageWorker onStart)
        {
            float influencePerRareTick = 0f;
            if (onCycle != null)
            {
                foreach (Roll roll in onCycle.Rolls)
                {
                    if (roll.actionsOnSuccess.Any(action => Applies(action)))
                    {
                        influencePerRareTick += roll.AbstractStrength() * influencePerAction;
                    }
                }
            }
            if (influencePerRareTick <= 0f)
            {
                return float.MaxValue;
            }
            return assumedInitialValue / influencePerRareTick;

            bool Applies(RollAction action)
            {
                return action is RollAction_Brainwash brainwash && brainwash.enslave == enslave;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (target == VoreRole.Invalid)
            {
                yield return "Required field \"target\" must be set";
            }
            if (assumedInitialValue <= 0f)
            {
                yield return "Field \"assumedInitialValue\" must be larger than 0 (only used for duration estimation)";
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref target, "target", VoreRole.Prey);
            Scribe_Values.Look(ref enslave, "enslave");
            Scribe_Values.Look(ref passWhenAlreadyJoined, "passWhenAlreadyJoined", true);
            Scribe_Values.Look(ref assumedInitialValue, "assumedInitialValue", 10f);
        }
    }
}