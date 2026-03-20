using HarmonyLib;
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
    [HarmonyPatch(typeof(VoreValidator), nameof(VoreValidator.CalculateVoreCapacity))]

    public static class CalculateVoreCapacity
    {
        //pardon me
        public static bool Prefix(this Pawn pawn, ref float _result)
        {
            /*
            float capacity = pawn.BodySize;
            capacity *= RV2Mod.Settings.cheats.BodySizeToVoreCapacity;
            //希望有效
            
            QuirkManager quirkManager = pawn.QuirkManager();
            if (quirkManager != null)
            {
                if (quirkManager.HasComp<QuirkComp_ValueModifier>())
                {
                    capacity = quirkManager.ModifyValue("StorageCapacity", capacity);
                }
            }

            // override the calculated capacity if the user set a special lower limit
            capacity = Math.Max(capacity, RV2Mod.Settings.cheats.MinimumVoreCapacity);
            */
            _result = pawn.GetStatValue(StatDef.Named("RV2_Capacity"));

            return false;
        }
    }
}
