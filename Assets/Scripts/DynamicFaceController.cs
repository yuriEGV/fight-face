using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Controla el GameObject de la cabeza del personaje.
    /// Intercambia dinámicamente entre las 4 caras (Base, Enojo, Dolor, KO)
    /// y añade micro-animaciones cómicas de rebote (squash & stretch, sacudida).
    /// </summary>
    public class DynamicFaceController : MonoBehaviour
    {
        [Header("Renderizador")]
        [SerializeField] private SpriteRenderer headRenderer;

        [Header("Perfil de Caras")]
        [SerializeField] private FaceProfile profile;

        [Header("Estado Actual")]
        [SerializeField] private FaceType currentEmotion = FaceType.Base;

        private Coroutine temporaryFaceCoroutine;
        private Vector3 initialLocalScale = Vector3.one;
        private Vector3 initialLocalPosition = Vector3.zero;

        public FaceProfile Profile => profile;
        public FaceType CurrentEmotion => currentEmotion;
        public SpriteRenderer HeadRenderer => headRenderer;

        private void Awake()
        {
            if (headRenderer == null)
            {
                headRenderer = GetComponent<SpriteRenderer>();
                if (headRenderer == null)
                {
                    headRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            initialLocalScale = transform.localScale;
            initialLocalPosition = transform.localPosition;
        }

        /// <summary>
        /// Asigna un nuevo perfil de 4 caras al luchador y actualiza la cabeza.
        /// </summary>
        public void SetProfile(FaceProfile newProfile)
        {
            profile = newProfile;
            ApplyEmotion(currentEmotion);
        }

        /// <summary>
        /// Cambia la emoción activa de forma permanente (hasta la próxima orden).
        /// </summary>
        public void SetFace(FaceType newEmotion)
        {
            if (currentEmotion == FaceType.KO) return; // No cambiar si ya está noqueado

            if (temporaryFaceCoroutine != null)
            {
                StopCoroutine(temporaryFaceCoroutine);
                temporaryFaceCoroutine = null;
            }

            currentEmotion = newEmotion;
            ApplyEmotion(newEmotion);
        }

        /// <summary>
        /// Muestra una emoción durante un tiempo determinado (ej: Enojo durante un golpe, o Dolor al recibir daño)
        /// y luego regresa a la cara Base automáticamente.
        /// </summary>
        public void ShowTemporaryFace(FaceType emotion, float duration)
        {
            if (currentEmotion == FaceType.KO) return;

            if (temporaryFaceCoroutine != null)
            {
                StopCoroutine(temporaryFaceCoroutine);
            }

            temporaryFaceCoroutine = StartCoroutine(TemporaryFaceRoutine(emotion, duration));
        }

        private IEnumerator TemporaryFaceRoutine(FaceType emotion, float duration)
        {
            currentEmotion = emotion;
            ApplyEmotion(emotion);

            // Reacción física cómica: sacudida o estiramiento de cabeza
            if (emotion == FaceType.Dolor)
            {
                StartCoroutine(HurtShakeRoutine(duration));
            }
            else if (emotion == FaceType.Enojo)
            {
                StartCoroutine(AngryPopRoutine(duration));
            }

            yield return new WaitForSeconds(duration);

            if (currentEmotion != FaceType.KO)
            {
                currentEmotion = FaceType.Base;
                ApplyEmotion(FaceType.Base);
            }
            temporaryFaceCoroutine = null;
        }

        /// <summary>
        /// Aplica la textura correspondiente del perfil al SpriteRenderer.
        /// </summary>
        private void ApplyEmotion(FaceType emotion)
        {
            if (profile == null) return;
            Sprite spriteToUse = profile.GetSprite(emotion);
            if (spriteToUse != null && headRenderer != null)
            {
                headRenderer.sprite = spriteToUse;
            }
        }

        /// <summary>
        /// Efecto cómico al recibir golpe: la cabeza se sacude de dolor.
        /// </summary>
        private IEnumerator HurtShakeRoutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float shakeX = Random.Range(-0.1f, 0.1f);
                float shakeY = Random.Range(-0.08f, 0.08f);
                transform.localPosition = initialLocalPosition + new Vector3(shakeX, shakeY, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = initialLocalPosition;
        }

        /// <summary>
        /// Efecto cómico al atacar: la cabeza se agranda con furia/odio (Enojo).
        /// </summary>
        private IEnumerator AngryPopRoutine(float duration)
        {
            Vector3 angryScale = initialLocalScale * 1.15f;
            float half = duration * 0.5f;
            float t = 0f;

            while (t < half)
            {
                transform.localScale = Vector3.Lerp(initialLocalScale, angryScale, t / half);
                t += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                transform.localScale = Vector3.Lerp(angryScale, initialLocalScale, t / half);
                t += Time.deltaTime;
                yield return null;
            }

            transform.localScale = initialLocalScale;
        }

        /// <summary>
        /// Vuelve la cabeza a la pose neutra.
        /// </summary>
        public void ResetToBaseFace()
        {
            currentEmotion = FaceType.Base;
            ApplyEmotion(FaceType.Base);
            transform.localPosition = initialLocalPosition;
            transform.localScale = initialLocalScale;
        }
    }
}
