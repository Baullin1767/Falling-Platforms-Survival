using UnityEngine;

namespace FallingPlatformsSurvival
{
    public static class PlaceholderSpriteLibrary
    {
        private static Sprite squareSprite;

        public static Sprite SquareSprite
        {
            get
            {
                if (squareSprite == null)
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    var pixels = new[]
                    {
                        Color.white, Color.white,
                        Color.white, Color.white
                    };

                    texture.SetPixels(pixels);
                    texture.Apply();

                    squareSprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        16f);
                    squareSprite.name = "PlaceholderSquare";
                    squareSprite.hideFlags = HideFlags.HideAndDontSave;
                }

                return squareSprite;
            }
        }
    }
}
