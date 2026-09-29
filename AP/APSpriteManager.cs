using System.IO;
using System.Reflection;
using UnityEngine;

namespace WoLArchipelago
{
    /// <summary>
    /// Sprite manager responsible for loading and caching custom embedded sprite resources.
    /// </summary>
    public static class APSpriteManager
    {
        private const string ApSpriteResourcePath = "WoLArchipelago.UI.Sprites.AP.png";

        private const float PixelsPerUnit = 230f;
        private static readonly Vector2 DefaultPivot = new Vector2(0.5f, 0.5f);

        private static Sprite _apSprite;

        public static Sprite APSprite
        {
            get
            {
                if (_apSprite == null)
                {
                    _apSprite = LoadSpriteFromResource(ApSpriteResourcePath);
                }
                return _apSprite;
            }
        }

        /// <summary>
        /// Reads an embedded PNG image resource from the assembly and converts it into a Unity Sprite
        /// </summary>
        private static Sprite LoadSpriteFromResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Plugin.Log.LogError(string.Format("[AP] Embedded resource not found: {0}", resourceName));
                return null;
            }

            byte[] bytes = new byte[stream.Length];
            int bytesRead = stream.Read(bytes, 0, bytes.Length);

            if (bytesRead == 0)
            {
                Plugin.Log.LogError(string.Format("[AP] Failed to read resource stream: {0}", resourceName));
                return null;
            }

            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                texture.filterMode = FilterMode.Point;
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    DefaultPivot,
                    PixelsPerUnit
                );
            }

            Plugin.Log.LogError(string.Format("[AP] Failed to decode image data into Texture2D for resource: {0}", resourceName));

            return null;
        }
    }
}