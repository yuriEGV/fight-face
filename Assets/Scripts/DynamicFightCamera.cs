using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Cámara dinámica estilo Street Fighter / Smash Bros:
    /// - Sigue el punto medio entre ambos jugadores.
    /// - Acerca el zoom (OrthographicSize) dinámicamente cuando están cuerpo a cuerpo.
    /// - Aleja el zoom para mantener a ambos dentro del encuadre en saltos y distancias largas.
    /// - Zoom dramático cinematográfico al ocurrir un K.O.
    /// </summary>
    public class DynamicFightCamera : MonoBehaviour
    {
        public static DynamicFightCamera Instance { get; private set; }

        [Header("Objetivos")]
        public Transform targetP1;
        public Transform targetP2;

        [Header("Zoom Dinámico")]
        public float minZoom = 3.8f;      // Zoom cercano cuerpo a cuerpo
        public float maxZoom = 5.6f;      // Zoom lejano cuando se separan
        public float koZoom = 3.2f;       // Zoom dramático de K.O.
        public float zoomSpeed = 4.0f;
        public float moveSpeed = 6.0f;

        [Header("Límites del Escenario")]
        public float minX = -4.5f;
        public float maxX = 4.5f;
        public float defaultY = 0.2f;

        private Camera cam;
        private bool isKOZoomActive = false;
        private Transform koFocusTarget = null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            FindFighters();
        }

        public void FindFighters()
        {
            if (targetP1 == null)
            {
                GameObject p1 = GameObject.Find("Player1");
                if (p1 != null) targetP1 = p1.transform;
            }
            if (targetP2 == null)
            {
                GameObject p2 = GameObject.Find("Player2");
                if (p2 != null) targetP2 = p2.transform;
            }
        }

        public void TriggerKOZoom(Transform winner)
        {
            isKOZoomActive = true;
            koFocusTarget = winner;
        }

        public void ResetCamera()
        {
            isKOZoomActive = false;
            koFocusTarget = null;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            if (targetP1 == null || targetP2 == null)
            {
                FindFighters();
                if (targetP1 == null || targetP2 == null) return;
            }

            // Si está en zoom dramático de K.O.
            if (isKOZoomActive)
            {
                Vector3 koTarget = koFocusTarget != null ? koFocusTarget.position : (targetP1.position + targetP2.position) * 0.5f;
                Vector3 desiredPos = new Vector3(Mathf.Clamp(koTarget.x, minX, maxX), koTarget.y + 0.5f, cam.transform.position.z);
                cam.transform.position = Vector3.Lerp(cam.transform.position, desiredPos, Time.unscaledDeltaTime * 3.5f);
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, koZoom, Time.unscaledDeltaTime * 3.5f);
                return;
            }

            // Posición media entre los dos luchadores
            Vector3 midPoint = (targetP1.position + targetP2.position) * 0.5f;
            float targetX = Mathf.Clamp(midPoint.x, minX, maxX);
            float targetY = Mathf.Clamp(midPoint.y * 0.35f + defaultY, -0.5f, 1.5f);

            Vector3 targetPosition = new Vector3(targetX, targetY, cam.transform.position.z);
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, Time.deltaTime * moveSpeed);

            // Calcular distancia horizontal entre luchadores
            float distance = Mathf.Abs(targetP1.position.x - targetP2.position.x);

            // Interpolación de zoom según distancia
            // Distancia típica: entre 1.5 y 7.0 unidades
            float t = Mathf.InverseLerp(1.8f, 6.5f, distance);
            float desiredZoom = Mathf.Lerp(minZoom, maxZoom, t);

            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredZoom, Time.deltaTime * zoomSpeed);
        }
    }
}
