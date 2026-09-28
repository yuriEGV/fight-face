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

        private readonly string[] comicWords = { "¡POW!", "¡BAM!", "¡OUCH!", "¡WHACK!", "¡KAPOW!", "¡SMACK!" };
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
            // 1. Crear texto flotante de impacto cómic
            string word = comicWords[Random.Range(0, comicWords.Length)];
            CreateComicTextEffect(position, word, isHeavy);

            // 2. Hit-Stop arcade (micro pausa)
            StartCoroutine(HitStopRoutine(isHeavy ? 0.08f : 0.04f));

            // 3. Sacudida de cámara
            StartCoroutine(CameraShakeRoutine(isHeavy ? 0.2f : 0.1f, isHeavy ? 0.18f : 0.08f));
        }

        private void CreateComicTextEffect(Vector2 pos, string word, bool isHeavy)
        {
            GameObject textObj = new GameObject("HitPopup_" + word);
            textObj.transform.position = new Vector3(pos.x, pos.y + 0.3f, -1f);

            var textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = word;
            textMesh.fontSize = isHeavy ? 48 : 36;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = isHeavy ? new Color(1f, 0.2f, 0.1f) : new Color(1f, 0.9f, 0.2f);

            // Orientar y escalar
            float initialScale = isHeavy ? 0.025f : 0.018f;
            textObj.transform.localScale = Vector3.one * initialScale;
            textObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

            StartCoroutine(AnimateComicText(textObj, textMesh));
        }

        private IEnumerator AnimateComicText(GameObject obj, TextMesh tm)
        {
            float duration = 0.55f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Vector3 targetPos = startPos + new Vector3(Random.Range(-0.2f, 0.2f), 0.8f, 0);
            Vector3 startScale = obj.transform.localScale;
            Color startColor = tm.color;

            // Pop inicial de escala
            obj.transform.localScale = startScale * 1.5f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                obj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                obj.transform.localScale = Vector3.Lerp(startScale * 1.5f, startScale * 0.7f, t);

                // Desvanecimiento alfa
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                tm.color = c;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Destroy(obj);
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
        }

        private IEnumerator CameraShakeRoutine(float duration, float magnitude)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                mainCam.transform.position = new Vector3(camInitialPos.x + x, camInitialPos.y + y, camInitialPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCam.transform.position = camInitialPos;
        }
    }
}
