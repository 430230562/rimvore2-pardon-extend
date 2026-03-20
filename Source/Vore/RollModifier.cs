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
    public class RollModifier_StatWithMin : TargetedRollModifier
    {
        public StatDef stat;
        public float minValue;

        protected override bool TryGetModifier(VoreTrackerRecord record, out float modifier)
        {
            Pawn pawn = TargetPawn(record);
            modifier = pawn.GetStatValue(stat);
            if(modifier < minValue)
            {
                modifier = minValue;
            }
            return true;
        }

        protected override string Explain(float oldValue, float modifier, float newValue)
        {
            return base.Explain(oldValue, modifier, newValue) + " stat: " + stat.defName + "min value: " + minValue;
        }

        public override float AbstractModifyValue(float value)
        {
            return value;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (stat == null)
            {
                yield return "Required field \"stat\" not set";
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref stat, "stat");
            Scribe_Values.Look(ref minValue, "minValue", 0f);
        }
    }
}
