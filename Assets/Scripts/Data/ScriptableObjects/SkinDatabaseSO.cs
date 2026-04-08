using System.Collections.Generic;
using UIModule.UI.Shop;
using UnityEngine;

namespace UIModule.Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SkinDatabase", menuName = "UI Module/Skin Database")]
    public sealed class SkinDatabaseSO : ScriptableObject
    {
        public List<SkinData> skins = new();
    }
}
