using System.Collections.Generic;
using UnityEngine;

namespace UIModule.UI.Windows
{
    public sealed class WindowsManager : MonoBehaviour
    {
        [SerializeField] private GameObject dimOverlay;
        [SerializeField] private BaseWindow privacyPolicyWindow;
        [SerializeField] private BaseWindow aboutWindow;
        [SerializeField] private BaseWindow leaderboardWindow;
        [SerializeField] private BaseWindow shopWindow;

        private readonly Dictionary<WindowType, BaseWindow> windowsByType = new();
        private BaseWindow currentWindow;
        private WindowType currentWindowType = WindowType.None;

        private void Awake()
        {
            RebuildLookup();
            CloseAllWindows();
        }

        public void OpenWindow(WindowType type)
        {
            if (type == WindowType.None)
            {
                CloseCurrentWindow();
                return;
            }

            if (!windowsByType.TryGetValue(type, out var requestedWindow) || requestedWindow == null)
            {
                Debug.LogWarning($"Window '{type}' is not configured on {nameof(WindowsManager)}.");
                return;
            }

            if (currentWindowType == type && currentWindow != null)
            {
                requestedWindow.Show();
                SetOverlayVisible(true);
                return;
            }

            CloseCurrentWindow();

            currentWindow = requestedWindow;
            currentWindowType = type;
            SetOverlayVisible(true);
            currentWindow.Show();
        }

        public void CloseCurrentWindow()
        {
            if (currentWindow != null)
            {
                currentWindow.Hide();
            }

            currentWindow = null;
            currentWindowType = WindowType.None;
            SetOverlayVisible(false);
        }

        public void CloseAllWindows()
        {
            foreach (var window in windowsByType.Values)
            {
                if (window == null)
                {
                    continue;
                }

                window.Initialize(this);
                window.Hide();
            }

            currentWindow = null;
            currentWindowType = WindowType.None;
            SetOverlayVisible(false);
        }

        public bool IsAnyWindowOpen()
        {
            return currentWindow != null;
        }

        private void RebuildLookup()
        {
            windowsByType.Clear();
            Register(WindowType.PrivacyPolicy, privacyPolicyWindow);
            Register(WindowType.About, aboutWindow);
            Register(WindowType.Leaderboard, leaderboardWindow);
            Register(WindowType.Shop, shopWindow);
        }

        private void Register(WindowType type, BaseWindow window)
        {
            if (window == null)
            {
                return;
            }

            window.Initialize(this);
            windowsByType[type] = window;
        }

        private void SetOverlayVisible(bool visible)
        {
            if (dimOverlay != null)
            {
                dimOverlay.SetActive(visible);
            }
        }
    }
}
