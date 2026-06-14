using UnityEngine;
using UnityEngine.UI;

namespace ColorPool
{
    /// <summary>
    /// Minimal portrait HUD built at runtime: a painted-% progress bar, a shot counter, and a
    /// "complete" flash. Reads PaintSurface/GameManager each frame; no scene wiring required.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        Image fill;
        Text pctText;
        Text shotsText;
        Text winText;

        void Start() { Build(); }

        void Build()
        {
            var canvasGO = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var barBg = NewRect("BarBg", canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(840f, 40f));
            AddImage(barBg, new Color(0.22f, 0.19f, 0.34f, 0.95f));

            var fillRect = NewRect("BarFill", barBg, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 40f));
            fill = AddImage(fillRect, new Color(0.36f, 0.78f, 0.5f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            var pctRect = NewRect("Pct", barBg, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 40f));
            pctText = AddText(pctRect, font, 24, TextAnchor.MiddleCenter, Color.white);

            var shotsRect = NewRect("Shots", canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(840f, 34f));
            shotsText = AddText(shotsRect, font, 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));

            var winRect = NewRect("Win", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(900f, 80f));
            winText = AddText(winRect, font, 52, TextAnchor.MiddleCenter, Color.white);
            winText.gameObject.SetActive(false);
        }

        void Update()
        {
            float p = PaintSurface.Instance != null ? PaintSurface.Instance.Progress : 0f;
            if (fill != null) fill.fillAmount = p;
            if (pctText != null) pctText.text = Mathf.RoundToInt(p * 100f) + "%";
            if (shotsText != null && GameManager.Instance != null) shotsText.text = "Shots: " + GameManager.Instance.Shots;
            if (winText != null && GameManager.Instance != null)
                winText.gameObject.SetActive(GameManager.Instance.State == GameState.Win);
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Image AddImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            return img;
        }

        static Text AddText(RectTransform rt, Font font, int size, TextAnchor anchor, Color c)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = c;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }
}
