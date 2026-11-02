using Verse;

namespace PRV2E
{
    /// <summary>
    /// 定期检查所有地图上被标记为“体内牢房”或“转化原料”的囚犯，并为其配对执行者。
    /// 注意：类名不能再改，存档中的 Game.component 是以类型名保存的。
    /// </summary>
    public class GameComponent_InBodyCell : GameComponent
    {
        private const int CheckIntervalTicks = 250;
        private int nextCheckTick;

        public GameComponent_InBodyCell(Game game) { }

        public override void GameComponentTick()
        {
            if(Find.TickManager == null)
            {
                return;
            }
            int currentTick = Find.TickManager.TicksGame;
            if(currentTick < nextCheckTick)
            {
                return;
            }
            nextCheckTick = currentTick + CheckIntervalTicks;

            foreach(Map map in Find.Maps)
            {
                PrisonerVoreUtility.ProcessMap(map);
            }
        }
    }
}
