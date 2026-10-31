using HarmonyLib;
using RimVore2;
using RimWorld;
using Verse;

namespace PRV2E
{
    /// <summary>
    /// 囚犯被真正吞入体内后，释放其原床位的占用，让床位可以分配给其他囚犯。
    /// </summary>
    [HarmonyPatch(typeof(PreVoreUtility), nameof(PreVoreUtility.PopulateRecord))]
    public static class Patch_PreVoreUtility_PopulateRecord_ReleasePrisonerBed
    {
        public static void Postfix(ref VoreTrackerRecord record)
        {
            Pawn prey = record?.Prey;
            if(prey == null || !prey.IsPrisonerOfColony || !prey.IsInBodyCell())
            {
                return;
            }
            Building_Bed bed = prey.ownership?.OwnedBed;
            if(bed == null || !prey.ownership.UnclaimBed())
            {
                return;
            }
            bed.NotifyRoomAssignedPawnsChanged();
        }
    }
}
