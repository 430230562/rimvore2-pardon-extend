using RimWorld;
using Verse;
using Verse.AI.Group;

namespace PRV2E
{
    public class StageFailTrigger_PawnNotVored : RimVore2.StageFailTrigger_PawnNotVored
    {
        public override bool Failed(LordJob_Ritual ritual, TargetInfo spot, TargetInfo focus)
        {
            Pawn prey = ritual.assignments.FirstAssignedPawn(preyId);
            if (prey?.Dead == true && prey.health?.killedByRitual == true)
            {
                return false;
            }

            return base.Failed(ritual, spot, focus);
        }
    }
}