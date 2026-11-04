using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Remates y K.O. cómicos al derrotar a un oponente:
    /// - 🥧 El Gran Pastelazo de Merengue: Un pastel de crema vuela directo a la cara del derrotado con ¡SPLAT! y merengue volando.
    /// - 🔨 El Yunque de Caricatura (100 Tons): Cae con silbido desde el cielo, aplastando cómicamente al perdedor con pajaritos y estrellas mareadas girando.
    /// - Sonidos cómicos y confeti de victoria.
    /// </summary>
    public class ComicFinisherManager : MonoBehaviour
    {
        public static ComicFinisherManager Instance { get; private set; }

        public enum FinisherType
        {
            PieInTheFace = 0,
            CartoonAnvil = 1
        }

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
        }

        /// <summary>
        /// Lanza el remate cómico en la posición del luchador derrotado.
        /// </summary>
        public void TriggerComicFinisher(Vector3 victimPosition, int attackerDir = 1)
        {
            FinisherType type = (Random.value > 0.5f) ? FinisherType.PieInTheFace : FinisherType.CartoonAnvil;
            if (type == FinisherType.PieInTheFace)
            {
                StartCoroutine(PieFinisherRoutine(victimPosition, attackerDir));
            }
            else
            {
                StartCoroutine(AnvilFinisherRoutine(victimPosition));
            }
        }

        private IEnumerator PieFinisherRoutine(Vector3 victimPos, int attackerDir)
        {
            // Esperar que la animación de caída comience
            yield return new WaitForSeconds(0.20f);

            GameObject pieObj = new GameObject("Comic_FlyingPie");
            var sr = pieObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePieSprite();
            sr.sortingOrder = 30;

            Vector3 startPos = victimPos + new Vector3(-attackerDir * 4.5f, 1.2f, 0);
            Vector3 targetPos = victimPos + new Vector3(0, 0.85f, 0);
            pieObj.transform.position = startPos;
            pieObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            float t = 0f;
            float duration = 0.35f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float progress = t / duration;
                Vector3 current = Vector3.Lerp(startPos, targetPos, progress);
                // Arco parábola
                current.y += Mathf.Sin(progress * Mathf.PI) * 0.6f;
                pieObj.transform.position = current;
                pieObj.transform.Rotate(0, 0, -attackerDir * 720f * Time.deltaTime);
                yield return null;
            }

            // ¡¡¡SPLAT!!!
            pieObj.transform.position = targetPos;
            pieObj.transform.rotation = Quaternion.identity;
            sr.sprite = CreatePieSplatSprite();
            pieObj.transform.localScale = new Vector3(2.2f, 2.0f, 1f);

            // Sonido cómico de pastelazo
            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.PlayHitHeavy();
            }

            if (FightImpactManager.Instance != null)
            {
                FightImpactManager.Instance.ImpactoPesado(0.08f, 0.035f);
            }

            // Partículas de crema volando
            SpawnCreamSplashes(targetPos);

            // Estrellitas mareadas sobre la cabeza
            SpawnDizzyStars(targetPos + new Vector3(0, 0.7f, 0));

            yield return new WaitForSeconds(3.5f);
            Destroy(pieObj);
        }

        private IEnumerator AnvilFinisherRoutine(Vector3 victimPos)
        {
            yield return new WaitForSeconds(0.25f);

            GameObject anvilObj = new GameObject("Comic_Anvil");
            var sr = anvilObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateAnvilSprite();
            sr.sortingOrder = 30;

            Vector3 startPos = victimPos + new Vector3(0, 6.0f, 0);
            Vector3 targetPos = victimPos + new Vector3(0, 0.5f, 0);
            anvilObj.transform.position = startPos;
            anvilObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

            float t = 0f;
            float duration = 0.38f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = t / duration;
                // Aceleración de caída libre
                float curved = p * p;
                anvilObj.transform.position = Vector3.Lerp(startPos, targetPos, curved);
                yield return null;
            }

            anvilObj.transform.position = targetPos;

            // Sonido metálico estruendoso cómico
            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.PlayHitHeavy();
            }

            if (FightImpactManager.Instance != null)
            {
                FightImpactManager.Instance.ImpactoPesado(0.12f, 0.06f);
            }

            SpawnDizzyStars(targetPos + new Vector3(0, 1.1f, 0));

            yield return new WaitForSeconds(3.5f);
            Destroy(anvilObj);
        }

        private void SpawnCreamSplashes(Vector3 center)
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject drop = new GameObject($"CreamDrop_{i}");
                var sr = drop.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCreamDropSprite();
                sr.sortingOrder = 31;
                drop.transform.position = center;
                Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(0.8f, 2.2f);
                StartCoroutine(CreamDropMotionRoutine(drop, dir));
            }
        }

        private IEnumerator CreamDropMotionRoutine(GameObject drop, Vector2 velocity)
        {
            float t = 0f;
            Vector3 pos = drop.transform.position;
            while (t < 0.8f && drop != null)
            {
                t += Time.deltaTime;
                velocity.y -= 9.8f * Time.deltaTime; // gravedad
                pos += (Vector3)velocity * Time.deltaTime;
                drop.transform.position = pos;
                yield return null;
            }
            if (drop != null) Destroy(drop);
        }

        private void SpawnDizzyStars(Vector3 center)
        {
            GameObject starsRoot = new GameObject("DizzyStarsGroup");
            starsRoot.transform.position = center;

            for (int i = 0; i < 4; i++)
            {
                GameObject star = new GameObject($"Star_{i}");
                star.transform.SetParent(starsRoot.transform, false);
                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = CreateStarSprite();
                sr.sortingOrder = 32;
                float angle = i * (360f / 4f) * Mathf.Deg2Rad;
                star.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.45f, Mathf.Sin(angle) * 0.20f, 0);
            }

            StartCoroutine(RotateDizzyStarsRoutine(starsRoot));
        }

        private IEnumerator RotateDizzyStarsRoutine(GameObject starsRoot)
        {
            float elapsed = 0f;
            while (elapsed < 3.2f && starsRoot != null)
            {
                elapsed += Time.deltaTime;
                starsRoot.transform.Rotate(0, 0, 180f * Time.deltaTime);
                yield return null;
            }
            if (starsRoot != null) Destroy(starsRoot);
        }

        // ================= SPRITES PROCEDURALES =================
        private Sprite CreatePieSprite()
        {
            int w = 64, h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 crust = new Color32(210, 145, 60, 255);
            Color32 cream = new Color32(255, 255, 255, 255);
            Color32 cherry = new Color32(230, 30, 45, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w / 2f) / (w / 2f);
                    float ny = y / (float)h;
                    int idx = y * w + x;
                    pixels[idx] = empty;

                    // Base de masa
                    if (ny < 0.35f && Mathf.Abs(nx) < 0.85f - (ny * 0.4f))
                    {
                        pixels[idx] = crust;
                    }
                    // Merengue esponjoso
                    else if (ny >= 0.35f && ny < 0.82f && Mathf.Abs(nx) < 0.75f - ((ny - 0.35f) * 0.6f))
                    {
                        pixels[idx] = cream;
                    }
                    // Cereza superior
                    else if (ny >= 0.82f && Mathf.Abs(nx) < 0.15f && ny < 0.98f)
                    {
                        pixels[idx] = cherry;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 48f);
        }

        private Sprite CreatePieSplatSprite()
        {
            int size = 80;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 cream = new Color32(255, 255, 255, 255);
            Color32 crust = new Color32(210, 145, 60, 255);
            Color32 cherry = new Color32(230, 30, 45, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    int idx = y * size + x;
                    pixels[idx] = empty;

                    // Salpicadura irregular
                    float angle = Mathf.Atan2(dy, dx);
                    float blob = 0.65f + Mathf.Sin(angle * 6f) * 0.20f + Mathf.Cos(angle * 9f) * 0.12f;

                    if (dist < blob)
                    {
                        if (dist > blob * 0.75f && (x + y) % 3 == 0) pixels[idx] = crust;
                        else if (dist < 0.18f) pixels[idx] = cherry;
                        else pixels[idx] = cream;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);
        }

        private Sprite CreateAnvilSprite()
        {
            int w = 80, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 darkIron = new Color32(50, 55, 65, 255);
            Color32 midIron = new Color32(85, 95, 110, 255);
            Color32 brightIron = new Color32(130, 145, 165, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w / 2f) / (w / 2f);
                    float ny = y / (float)h;
                    int idx = y * w + x;
                    pixels[idx] = empty;

                    // Base ancha
                    if (ny < 0.25f && Mathf.Abs(nx) < 0.85f)
                    {
                        pixels[idx] = (ny < 0.10f) ? darkIron : midIron;
                    }
                    // Cuello angosto
                    else if (ny < 0.60f && Mathf.Abs(nx) < 0.35f)
                    {
                        pixels[idx] = (nx < 0) ? darkIron : midIron;
                    }
                    // Cuerno y lomo del yunque
                    else if (ny >= 0.60f && ny < 0.95f)
                    {
                        if (nx < 0.85f && nx > -0.95f)
                        {
                            // Punta del cuerno a la izquierda
                            if (nx < -0.40f && ny > 0.80f) pixels[idx] = brightIron;
                            else pixels[idx] = (ny > 0.85f) ? brightIron : midIron;
                        }
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.25f), 48f);
        }

        private Sprite CreateCreamDropSprite()
        {
            int s = 24;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[s * s];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 cream = new Color32(255, 255, 255, 255);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - s / 2f) / (s / 2f);
                    float dy = (y - s / 2f) / (s / 2f);
                    pixels[y * s + x] = (dx * dx + dy * dy < 0.75f) ? cream : empty;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
        }

        private Sprite CreateStarSprite()
        {
            int s = 32;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[s * s];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 yellow = new Color32(255, 235, 60, 255);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs(x - s / 2f) / (s / 2f);
                    float dy = Mathf.Abs(y - s / 2f) / (s / 2f);
                    pixels[y * s + x] = (dx + dy < 0.85f) ? yellow : empty;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 48f);
        }
    }
}
