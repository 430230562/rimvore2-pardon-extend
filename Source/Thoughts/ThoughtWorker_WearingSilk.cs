using System.Collections.Generic;
using RimVore2;
using RimWorld;
using Verse;

namespace PRV2E
{
    /// <summary>
    /// Situational thought for pawns wearing apparel made from living silk (a vore product).
    /// The stage is picked from the pawn's vore product preference quirk, so pawns who enjoy
    /// processed prey get a mood buff while pawns who are disgusted by it get a penalty.
    /// </summary>
    public class ThoughtWorker_WearingSilk : ThoughtWorker
    {
        // Ordered from best to worst preference, must match the thought's stage indices
        private static readonly string[] PreferenceQuirkNames =
        {
            "ProductPreference_Loves",
            "ProductPreference_Likes",
            "ProductPreference_Neutral",
            "ProductPreference_Dislikes",
            "ProductPreference_Hates"
        };
        private const int NeutralStage = 2;

        private static ThingDef silkDef;
        private static QuirkDef[] preferenceQuirks;

        private static ThingDef SilkDef
        {
            get
            {
                if(silkDef == null)
                {
                    silkDef = DefDatabase<ThingDef>.GetNamedSilentFail("RV2_Resource_Silk");
                }
                return silkDef;
            }
        }

        private static QuirkDef[] PreferenceQuirks
        {
            get
            {
                if(preferenceQuirks == null)
                {
                    preferenceQuirks = new QuirkDef[PreferenceQuirkNames.Length];
                    for(int i = 0; i < PreferenceQuirkNames.Length; i++)
                    {
                        preferenceQuirks[i] = DefDatabase<QuirkDef>.GetNamedSilentFail(PreferenceQuirkNames[i]);
                    }
                }
                return preferenceQuirks;
            }
        }

        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            ThingDef silk = SilkDef;
            if(silk == null || !IsWearingStuff(pawn, silk))
            {
                return ThoughtState.Inactive;
            }
            int stage = PreferenceStage(pawn);
            if(stage < 0 || stage == NeutralStage)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(stage);
        }

        private static bool IsWearingStuff(Pawn pawn, ThingDef stuff)
        {
            List<Apparel> wornApparel = pawn.apparel?.WornApparel;
            if(wornApparel == null)
            {
                return false;
            }
            for(int i = 0; i < wornApparel.Count; i++)
            {
                if(wornApparel[i].Stuff == stuff)
                {
                    return true;
                }
            }
            return false;
        }

        private static int PreferenceStage(Pawn pawn)
        {
            QuirkManager quirks = pawn.QuirkManager();
            if(quirks == null)
            {
                return -1;
            }
            QuirkDef[] quirksToCheck = PreferenceQuirks;
            for(int i = 0; i < quirksToCheck.Length; i++)
            {
                QuirkDef quirkDef = quirksToCheck[i];
                if(quirkDef != null && quirks.HasQuirk(quirkDef))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
