using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective
{
    public static class DetectiveUIWidgets
    {
        private static TMP_FontAsset fontAsset;
        private static Sprite circleSprite;

        public static TMP_FontAsset GetFont()
        {
            if (fontAsset != null)
            {
                return fontAsset;
            }

            string[] candidates =
            {
                @"C:\Windows\Fonts\Deng.ttf",
                @"C:\Windows\Fonts\simhei.ttf",
                @"C:\Windows\Fonts\simkai.ttf",
                @"C:\Windows\Fonts\simfang.ttf"
            };

            foreach (string path in candidates)
            {
                if (!System.IO.File.Exists(path))
                {
                    continue;
                }

                try
                {
                    var font = new Font(path);
                    if (font != null)
                    {
                        fontAsset = TMP_FontAsset.CreateFontAsset(font);
                        fontAsset.name = "DetectiveOSFont";
                        break;
                    }
                }
                catch
                {
                }
            }

            if (fontAsset == null)
            {
                fontAsset = TMP_Settings.defaultFontAsset;
            }

            return fontAsset;
        }

        public static Sprite GetCircleSprite()
        {
            if (circleSprite != null)
            {
                return circleSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "DetectiveCircle";
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleSprite;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = GetFont();
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
