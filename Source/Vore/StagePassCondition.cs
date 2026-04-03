using RimVore2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRV2E
{
    public class StagePassCondition_Age : TargetedStagePassCondition
    {
        public override float AbstractDuration(StageWorker onCycle, StageWorker onStart)
        {
            return 100f;
        }

    }
}
