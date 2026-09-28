using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Manejador de impacto profesional estilo Street Fighter / Mortal Kombat:
    /// - Hitstop: Congela el juego durante una fracción de segundo en el instante exacto de conectar un golpe.
    /// - Screen Shake: Vibración de cámara en tiempo real durante y tras el impacto.
    /// - Efectos de sonido y textos cómic en el punto de contacto.
    /// </summary>
    public class FightImpactManager : MonoBehaviour
    {
        public static FightImpactManager Instance { get; private set; }

        private Camera mainCamera;
        private Vector3 camInitialPos;
        private Coroutine activeShakeRoutine;

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

            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                camInitialPos = mainCamera.transform.localPosition;
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    camInitialPos = mainCamera.transform.localPosition;
                }
            }
        }

        /// <summary>
        /// Ejecuta el impacto estilo Street Fighter / Mortal Kombat
        /// </summary>
        public void ImpactoPesado(float duracionHitstop = 0.08f, float intensidadShake = 0.2f)
        {
            StartCoroutine(RutinaHitstop(duracionHitstop));
            
            if (activeShakeRoutine != null)
            {
                StopCoroutine(activeShakeRoutine);
            }
            activeShakeRoutine = StartCoroutine(RutinaScreenShake(duracionHitstop + 0.07f, intensidadShake));
        }

        /// <summary>
        /// Impacto ligero para golpes rápidos
        /// </summary>
        public void ImpactoLigero(float duracionHitstop = 0.04f, float intensidadShake = 0.1f)
        {
            ImpactoPesado(duracionHitstop, intensidadShake);
        }

        private IEnumerator RutinaHitstop(float duracion)
        {
            // Detiene el tiempo global del motor
            Time.timeScale = 0f;
            // Espera en tiempo real sin verse afectado por timeScale
            yield return new WaitForSecondsRealtime(duracion);
            Time.timeScale = 1f;
        }

        private IEnumerator RutinaScreenShake(float duracion, float magnitud)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) yield break;

            Transform cam = mainCamera.transform;
            Vector3 posOriginal = cam.localPosition;
            float tiempo = 0f;

            while (tiempo < duracion)
            {
                float x = Random.Range(-1f, 1f) * magnitud;
                float y = Random.Range(-1f, 1f) * magnitud;

                cam.localPosition = new Vector3(posOriginal.x + x, posOriginal.y + y, posOriginal.z);
                tiempo += Time.unscaledDeltaTime;
                yield return null;
            }

            cam.localPosition = posOriginal;
            activeShakeRoutine = null;
        }
    }
}
