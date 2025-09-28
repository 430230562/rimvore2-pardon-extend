using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace RV2_PadonExtend
{
    public class Main
    {
        public Main()
        {
            new Harmony("Pardon.RV2Extend").PatchAll();
        }

        public const string id = "Pardon.RV2Extend";


    }
}
