using HarmonyLib;
using RimVore2;
using RimWorld;
using System;
using Verse;

namespace PRV2E
{
    /// <summary>
    /// 被标记为体内牢房 / 转化原料的囚犯真正被吞入体内后，释放其原床位的占用，
    /// 让床位可以分配给其他囚犯。
    /// </summary>
    [HarmonyPatch(typeof(PreVoreUtility), nameof(PreVoreUtility.PopulateRecord))]
    public static class Patch_PreVoreUtility_PopulateRecord_ReleasePrisonerBed
    {
        public static void Postfix(ref VoreTrackerRecord record)
        {
            try
            {
                Pawn prey = record?.Prey;
                if(prey == null || !prey.IsPrisonerOfColony || !prey.IsPrisonerVoreTransfer())
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
            catch(Exception e)
            {
                // 这里抛异常会中断 PreVoreUtility.PopulateRecord，导致 vore 记录无法建立
                Log.Error($"PRV2E: failed to release prisoner bed during vore: {e}");
            }
        }
    }
}
