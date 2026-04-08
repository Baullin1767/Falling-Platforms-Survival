namespace UIModule.UI.Services
{
    public interface ISaveService
    {
        bool HasKey(string key);

        int GetInt(string key, int defaultValue = 0);

        void SetInt(string key, int value);

        void Save();
    }
}
