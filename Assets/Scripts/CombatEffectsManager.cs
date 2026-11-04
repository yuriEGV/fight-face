using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Manejador de efectos de combate estilo Mugen / Cómic:
    /// - Textos de impacto flotantes ("¡POW!", "¡BAM!", "¡OUCH!").
    /// - Hit-Stop (micro-pausa de impacto).
    /// - Sacudida de cámara (Camera Shake).
    /// </summary>
    public class CombatEffectsManager : MonoBehaviour
    {
        public static CombatEffectsManager Instance { get; private set; }

        private readonly string[] comicWords = {
            "¡POW!", "¡ZAS!", "¡TOMA!", "¡BOING!", "¡PLAF!", "¡ÑOQUI!", "¡KAPOW!",
            "¡AUCH!", "¡EN LA JETA!", "¡CUACK!", "¡PUM!", "¡OUCH!", "¡CRUNCH!",
            "¡ZAPATAZO!", "¡BANG!", "¡CHAO PESCAO!", "¡BOOOM!", "¡PLOP!",
            "¡DALE!", "¡SOBA!", "¡SÁCUDETE!", "¡CATAPLUM!", "¡CHISPOTEÓ!", "¡PAM!"
        };

        private readonly Color[] comicColors = {
            new Color(1f, 0.88f, 0f),      // Amarillo Cómic
            new Color(1f, 0.25f, 0.15f),   // Rojo Fuego
            new Color(0.1f, 0.9f, 1f),     // Cian Neón
            new Color(1f, 0.1f, 0.6f),     // Rosa Chicle
            new Color(0.35f, 1f, 0.2f),    // Verde Lima
            new Color(1f, 0.55f, 0f)       // Naranja Brillante
        };

        private Camera mainCam;
        private Vector3 camInitialPos;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            mainCam = Camera.main;
            if (mainCam != null)
            {
                camInitialPos = mainCam.transform.position;
            }
        }

        public void SpawnHitEffect(Vector2 position, bool isHeavy)
        {
            // 1. Texto cómic flotante con sombra y colores vivos
            string word = comicWords[Random.Range(0, comicWords.Length)];
            Color col = comicColors[Random.Range(0, comicColors.Length)];
            CreateComicTextEffect(position, word, isHeavy, col);

            // 2. Micro hit-stop (pausa arcade)
            StartCoroutine(HitStopRoutine(isHeavy ? 0.06f : 0.035f));

            // 3. Si es golpe fuerte, desatar explosión de confeti gracioso
            if (isHeavy)
            {
                SpawnConfettiBurst(position, 14);
            }
        }

        public void SpawnCustomHitEffect(Vector2 position, string word, Color color, bool isHeavy)
        {
            CreateComicTextEffect(position, word, isHeavy, color);
            StartCoroutine(HitStopRoutine(isHeavy ? 0.06f : 0.035f));
            if (isHeavy)
            {
                SpawnConfettiBurst(position, 14);
            }
        }

        public void SpawnBlockEffect(Vector2 position)
        {
            CreateComicTextEffect(position, "¡BLOCK!", false, new Color(0.2f, 0.75f, 1f));
            StartCoroutine(HitStopRoutine(0.025f));
        }

        public void SpawnStunStars(Transform targetHead, float duration = 2.0f)
        {
            if (targetHead == null) return;
            StartCoroutine(StunStarsRoutine(targetHead, duration));
        }

        private IEnumerator StunStarsRoutine(Transform head, float duration)
        {
            GameObject starsObj = new GameObject("StunStars");
            starsObj.transform.SetParent(head, false);
            starsObj.transform.localPosition = new Vector3(0, 0.65f, -0.5f);

            var tm = starsObj.AddComponent<TextMesh>();
            tm.text = "★  💫  ★";
            tm.fontSize = 28;
            tm.characterSize = 1f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.95f, 0.2f);
            starsObj.transform.localScale = Vector3.one * 0.025f;

            float elapsed = 0f;
            while (elapsed < duration && head != null)
            {
                elapsed += Time.deltaTime;
                float angle = elapsed * 360f;
                starsObj.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(angle * Mathf.Deg2Rad) * 20f);
                starsObj.transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.25f, 0.65f + Mathf.Sin(angle * Mathf.Deg2Rad * 2f) * 0.08f, -0.5f);
                yield return null;
            }

            Destroy(starsObj);
        }

        private void CreateComicTextEffect(Vector2 pos, string word, bool isHeavy, Color? customColor = null)
        {
            GameObject containerObj = new GameObject("HitPopup_" + word);
            containerObj.transform.position = new Vector3(pos.x, pos.y + 0.35f, -1.5f);

            // Sombra cómic negra gruesa detrás
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(containerObj.transform, false);
            shadowObj.transform.localPosition = new Vector3(0.045f, -0.045f, 0.05f);
            var shadowMesh = shadowObj.AddComponent<TextMesh>();
            shadowMesh.text = word;
            shadowMesh.fontSize = isHeavy ? 52 : 38;
            shadowMesh.alignment = TextAlignment.Center;
            shadowMesh.anchor = TextAnchor.MiddleCenter;
            shadowMesh.color = new Color(0f, 0f, 0f, 0.95f);

            // Texto frontal brillante
            GameObject frontObj = new GameObject("FrontText");
            frontObj.transform.SetParent(containerObj.transform, false);
            frontObj.transform.localPosition = Vector3.zero;
            var textMesh = frontObj.AddComponent<TextMesh>();
            textMesh.text = word;
            textMesh.fontSize = isHeavy ? 52 : 38;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = customColor.HasValue ? customColor.Value : (isHeavy ? new Color(1f, 0.25f, 0.15f) : new Color(1f, 0.92f, 0.1f));

            // Escala y ángulo cómico
            float initialScale = isHeavy ? 0.026f : 0.019f;
            containerObj.transform.localScale = Vector3.one * initialScale;
            containerObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-18f, 18f));

            StartCoroutine(AnimateComicText(containerObj, textMesh, shadowMesh));
        }

        private IEnumerator AnimateComicText(GameObject obj, TextMesh front, TextMesh shadow)
        {
            float duration = 0.58f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Vector3 targetPos = startPos + new Vector3(Random.Range(-0.25f, 0.25f), 0.85f, 0);
            Vector3 baseScale = obj.transform.localScale;
            Color frontStartColor = front.color;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                obj.transform.position = Vector3.Lerp(startPos, targetPos, t);

                // Rebote elástico inicial estilo cómic (Pop-in y flotación)
                float scaleMod = (t < 0.2f) ? Mathf.Lerp(0.3f, 1.45f, t / 0.2f) : Mathf.Lerp(1.45f, 0.85f, (t - 0.2f) / 0.8f);
                obj.transform.localScale = baseScale * scaleMod;

                // Desvanecimiento suave al final
                float alpha = (t < 0.65f) ? 1f : Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                Color fc = frontStartColor;
                fc.a = alpha;
                front.color = fc;

                Color sc = Color.black;
                sc.a = alpha * 0.9f;
                shadow.color = sc;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(obj);
        }

        /// <summary>
        /// Explosión cómica de serpentinas y confeti de colores
        /// </summary>
        public void SpawnConfettiBurst(Vector2 position, int count = 12)
        {
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(ConfettiParticleRoutine(position));
            }
        }

        private IEnumerator ConfettiParticleRoutine(Vector2 origin)
        {
            GameObject p = new GameObject("ConfettiBit");
            p.transform.position = new Vector3(origin.x, origin.y, -1.2f);
            var sr = p.AddComponent<SpriteRenderer>();

            Texture2D tex = new Texture2D(6, 12);
            Color[] cols = new Color[72];
            Color c = comicColors[Random.Range(0, comicColors.Length)];
            for (int j = 0; j < cols.Length; j++) cols[j] = c;
            tex.SetPixels(cols);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 6, 12), new Vector2(0.5f, 0.5f), 100f);
            sr.sortingOrder = 25;

            Vector2 velocity = new Vector2(Random.Range(-2.8f, 2.8f), Random.Range(2.5f, 5.2f));
            float rotSpeed = Random.Range(-480f, 480f);
            float lifetime = Random.Range(0.65f, 1.1f);
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                velocity.y -= 9.8f * Time.deltaTime; // Gravedad cómica
                p.transform.position += (Vector3)(velocity * Time.deltaTime);
                p.transform.Rotate(0, 0, rotSpeed * Time.deltaTime);

                if (elapsed > lifetime * 0.7f)
                {
                    float a = Mathf.Lerp(1f, 0f, (elapsed - lifetime * 0.7f) / (lifetime * 0.3f));
                    sr.color = new Color(c.r, c.g, c.b, a);
                }
                yield return null;
            }

            Destroy(p);
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }
    }
}
