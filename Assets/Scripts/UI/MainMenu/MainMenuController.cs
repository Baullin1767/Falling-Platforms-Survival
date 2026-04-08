using System;
using UIModule.UI.Windows;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UIModule.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private WindowsManager windowsManager;
        [SerializeField] private string gameplaySceneName = "GameScene";
        [Header("Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button aboutButton;
        [SerializeField] private Button privacyPolicyButton;

        private void Start()
        {
            startGameButton.onClick.AddListener(OnStartPressed);
            shopButton.onClick.AddListener(OnShopPressed);
            leaderboardButton.onClick.AddListener(OnLeaderboardPressed);
            aboutButton.onClick.AddListener(OnAboutPressed);
            privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyPressed);
        }

        private void OnStartPressed()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogWarning("Gameplay scene name is not configured.");
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnPrivacyPolicyPressed()
        {
            windowsManager?.OpenWindow(WindowType.PrivacyPolicy);
        }

        private void OnAboutPressed()
        {
            windowsManager?.OpenWindow(WindowType.About);
        }

        private void OnLeaderboardPressed()
        {
            windowsManager?.OpenWindow(WindowType.Leaderboard);
        }

        private void OnShopPressed()
        {
            windowsManager?.OpenWindow(WindowType.Shop);
        }
    }
}
