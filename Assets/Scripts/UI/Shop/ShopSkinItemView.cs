using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.UI.Shop
{
    public sealed class ShopSkinItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image skinPreviewImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectedHighlight;

        private int index;
        private Action<int> onSelected;

        public void Bind(SkinData data, int itemIndex, bool isSelected, Action<int> selectedCallback)
        {
            index = itemIndex;
            onSelected = selectedCallback;

            if (skinPreviewImage != null)
            {
                skinPreviewImage.sprite = data != null ? data.skinSprite : null;
                skinPreviewImage.enabled = data != null && data.skinSprite != null;
            }

            if (nameText != null)
            {
                nameText.text = data != null ? data.displayName : "Unknown";
            }

            SetSelected(isSelected);

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(isSelected);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        private void HandleClicked()
        {
            onSelected?.Invoke(index);
        }
    }
}
