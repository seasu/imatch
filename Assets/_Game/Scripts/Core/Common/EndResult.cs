namespace Game.Core
{
    public class EndResult
    {
        public LevelResult Result;
        public int MovesUsed;
        public int? BossHpRemaining;   // null for Normal levels
        public int CoinsGained;
        public int BoostersUsed;
        public float DurationSec;
    }
}
