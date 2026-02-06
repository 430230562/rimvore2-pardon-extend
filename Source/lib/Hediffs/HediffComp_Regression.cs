using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace pardon_RV2
{
    public class HediffComp_Regression : HediffComp
    {
        public HediffCompProperties_Regression Props => (HediffCompProperties_Regression)props;

        private long minAgeTicks = 0;

        private int mul = 0;

        public override void CompPostMake()
        {
            if (Props.limitMinAge)
            {
                minAgeTicks = Pawn.ageTracker.AdultMinAgeTicks;
            }
            else
            {
                minAgeTicks = 0;
            }
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if ( Pawn.ageTracker.AgeBiologicalTicks > minAgeTicks ) {
                Pawn.ageTracker.AgeBiologicalTicks = (long)Math.Max(Pawn.ageTracker.AgeBiologicalTicks - Props.RegressionStrength * parent.Severity * delta, 0);

                foreach (Hediff hediff in Pawn.health.hediffSet.hediffs.Where((Hediff diff) => diff.def.chronic))
                {
                    if (Rand.Chance(0.0005f * mul - 0.5f)) {
                        Pawn.health.RemoveHediff(Pawn.health.hediffSet.GetFirstHediffOfDef(hediff.def, false));
                        mul = 0;
                    }else if (hediff.TryGetComp<HediffComp_SeverityPerDay>() != null) {
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
        public override string CompTipStringExtra => string.Concat("AgingSpeed".Translate() + ": x", 0.ToString());
    }
}
