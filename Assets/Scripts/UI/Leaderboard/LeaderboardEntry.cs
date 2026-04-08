using System;

namespace UIModule.UI.Leaderboard
{
    [Serializable]
    public sealed class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int score;
    }
}
