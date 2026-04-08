using TMPro;
using UnityEngine;

namespace UIModule.UI.Leaderboard
{
    public sealed class LeaderboardItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text scoreText;

        public void Bind(LeaderboardEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (rankText != null)
            {
                rankText.text = entry.rank.ToString();
            }

            if (playerNameText != null)
            {
                playerNameText.text = entry.playerName;
            }

            if (scoreText != null)
            {
                scoreText.text = entry.score.ToString();
            }
        }
    }
}
