using System.IO;
using System.Reflection;
using UnityEngine;

namespace WoLArchipelago
{
    public static class APSpriteManager
    {
        private static Sprite apSprite;

        public static Sprite APSprite
        {
            get
            {
                if (apSprite == null)
                {
                    apSprite = LoadSpriteFromResource("WoLArchipelago.UI.Sprites.AP.png");
                }
                return apSprite;
            }
        }

        private static Sprite LoadSpriteFromResource(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Plugin.Log.LogError($"[AP] Resource not found : {resourceName}");
                    return null;
                }

                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);

                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Point;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 230f);
                }
            }

            return null;
        }
    }
}