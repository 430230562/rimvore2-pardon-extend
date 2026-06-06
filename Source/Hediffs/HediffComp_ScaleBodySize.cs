using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PRV2E
{
    public class HediffCompProperties_ScaleBodySize : HediffCompProperties
    {
        public float scaleFactor = 1.0f; // 每1点 severity 增加的体型倍数

        public HediffCompProperties_ScaleBodySize()
        {
            compClass = typeof(HediffCompProperties_ScaleBodySize);
        }
    }
    public class HediffComp_ScaleBodySize : HediffComp

    {
        public HediffCompProperties_ScaleBodySize Props => (HediffCompProperties_ScaleBodySize)props;

        public float CurrentScaleOffset => parent.Severity * Props.scaleFactor;

        private static readonly FieldInfo BodyRenderScaleField = AccessTools.Field(typeof(PawnRenderer), "bodyRenderScale");

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            // 每 30 帧（约0.5秒）更新一次，完全够用且不消耗性能
            if (Pawn.IsHashIntervalTick(30))
            {
                ApplyScale();
            }
        }

        private void ApplyScale()
        {
            if (Pawn?.Drawer?.renderer == null) return;

            float extraScale = parent.Severity * Props.scaleFactor;
            float finalScale = 1f + extraScale;

            // 通过反射直接写入私有字段
            BodyRenderScaleField?.SetValue(Pawn.Drawer.renderer, finalScale);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            // 移除时确保重绘，体型复原
            Pawn.Drawer.renderer.SetAllGraphicsDirty();

            
        }
    }
}
