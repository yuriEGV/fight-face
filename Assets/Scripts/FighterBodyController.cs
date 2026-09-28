using System.Collections;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Controlador del cuerpo articulado 2D del luchador basado en el sistema de Huesos/Rig 2D:
    ///
    ///               CABEZA (Face Sticker con 4 emociones)
    ///                  ●
    ///                  │
    ///               CUELLO
    ///                  ●
    ///           ┌──────┴──────┐
    ///           ●             ●
    ///        HOMBRO        HOMBRO
    ///           │             │
    ///         CODO           CODO
    ///           │             │
    ///        MUÑECA         MUÑECA
    ///
    ///               TORSO
    ///                  │
    ///           ┌──────┴──────┐
    ///         CADERA        CADERA
    ///            │             │
    ///          RODILLA       RODILLA
    ///            │             │
    ///         TOBILLO       TOBILLO
    ///
    /// Y para "El Dos Cabezas" (mutante especial de dos cabezas):
    ///         😡         😱
    ///          \         /
    ///           \       /
    ///          CUELLO_L CUELLO_R
    ///               │
    ///             TORSO
    ///
    /// - Cada articulación utiliza tapas de unión circulares superpuestas (ball-and-socket)
    ///   para garantizar que los miembros NUNCA se separen ni aparezcan huecos al rotar.
    /// - Soporta los 5 personajes canónicos:
    ///   1. El Gordo (Luchador de sumo/wrestling con overol a rayas azul y blanco).
    ///   2. El Flaco (Maestro de artes marciales estilo Bruce Lee, pantalones verdes y torso marcado).
    ///   3. El Musculoso (Campeón peso pesado de Muay Thai, shorts dorados y guantes rojos).
    ///   4. La Mujer (Peleadora de kickboxing con top deportivo y mallas atléticas).
    ///   5. El Dos Cabezas (Coloso de doble cuello y dos cabezas animadas simultáneamente).
    /// </summary>
    public class FighterBodyController : MonoBehaviour
    {
        [Header("Núcleo y Columna (Rig 2D)")]
        public Transform pelvis;         // CADERA / Centro de masa del esqueleto
        public Transform torso;          // TORSO
        public Transform neckPoint;      // CUELLO (Cabeza central o Izquierda 😡 en Dos Cabezas)
        public Transform neckPointLeft;  // CUELLO IZQ (Dos Cabezas)
        public Transform neckPointRight; // CUELLO DER (Dos Cabezas 😱)

        [Header("Brazos Articulados")]
        public Transform leftArm;        // HOMBRO DELANTERO / Lead Shoulder
        public Transform leftForearm;    // CODO DELANTERO / Lead Forearm
        public Transform leftFist;       // MUÑECA DELANTERA / Lead Fist
        public Transform rightArm;       // HOMBRO TRASERO / Rear Shoulder
        public Transform rightForearm;   // CODO TRASERO / Rear Forearm
        public Transform rightFist;      // MUÑECA TRASERA / Rear Fist

        [Header("Piernas Articuladas")]
        public Transform leftLeg;        // CADERA / MUSLO DELANTERO
        public Transform leftCalf;       // RODILLA / PANTORRILLA DELANTERA
        public Transform leftFoot;       // TOBILLO / BOTA DELANTERA
        public Transform rightLeg;       // CADERA / MUSLO TRASERO
        public Transform rightCalf;      // RODILLA / PANTORRILLA TRASERA
        public Transform rightFoot;      // TOBILLO / BOTA TRASERA

        [Header("Efectos")]
        public Transform groundShadow;
        public GameObject punchTrailObj;
        public GameObject kickTrailObj;

        [Header("Cuerpo Clásico")]
        public FighterBodyType currentBodyType = FighterBodyType.Gordo;
        public SpriteRenderer bodySpriteRenderer;
        public bool isUsingClassicSpriteBody = false;

        [Header("Extremidades Articuladas Dinámicas (Compatibilidad)")]
        public Transform articulatedPunchFist;
        public SpriteRenderer articulatedPunchSr;
        public Transform articulatedKickFoot;
        public SpriteRenderer articulatedKickSr;

        [Header("Paleta de Colores")]
        public Color suitColor = new Color(0.18f, 0.45f, 0.85f);
        public Color gloveColor = new Color(0.95f, 0.22f, 0.22f);
        public Color skinColor = new Color(1f, 0.85f, 0.72f);
        public Color bootColor = new Color(0.25f, 0.18f, 0.12f);

        // Estado y animación
        private SpriteRenderer[] allRenderers;
        private Color[] originalColors;
        private float walkCycle = 0f;
        private float breatheCycle = 0f;
        private bool isAttacking = false;
        private bool isKO = false;
        private bool isHurt = false;

        // Poses base locales del rig esquelético
        private Vector3 pelvisBasePos = new Vector3(0, 0.75f, 0);
        private Vector3 torsoBasePos = new Vector3(0, 0.16f, 0);
        private Quaternion leadShoulderBaseRot = Quaternion.Euler(0, 0, 22f);
        private Quaternion leadForearmBaseRot = Quaternion.Euler(0, 0, 68f);
        private Quaternion rearShoulderBaseRot = Quaternion.Euler(0, 0, -14f);
        private Quaternion rearForearmBaseRot = Quaternion.Euler(0, 0, 75f);
        private Quaternion leadThighBaseRot = Quaternion.Euler(0, 0, -8f);
        private Quaternion leadCalfBaseRot = Quaternion.Euler(0, 0, 14f);
        private Quaternion rearThighBaseRot = Quaternion.Euler(0, 0, 12f);
        private Quaternion rearCalfBaseRot = Quaternion.Euler(0, 0, -10f);

        private Coroutine activeAttackRoutine;
        private Coroutine activeHurtRoutine;

        private void Awake()
        {
            BuildArticulatedHierarchy(currentBodyType);
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
        /// Aplica y reconstruye el esqueleto articulado según el personaje elegido.
        /// </summary>
        public void SetClassicBody(FighterBodyType bodyType)
        {
            currentBodyType = bodyType;
            BuildArticulatedHierarchy(bodyType);
            CacheRenderers();
        }

        /// <summary>
        /// Construye el esqueleto 2D jerárquico exacto solicitado por el usuario con uniones superpuestas.
        /// </summary>
        public void BuildArticulatedHierarchy(FighterBodyType bodyType)
        {
            // Eliminar restos antiguos no jerárquicos
            Transform oldSprite = transform.Find("ClassicBodySprite");
            if (oldSprite != null) Destroy(oldSprite.gameObject);

            // 1. SOMBRA EN LA LONA DEL RING (Y = -0.05)
            if (groundShadow == null)
            {
                Transform exShadow = transform.Find("GroundShadow");
                if (exShadow != null) groundShadow = exShadow;
                else
                {
                    GameObject sObj = new GameObject("GroundShadow");
                    sObj.transform.SetParent(transform, false);
                    sObj.transform.localPosition = new Vector3(0, -0.05f, 0);
                    groundShadow = sObj.transform;
                    var sr = sObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateOvalShadowSprite();
                    sr.sortingOrder = -5;
                }
            }

            // 2. CADERA / PELVIS NÚCLEO (Raíz móvil del esqueleto a Y = 0.75)
            if (pelvis == null)
            {
                Transform exPelvis = transform.Find("Pelvis_Core");
                if (exPelvis != null) pelvis = exPelvis;
                else
                {
                    GameObject pObj = new GameObject("Pelvis_Core");
                    pObj.transform.SetParent(transform, false);
                    pelvis = pObj.transform;
                }
            }
            pelvis.localPosition = pelvisBasePos;
            var pelvisSr = EnsureSpriteRenderer(pelvis.gameObject, 6);
            pelvisSr.sprite = GenerateProceduralPartSprite("Pelvis", bodyType);

            // 3. PIERNA TRASERA (detrás del cuerpo, sortingOrder 1, 2, 3)
            // Cadera -> Muslo Trasero
            if (rightLeg == null)
            {
                Transform ex = pelvis.Find("Thigh_Rear");
                if (ex != null) rightLeg = ex;
                else
                {
                    GameObject rThighObj = new GameObject("Thigh_Rear");
                    rThighObj.transform.SetParent(pelvis, false);
                    rightLeg = rThighObj.transform;
                }
            }
            rightLeg.localPosition = new Vector3(-0.16f, -0.06f, 0);
            var rThighSr = EnsureSpriteRenderer(rightLeg.gameObject, 1);
            rThighSr.sprite = GenerateProceduralPartSprite("Thigh", bodyType);

            // Rodilla -> Pantorrilla Trasera
            if (rightCalf == null)
            {
                Transform ex = rightLeg.Find("Calf_Rear");
                if (ex != null) rightCalf = ex;
                else
                {
                    GameObject rCalfObj = new GameObject("Calf_Rear");
                    rCalfObj.transform.SetParent(rightLeg, false);
                    rightCalf = rCalfObj.transform;
                }
            }
            rightCalf.localPosition = new Vector3(0f, -0.32f, 0);
            var rCalfSr = EnsureSpriteRenderer(rightCalf.gameObject, 2);
            rCalfSr.sprite = GenerateProceduralPartSprite("Calf", bodyType);

            // Tobillo -> Bota Trasera
            if (rightFoot == null)
            {
                Transform ex = rightCalf.Find("Foot_Rear");
                if (ex != null) rightFoot = ex;
                else
                {
                    GameObject rFootObj = new GameObject("Foot_Rear");
                    rFootObj.transform.SetParent(rightCalf, false);
                    rightFoot = rFootObj.transform;
                }
            }
            rightFoot.localPosition = new Vector3(0.04f, -0.30f, 0);
            var rFootSr = EnsureSpriteRenderer(rightFoot.gameObject, 3);
            rFootSr.sprite = GenerateProceduralPartSprite("Foot", bodyType);

            // 4. PIERNA DELANTERA (frente a la pelvis, sortingOrder 9, 10, 11)
            // Cadera -> Muslo Delantero
            if (leftLeg == null)
            {
                Transform ex = pelvis.Find("Thigh_Lead");
                if (ex != null) leftLeg = ex;
                else
                {
                    GameObject lThighObj = new GameObject("Thigh_Lead");
                    lThighObj.transform.SetParent(pelvis, false);
                    leftLeg = lThighObj.transform;
                }
            }
            leftLeg.localPosition = new Vector3(0.16f, -0.06f, 0);
            var lThighSr = EnsureSpriteRenderer(leftLeg.gameObject, 9);
            lThighSr.sprite = GenerateProceduralPartSprite("Thigh", bodyType);

            // Rodilla -> Pantorrilla Delantera
            if (leftCalf == null)
            {
                Transform ex = leftLeg.Find("Calf_Lead");
                if (ex != null) leftCalf = ex;
                else
                {
                    GameObject lCalfObj = new GameObject("Calf_Lead");
                    lCalfObj.transform.SetParent(leftLeg, false);
                    leftCalf = lCalfObj.transform;
                }
            }
            leftCalf.localPosition = new Vector3(0f, -0.32f, 0);
            var lCalfSr = EnsureSpriteRenderer(leftCalf.gameObject, 10);
            lCalfSr.sprite = GenerateProceduralPartSprite("Calf", bodyType);

            // Tobillo -> Bota Delantera
            if (leftFoot == null)
            {
                Transform ex = leftCalf.Find("Foot_Lead");
                if (ex != null) leftFoot = ex;
                else
                {
                    GameObject lFootObj = new GameObject("Foot_Lead");
                    lFootObj.transform.SetParent(leftCalf, false);
                    leftFoot = lFootObj.transform;
                }
            }
            leftFoot.localPosition = new Vector3(0.04f, -0.30f, 0);
            var lFootSr = EnsureSpriteRenderer(leftFoot.gameObject, 11);
            lFootSr.sprite = GenerateProceduralPartSprite("Foot", bodyType);
            articulatedKickFoot = leftFoot;
            articulatedKickSr = lFootSr;

            // 5. TORSO (Hijo de la Pelvis, superponiéndose suavemente en la cintura)
            if (torso == null)
            {
                Transform ex = pelvis.Find("Torso");
                if (ex != null) torso = ex;
                else
                {
                    GameObject tObj = new GameObject("Torso");
                    tObj.transform.SetParent(pelvis, false);
                    torso = tObj.transform;
                }
            }
            torso.localPosition = torsoBasePos;
            var torsoSr = EnsureSpriteRenderer(torso.gameObject, 7);
            torsoSr.sprite = GenerateProceduralPartSprite("Torso", bodyType);

            // 6. CUELLO Y CABEZA
            if (neckPoint == null)
            {
                Transform ex = torso.Find("NeckPoint");
                if (ex != null) neckPoint = ex;
                else
                {
                    GameObject neckObj = new GameObject("NeckPoint");
                    neckObj.transform.SetParent(torso, false);
                    neckPoint = neckObj.transform;
                }
            }

            if (bodyType == FighterBodyType.DosCabezas)
            {
                // RIG ESPECIAL DE DOS CABEZAS (😡 y 😱)
                neckPoint.localPosition = new Vector3(-0.22f, 0.62f, 0);
                neckPointLeft = neckPoint;
                var nSrL = EnsureSpriteRenderer(neckPoint.gameObject, 8);
                nSrL.sprite = GenerateProceduralPartSprite("Neck", bodyType);

                if (neckPointRight == null)
                {
                    Transform ex = torso.Find("NeckPointRight");
                    if (ex != null) neckPointRight = ex;
                    else
                    {
                        GameObject rNeck = new GameObject("NeckPointRight");
                        rNeck.transform.SetParent(torso, false);
                        neckPointRight = rNeck.transform;
                    }
                }
                neckPointRight.localPosition = new Vector3(0.22f, 0.62f, 0);
                var nSrR = EnsureSpriteRenderer(neckPointRight.gameObject, 8);
                nSrR.sprite = GenerateProceduralPartSprite("Neck", bodyType);
            }
            else
            {
                // RIG NORMAL (1 CUELLO CENTRAL)
                neckPoint.localPosition = new Vector3(0f, 0.65f, 0);
                var neckSr = EnsureSpriteRenderer(neckPoint.gameObject, 8);
                neckSr.sprite = GenerateProceduralPartSprite("Neck", bodyType);

                if (neckPointRight != null)
                {
                    Destroy(neckPointRight.gameObject);
                    neckPointRight = null;
                }
            }

            // 7. BRAZO TRASERO (detrás del torso, sortingOrder 3, 4, 5)
            // Hombro Trasero
            if (rightArm == null)
            {
                Transform ex = torso.Find("Shoulder_Rear");
                if (ex != null) rightArm = ex;
                else
                {
                    GameObject rShoulderObj = new GameObject("Shoulder_Rear");
                    rShoulderObj.transform.SetParent(torso, false);
                    rightArm = rShoulderObj.transform;
                }
            }
            rightArm.localPosition = new Vector3(-0.25f, 0.46f, 0);
            var rArmSr = EnsureSpriteRenderer(rightArm.gameObject, 3);
            rArmSr.sprite = GenerateProceduralPartSprite("UpperArm", bodyType);

            // Codo Trasero
            if (rightForearm == null)
            {
                Transform ex = rightArm.Find("Forearm_Rear");
                if (ex != null) rightForearm = ex;
                else
                {
                    GameObject rForearmObj = new GameObject("Forearm_Rear");
                    rForearmObj.transform.SetParent(rightArm, false);
                    rightForearm = rForearmObj.transform;
                }
            }
            rightForearm.localPosition = new Vector3(0f, -0.28f, 0);
            var rForearmSr = EnsureSpriteRenderer(rightForearm.gameObject, 4);
            rForearmSr.sprite = GenerateProceduralPartSprite("Forearm", bodyType);

            // Muñeca / Puño Trasero
            if (rightFist == null)
            {
                Transform ex = rightForearm.Find("Fist_Rear");
                if (ex != null) rightFist = ex;
                else
                {
                    GameObject rFistObj = new GameObject("Fist_Rear");
                    rFistObj.transform.SetParent(rightForearm, false);
                    rightFist = rFistObj.transform;
                }
            }
            rightFist.localPosition = new Vector3(0f, -0.24f, 0);
            var rFistSr = EnsureSpriteRenderer(rightFist.gameObject, 5);
            rFistSr.sprite = GenerateProceduralPartSprite("Hand", bodyType);

            // 8. BRAZO DELANTERO (al frente en guardia atlética, sortingOrder 13, 14, 15)
            // Hombro Delantero
            if (leftArm == null)
            {
                Transform ex = torso.Find("Shoulder_Lead");
                if (ex != null) leftArm = ex;
                else
                {
                    GameObject lShoulderObj = new GameObject("Shoulder_Lead");
                    lShoulderObj.transform.SetParent(torso, false);
                    leftArm = lShoulderObj.transform;
                }
            }
            leftArm.localPosition = new Vector3(0.25f, 0.46f, 0);
            var lArmSr = EnsureSpriteRenderer(leftArm.gameObject, 13);
            lArmSr.sprite = GenerateProceduralPartSprite("UpperArm", bodyType);

            // Codo Delantero
            if (leftForearm == null)
            {
                Transform ex = leftArm.Find("Forearm_Lead");
                if (ex != null) leftForearm = ex;
                else
                {
                    GameObject lForearmObj = new GameObject("Forearm_Lead");
                    lForearmObj.transform.SetParent(leftArm, false);
                    leftForearm = lForearmObj.transform;
                }
            }
            leftForearm.localPosition = new Vector3(0f, -0.28f, 0);
            var lForearmSr = EnsureSpriteRenderer(leftForearm.gameObject, 14);
            lForearmSr.sprite = GenerateProceduralPartSprite("Forearm", bodyType);

            // Muñeca / Puño Delantero
            if (leftFist == null)
            {
                Transform ex = leftForearm.Find("Fist_Lead");
                if (ex != null) leftFist = ex;
                else
                {
                    GameObject lFistObj = new GameObject("Fist_Lead");
                    lFistObj.transform.SetParent(leftForearm, false);
                    leftFist = lFistObj.transform;
                }
            }
            leftFist.localPosition = new Vector3(0f, -0.24f, 0);
            var lFistSr = EnsureSpriteRenderer(leftFist.gameObject, 15);
            lFistSr.sprite = GenerateProceduralPartSprite("Hand", bodyType);
            articulatedPunchFist = leftFist;
            articulatedPunchSr = lFistSr;

            // 9. ESTELAS DE VELOCIDAD
            if (punchTrailObj == null && leftFist != null)
            {
                GameObject trail = new GameObject("PunchTrail");
                trail.transform.SetParent(leftFist, false);
                trail.transform.localPosition = new Vector3(-0.35f, 0, 0);
                var sr = trail.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSpeedTrailSprite(90, 45);
                sr.sortingOrder = 16;
                punchTrailObj = trail;
            }
            if (punchTrailObj != null) punchTrailObj.SetActive(false);

            if (kickTrailObj == null && leftCalf != null)
            {
                GameObject kTrail = new GameObject("KickTrail");
                kTrail.transform.SetParent(leftCalf, false);
                kTrail.transform.localPosition = new Vector3(-0.30f, -0.15f, 0);
                var sr = kTrail.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSpeedTrailSprite(100, 50);
                sr.sortingOrder = 16;
                kickTrailObj = kTrail;
            }
            if (kickTrailObj != null) kickTrailObj.SetActive(false);

            // Establecer pose de guardia inicial
            ResetJointsToStance();
        }

        private SpriteRenderer EnsureSpriteRenderer(GameObject go, int sortingOrder)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        // ----------------- ANIMACIÓN ARTICULADA CONTINUA EN UPDATE -----------------

        public void UpdateAnimation(float moveInput, bool isGrounded)
        {
            if (isKO || isAttacking || isHurt) return;

            if (!isGrounded)
            {
                // Pose aérea / Salto atlético
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, 32f);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, -38f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, -24f);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(0, 0, 35f);
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, 45f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -30f);
                return;
            }

            if (Mathf.Abs(moveInput) > 0.1f)
            {
                // ZANCADA DE CAMINATA: oscilación coordinada de piernas y brazos con rebote de pelvis
                walkCycle += Time.deltaTime * 14f;
                float stride = Mathf.Sin(walkCycle) * 26f;
                float hipBob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.04f;

                if (pelvis != null) pelvis.localPosition = pelvisBasePos + new Vector3(0, hipBob, 0);
                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(walkCycle) * 3f);

                // Piernas
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, stride);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-stride * 0.85f, 0f, 35f));

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, -stride);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(stride * 0.85f, 0f, 35f));

                // Brazos en contra-fase natural
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, 20f - stride * 0.6f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0, 0, 65f);

                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -14f + stride * 0.6f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(0, 0, 72f);
            }
            else
            {
                // GUARDIA ACTIVA EN REPOSO (Idle Breathing): respiración profunda y balanceo atlético
                breatheCycle += Time.deltaTime * 3.2f;
                float breath = Mathf.Sin(breatheCycle);
                float slowSway = Mathf.Sin(breatheCycle * 0.5f);

                if (pelvis != null) pelvis.localPosition = pelvisBasePos + new Vector3(0, breath * 0.025f, 0);
                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, slowSway * 1.5f);

                if (neckPoint != null)
                {
                    neckPoint.localRotation = Quaternion.Euler(0, 0, slowSway * 1.8f);
                }
                if (neckPointRight != null)
                {
                    neckPointRight.localRotation = Quaternion.Euler(0, 0, -slowSway * 1.8f);
                }

                // Guardia delantera cubriendo el mentón
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, 22f + breath * 3.5f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0, 0, 68f + breath * 4.5f);

                // Guardia trasera cubriendo la mandíbula
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -14f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(0, 0, 75f);

                // Piernas firmes sobre la lona
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, -8f + breath * 1.2f);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, 14f - breath * 1.2f);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, 12f - breath * 1.2f);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(0, 0, -10f + breath * 1.2f);
            }
        }

        // ----------------- PUÑETAZO CON EXTENSIÓN COMPLETA DE ARTICULACIONES -----------------

        public void PlayPunchAnimation(float duration)
        {
            PlayPunchAnimation(duration * 0.25f, duration * 0.38f, duration * 0.37f);
        }

        public void PlayPunchAnimation(float startup, float active, float recovery)
        {
            if (isKO) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(PunchArticulatedRoutine(startup, active, recovery));
        }

        private IEnumerator PunchArticulatedRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;

            // 1. STARTUP: Carga del golpe, retroceso de hombro y flexión cerrada del codo
            float t = 0f;
            while (t < startup)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / startup);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(0, 0, -14f), progress);
                if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(leadShoulderBaseRot, Quaternion.Euler(0, 0, -28f), progress);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Lerp(leadForearmBaseRot, Quaternion.Euler(0, 0, 105f), progress);
                yield return null;
            }

            // 2. ACTIVE: Impacto y extensión explosiva del brazo con estela de velocidad
            if (punchTrailObj != null) punchTrailObj.SetActive(true);

            t = 0f;
            while (t < active)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / active);

                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-14f, 22f, progress));
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-28f, 48f, progress));
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(105f, 0f, progress));
                yield return null;
            }

            // 3. RECOVERY: Retracción suave a la pose de guardia
            if (punchTrailObj != null) punchTrailObj.SetActive(false);

            t = 0f;
            while (t < recovery)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / recovery);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 22f), Quaternion.identity, progress);
                if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 48f), leadShoulderBaseRot, progress);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 0f), leadForearmBaseRot, progress);
                yield return null;
            }

            ResetJointsToStance();
            isAttacking = false;
            activeAttackRoutine = null;
        }

        // ----------------- PATADA CON EXTENSIÓN DE RODILLA Y CADERA -----------------

        public void PlayKickAnimation(float duration)
        {
            PlayKickAnimation(duration * 0.25f, duration * 0.38f, duration * 0.37f);
        }

        public void PlayKickAnimation(float startup, float active, float recovery)
        {
            if (isKO) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(KickArticulatedRoutine(startup, active, recovery));
        }

        private IEnumerator KickArticulatedRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;

            // 1. STARTUP: El torso se inclina para equilibrar y la cadera sube con la rodilla doblada
            float t = 0f;
            while (t < startup)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / startup);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(0, 0, -25f), progress);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(leadThighBaseRot, Quaternion.Euler(0, 0, 60f), progress);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Lerp(leadCalfBaseRot, Quaternion.Euler(0, 0, -55f), progress);
                yield return null;
            }

            // 2. ACTIVE: Proyección de la patada y latigazo de la rodilla
            if (kickTrailObj != null) kickTrailObj.SetActive(true);

            t = 0f;
            while (t < active)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / active);

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(60f, 45f, progress));
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-55f, 35f, progress));
                yield return null;
            }

            // 3. RECOVERY: Bajar la pierna a la lona
            if (kickTrailObj != null) kickTrailObj.SetActive(false);

            t = 0f;
            while (t < recovery)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / recovery);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -25f), Quaternion.identity, progress);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 45f), leadThighBaseRot, progress);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 35f), leadCalfBaseRot, progress);
                yield return null;
            }

            ResetJointsToStance();
            isAttacking = false;
            activeAttackRoutine = null;
        }

        // ----------------- REACCIÓN DE DAÑO (HURT / HITSTUN) -----------------

        public void PlayHurtAnimation(float duration)
        {
            if (isKO) return;
            if (activeHurtRoutine != null) StopCoroutine(activeHurtRoutine);
            activeHurtRoutine = StartCoroutine(HurtRoutine(duration));
        }

        private IEnumerator HurtRoutine(float duration)
        {
            isHurt = true;

            // Flash de impacto rojo en todos los renderizadores
            ApplyDamageFlash(new Color(1f, 0.3f, 0.3f));

            // Sacudida violenta hacia atrás
            if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, -28f);
            if (neckPoint != null) neckPoint.localRotation = Quaternion.Euler(0, 0, -24f);
            if (neckPointRight != null) neckPointRight.localRotation = Quaternion.Euler(0, 0, 24f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, -36f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -32f);

            yield return new WaitForSeconds(duration * 0.65f);

            // Retorno gradual de color y pose
            RestoreOriginalColors();

            float t = 0f;
            float returnTime = duration * 0.35f;
            while (t < returnTime)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / returnTime);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -28f), Quaternion.identity, progress);
                if (neckPoint != null) neckPoint.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -24f), Quaternion.identity, progress);
                if (neckPointRight != null) neckPointRight.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 24f), Quaternion.identity, progress);
                if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -36f), leadShoulderBaseRot, progress);
                if (rightArm != null) rightArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -32f), rearShoulderBaseRot, progress);
                yield return null;
            }

            ResetJointsToStance();
            isHurt = false;
            activeHurtRoutine = null;
        }

        // ----------------- CAÍDA POR KNOCKOUT (K.O.) EN EL CUADRILÁTERO -----------------

        public void PlayKOFall()
        {
            isKO = true;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            if (activeHurtRoutine != null) StopCoroutine(activeHurtRoutine);
            if (punchTrailObj != null) punchTrailObj.SetActive(false);
            if (kickTrailObj != null) kickTrailObj.SetActive(false);

            StartCoroutine(KORagdollRoutine());
        }

        public void PlayKODefeatedAnimation()
        {
            PlayKOFall();
        }

        private IEnumerator KORagdollRoutine()
        {
            float duration = 0.85f;
            float t = 0f;

            Vector3 startPelvis = pelvis != null ? pelvis.localPosition : pelvisBasePos;
            Vector3 koPelvisPos = new Vector3(0, 0.24f, 0); // La pelvis colapsa sobre la lona

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float ease = Mathf.SmoothStep(0f, 1f, p);

                if (pelvis != null) pelvis.localPosition = Vector3.Lerp(startPelvis, koPelvisPos, ease);

                // El torso cae colapsado hacia atrás (-75°)
                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -75f, ease));

                // Cuello y cabeza reclinados contra la lona
                if (neckPoint != null) neckPoint.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -35f, ease));
                if (neckPointRight != null) neckPointRight.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -40f, ease));

                // Brazos caen inertes a los lados
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(22f, -85f, ease));
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(68f, 10f, ease));

                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-14f, 80f, ease));
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(75f, 15f, ease));

                // Piernas dobladas en el suelo
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-8f, 40f, ease));
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(14f, -70f, ease));

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(12f, -50f, ease));
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-10f, 65f, ease));

                yield return null;
            }
        }

        public void ResetJointsToStance()
        {
            if (isKO) return;

            if (pelvis != null) pelvis.localPosition = pelvisBasePos;
            if (torso != null) { torso.localPosition = torsoBasePos; torso.localRotation = Quaternion.identity; }
            if (neckPoint != null) neckPoint.localRotation = Quaternion.identity;
            if (neckPointRight != null) neckPointRight.localRotation = Quaternion.identity;

            if (leftArm != null) leftArm.localRotation = leadShoulderBaseRot;
            if (leftForearm != null) leftForearm.localRotation = leadForearmBaseRot;
            if (leftFist != null) leftFist.localRotation = Quaternion.identity;

            if (rightArm != null) rightArm.localRotation = rearShoulderBaseRot;
            if (rightForearm != null) rightForearm.localRotation = rearForearmBaseRot;
            if (rightFist != null) rightFist.localRotation = Quaternion.identity;

            if (leftLeg != null) leftLeg.localRotation = leadThighBaseRot;
            if (leftCalf != null) leftCalf.localRotation = leadCalfBaseRot;
            if (leftFoot != null) leftFoot.localRotation = Quaternion.identity;

            if (rightLeg != null) rightLeg.localRotation = rearThighBaseRot;
            if (rightCalf != null) rightCalf.localRotation = rearCalfBaseRot;
            if (rightFoot != null) rightFoot.localRotation = Quaternion.identity;
        }

        public void ResetBody()
        {
            isKO = false;
            isAttacking = false;
            isHurt = false;
            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeHurtRoutine != null) { StopCoroutine(activeHurtRoutine); activeHurtRoutine = null; }
            RestoreOriginalColors();
            ResetJointsToStance();
        }

        private void ApplyDamageFlash(Color flashColor)
        {
            if (allRenderers == null) return;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && (groundShadow == null || allRenderers[i].gameObject != groundShadow.gameObject))
                {
                    allRenderers[i].color = flashColor;
                }
            }
        }

        private void RestoreOriginalColors()
        {
            if (allRenderers == null || originalColors == null) return;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && i < originalColors.Length)
                {
                    allRenderers[i].color = originalColors[i];
                }
            }
        }

        // ----------------- SPRITES PROCEDURALES DE ALTA CALIDAD CON TAPAS DE UNIÓN SUPERPUESTAS -----------------

        private Sprite GenerateProceduralPartSprite(string partName, FighterBodyType bodyType)
        {
            // Paletas por personaje
            Color primaryCloth;
            Color secondaryCloth;
            Color skin;
            Color gloves;
            Color boots;

            switch (bodyType)
            {
                case FighterBodyType.Gordo:
                    primaryCloth = new Color(0.20f, 0.45f, 0.85f); // Singlet a rayas azul
                    secondaryCloth = new Color(0.95f, 0.95f, 0.98f); // Rayas blancas
                    skin = new Color(1f, 0.82f, 0.70f);
                    gloves = new Color(0.90f, 0.18f, 0.18f); // Guantes rojos
                    boots = new Color(0.35f, 0.22f, 0.14f); // Botas cuero marrón
                    break;
                case FighterBodyType.Flaco:
                    primaryCloth = new Color(0.18f, 0.50f, 0.28f); // Pantalón kung-fu verde
                    secondaryCloth = new Color(0.12f, 0.12f, 0.15f); // Cinturón negro
                    skin = new Color(0.98f, 0.80f, 0.65f);
                    gloves = new Color(0.18f, 0.18f, 0.20f); // Vendas negras
                    boots = new Color(0.15f, 0.15f, 0.18f); // Zapatillas kung-fu
                    break;
                case FighterBodyType.Musculoso:
                    primaryCloth = new Color(0.95f, 0.80f, 0.15f); // Shorts Muay Thai oro
                    secondaryCloth = new Color(0.85f, 0.15f, 0.15f); // Borde rojo
                    skin = new Color(0.92f, 0.72f, 0.55f); // Bronceado atlético
                    gloves = new Color(0.92f, 0.15f, 0.15f); // Guantes rojos pro
                    boots = new Color(0.85f, 0.20f, 0.20f); // Tobilleras rojas
                    break;
                case FighterBodyType.Mujer:
                    primaryCloth = new Color(0.15f, 0.15f, 0.18f); // Top negro
                    secondaryCloth = new Color(0.95f, 0.20f, 0.65f); // Franjas magenta
                    skin = new Color(1f, 0.85f, 0.74f);
                    gloves = new Color(0.95f, 0.22f, 0.65f); // Guantes magenta
                    boots = new Color(0.20f, 0.18f, 0.22f); // Botas combate
                    break;
                case FighterBodyType.Mujer2:
                    primaryCloth = new Color(0.85f, 0.15f, 0.22f); // Gi de combate rojo carmesí
                    secondaryCloth = new Color(0.98f, 0.82f, 0.15f); // Ribete y faja dorada
                    skin = new Color(1f, 0.86f, 0.76f);
                    gloves = new Color(0.15f, 0.15f, 0.18f); // Muñequeras negras de combate
                    boots = new Color(0.85f, 0.15f, 0.22f); // Zapatillas de combate rojas
                    break;
                case FighterBodyType.DosCabezas:
                default:
                    primaryCloth = new Color(0.25f, 0.26f, 0.30f); // Arnés combate carbón
                    secondaryCloth = new Color(0.85f, 0.70f, 0.25f); // Tachuelas doradas
                    skin = new Color(0.95f, 0.76f, 0.60f);
                    gloves = new Color(0.40f, 0.40f, 0.45f); // Guanteletes hierro
                    boots = new Color(0.15f, 0.15f, 0.18f); // Botas pesadas
                    break;
            }

            if (partName.Contains("Torso"))
            {
                int w = bodyType == FighterBodyType.DosCabezas ? 130 : (bodyType == FighterBodyType.Gordo ? 120 : 96);
                int h = 95;
                return CreateArticulatedTorso(w, h, bodyType, primaryCloth, secondaryCloth, skin);
            }
            if (partName.Contains("Pelvis"))
            {
                int w = bodyType == FighterBodyType.Gordo ? 110 : (bodyType == FighterBodyType.Musculoso ? 100 : 90);
                int h = 55;
                return CreateArticulatedPelvis(w, h, bodyType, primaryCloth, secondaryCloth);
            }
            if (partName.Contains("Neck"))
            {
                return CreateArticulatedNeck(32, 35, skin);
            }
            if (partName.Contains("UpperArm"))
            {
                int w = (bodyType == FighterBodyType.Gordo || bodyType == FighterBodyType.Musculoso) ? 54 : 44;
                int h = 65;
                return CreateArticulatedLimb(w, h, skin, true, false, Color.clear);
            }
            if (partName.Contains("Forearm"))
            {
                int w = (bodyType == FighterBodyType.Gordo || bodyType == FighterBodyType.Musculoso) ? 48 : 40;
                int h = 62;
                return CreateArticulatedLimb(w, h, skin, false, true, gloves);
            }
            if (partName.Contains("Hand"))
            {
                int s = (bodyType == FighterBodyType.Gordo || bodyType == FighterBodyType.Musculoso) ? 52 : 46;
                return CreateArticulatedGlove(s, gloves);
            }
            if (partName.Contains("Thigh"))
            {
                int w = (bodyType == FighterBodyType.Gordo || bodyType == FighterBodyType.Musculoso) ? 62 : 52;
                int h = 75;
                return CreateArticulatedThigh(w, h, bodyType, primaryCloth, skin);
            }
            if (partName.Contains("Calf"))
            {
                int w = (bodyType == FighterBodyType.Gordo || bodyType == FighterBodyType.Musculoso) ? 54 : 44;
                int h = 70;
                return CreateArticulatedCalf(w, h, skin, boots);
            }
            if (partName.Contains("Foot"))
            {
                return CreateArticulatedFoot(64, 38, boots);
            }

            return CreateOvalShadowSprite();
        }

        private Sprite CreateArticulatedTorso(int w, int h, FighterBodyType bodyType, Color primary, Color secondary, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Vector2 center = new Vector2(w / 2f, h * 0.45f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - center.x) / (w * 0.44f);
                    float dy = (y - center.y) / (h * 0.48f);
                    float dSq = (dx * dx) + (dy * dy);

                    if (dSq <= 1.0f)
                    {
                        if (dSq >= 0.88f)
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else
                        {
                            // Interior con diseño estilizado
                            Color col;
                            if (bodyType == FighterBodyType.Gordo)
                            {
                                // Overol con franjas verticales azules y blancas
                                bool isStripe = ((x / 14) % 2 == 0);
                                if (y > h * 0.68f) col = skin; // Pecho descubierto
                                else col = isStripe ? primary : secondary;
                            }
                            else if (bodyType == FighterBodyType.Flaco)
                            {
                                // Torso musculoso descubierto con abdominales y sombras
                                col = skin;
                                if (Mathf.Abs(x - center.x) < 2f && y < h * 0.65f) col = Color.Lerp(skin, outline, 0.25f); // Línea alba
                                else if (y > h * 0.50f && y < h * 0.53f && Mathf.Abs(dx) < 0.6f) col = Color.Lerp(skin, outline, 0.20f); // Pectoral inferior
                            }
                            else if (bodyType == FighterBodyType.Musculoso)
                            {
                                // Heavyweight musculoso con pectorales gigantes
                                col = skin;
                                if (Mathf.Abs(x - center.x) < 2.5f && y < h * 0.70f) col = Color.Lerp(skin, outline, 0.30f);
                                else if (y > h * 0.52f && y < h * 0.56f && Mathf.Abs(dx) < 0.7f) col = Color.Lerp(skin, outline, 0.25f);
                            }
                            else if (bodyType == FighterBodyType.Mujer)
                            {
                                // Top deportivo negro con ribete magenta
                                if (y > h * 0.45f && y < h * 0.82f) col = (y > h * 0.78f) ? secondary : primary;
                                else col = skin;
                            }
                            else if (bodyType == FighterBodyType.Mujer2)
                            {
                                // Gi de artes marciales / Kimono cruzado rojo carmesí con faja dorada
                                if (y > h * 0.35f && y < h * 0.88f)
                                {
                                    bool isSash = (y < h * 0.48f);
                                    col = isSash ? secondary : primary;
                                }
                                else col = skin;
                            }
                            else // Dos Cabezas
                            {
                                // Arnés táctico con correas cruzadas
                                bool isHarness = (Mathf.Abs((x - center.x) - (y - center.y)) < 6 || Mathf.Abs((x - center.x) + (y - center.y)) < 6);
                                col = isHarness ? primary : skin;
                            }

                            // Sombreado de borde sutil
                            col = Color.Lerp(col, outline, dSq * 0.20f);
                            tex.SetPixel(x, y, col);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            // Pivote en (0.5, 0.15) para descansar perfectamente superpuesto dentro de la pelvis
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 100f);
        }

        private Sprite CreateArticulatedPelvis(int w, int h, FighterBodyType bodyType, Color primary, Color secondary)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Vector2 c = new Vector2(w / 2f, h / 2f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.45f);
                    float dy = (y - c.y) / (h * 0.44f);
                    float dSq = (dx * dx) + (dy * dy);

                    if (dSq <= 1.0f)
                    {
                        if (dSq >= 0.86f)
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else
                        {
                            Color col;
                            if (bodyType == FighterBodyType.Gordo)
                            {
                                bool isStripe = ((x / 14) % 2 == 0);
                                col = isStripe ? primary : secondary;
                            }
                            else if (bodyType == FighterBodyType.Flaco)
                            {
                                col = (y > h * 0.70f) ? secondary : primary; // Cinturón negro
                            }
                            else if (bodyType == FighterBodyType.Musculoso)
                            {
                                col = (y > h * 0.72f) ? new Color(0.12f, 0.12f, 0.15f) : primary; // Cintura negra de shorts dorados
                            }
                            else
                            {
                                col = (y > h * 0.72f) ? secondary : primary;
                            }
                            tex.SetPixel(x, y, col);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.50f), 100f);
        }

        private Sprite CreateArticulatedNeck(int w, int h, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Vector2 c = new Vector2(w / 2f, h / 2f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.38f);
                    float dy = (y - c.y) / (h * 0.46f);
                    float dSq = (dx * dx) + (dy * dy);

                    if (dSq <= 1.0f)
                    {
                        if (dSq >= 0.84f) tex.SetPixel(x, y, outline);
                        else tex.SetPixel(x, y, skin);
                    }
                    else tex.SetPixel(x, y, clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.20f), 100f);
        }

        private Sprite CreateArticulatedLimb(int w, int h, Color skin, bool isUpper, bool hasWraps, Color wrapColor)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);

            // Centros de las dos tapas circulares superpuestas (ball-joints)
            Vector2 topJoint = new Vector2(w / 2f, h * 0.85f);
            Vector2 bottomJoint = new Vector2(w / 2f, h * 0.15f);
            float topRadius = w * 0.42f;
            float bottomRadius = w * 0.38f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01((y - bottomJoint.y) / (topJoint.y - bottomJoint.y));
                    float currRadius = Mathf.Lerp(bottomRadius, topRadius, t);
                    float distCenter = Mathf.Abs(x - w / 2f);

                    bool inside = false;
                    bool edge = false;

                    // Comprobar tapa superior
                    if (y >= topJoint.y)
                    {
                        float d = Vector2.Distance(p, topJoint);
                        if (d <= topRadius)
                        {
                            inside = true;
                            if (d >= topRadius - 2.5f) edge = true;
                        }
                    }
                    // Comprobar tapa inferior
                    else if (y <= bottomJoint.y)
                    {
                        float d = Vector2.Distance(p, bottomJoint);
                        if (d <= bottomRadius)
                        {
                            inside = true;
                            if (d >= bottomRadius - 2.5f) edge = true;
                        }
                    }
                    // Cuerpo cilíndrico de unión
                    else
                    {
                        if (distCenter <= currRadius)
                        {
                            inside = true;
                            if (distCenter >= currRadius - 2.5f) edge = true;
                        }
                    }

                    if (inside)
                    {
                        if (edge)
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else
                        {
                            Color c = skin;
                            if (hasWraps && y < h * 0.45f)
                            {
                                c = wrapColor; // Vendas / cintas en el antebrazo
                            }
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            // Pivote exacto en la tapa circular superior (0.5, 0.85)
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.85f), 100f);
        }

        private Sprite CreateArticulatedGlove(int size, Color gloveCol)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color highlight = Color.Lerp(gloveCol, Color.white, 0.45f);

            Vector2 c = new Vector2(size / 2f, size * 0.50f);
            float radius = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= radius)
                    {
                        if (d >= radius - 2.6f) tex.SetPixel(x, y, outline);
                        else if (Vector2.Distance(new Vector2(x, y), c + new Vector2(-4, 5)) < radius * 0.35f)
                            tex.SetPixel(x, y, highlight);
                        else tex.SetPixel(x, y, gloveCol);
                    }
                    else tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.65f), 100f);
        }

        private Sprite CreateArticulatedThigh(int w, int h, FighterBodyType bodyType, Color cloth, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);

            Vector2 topJoint = new Vector2(w / 2f, h * 0.85f);
            Vector2 bottomJoint = new Vector2(w / 2f, h * 0.15f);
            float topRadius = w * 0.44f;
            float bottomRadius = w * 0.40f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01((y - bottomJoint.y) / (topJoint.y - bottomJoint.y));
                    float currRadius = Mathf.Lerp(bottomRadius, topRadius, t);
                    float distCenter = Mathf.Abs(x - w / 2f);

                    bool inside = false;
                    bool edge = false;

                    if (y >= topJoint.y)
                    {
                        float d = Vector2.Distance(p, topJoint);
                        if (d <= topRadius) { inside = true; if (d >= topRadius - 2.5f) edge = true; }
                    }
                    else if (y <= bottomJoint.y)
                    {
                        float d = Vector2.Distance(p, bottomJoint);
                        if (d <= bottomRadius) { inside = true; if (d >= bottomRadius - 2.5f) edge = true; }
                    }
                    else
                    {
                        if (distCenter <= currRadius) { inside = true; if (distCenter >= currRadius - 2.5f) edge = true; }
                    }

                    if (inside)
                    {
                        if (edge) tex.SetPixel(x, y, outline);
                        else
                        {
                            Color c;
                            // En Flaco y Mujer2 el pantalón cubre toda la pierna
                            if (bodyType == FighterBodyType.Flaco || bodyType == FighterBodyType.Mujer2) c = cloth;
                            // En Gordo / Musculoso / Mujer, parte superior son los shorts y luego pierna
                            else c = (y > h * 0.45f) ? cloth : skin;
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.85f), 100f);
        }

        private Sprite CreateArticulatedCalf(int w, int h, Color skin, Color bootCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);

            Vector2 topJoint = new Vector2(w / 2f, h * 0.85f);
            Vector2 bottomJoint = new Vector2(w / 2f, h * 0.15f);
            float topRadius = w * 0.42f;
            float bottomRadius = w * 0.38f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float t = Mathf.Clamp01((y - bottomJoint.y) / (topJoint.y - bottomJoint.y));
                    float currRadius = Mathf.Lerp(bottomRadius, topRadius, t);
                    float distCenter = Mathf.Abs(x - w / 2f);

                    bool inside = false;
                    bool edge = false;

                    if (y >= topJoint.y)
                    {
                        float d = Vector2.Distance(p, topJoint);
                        if (d <= topRadius) { inside = true; if (d >= topRadius - 2.5f) edge = true; }
                    }
                    else if (y <= bottomJoint.y)
                    {
                        float d = Vector2.Distance(p, bottomJoint);
                        if (d <= bottomRadius) { inside = true; if (d >= bottomRadius - 2.5f) edge = true; }
                    }
                    else
                    {
                        if (distCenter <= currRadius) { inside = true; if (distCenter >= currRadius - 2.5f) edge = true; }
                    }

                    if (inside)
                    {
                        if (edge) tex.SetPixel(x, y, outline);
                        else
                        {
                            Color c = (y < h * 0.65f) ? bootCol : skin; // Caña de la bota
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.85f), 100f);
        }

        private Sprite CreateArticulatedFoot(int w, int h, Color bootCol)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color soleCol = new Color(0.9f, 0.9f, 0.9f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Forma anatómica de zapato/bota de boxeo apoyada en la lona
                    float nx = (float)x / w;
                    float ny = (float)y / h;

                    bool inside = false;
                    bool edge = false;

                    if (ny < 0.20f && nx >= 0.15f && nx <= 0.95f)
                    {
                        inside = true; // Suela
                    }
                    else if (nx >= 0.20f && nx <= 0.55f && ny < 0.85f)
                    {
                        inside = true; // Empeine / Tobillo
                    }
                    else if (nx > 0.55f && nx < 0.92f && ny < 0.55f)
                    {
                        inside = true; // Punta del pie
                    }

                    if (inside)
                    {
                        if (x <= 13 || x >= w - 3 || y <= 1 || y >= h - 3) edge = true;
                        if (edge) tex.SetPixel(x, y, outline);
                        else tex.SetPixel(x, y, (ny < 0.22f) ? soleCol : bootCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.35f, 0.72f), 100f);
        }

        private Sprite CreateOvalShadowSprite()
        {
            int w = 80, h = 30;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color shadowCol = new Color(0, 0, 0, 0.45f);

            Vector2 center = new Vector2(w / 2f, h / 2f);
            float rx = w * 0.46f;
            float ry = h * 0.44f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - center.x) / rx;
                    float dy = (y - center.y) / ry;
                    float dSq = (dx * dx) + (dy * dy);
                    if (dSq <= 1f)
                    {
                        Color c = shadowCol;
                        c.a = Mathf.Lerp(0.45f, 0f, dSq);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateSpeedTrailSprite(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color yellow = new Color(1f, 0.88f, 0.2f, 0.85f);
            Color white = new Color(1f, 1f, 1f, 0.95f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float tX = (float)x / w;
                    float dY = Mathf.Abs(y - h / 2f) / (h / 2f);

                    if (dY < (1f - (1f - tX) * 0.5f))
                    {
                        Color c = Color.Lerp(yellow, white, tX);
                        c.a = Mathf.Lerp(0f, 0.85f, tX);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.85f, 0.5f), 100f);
        }
    }
}
