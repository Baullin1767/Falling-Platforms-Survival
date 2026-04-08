using System.Collections.Generic;
using UIModule.Data.ScriptableObjects;
using UIModule.UI.Services;

namespace UIModule.UI.Shop
{
    public sealed class SkinService
    {
        public const string SelectedSkinIndexKey = "selected_skin_index";

        private readonly SkinDatabaseSO database;
        private readonly ISaveService saveService;

        private int currentSkinIndex = -1;

        public SkinService(SkinDatabaseSO skinDatabase, ISaveService persistence)
        {
            database = skinDatabase;
            saveService = persistence;
        }

        public void Initialize()
        {
            currentSkinIndex = ResolveValidatedIndex(saveService.GetInt(SelectedSkinIndexKey, 0));

            if (HasAnySkins())
            {
                SaveCurrentIndex();
            }
        }

        public int GetCurrentSkinIndex()
        {
            return currentSkinIndex;
        }

        public void SetCurrentSkinIndex(int index)
        {
            currentSkinIndex = ResolveValidatedIndex(index);

            if (HasAnySkins())
            {
                SaveCurrentIndex();
            }
        }

        public SkinData GetCurrentSkin()
        {
            var skins = GetAllSkins();

            if (currentSkinIndex < 0 || currentSkinIndex >= skins.Count)
            {
                return null;
            }

            return skins[currentSkinIndex];
        }

        public IReadOnlyList<SkinData> GetAllSkins()
        {
            return database != null && database.skins != null
                ? database.skins
                : System.Array.Empty<SkinData>();
        }

        private bool HasAnySkins()
        {
            return GetAllSkins().Count > 0;
        }

        private int ResolveValidatedIndex(int index)
        {
            var skins = GetAllSkins();

            if (skins.Count == 0)
            {
                return -1;
            }

            if (index < 0 || index >= skins.Count)
            {
                return 0;
            }

            return index;
        }

        private void SaveCurrentIndex()
        {
            saveService.SetInt(SelectedSkinIndexKey, currentSkinIndex);
            saveService.Save();
        }
    }
}
