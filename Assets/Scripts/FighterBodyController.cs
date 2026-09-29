using System.Collections;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Controlador del cuerpo del luchador 2D.
    /// Utiliza directamente las ilustraciones originales recortadas de los personajes (headless)
    /// extraídas del arte del usuario:
    /// - El Gordo (Sumo / Luchador con overol a rayas azul y blanco)
    /// - El Flaco (Maestro de Kung-Fu ágil, estilo Bruce Lee)
    /// - El Musculoso (Campeón de Muay Thai y boxeo, shorts dorados)
    /// - La Mujer (Peleadora de kickboxing con top y mallas)
    /// - La Guerrera (Maestra de ninjutsu y artes marciales)
    /// - El Dos Cabezas (Mutante colosal con cuello doble)
    ///
    /// Sobre el cuello de cada cuerpo se monta el Sticker ovalado erguido con las 4 expresiones
    /// faciales del jugador tomadas con la webcam.
    /// Incluye animación 2D viva de brawler: respiración idle, zancada al caminar,
    /// embestida en puñetazos y patadas, sacudida en hitstun y caída dramática al ring en K.O.
    /// </summary>
    public class FighterBodyController : MonoBehaviour
    {
        [Header("Personaje y Renderizador Principal")]
        public FighterBodyType currentBodyType = FighterBodyType.Gordo;
        public SpriteRenderer bodySpriteRenderer;

        [Header("Puntos de Cuello (Neck Anchors)")]
        public Transform neckPoint;      // Cuello principal (o cuello izquierdo en Dos Cabezas)
        public Transform neckPointRight; // Cuello derecho (exclusivo para El Dos Cabezas)

        [Header("Efectos y Sombra")]
        public Transform groundShadow;
        public GameObject punchTrailObj;
        public GameObject kickTrailObj;

        [Header("Paleta de Colores")]
        public Color suitColor = new Color(0.18f, 0.45f, 0.85f);
        public Color gloveColor = new Color(0.95f, 0.22f, 0.22f);
        public Color skinColor = new Color(1f, 0.85f, 0.72f);
        public Color bootColor = new Color(0.25f, 0.18f, 0.12f);

        // Campos de compatibilidad con código existente
        [HideInInspector] public Transform pelvis;
        [HideInInspector] public Transform torso;
        [HideInInspector] public Transform leftArm;
        [HideInInspector] public Transform leftForearm;
        [HideInInspector] public Transform leftFist;
        [HideInInspector] public Transform rightArm;
        [HideInInspector] public Transform rightForearm;
        [HideInInspector] public Transform rightFist;
        [HideInInspector] public Transform leftLeg;
        [HideInInspector] public Transform leftCalf;
        [HideInInspector] public Transform leftFoot;
        [HideInInspector] public Transform rightLeg;
        [HideInInspector] public Transform rightCalf;
        [HideInInspector] public Transform rightFoot;
        [HideInInspector] public Transform articulatedPunchFist;
        [HideInInspector] public SpriteRenderer articulatedPunchSr;
        [HideInInspector] public Transform articulatedKickFoot;
        [HideInInspector] public SpriteRenderer articulatedKickSr;
        [HideInInspector] public bool isUsingClassicSpriteBody = true;

        // Estado y ciclos de animación
        private SpriteRenderer[] allRenderers;
        private Color[] originalColors;
        private float walkCycle = 0f;
        private float breatheCycle = 0f;
        private bool isAttacking = false;
        private bool isKO = false;
        private bool isHurt = false;

        private Coroutine activeAttackRoutine;
        private Coroutine activeHurtRoutine;
        private Coroutine activeKORoutine;

        private void Awake()
        {
            EnsureBodyComponents();
            SetClassicBody(currentBodyType);
        }

        private void EnsureBodyComponents()
        {
            // 1. Renderizador del cuerpo de la ilustración real
            if (bodySpriteRenderer == null)
            {
                Transform bodyChild = transform.Find("BodyArtwork");
                if (bodyChild == null)
                {
                    GameObject bObj = new GameObject("BodyArtwork");
                    bObj.transform.SetParent(transform, false);
                    bObj.transform.localPosition = Vector3.zero;
                    bodyChild = bObj.transform;
                }
                bodySpriteRenderer = bodyChild.GetComponent<SpriteRenderer>();
                if (bodySpriteRenderer == null)
                {
                    bodySpriteRenderer = bodyChild.gameObject.AddComponent<SpriteRenderer>();
                }
            }
            bodySpriteRenderer.sortingOrder = 8;

            // 2. Punto del cuello (Neck Point)
            if (neckPoint == null)
            {
                Transform nChild = transform.Find("NeckPoint");
                if (nChild == null)
                {
                    GameObject nObj = new GameObject("NeckPoint");
                    nObj.transform.SetParent(transform, false);
                    nChild = nObj.transform;
                }
                neckPoint = nChild;
            }

            // 3. Cuello derecho para Dos Cabezas
            if (neckPointRight == null)
            {
                Transform nrChild = transform.Find("NeckPointRight");
                if (nrChild == null)
                {
                    GameObject nrObj = new GameObject("NeckPointRight");
                    nrObj.transform.SetParent(transform, false);
                    nrChild = nrObj.transform;
                }
                neckPointRight = nrChild;
            }

            // 4. Sombra en el suelo del cuadrilátero
            if (groundShadow == null)
            {
                Transform sChild = transform.Find("GroundShadow");
                if (sChild == null)
                {
                    GameObject sObj = new GameObject("GroundShadow");
                    sObj.transform.SetParent(transform, false);
                    sObj.transform.localPosition = new Vector3(0, -0.05f, 0);
                    sObj.transform.localScale = new Vector3(1.3f, 0.45f, 1f);
                    var sr = sObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateOvalShadowSprite();
                    sr.sortingOrder = -5;
                    sChild = sObj.transform;
                }
                groundShadow = sChild;
            }

            // Compatibilidad: asignar transform raíz a torso y pelvis
            if (torso == null) torso = transform;
            if (pelvis == null) pelvis = transform;
        }

        /// <summary>
        /// Aplica la ilustración auténtica correspondiente al personaje y coloca el cuello exactamente en la apertura del torso.
        /// </summary>
        public void SetClassicBody(FighterBodyType bodyType)
        {
            currentBodyType = bodyType;
            EnsureBodyComponents();

            // Cargar el sprite original headless recortado del arte del usuario
            Sprite authenticBody = FaceLoader.LoadFighterBodySprite(bodyType);
            if (authenticBody != null && bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sprite = authenticBody;
                bodySpriteRenderer.color = Color.white;
            }

            // Calibrar la posición exacta del cuello para cada uno de los 6 luchadores
            Vector3 neckPos = FaceLoader.GetNeckLocalOffset(bodyType);
            if (neckPoint != null)
            {
                neckPoint.localPosition = neckPos;
            }

            if (neckPointRight != null)
            {
                neckPointRight.localPosition = FaceLoader.GetRightNeckLocalOffset(bodyType);
                neckPointRight.gameObject.SetActive(bodyType == FighterBodyType.DosCabezas);
            }

            // Ajustar ancho de la sombra según el tamaño del cuerpo
            if (groundShadow != null)
            {
                float shadowW = (bodyType == FighterBodyType.Gordo) ? 1.6f : ((bodyType == FighterBodyType.Flaco) ? 1.1f : 1.35f);
                groundShadow.localScale = new Vector3(shadowW, 0.45f, 1f);
            }

            CacheRenderers();
        }

        public void CacheRenderers()
        {
            allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            originalColors = new Color[allRenderers.Length];
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    originalColors[i] = allRenderers[i].color;
                }
            }
        }

        /// <summary>
        /// Animación viva del cuerpo 2D: respiración idle, zancada al correr y salto.
        /// </summary>
        public void UpdateAnimation(float moveX, bool isGrounded)
        {
            if (isKO || isAttacking || isHurt) return;

            if (!isGrounded)
            {
                // En el aire: ligera inclinación y suspensión
                float airTilt = -3f * Mathf.Sign(moveX);
                transform.localPosition = new Vector3(0, 0.08f, 0);
                transform.localRotation = Quaternion.Euler(0, 0, airTilt);
            }
            else if (Mathf.Abs(moveX) > 0.1f)
            {
                // Caminando: zancada rítmica con rebote vertical e inclinación hacia adelante
                walkCycle += Time.deltaTime * 11f;
                float bob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.06f;
                float tilt = Mathf.Sin(walkCycle) * 3f;

                transform.localPosition = new Vector3(0, bob, 0);
                transform.localRotation = Quaternion.Euler(0, 0, tilt);

                if (groundShadow != null)
                {
                    groundShadow.localScale = new Vector3(
                        (currentBodyType == FighterBodyType.Gordo ? 1.6f : 1.3f) * (1f - bob * 0.5f),
                        0.45f,
                        1f
                    );
                }
            }
            else
            {
                // Idle: respiración suave con cadencia brawler
                breatheCycle += Time.deltaTime * 3.5f;
                float bob = Mathf.Sin(breatheCycle) * 0.03f;

                transform.localPosition = new Vector3(0, bob, 0);
                transform.localRotation = Quaternion.identity;

                if (groundShadow != null)
                {
                    groundShadow.localScale = new Vector3(
                        (currentBodyType == FighterBodyType.Gordo ? 1.6f : 1.3f) * (1f + bob * 0.3f),
                        0.45f,
                        1f
                    );
                }
            }
        }

        /// <summary>
        /// Animación de Puñetazo: Embestida dinámica hacia adelante con retroceso a guardia.
        /// </summary>
        public void PlayPunchAnimation(float startup, float active, float recovery)
        {
            if (isKO) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(PunchMotionRoutine(startup, active, recovery));
        }

        private IEnumerator PunchMotionRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;

            // 1. Startup: preparación ligera hacia atrás
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;
            Vector3 prepPos = startPos + new Vector3(-0.06f, 0.02f, 0);
            Quaternion prepRot = Quaternion.Euler(0, 0, -3f);

            float t = 0f;
            while (t < startup)
            {
                t += Time.deltaTime;
                float frac = t / startup;
                transform.localPosition = Vector3.Lerp(startPos, prepPos, frac);
                transform.localRotation = Quaternion.Lerp(startRot, prepRot, frac);
                yield return null;
            }

            // 2. Active: estocada potente hacia adelante
            Vector3 strikePos = startPos + new Vector3(0.28f, -0.02f, 0);
            Quaternion strikeRot = Quaternion.Euler(0, 0, 5f);

            t = 0f;
            while (t < active)
            {
                t += Time.deltaTime;
                float frac = t / active;
                transform.localPosition = Vector3.Lerp(prepPos, strikePos, frac);
                transform.localRotation = Quaternion.Lerp(prepRot, strikeRot, frac);
                yield return null;
            }

            // 3. Recovery: retorno ágil a la pose de guardia
            t = 0f;
            while (t < recovery)
            {
                t += Time.deltaTime;
                float frac = t / recovery;
                transform.localPosition = Vector3.Lerp(strikePos, Vector3.zero, frac);
                transform.localRotation = Quaternion.Lerp(strikeRot, Quaternion.identity, frac);
                yield return null;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            isAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Animación de Patada: Inclinación hacia atrás y estiramiento con impulso frontal.
        /// </summary>
        public void PlayKickAnimation(float startup, float active, float recovery)
        {
            if (isKO) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(KickMotionRoutine(startup, active, recovery));
        }

        private IEnumerator KickMotionRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;

            Vector3 startPos = transform.localPosition;
            Vector3 prepPos = startPos + new Vector3(-0.08f, 0.04f, 0);
            Quaternion prepRot = Quaternion.Euler(0, 0, -7f);

            float t = 0f;
            while (t < startup)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(startPos, prepPos, t / startup);
                transform.localRotation = Quaternion.Lerp(Quaternion.identity, prepRot, t / startup);
                yield return null;
            }

            Vector3 strikePos = startPos + new Vector3(0.32f, 0.05f, 0);
            Quaternion strikeRot = Quaternion.Euler(0, 0, 8f);

            t = 0f;
            while (t < active)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(prepPos, strikePos, t / active);
                transform.localRotation = Quaternion.Lerp(prepRot, strikeRot, t / active);
                yield return null;
            }

            t = 0f;
            while (t < recovery)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(strikePos, Vector3.zero, t / recovery);
                transform.localRotation = Quaternion.Lerp(strikeRot, Quaternion.identity, t / recovery);
                yield return null;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            isAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Reacción a golpe (Hitstun): Destello rojo cómic, sacudida horizontal e inclinación de dolor.
        /// </summary>
        public void PlayHurtAnimation(float duration)
        {
            if (isKO) return;
            if (activeHurtRoutine != null) StopCoroutine(activeHurtRoutine);
            activeHurtRoutine = StartCoroutine(HurtMotionRoutine(duration));
        }

        private IEnumerator HurtMotionRoutine(float duration)
        {
            isHurt = true;

            // Destello rojo intenso de impacto cómic
            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
            }

            Quaternion hurtRot = Quaternion.Euler(0, 0, -9f);
            Vector3 baseHurtPos = new Vector3(-0.12f, 0.04f, 0);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float shakeX = Random.Range(-0.08f, 0.08f);
                float shakeY = Random.Range(-0.04f, 0.04f);
                transform.localPosition = baseHurtPos + new Vector3(shakeX, shakeY, 0);
                transform.localRotation = hurtRot;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restaurar color y pose normal
            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.color = Color.white;
            }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            isHurt = false;
            activeHurtRoutine = null;
        }

        /// <summary>
        /// Animación de K.O. (Derrota): El luchador cae de espaldas sobre la lona del cuadrilátero.
        /// </summary>
        public void PlayKODefeatedAnimation()
        {
            isKO = true;
            isAttacking = false;
            isHurt = false;

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            if (activeHurtRoutine != null) StopCoroutine(activeHurtRoutine);
            if (activeKORoutine != null) StopCoroutine(activeKORoutine);

            activeKORoutine = StartCoroutine(KOFallRoutine());
        }

        private IEnumerator KOFallRoutine()
        {
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            Vector3 floorPos = new Vector3(-0.35f, -0.30f, 0);
            Quaternion fallenRot = Quaternion.Euler(0, 0, -85f);

            float duration = 0.50f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Curva de caída parabólica
                float arcY = Mathf.Sin(t * Mathf.PI) * 0.15f;
                transform.localPosition = Vector3.Lerp(startPos, floorPos, t) + new Vector3(0, arcY, 0);
                transform.localRotation = Quaternion.Slerp(startRot, fallenRot, t);

                yield return null;
            }

            transform.localPosition = floorPos;
            transform.localRotation = fallenRot;
        }

        /// <summary>
        /// Restablece el cuerpo a la pose de combate recta y limpia efectos.
        /// </summary>
        public void ResetBody()
        {
            ResetJointsToStance();
        }

        public void ResetJointsToStance()
        {
            isKO = false;
            isAttacking = false;
            isHurt = false;

            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeHurtRoutine != null) { StopCoroutine(activeHurtRoutine); activeHurtRoutine = null; }
            if (activeKORoutine != null) { StopCoroutine(activeKORoutine); activeKORoutine = null; }

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.color = Color.white;
            }
        }

        private Sprite CreateOvalShadowSprite()
        {
            int w = 64;
            int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color shadow = new Color(0f, 0f, 0.05f, 0.55f);

            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float rx = w * 0.48f;
            float ry = h * 0.45f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - cx) / rx;
                    float ny = (y - cy) / ry;
                    float dist = (nx * nx) + (ny * ny);
                    if (dist <= 1.0f)
                    {
                        float alpha = Mathf.Clamp01(1f - dist) * 0.55f;
                        tex.SetPixel(x, y, new Color(0f, 0f, 0.05f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
