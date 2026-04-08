using System;
using UnityEngine;

namespace UIModule.UI.Shop
{
    [Serializable]
    public sealed class SkinData
    {
        public string id;
        public string displayName;
        public Sprite skinSprite;
        public bool unlocked = true;
    }
}
