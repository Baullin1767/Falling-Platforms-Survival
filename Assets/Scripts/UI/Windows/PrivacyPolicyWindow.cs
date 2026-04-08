using TMPro;
using UnityEngine;

namespace UIModule.UI.Windows
{
    public sealed class PrivacyPolicyWindow : BaseWindow
    {
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private TextAsset contentAsset;
        [SerializeField] [TextArea(6, 20)] private string fallbackContent;

        public override void Initialize(WindowsManager windowsManager)
        {
            base.Initialize(windowsManager);
            RefreshContent();
        }

        private void RefreshContent()
        {
            if (contentText == null)
            {
                return;
            }

            contentText.text = contentAsset != null ? contentAsset.text : fallbackContent;
        }
    }
}
