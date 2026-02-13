using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;
using RimVore2;

namespace RimVore2_Pardon
{
    public class HediffComp_Regression : HediffComp
    {
        public HediffCompProperties_Regression Props => (HediffCompProperties_Regression)props;

        private int mul = 0;

        private float AgingSpeed = 0f;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if ((Pawn.ageTracker.AgeBiologicalTicks > Pawn.ageTracker.AdultMinAgeTicks || Props.limitMinAge) && Find.TickManager.TicksGame % 60 == 0)
            {
                //更新描述文本
                AgingSpeed = 1 - Props.RegressionStrength * parent.Severity;

                Pawn.ageTracker.AgeBiologicalTicks = (long)Math.Max(Pawn.ageTracker.AgeBiologicalTicks - Props.RegressionStrength * parent.Severity * 60, 0);

                foreach (Hediff hediff in Pawn.health.hediffSet.hediffs.Where((Hediff diff) => diff.def.chronic))
                {
                    if (Rand.Chance(0.0005f * mul - 0.5f))
                    {
                        Pawn.health.RemoveHediff(Pawn.health.hediffSet.GetFirstHediffOfDef(hediff.def, false));
                        mul = 0;
                    }
                    else if (hediff.TryGetComp<HediffComp_SeverityPerDay>() != null)
                    {
                        hediff.Severity -= 0.0005f;
                        if (hediff.Severity <= 0)
                        {
                            Pawn.health.RemoveHediff(Pawn.health.hediffSet.GetFirstHediffOfDef(hediff.def, false));
                        }
                    }
                }

                mul += 1;

            }
        }
        public override string CompTipStringExtra => string.Concat("AgingSpeed".Translate() + ": x", AgingSpeed.ToString());
    }

    public class HediffCompProperties_Regression : HediffCompProperties
    {
        public float RegressionStrength = 20f;

        public bool limitMinAge = true;

        public HediffCompProperties_Regression()
        {
            compClass = typeof(HediffComp_Regression);
        }
    }
}
