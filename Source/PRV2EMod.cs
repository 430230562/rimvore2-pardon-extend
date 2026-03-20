using HarmonyLib;
using System.Reflection;
using Verse;

namespace PRV2E
{
    [StaticConstructorOnStartup]
    public class PRV2EMod : Mod
    {
        public PRV2EMod(ModContentPack content) : base(content)
        {
            // 初始化Harmony
            var harmony = new Harmony("tourswen.EndfieldPerlica");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
