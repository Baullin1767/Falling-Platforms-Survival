using UnityEngine;

namespace UIModule.UI.Services
{
    public sealed class PlayerPrefsSaveService : ISaveService
    {
        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
