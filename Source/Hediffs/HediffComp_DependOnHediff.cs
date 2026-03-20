using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PRV2E
{
    public class HediffCompProperties_DependOnHediff : HediffCompProperties
    {
        public List<HediffDef> hediffs = new List<HediffDef>();

        public HediffCompProperties_DependOnHediff()
        {
            compClass = typeof(HediffComp_DependOnHediff);
        }
    }
    public class HediffComp_DependOnHediff : HediffComp
    {
        //为了优化
        public int removalCheckInterval = 600;

        public HediffCompProperties_DependOnHediff Props => (HediffCompProperties_DependOnHediff)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Find.TickManager.TicksGame % removalCheckInterval == 0 && ShouldRemove())
            {
                parent.pawn.health.RemoveHediff(parent);
            }
        }

        private bool ShouldRemove()
        {
            if (base.CompShouldRemove)
            {
                return true;
            }
            foreach (HediffDef hediff in Props.hediffs)
            {
                Hediff firstHediffOfDef = Pawn.health.hediffSet.GetFirstHediffOfDef(hediff);
                if (firstHediffOfDef != null)
                {
                    return false;
                }
            }
            return true;
        }

        private string HediffListToString()
        {
            string result = " ";
            foreach (HediffDef hediff in Props.hediffs)
            {
                result += hediff.label.Translate() + "\n";
            }
            return result;
        }

        //这会返回一个正确的string吗? 答案是不会,
        //public override string CompTipStringExtra => string.Concat("DependOn".Translate(), Props.hediffs.ToString().Translate());

        //能成功吗? 可以
        public override string CompTipStringExtra => "DependOn".Translate() + HediffListToString();
    }
}
