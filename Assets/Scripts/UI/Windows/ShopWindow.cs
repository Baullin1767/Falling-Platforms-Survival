using System.Collections.Generic;
using TMPro;
using UIModule.Data.ScriptableObjects;
using UIModule.UI.Services;
using UIModule.UI.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace UIModule.UI.Windows
{
    public sealed class ShopWindow : BaseWindow
    {
        [SerializeField] private SkinDatabaseSO skinDatabase;
        [SerializeField] private ShopSkinItemView itemPrefab;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private Image selectedSkinPreview;
        [SerializeField] private TMP_Text selectedSkinName;
        [SerializeField] private TMP_Text emptyStateLabel;

        private readonly List<ShopSkinItemView> spawnedItems = new();
        private SkinService skinService;

        public override void Initialize(WindowsManager windowsManager)
        {
            base.Initialize(windowsManager);
            skinService = new SkinService(skinDatabase, new PlayerPrefsSaveService());
            skinService.Initialize();
            BuildList();
            RefreshSelection();
        }

        public override void Show()
        {
            base.Show();

            skinService ??= new SkinService(skinDatabase, new PlayerPrefsSaveService());
            skinService.Initialize();
            BuildList();
            RefreshSelection();
        }

        private void BuildList()
        {
            if (itemPrefab == null || contentRoot == null || skinService == null)
            {
                return;
            }

            foreach (var item in spawnedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            spawnedItems.Clear();

            var skins = skinService.GetAllSkins();
            for (var i = 0; i < skins.Count; i++)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(skins[i], i, i == skinService.GetCurrentSkinIndex(), HandleSkinSelected);
                spawnedItems.Add(item);
            }
        }

        private void HandleSkinSelected(int index)
        {
            skinService.SetCurrentSkinIndex(index);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (skinService == null)
            {
                return;
            }

            var currentSkin = skinService.GetCurrentSkin();
            var hasSkin = currentSkin != null;

            if (selectedSkinPreview != null)
            {
                selectedSkinPreview.enabled = hasSkin && currentSkin.skinSprite != null;
                selectedSkinPreview.sprite = hasSkin ? currentSkin.skinSprite : null;
            }

            if (selectedSkinName != null)
            {
                selectedSkinName.text = hasSkin ? currentSkin.displayName : "No skins configured";
            }

            if (emptyStateLabel != null)
            {
                emptyStateLabel.gameObject.SetActive(!hasSkin);
                emptyStateLabel.text = "Add skins to SkinDatabaseSO to populate this shop.";
            }

            var currentIndex = skinService.GetCurrentSkinIndex();
            for (var i = 0; i < spawnedItems.Count; i++)
            {
                var item = spawnedItems[i];
                if (item != null)
                {
                    item.SetSelected(i == currentIndex);
                }
            }
        }
    }
}
