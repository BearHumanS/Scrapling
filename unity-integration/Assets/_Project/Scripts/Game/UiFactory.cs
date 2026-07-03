using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceDungeon.Game
{
    /// <summary>
    /// UGUI를 코드로 생성하는 헬퍼. 프리팹/인스펙터 배선 없이 씬을 구성한다 (Phase 1 스캐폴드용).
    /// 실제 아트가 들어오는 Phase 2에서 프리팹 기반으로 교체한다.
    /// </summary>
    public static class UiFactory
    {
        public static Font DefaultFont =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        private static Font _font;

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(null);
            }
            return canvas;
        }

        /// <summary>정규화 앵커로 배치되는 빈 RectTransform.</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
                                 Vector2 anchorMin, Vector2 anchorMax, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button ActionButton(Transform parent, string name, string label, int fontSize,
                                          Vector2 anchorMin, Vector2 anchorMax,
                                          Color bg, UnityEngine.Events.UnityAction onClick)
        {
            var img = Panel(parent, name, anchorMin, anchorMax, bg);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            Label(img.transform, "Label", label, fontSize, Color.white, Vector2.zero, Vector2.one);
            return btn;
        }

        /// <summary>anchorMax.x 조절로 채워지는 게이지 바. 반환된 fill의 SetRatio로 갱신.</summary>
        public static (Image bg, Image fill) Bar(Transform parent, string name,
                                                 Vector2 anchorMin, Vector2 anchorMax,
                                                 Color bgColor, Color fillColor)
        {
            var bg = Panel(parent, name, anchorMin, anchorMax, bgColor);
            var fill = Panel(bg.transform, "Fill", Vector2.zero, Vector2.one, fillColor);
            return (bg, fill);
        }

        public static void SetRatio(Image fill, float ratio)
        {
            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }
    }
}
