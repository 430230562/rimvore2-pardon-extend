using RimVore2;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RV2_Pardon
{
    public class RollAction_Regression : RollAction
    {
        public float RegressionStrength = 10f;

        public override bool TryAction(VoreTrackerRecord record, float rollStrength)
        {
            base.TryAction(record, rollStrength);

            if (record.Prey.ageTracker.AgeBiologicalTicks > record.Prey.ageTracker.AdultMinAgeTicks) {
                record.Prey.ageTracker.AgeBiologicalTicks -= (long)(RegressionStrength * rollStrength);
                Message message = new Message();

                foreach (Hediff hediff in record.Prey.health.hediffSet.hediffs.Where((Hediff diff) => diff.def.chronic)){
                    if (Rand.Chance(0.005f))
                    {
                        message = new Message("HealingCureHediff".Translate(record.Prey, hediff.def.label), MessageTypeDefOf.PositiveEvent, new LookTargets(record.Prey));
                        Messages.Message(message, true);
                        record.Prey.health.RemoveHediff(record.Prey.health.hediffSet.GetFirstHediffOfDef(hediff.def, false));
                    }
                }

                return true;
            }
            else { return false; }
        }
    }
}
