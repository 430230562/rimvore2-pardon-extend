using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace pardon_RV2
{
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
