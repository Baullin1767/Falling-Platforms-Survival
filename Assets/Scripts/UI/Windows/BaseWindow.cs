using UnityEngine;
using UnityEngine.UI;

namespace UIModule.UI.Windows
{
    public abstract class BaseWindow : MonoBehaviour
    {
        [SerializeField] private Button closeButton;

        protected WindowsManager WindowsManager { get; private set; }

        public virtual void Initialize(WindowsManager windowsManager)
        {
            WindowsManager = windowsManager;
            ConfigureCloseButton();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnClosePressed()
        {
            if (WindowsManager != null)
            {
                WindowsManager.CloseCurrentWindow();
                return;
            }

            Hide();
        }

        protected virtual void OnDestroy()
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(OnClosePressed);
        }

        private void ConfigureCloseButton()
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveListener(OnClosePressed);
            closeButton.onClick.AddListener(OnClosePressed);
        }
    }
}
