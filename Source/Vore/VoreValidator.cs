using HarmonyLib;
using RimVore2;
using RimWorld;
using Verse;

namespace PRV2E
{
    // 新语法：使用 HarmonyPatch 特性的标准写法，明确指定目标类、目标方法
    [HarmonyPatch(typeof(VoreValidator), nameof(VoreValidator.CalculateVoreCapacity))]
    public static class CalculateVoreCapacity_Patch
    {
        // Prefix 方法遵循 Harmony 2.x 规范：
        // 1. 不再使用扩展方法（移除 this 关键字）
        // 2. 方法名统一为 Prefix（大小写敏感）
        // 3. 参数通过 Harmony 自动注入，使用 ref 接收返回值
        public static bool Prefix(Pawn pawn, ref float __result)
        {
            // 原有核心逻辑保留：用 StatDef "RV2_Capacity" 的值覆盖容量计算结果
            __result = pawn.GetStatValue(StatDef.Named("RV2_Capacity"));

            // 返回 false 跳过原方法执行（原有逻辑不变）
            return false;
        }
    }
}
