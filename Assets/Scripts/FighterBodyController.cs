using System.Collections;
using System.IO;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Controlador del cuerpo articulado 2D del luchador ("DECONSTRUCTED FIGHTER"):
    /// - Esqueleto jerárquico modular: Pelvis -> Torso -> Cuello/Cabeza, Hombros -> Codos -> Puños, Caderas -> Rodillas -> Botas.
    /// - Soporte para sprites modulares de alta resolución (El Gordo, El Flaco, etc.) y texturas estilizadas.
    /// - Animaciones procedurales vivas en tiempo real: Respiración con guardia atlética, Zancada de caminata,
    ///   Puñetazo con extensión completa de codo y hombro, Patada con chamber y estiramiento de rodilla,
    ///   Reacción de dolor con sacudida y K.O. ragdoll contra la lona del MGM Grand.
    /// </summary>
    public class FighterBodyController : MonoBehaviour
    {
        [Header("Núcleo y Columna")]
        public Transform pelvis;
        public Transform torso;
        public Transform neckPoint;
        public Transform neckPointLeft;
        public Transform neckPointRight;

        [Header("Brazos Articulados")]
        public Transform leftArm;       // Hombro delantero / Lead Shoulder
        public Transform leftForearm;   // Codo / Antebrazo delantero
        public Transform leftFist;      // Muñeca / Puño delantero
        public Transform rightArm;      // Hombro trasero / Rear Shoulder
        public Transform rightForearm;  // Codo / Antebrazo trasero
        public Transform rightFist;     // Muñeca / Puño trasero

        [Header("Piernas Articuladas")]
        public Transform leftLeg;       // Cadera / Muslo delantero
        public Transform leftCalf;      // Rodilla / Pantorrilla delantera
        public Transform leftFoot;      // Tobillo / Bota delantera
        public Transform rightLeg;      // Cadera / Muslo trasero
        public Transform rightCalf;     // Rodilla / Pantorrilla trasera
        public Transform rightFoot;     // Tobillo / Bota trasera

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
        private bool isAttacking = false;
        private bool isKO = false;
        private bool isHurt = false;

        // Poses base locales para interpolación
        private Vector3 pelvisBasePos = new Vector3(0, 0.72f, 0);
        private Vector3 torsoBasePos = new Vector3(0, 0.35f, 0);
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
        /// Aplica y reconstruye el esqueleto articulado con las partes de El Gordo, El Flaco o personajes modulares.
        /// </summary>
        public void SetClassicBody(FighterBodyType bodyType)
        {
            currentBodyType = bodyType;
            BuildArticulatedHierarchy(bodyType);
            CacheRenderers();
        }

        /// <summary>
        /// Construye el esqueleto 2D jerárquico exacto según el esquema modular enviado por el usuario.
        /// </summary>
        public void BuildArticulatedHierarchy(FighterBodyType bodyType)
        {
            // Eliminar cualquier quad monolítico antiguo "ClassicBodySprite"
            Transform oldSprite = transform.Find("ClassicBodySprite");
            if (oldSprite != null) Destroy(oldSprite.gameObject);

            // 1. Sombra en la lona del ring (pies en y = 0)
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

            // 2. Pelvis Núcleo (Raíz móvil del cuerpo)
            if (pelvis == null)
            {
                Transform exPelvis = transform.Find("Pelvis_Core");
                if (exPelvis != null) pelvis = exPelvis;
                else
                {
                    GameObject pObj = new GameObject("Pelvis_Core");
                    pObj.transform.SetParent(transform, false);
                    pObj.transform.localPosition = pelvisBasePos;
                    pelvis = pObj.transform;
                }
            }
            pelvis.localPosition = pelvisBasePos;
            var pelvisSr = EnsureSpriteRenderer(pelvis.gameObject, 5);
            pelvisSr.sprite = LoadOrGeneratePart(bodyType, "Pelvis", 0.40f);

            // 3. Piernas (Caderas -> Rodillas -> Pies)
            // Pierna Trasera (detrás del cuerpo, sortingOrder 2)
            if (rightLeg == null)
            {
                GameObject rThighObj = new GameObject("Thigh_Rear");
                rThighObj.transform.SetParent(pelvis, false);
                rThighObj.transform.localPosition = new Vector3(-0.16f, -0.08f, 0);
                rightLeg = rThighObj.transform;
            }
            var rThighSr = EnsureSpriteRenderer(rightLeg.gameObject, 2);
            rThighSr.sprite = LoadOrGeneratePart(bodyType, "Leg_Thigh_R", 0.50f);

            if (rightCalf == null)
            {
                GameObject rCalfObj = new GameObject("Calf_Rear");
                rCalfObj.transform.SetParent(rightLeg, false);
                rCalfObj.transform.localPosition = new Vector3(-0.02f, -0.32f, 0);
                rightCalf = rCalfObj.transform;
            }
            var rCalfSr = EnsureSpriteRenderer(rightCalf.gameObject, 2);
            rCalfSr.sprite = LoadOrGeneratePart(bodyType, "Leg_Calf_R", 0.45f);

            // Pierna Delantera (frente a la pelvis, sortingOrder 6)
            if (leftLeg == null)
            {
                GameObject lThighObj = new GameObject("Thigh_Lead");
                lThighObj.transform.SetParent(pelvis, false);
                lThighObj.transform.localPosition = new Vector3(0.16f, -0.08f, 0);
                leftLeg = lThighObj.transform;
            }
            var lThighSr = EnsureSpriteRenderer(leftLeg.gameObject, 6);
            lThighSr.sprite = LoadOrGeneratePart(bodyType, "Leg_Thigh_L", 0.50f);

            if (leftCalf == null)
            {
                GameObject lCalfObj = new GameObject("Calf_Lead");
                lCalfObj.transform.SetParent(leftLeg, false);
                lCalfObj.transform.localPosition = new Vector3(0.02f, -0.32f, 0);
                leftCalf = lCalfObj.transform;
            }
            var lCalfSr = EnsureSpriteRenderer(leftCalf.gameObject, 6);
            lCalfSr.sprite = LoadOrGeneratePart(bodyType, "Leg_Calf_L", 0.45f);

            // 4. Torso (Unido a la Pelvis)
            if (torso == null)
            {
                GameObject tObj = new GameObject("Torso");
                tObj.transform.SetParent(pelvis, false);
                tObj.transform.localPosition = torsoBasePos;
                torso = tObj.transform;
            }
            torso.localPosition = torsoBasePos;
            var torsoSr = EnsureSpriteRenderer(torso.gameObject, 7);
            torsoSr.sprite = LoadOrGeneratePart(bodyType, "Torso", 0.82f);

            // 5. Cuello / NeckPoint (En la apertura superior del cuello del torso)
            if (neckPoint == null)
            {
                GameObject neckObj = new GameObject("NeckPoint");
                neckObj.transform.SetParent(torso, false);
                neckPoint = neckObj.transform;
            }
            // Altura del cuello directamente sobre el pecho
            neckPoint.localPosition = new Vector3(0f, 0.52f, 0);

            // Dos Cabezas compatibilidad
            if (bodyType == FighterBodyType.DosCabezas)
            {
                neckPoint.localPosition = new Vector3(-0.20f, 0.48f, 0);
                if (neckPointRight == null)
                {
                    GameObject rNeck = new GameObject("NeckPointRight");
                    rNeck.transform.SetParent(torso, false);
                    neckPointRight = rNeck.transform;
                }
                neckPointRight.localPosition = new Vector3(0.20f, 0.48f, 0);
            }

            // 6. Brazo Trasero (Hombro -> Codo -> Mano, detrás del torso, sortingOrder 3-4)
            if (rightArm == null)
            {
                GameObject rShoulderObj = new GameObject("Shoulder_Rear");
                rShoulderObj.transform.SetParent(torso, false);
                rShoulderObj.transform.localPosition = new Vector3(-0.20f, 0.28f, 0);
                rightArm = rShoulderObj.transform;
            }
            var rArmSr = EnsureSpriteRenderer(rightArm.gameObject, 3);
            rArmSr.sprite = LoadOrGeneratePart(bodyType, "Arm_Upper_R", 0.45f);

            if (rightForearm == null)
            {
                GameObject rForearmObj = new GameObject("Forearm_Rear");
                rForearmObj.transform.SetParent(rightArm, false);
                rForearmObj.transform.localPosition = new Vector3(-0.10f, -0.26f, 0);
                rightForearm = rForearmObj.transform;
            }
            var rForearmSr = EnsureSpriteRenderer(rightForearm.gameObject, 4);
            rForearmSr.sprite = LoadOrGeneratePart(bodyType, "Arm_Forearm_R", 0.40f);

            if (rightFist == null)
            {
                GameObject rFistObj = new GameObject("Fist_Rear");
                rFistObj.transform.SetParent(rightForearm, false);
                rFistObj.transform.localPosition = new Vector3(-0.06f, -0.22f, 0);
                rightFist = rFistObj.transform;
            }
            var rFistSr = EnsureSpriteRenderer(rightFist.gameObject, 4);
            rFistSr.sprite = LoadOrGeneratePart(bodyType, "Arm_Hand_R", 0.28f);

            // 7. Brazo Delantero (Hombro -> Codo -> Puño, al frente en guardia, sortingOrder 14-16)
            if (leftArm == null)
            {
                GameObject lShoulderObj = new GameObject("Shoulder_Lead");
                lShoulderObj.transform.SetParent(torso, false);
                lShoulderObj.transform.localPosition = new Vector3(0.22f, 0.28f, 0);
                leftArm = lShoulderObj.transform;
            }
            var lArmSr = EnsureSpriteRenderer(leftArm.gameObject, 14);
            lArmSr.sprite = LoadOrGeneratePart(bodyType, "Arm_Upper_L", 0.45f);

            if (leftForearm == null)
            {
                GameObject lForearmObj = new GameObject("Forearm_Lead");
                lForearmObj.transform.SetParent(leftArm, false);
                lForearmObj.transform.localPosition = new Vector3(0.12f, -0.26f, 0);
                leftForearm = lForearmObj.transform;
            }
            var lForearmSr = EnsureSpriteRenderer(leftForearm.gameObject, 15);
            lForearmSr.sprite = LoadOrGeneratePart(bodyType, "Arm_Forearm_L", 0.40f);

            if (leftFist == null)
            {
                GameObject lFistObj = new GameObject("Fist_Lead");
                lFistObj.transform.SetParent(leftForearm, false);
                lFistObj.transform.localPosition = new Vector3(0.08f, -0.22f, 0);
                leftFist = lFistObj.transform;
            }
            var lFistSr = EnsureSpriteRenderer(leftFist.gameObject, 16);
            lFistSr.sprite = LoadOrGeneratePart(bodyType, "Arm_Hand_L", 0.28f);
            articulatedPunchFist = leftFist;
            articulatedPunchSr = lFistSr;

            // 8. Estela de Velocidad (Whoosh Trail) en el puño delantero
            if (punchTrailObj == null && leftFist != null)
            {
                GameObject trail = new GameObject("PunchTrail");
                trail.transform.SetParent(leftFist, false);
                trail.transform.localPosition = new Vector3(-0.35f, 0, 0);
                var sr = trail.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSpeedTrailSprite(90, 45);
                sr.sortingOrder = 17;
                punchTrailObj = trail;
            }
            if (punchTrailObj != null) punchTrailObj.SetActive(false);

            // 9. Estela de Patada
            if (kickTrailObj == null && leftCalf != null)
            {
                GameObject kTrail = new GameObject("KickTrail");
                kTrail.transform.SetParent(leftCalf, false);
                kTrail.transform.localPosition = new Vector3(-0.30f, -0.15f, 0);
                var sr = kTrail.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSpeedTrailSprite(100, 50);
                sr.sortingOrder = 17;
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

        private Sprite LoadOrGeneratePart(FighterBodyType bodyType, string partName, float desiredWorldHeight)
        {
            string charFolder = bodyType == FighterBodyType.Gordo ? "Gordo" : (bodyType == FighterBodyType.Flaco ? "Flaco" : "");
            Sprite modularSprite = null;

            if (!string.IsNullOrEmpty(charFolder))
            {
                modularSprite = FaceLoader.LoadModularPartSprite(charFolder, partName);
                if (modularSprite == null && partName.EndsWith("_L"))
                {
                    // Fallback a versión R si no existe L separada
                    string altPart = partName.Substring(0, partName.Length - 2) + "_R";
                    modularSprite = FaceLoader.LoadModularPartSprite(charFolder, altPart);
                }
            }

            if (modularSprite != null)
            {
                return modularSprite;
            }

            // Fallback procedimental con sombreado y colores profesionales
            return GenerateProceduralPartSprite(partName, bodyType);
        }

        private Sprite GenerateProceduralPartSprite(string partName, FighterBodyType bodyType)
        {
            int w = 64, h = 64;
            Color primary = suitColor;
            Color skin = skinColor;
            Color accent = gloveColor;

            if (partName.Contains("Torso"))
            {
                w = 90; h = 110;
                return CreateMuscularTorsoTexture(w, h, primary, skin);
            }
            if (partName.Contains("Pelvis"))
            {
                w = 80; h = 50;
                return CreatePelvisTexture(w, h, primary);
            }
            if (partName.Contains("Upper"))
            {
                w = 46; h = 60;
                return CreateLimbTexture(w, h, skin);
            }
            if (partName.Contains("Forearm"))
            {
                w = 42; h = 56;
                return CreateLimbTexture(w, h, skin);
            }
            if (partName.Contains("Hand"))
            {
                w = 48; h = 48;
                return CreateProBoxingGlove(w, accent);
            }
            if (partName.Contains("Thigh"))
            {
                w = 54; h = 68;
                return CreateThighTexture(w, h, primary, skin);
            }
            if (partName.Contains("Calf"))
            {
                w = 48; h = 64;
                return CreateCalfBootTexture(w, h, skin, bootColor);
            }

            return CreateLimbTexture(40, 40, skin);
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
                // Zancada de caminata: oscilación contraria de brazos y piernas con rebote de pelvis
                walkCycle += Time.deltaTime * 14f;
                float stride = Mathf.Sin(walkCycle) * 30f;
                float hipBob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.05f;

                if (pelvis != null) pelvis.localPosition = pelvisBasePos + new Vector3(0, hipBob, 0);
                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(walkCycle) * 3f);

                // Piernas
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, stride);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(-stride * 0.85f, 0f, 38f));

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, -stride);
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(stride * 0.85f, 0f, 38f));

                // Brazos en contra-fase natural
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, 20f - stride * 0.7f);
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0, 0, 65f);

                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -14f + stride * 0.7f);
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(0, 0, 72f);
            }
            else
            {
                // Idle de combate profesional: respiración con ritmo de boxeo y guardia alta
                walkCycle = 0f;
                float breath = Mathf.Sin(Time.time * 5.2f);
                float slowSway = Mathf.Sin(Time.time * 2.6f);

                if (pelvis != null) pelvis.localPosition = pelvisBasePos;
                if (torso != null)
                {
                    torso.localPosition = torsoBasePos + new Vector3(0, breath * 0.035f, 0);
                    torso.localRotation = Quaternion.Euler(0, 0, slowSway * 1.5f);
                }

                if (neckPoint != null)
                {
                    neckPoint.localRotation = Quaternion.Euler(0, 0, slowSway * 2.2f);
                }

                // Guardia delantera con rebote sutil
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

            // 1. STARTUP: Anticipación (carga del golpe, retroceso de hombro y flexión cerrada del codo)
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

                // El torso gira hacia el rival; el hombro se proyecta hacia adelante y el codo se extiende a 0°
                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-14f, 24f, progress));
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-28f, 50f, progress));
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

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 24f), Quaternion.identity, progress);
                if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 50f), leadShoulderBaseRot, progress);
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

            // 1. STARTUP: El torso se inclina hacia atrás para equilibrar y la cadera sube con la rodilla doblada
            float t = 0f;
            while (t < startup)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / startup);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(0, 0, -28f), progress);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(leadThighBaseRot, Quaternion.Euler(0, 0, 65f), progress);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Lerp(leadCalfBaseRot, Quaternion.Euler(0, 0, -60f), progress);
                yield return null;
            }

            // 2. ACTIVE: Proyección de la patada y latigazo de la rodilla
            if (kickTrailObj != null) kickTrailObj.SetActive(true);

            t = 0f;
            while (t < active)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / active);

                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(65f, 48f, progress));
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-60f, 40f, progress));
                yield return null;
            }

            // 3. RECOVERY: Bajar la pierna a la lona
            if (kickTrailObj != null) kickTrailObj.SetActive(false);

            t = 0f;
            while (t < recovery)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / recovery);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -28f), Quaternion.identity, progress);
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 48f), leadThighBaseRot, progress);
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 40f), leadCalfBaseRot, progress);
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
            if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, -32f);
            if (neckPoint != null) neckPoint.localRotation = Quaternion.Euler(0, 0, -26f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, -42f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -38f);

            yield return new WaitForSeconds(duration * 0.65f);

            // Retorno gradual de color y pose
            RestoreOriginalColors();

            float t = 0f;
            float returnTime = duration * 0.35f;
            while (t < returnTime)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / returnTime);

                if (torso != null) torso.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -32f), Quaternion.identity, progress);
                if (neckPoint != null) neckPoint.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -26f), Quaternion.identity, progress);
                if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -42f), leadShoulderBaseRot, progress);
                if (rightArm != null) rightArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -38f), rearShoulderBaseRot, progress);
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
            Vector3 koPelvisPos = new Vector3(0, 0.22f, 0); // La pelvis cae casi hasta la lona

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float ease = Mathf.SmoothStep(0f, 1f, p);

                if (pelvis != null) pelvis.localPosition = Vector3.Lerp(startPelvis, koPelvisPos, ease);

                // El torso cae colapsado hacia atrás (-75°)
                if (torso != null) torso.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -75f, ease));

                // La cabeza descansa en la lona
                if (neckPoint != null) neckPoint.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -40f, ease));

                // Piernas dobladas en la lona
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-8f, 55f, ease));
                if (leftCalf != null) leftCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(14f, -70f, ease));

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(12f, 40f, ease));
                if (rightCalf != null) rightCalf.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-10f, -65f, ease));

                // Brazos caídos flácidos
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(22f, -65f, ease));
                if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(68f, 20f, ease));

                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-14f, -80f, ease));
                if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(75f, 15f, ease));

                yield return null;
            }
        }

        public void ResetBody()
        {
            isKO = false;
            isAttacking = false;
            isHurt = false;
            ResetJointsToStance();
            RestoreOriginalColors();
        }

        private void ResetJointsToStance()
        {
            if (pelvis != null) pelvis.localPosition = pelvisBasePos;
            if (torso != null)
            {
                torso.localPosition = torsoBasePos;
                torso.localRotation = Quaternion.identity;
            }
            if (neckPoint != null) neckPoint.localRotation = Quaternion.identity;

            if (leftArm != null) leftArm.localRotation = leadShoulderBaseRot;
            if (leftForearm != null) leftForearm.localRotation = leadForearmBaseRot;
            if (rightArm != null) rightArm.localRotation = rearShoulderBaseRot;
            if (rightForearm != null) rightForearm.localRotation = rearForearmBaseRot;

            if (leftLeg != null) leftLeg.localRotation = leadThighBaseRot;
            if (leftCalf != null) leftCalf.localRotation = leadCalfBaseRot;
            if (rightLeg != null) rightLeg.localRotation = rearThighBaseRot;
            if (rightCalf != null) rightCalf.localRotation = rearCalfBaseRot;
        }

        private void ApplyDamageFlash(Color flashColor)
        {
            if (allRenderers == null) return;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].gameObject != groundShadow.gameObject)
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

        // ----------------- TEXTURAS Y SPRITES PROCEDURALES ESTILIZADOS -----------------

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

        private Sprite CreateMuscularTorsoTexture(int w, int h, Color cloth, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f);
            Vector2 c = new Vector2(w / 2f, h / 2f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.44f);
                    float dy = (y - c.y) / (h * 0.46f);
                    float dSq = (dx * dx) + (dy * dy);

                    if (dSq <= 1f)
                    {
                        if (dSq >= 0.88f || y <= 1 || y >= h - 2)
                            tex.SetPixel(x, y, outline);
                        else if (y > h * 0.65f)
                            tex.SetPixel(x, y, skin); // Pecho / Clavícula
                        else
                            tex.SetPixel(x, y, cloth); // Singlet de lucha
                    }
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.15f), 100f);
        }

        private Sprite CreatePelvisTexture(int w, int h, Color cloth)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f);
            Vector2 c = new Vector2(w / 2f, h / 2f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.45f);
                    float dy = (y - c.y) / (h * 0.42f);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        if ((dx * dx) + (dy * dy) >= 0.85f) tex.SetPixel(x, y, outline);
                        else tex.SetPixel(x, y, cloth);
                    }
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateLimbTexture(int w, int h, Color col)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f);
            Vector2 c = new Vector2(w / 2f, h / 2f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.42f);
                    float dy = (y - c.y) / (h * 0.45f);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        if ((dx * dx) + (dy * dy) >= 0.85f) tex.SetPixel(x, y, outline);
                        else tex.SetPixel(x, y, col);
                    }
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.85f), 100f);
        }

        private Sprite CreateProBoxingGlove(int size, Color gloveCol)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f);
            Color highlight = Color.Lerp(gloveCol, Color.white, 0.55f);
            Vector2 center = new Vector2(size / 2f, size * 0.52f);
            float radius = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= radius)
                    {
                        if (d >= radius - 2.8f) tex.SetPixel(x, y, outline);
                        else if (Vector2.Distance(new Vector2(x, y), center + new Vector2(-4, 6)) < radius * 0.35f)
                            tex.SetPixel(x, y, highlight);
                        else tex.SetPixel(x, y, gloveCol);
                    }
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateThighTexture(int w, int h, Color cloth, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w / 2f) / (w * 0.42f);
                    float dy = (y - h / 2f) / (h * 0.46f);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        if ((dx * dx) + (dy * dy) >= 0.85f) tex.SetPixel(x, y, outline);
                        else if (y > h * 0.45f) tex.SetPixel(x, y, cloth);
                        else tex.SetPixel(x, y, skin);
                    }
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.85f), 100f);
        }

        private Sprite CreateCalfBootTexture(int w, int h, Color skin, Color boot)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w / 2f) / (w * 0.40f);
                    float dy = (y - h / 2f) / (h * 0.46f);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        if ((dx * dx) + (dy * dy) >= 0.85f) tex.SetPixel(x, y, outline);
                        else if (y < h * 0.50f) tex.SetPixel(x, y, boot);
                        else tex.SetPixel(x, y, skin);
                    }
                    else tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.85f), 100f);
        }
    }
}
