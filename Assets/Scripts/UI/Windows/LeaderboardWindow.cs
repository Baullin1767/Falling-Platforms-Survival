using System.Collections.Generic;
using UIModule.UI.Leaderboard;
using UnityEngine;

namespace UIModule.UI.Windows
{
    public sealed class LeaderboardWindow : BaseWindow
    {
        [SerializeField] private LeaderboardItemView itemPrefab;
        [SerializeField] private Transform contentRoot;

        private readonly List<LeaderboardEntry> mockEntries = new()
        {
            new() { rank = 1, playerName = "Nova", score = 520 },
            new() { rank = 2, playerName = "Alex", score = 480 },
            new() { rank = 3, playerName = "Luna", score = 450 },
            new() { rank = 4, playerName = "Max", score = 430 },
            new() { rank = 5, playerName = "Zoe", score = 395 },
            new() { rank = 6, playerName = "Ethan", score = 370 },
            new() { rank = 7, playerName = "Mia", score = 340 },
            new() { rank = 8, playerName = "Leo", score = 300 },
            new() { rank = 9, playerName = "Ruby", score = 260 },
            new() { rank = 10, playerName = "Kai", score = 220 }
        };

        public override void Initialize(WindowsManager windowsManager)
        {
            base.Initialize(windowsManager);
            RebuildList();
        }

        public override void Show()
        {
            base.Show();
            RebuildList();
        }

        private void RebuildList()
        {
            if (itemPrefab == null || contentRoot == null)
            {
                return;
            }

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
            }

            foreach (var entry in mockEntries)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(entry);
            }
        }
    }
}
