using RimVore2;
using RimWorld;
using Verse;

namespace PRV2E
{
    public class RollAction_Brainwash : RollAction
    {
        public bool enslave = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enslave, "enslave");
        }

        public override bool TryAction(VoreTrackerRecord record, float rollStrength)
        {
            base.TryAction(record, rollStrength);

            if (record == null || PredatorPawn == null || PreyPawn == null)
            {
                return true;
            }

            // 关键修复：检查 PreyPawn.guest 是否为空，避免后续访问引发空引用
            if (PreyPawn.guest == null)
            {
                return true;
            }

            if (!enslave)
            {
                // 招募模式：降低抵抗值并让目标加入
                if (PreyPawn.guest.Recruitable)
                {
                    PreyPawn.guest.resistance -= rollStrength * 0.05f;
                    if (PreyPawn.guest.resistance <= 0f)
                    {
                        PreyPawn.guest.resistance = 0f;
                        PreyPawn.guest.joinStatus = JoinStatus.JoinAsColonist;
                    }
                }
                else if (Rand.Chance(0.05f))
                {
                    PreyPawn.guest.Recruitable = true;
                }

                // 派系转变
                if (PredatorPawn.Faction != null)
                {
                    PreyPawn.SetFaction(PredatorPawn.Faction);
                }
            }
            else
            {
                // 奴役模式：降低意志值
                if (PreyPawn.guest.will <= 0f)
                {
                    PreyPawn.guest.will = 0f;
                    // 将目标变为奴隶
                    if (PredatorPawn.Faction != null)
                    {
                        PreyPawn.SetFaction(PredatorPawn.Faction);

                        if (ModsConfig.IdeologyActive && PreyPawn.guest != null)
                        {
                            PreyPawn.guest.joinStatus = JoinStatus.JoinAsSlave;
                        }
                    }
                }
                else
                {
                    PreyPawn.guest.will -= rollStrength * 0.05f;
                }
            }

            // 意识形态转换：确保双方 ideo 均有效，避免空引用
            if (ModsConfig.IdeologyActive && PreyPawn.ideo != null && PredatorPawn.ideo != null && PreyPawn.ideo.Ideo != PredatorPawn.ideo.Ideo)
            {
                PreyPawn.ideo.IdeoConversionAttempt(rollStrength * 0.02f, PredatorPawn.ideo.Ideo, true);
            }

            return true;
        }
    }
}
