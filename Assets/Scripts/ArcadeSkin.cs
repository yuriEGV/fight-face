using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Estilo arcade Street Fighter II para toda la interfaz:
    /// botones biselados con degradado y borde oscuro, marcos dorados dobles en los paneles,
    /// textos en mayúsculas sin emojis (la fuente integrada no los dibuja y salen como cuadrados),
    /// y animación de resalte / pulsación en cada botón.
    /// Se aplica sobre la UI ya construida: no cambia ninguna lógica.
    /// </summary>
    public static class ArcadeSkin
    {
        private static Sprite s_Bevel, s_Frame, s_Backdrop;

        private static readonly string[] FramedPanels =
        {
            "RosterGrid", "Card_Player1", "Card_Player2", "Panel_StageSelector", "HeaderPanel", "BottomControlsBar",
            "Modal_Winner", "Modal_FaceCustomizer", "ExpressionGuideBox", "CenterBanner"
        };

        public static void Apply(Transform root)
        {
            if (root == null) return;
            EnsureSprites();

            // 1) Fondo de la pantalla de selección: degradado azul noche -> carmesí
            Transform select = FindDeep(root, "Panel_CharacterSelect");
            if (select != null)
            {
                var bg = select.GetComponent<Image>();
                if (bg != null)
                {
                    bg.sprite = s_Backdrop;
                    bg.type = Image.Type.Simple;
                    bg.color = Color.white;
                }
            }

            // 2) Paneles con marco dorado
            foreach (string n in FramedPanels)
            {
                Transform t = FindDeep(root, n);
                if (t == null) continue;
                var img = t.GetComponent<Image>();
                if (img != null && n != "HeaderPanel")
                {
                    img.sprite = s_Bevel;
                    img.type = Image.Type.Sliced;
                }
                AddFrame(t);
            }

            // Cabecera roja tipo cartel de arcade
            Transform header = FindDeep(root, "HeaderPanel");
            if (header != null)
            {
                var hImg = header.GetComponent<Image>();
                if (hImg == null) hImg = header.gameObject.AddComponent<Image>();
                hImg.sprite = s_Bevel;
                hImg.type = Image.Type.Sliced;
                hImg.color = new Color(0.55f, 0.04f, 0.10f, 0.96f);
                hImg.raycastTarget = false;
                header.SetAsFirstSibling();
                Transform title = FindDeep(header, "ArcadeTitle");
                if (title != null)
                {
                    var tt = title.GetComponent<Text>();
                    if (tt != null) tt.fontSize = 32;
                }
            }

            // 3) Botones
            foreach (Button b in root.GetComponentsInChildren<Button>(true))
            {
                StyleButton(b);
            }

            // 4) Textos: sin emojis, botones en mayúsculas, contorno para legibilidad
            foreach (Text t in root.GetComponentsInChildren<Text>(true))
            {
                string clean = Clean(t.text);
                bool inButton = t.GetComponentInParent<Button>() != null;
                t.text = inButton ? clean.ToUpperInvariant() : clean;
                if (inButton)
                {
                    t.raycastTarget = false; // Garantiza que el clic lo recibe el Image del Button
                    if (t.GetComponent<Outline>() == null)
                    {
                        var o = t.gameObject.AddComponent<Outline>();
                        o.effectColor = new Color(0, 0, 0, 0.75f);
                        o.effectDistance = new Vector2(1.2f, -1.2f);
                    }
                }
            }
        }

        private static void StyleButton(Button b)
        {
            var img = b.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = s_Bevel;
                img.type = Image.Type.Sliced;
                b.targetGraphic = img;
            }

            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.22f, 1.22f, 1.22f, 1f);
            cb.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            b.colors = cb;

            var nav = b.navigation;
            nav.mode = Navigation.Mode.None; // evita que Espacio/Enter "pulse" el último botón clicado
            b.navigation = nav;

            if (b.GetComponent<ArcadeButtonFX>() == null) b.gameObject.AddComponent<ArcadeButtonFX>();
        }

        private static void AddFrame(Transform panel)
        {
            if (panel.Find("ArcadeFrame") != null) return;
            var go = new GameObject("ArcadeFrame", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-3, -3);
            rt.offsetMax = new Vector2(3, 3);
            var img = go.AddComponent<Image>();
            img.sprite = s_Frame;
            img.type = Image.Type.Sliced;
            img.fillCenter = false;
            img.raycastTarget = false;
        }

        // ---------- Utilidades ----------
        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsSurrogate(c)) continue;                 // emojis (🥊 🏆 📸 🔄 ...)
                if (c >= 0x2600 && c <= 0x27BF) continue;          // símbolos varios (⚔ ★ ❤ ✨ ...)
                if (c == 0xFE0F || c == 0x200D) continue;          // selectores de variación
                sb.Append(c);
            }
            string r = sb.ToString();
            while (r.Contains("  ")) r = r.Replace("  ", " ");
            r = r.Replace("\n ", "\n").Replace(" \n", "\n");
            return r.Trim();
        }

        // ---------- Sprites procedurales ----------
        private static void EnsureSprites()
        {
            if (s_Bevel != null) return;
            s_Bevel = MakeRounded(48, 10f, 14f, false);
            s_Frame = MakeRounded(48, 10f, 14f, true);
            s_Backdrop = MakeBackdrop();
        }

        private static Sprite MakeRounded(int n, float radius, float border, bool frameOnly)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float half = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius; // <0 dentro
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }

                    float t = (float)y / (n - 1); // 0 abajo, 1 arriba
                    Color c;
                    if (frameOnly)
                    {
                        // Marco dorado doble: oro exterior, línea oscura, oro interior fino
                        if (d > -2.2f) c = Color.Lerp(new Color(1f, 0.88f, 0.28f), new Color(0.85f, 0.55f, 0.08f), 1f - t);
                        else if (d > -3.4f) c = new Color(0.12f, 0.05f, 0.02f);
                        else if (d > -4.6f) c = new Color(0.75f, 0.45f, 0.08f);
                        else { tex.SetPixel(x, y, Color.clear); continue; }
                    }
                    else
                    {
                        float shade = Mathf.Lerp(0.70f, 1.0f, Mathf.SmoothStep(0f, 1f, t));
                        c = new Color(shade, shade, shade, 1f);
                        if (d > -2.2f) c = new Color(0.05f, 0.05f, 0.09f);                  // contorno oscuro
                        else if (d > -3.8f) c = t > 0.5f ? Color.white : new Color(0.55f, 0.55f, 0.55f); // bisel luz/sombra
                    }
                    c.a = a;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private static Sprite MakeBackdrop()
        {
            int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color top = new Color(0.02f, 0.04f, 0.16f);
            Color mid = new Color(0.10f, 0.06f, 0.28f);
            Color bot = new Color(0.36f, 0.05f, 0.14f);
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = t > 0.5f ? Color.Lerp(mid, top, (t - 0.5f) * 2f) : Color.Lerp(bot, mid, t * 2f);
                for (int x = 0; x < w; x++)
                {
                    float vx = (x / (float)(w - 1) - 0.5f) * 2f;
                    float vig = 1f - Mathf.Clamp01(vx * vx * 0.55f);
                    tex.SetPixel(x, y, new Color(c.r * vig, c.g * vig, c.b * vig, 1f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    /// <summary>Resalte arcade: el botón crece suavemente al pasar el cursor sin interferir con eventos de clic.</summary>
    public class ArcadeButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool hover;
        private Vector3 baseScale = Vector3.one;

        private void Awake() { baseScale = transform.localScale; }
        private void OnDisable() { transform.localScale = baseScale; hover = false; }

        public void OnPointerEnter(PointerEventData e) { hover = true; }
        public void OnPointerExit(PointerEventData e) { hover = false; }

        private void Update()
        {
            float target = hover ? 1.05f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
        }
    }
}
