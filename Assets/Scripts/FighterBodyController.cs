using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Cuerpo de luchador ARTICULADO estilo Street Fighter / CPS-1.
    /// - Esqueleto procedural: pelvis, torso, cuello, 2 brazos (brazo/antebrazo/guante) y 2 piernas (muslo/pantorrilla/bota).
    /// - Animación por POSES (cinemática directa): guardia, caminar, salto, bloqueo, jab, cross, patada frontal,
    ///   patada circular alta, impacto, K.O. y victoria.
    /// - Los pies se apoyan automáticamente en el suelo (la cadera se calcula con la longitud de las piernas).
    /// - La cara que suben los usuarios (DynamicFaceController) cuelga de neckPoint, que es hijo del torso,
    ///   por lo que la cabeza acompaña cada inclinación del cuerpo.
    /// Convención de ángulos (cuerpo mirando a +X): positivo = hacia adelante (z antihorario) en brazos/piernas.
    /// </summary>
    public class FighterBodyController : MonoBehaviour
    {
        [Header("Personaje y Renderizador Principal")]
        public FighterBodyType currentBodyType = FighterBodyType.Gordo;
        public SpriteRenderer bodySpriteRenderer;
        public Transform bodyArtwork;

        [Header("Puntos de Cuello para Cabeza")]
        public Transform neckPoint;
        public Transform neckPointRight;

        [Header("Efectos y Sombra")]
        public Transform groundShadow;
        public GameObject punchTrailObj;
        public GameObject kickTrailObj;

        [Header("Paleta de Color")]
        public Color suitColor = Color.white;
        public Color gloveColor = new Color(0.95f, 0.25f, 0.25f);
        public Color skinColor = Color.white;
        public Color bootColor = new Color(0.18f, 0.18f, 0.22f);

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
        [HideInInspector] public bool isUsingModularRig = true;
        [HideInInspector] public bool isUsingClassicSpriteBody = false;

        // ======================= POSE =======================
        private struct Pose
        {
            public float HipX, Lift, Root, Torso, Head;
            public float BU, BF, FU, FF;       // brazo trasero / delantero (brazo, antebrazo)
            public float BT, BK, FT, FK;       // pierna trasera / delantera (muslo, rodilla)

            public static Pose Lerp(Pose a, Pose b, float t)
            {
                Pose r;
                r.HipX = L(a.HipX, b.HipX, t); r.Lift = L(a.Lift, b.Lift, t);
                r.Root = L(a.Root, b.Root, t); r.Torso = L(a.Torso, b.Torso, t);
                r.Head = L(a.Head, b.Head, t);
                r.BU = L(a.BU, b.BU, t); r.BF = L(a.BF, b.BF, t);
                r.FU = L(a.FU, b.FU, t); r.FF = L(a.FF, b.FF, t);
                r.BT = L(a.BT, b.BT, t); r.BK = L(a.BK, b.BK, t);
                r.FT = L(a.FT, b.FT, t); r.FK = L(a.FK, b.FK, t);
                return r;
            }
            private static float L(float a, float b, float t) { return a + (b - a) * t; }
        }

        // Poses base (cuerpo mirando a +X)
        private static Pose Stance()
        {
            return new Pose { HipX = 0.135f, Torso = 6f, Head = 0f, BU = 20f, BF = 115f, FU = 30f, FF = 100f, BT = -15f, BK = -30f, FT = 40f, FK = -55f };
        }
        private static Pose GuardPose()
        {
            Pose p = Stance();
            p.HipX = 0.10f; p.Torso = -3f; p.Head = -4f;
            p.FU = 38f; p.FF = 128f; p.BU = 34f; p.BF = 135f;
            p.FT = 48f; p.FK = -72f; p.BT = -8f; p.BK = -42f;
            return p;
        }
        private static Pose AirPose()
        {
            Pose p = Stance();
            p.HipX = 0f; p.Torso = 5f;
            p.FT = 55f; p.FK = -90f; p.BT = 12f; p.BK = -85f;
            p.FU = 60f; p.FF = 95f; p.BU = 35f; p.BF = 120f;
            return p;
        }
        private static Pose JabWindup()
        {
            Pose p = Stance(); p.HipX = 0.10f; p.Torso = 0f; p.FU = 8f; p.FF = 145f; return p;
        }
        private static Pose JabStrike()
        {
            Pose p = Stance(); p.HipX = 0.22f; p.Torso = 15f; p.Head = 6f;
            p.FU = 88f; p.FF = 4f; p.BU = 18f; p.BF = 125f; p.FT = 44f; p.FK = -60f; return p;
        }
        private static Pose CrossWindup()
        {
            Pose p = Stance(); p.HipX = 0.05f; p.Torso = -10f; p.Head = -3f;
            p.BU = -28f; p.BF = 115f; p.BT = -20f; p.BK = -38f; p.FT = 44f; p.FK = -62f; return p;
        }
        private static Pose CrossStrike()
        {
            Pose p = Stance(); p.HipX = 0.32f; p.Torso = 24f; p.Head = 9f;
            p.BU = 92f; p.BF = 0f; p.FU = 42f; p.FF = 112f;
            p.BT = -28f; p.BK = -18f; p.FT = 46f; p.FK = -66f; return p;
        }
        private static Pose KickWindup(bool heavy)
        {
            Pose p = Stance(); p.Torso = -6f; p.HipX = 0.05f;
            p.FT = heavy ? 62f : 75f; p.FK = heavy ? -105f : -118f;
            p.BT = -6f; p.BK = -14f; return p;
        }
        private static Pose KickStrike(bool heavy)
        {
            Pose p = Stance(); p.HipX = 0f; p.BT = -4f; p.BK = -6f;
            if (heavy)
            {
                // Patada circular alta
                p.Torso = -34f; p.Head = -10f; p.HipX = -0.06f;
                p.FT = 118f; p.FK = -8f;
                p.FU = -30f; p.FF = 30f; p.BU = 48f; p.BF = 80f;
            }
            else
            {
                // Patada frontal
                p.Torso = -14f; p.Head = -4f;
                p.FT = 88f; p.FK = -5f;
            }
            return p;
        }
        private static Pose HurtPose()
        {
            Pose p = Stance(); p.HipX = -0.06f; p.Torso = -20f; p.Head = -24f;
            p.BU = -18f; p.BF = 40f; p.FU = 8f; p.FF = 60f; p.FT = 30f; p.FK = -60f; p.BT = -22f; p.BK = -28f;
            return p;
        }

        // ======================= RIG =======================
        private struct Palette
        {
            public Color skin, top, shorts, belt, glove, boot;
            public bool hasTop, longPants, stripes, vneck;
            public float w, h;
        }

        private const float PPU = 256f;
        private const float ANKLE = 0.105f;
        private float thighLen = 0.5f, shinLen = 0.48f, torsoLen = 0.58f;

        private Transform pelvisT, torsoT;
        private Transform bThigh, bShin, bFoot, fThigh, fShin, fFoot;
        private Transform bArm, bFore, bFist, fArm, fFore, fFist;
        private readonly List<Object> ownedAssets = new List<Object>();
        private Color appliedSuit = Color.white, appliedGlove = new Color(0.95f, 0.25f, 0.25f);

        // Renderizadores y colores para efectos de hit flash
        private SpriteRenderer[] allRenderers;
        private Color[] originalColors;

        // Estado de animación
        private Pose shown, loco, over;
        private bool useOver = false;
        private float walkCycle = 0f;
        private float breatheCycle = 0f;
        private bool isAttacking = false;
        private bool isKO = false;
        private bool isHurt = false;
        private bool isGuarding = false;
        private bool isVictory = false;
        private bool lightPunchAlt = false;

        private Coroutine activeAttackRoutine;
        private Coroutine activeHurtRoutine;
        private Coroutine activeKORoutine;
        private Coroutine activeVictoryRoutine;

        private static Sprite s_ShadowSprite;

        private void Awake()
        {
            EnsureBodyComponents();
            SetClassicBody(currentBodyType);
        }

        private void Start()
        {
            CacheRenderers();
        }

        public void EnsureBodyComponents()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                string cName = child.name;
                if (cName != "GroundShadow" && cName != "BodyArtwork" && cName != "NeckPoint" && !cName.StartsWith("Head_Face"))
                {
                    Destroy(child.gameObject);
                }
            }

            if (bodyArtwork == null)
            {
                Transform bChild = transform.Find("BodyArtwork");
                if (bChild == null)
                {
                    GameObject bObj = new GameObject("BodyArtwork");
                    bObj.transform.SetParent(transform, false);
                    bObj.transform.localPosition = Vector3.zero;
                    bChild = bObj.transform;
                }
                bodyArtwork = bChild;
            }

            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = bodyArtwork.GetComponent<SpriteRenderer>();
            }
            if (bodySpriteRenderer != null) { bodySpriteRenderer.sprite = null; bodySpriteRenderer.enabled = false; }

            if (neckPoint == null)
            {
                Transform nChild = bodyArtwork.Find("NeckPoint");
                if (nChild == null) nChild = transform.Find("NeckPoint");
                if (nChild == null)
                {
                    GameObject nObj = new GameObject("NeckPoint");
                    nChild = nObj.transform;
                }
                nChild.SetParent(bodyArtwork, false);
                neckPoint = nChild;
            }

            if (neckPointRight == null)
            {
                Transform nrChild = bodyArtwork.Find("NeckPointRight");
                if (nrChild == null) nrChild = transform.Find("NeckPointRight");
                if (nrChild == null)
                {
                    GameObject nrObj = new GameObject("NeckPointRight");
                    nrChild = nrObj.transform;
                }
                nrChild.SetParent(bodyArtwork, false);
                neckPointRight = nrChild;
            }

            if (groundShadow == null)
            {
                Transform sChild = transform.Find("GroundShadow");
                if (sChild == null)
                {
                    GameObject sObj = new GameObject("GroundShadow");
                    sObj.transform.SetParent(transform, false);
                    sObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    sObj.transform.localScale = new Vector3(1.35f, 0.40f, 1f);
                    var sr = sObj.AddComponent<SpriteRenderer>();
                    sr.sprite = GetOrCreateShadowSprite();
                    sr.sortingOrder = -5;
                    sChild = sObj.transform;
                }
                groundShadow = sChild;
            }

            if (neckPoint != null)
            {
                Transform rootFighter = transform.parent != null ? transform.parent : transform;
                DynamicFaceController face = rootFighter.GetComponentInChildren<DynamicFaceController>(true);
                if (face != null && face.transform.parent != neckPoint)
                {
                    face.transform.SetParent(neckPoint, false);
                    face.transform.localPosition = Vector3.zero;
                }
            }
        }

        public void SetClassicBody(FighterBodyType bodyType)
        {
            currentBodyType = bodyType;
            EnsureBodyComponents();
            BuildRig(bodyType);

            if (neckPointRight != null)
            {
                neckPointRight.gameObject.SetActive(bodyType == FighterBodyType.DosCabezas);
            }

            if (groundShadow != null)
            {
                float sw = (bodyType == FighterBodyType.Gordo || bodyType == FighterBodyType.DosCabezas) ? 1.6f : 1.35f;
                groundShadow.localScale = new Vector3(sw, 0.40f, 1f);
            }
        }

        // ---------- Paletas por arquetipo ----------
        private Palette GetPalette(FighterBodyType t)
        {
            Palette p = new Palette();
            p.w = 1f; p.h = 1f;
            switch (t)
            {
                case FighterBodyType.Gordo:
                    p.w = 1.32f; p.h = 0.96f;
                    p.skin = new Color(0.95f, 0.76f, 0.62f);
                    p.hasTop = true; p.stripes = true; p.top = new Color(0.16f, 0.42f, 0.92f);
                    p.shorts = new Color(0.10f, 0.26f, 0.68f); p.belt = Color.white;
                    p.glove = new Color(0.12f, 0.35f, 0.88f); p.boot = new Color(0.08f, 0.12f, 0.32f);
                    break;
                case FighterBodyType.Flaco:
                    p.w = 0.82f; p.h = 1.0f;
                    p.skin = new Color(0.92f, 0.74f, 0.58f);
                    p.hasTop = false; p.longPants = true;
                    p.shorts = new Color(0.12f, 0.62f, 0.30f); p.belt = new Color(1f, 0.82f, 0.15f);
                    p.glove = new Color(0.92f, 0.92f, 0.86f); p.boot = new Color(0.10f, 0.10f, 0.12f);
                    break;
                case FighterBodyType.Musculoso:
                    p.w = 1.2f; p.h = 1.04f;
                    p.skin = new Color(0.78f, 0.55f, 0.38f);
                    p.hasTop = false;
                    p.shorts = new Color(0.96f, 0.72f, 0.10f); p.belt = new Color(0.1f, 0.1f, 0.12f);
                    p.glove = new Color(0.10f, 0.10f, 0.12f); p.boot = new Color(0.96f, 0.72f, 0.10f);
                    break;
                case FighterBodyType.Mujer:
                    p.w = 0.88f; p.h = 1.0f;
                    p.skin = new Color(0.96f, 0.78f, 0.66f);
                    p.hasTop = true; p.top = new Color(0.10f, 0.72f, 0.78f);
                    p.longPants = true; p.shorts = new Color(0.08f, 0.48f, 0.58f); p.belt = Color.white;
                    p.glove = new Color(0.20f, 0.85f, 0.95f); p.boot = new Color(0.06f, 0.35f, 0.42f);
                    break;
                case FighterBodyType.Mujer2:
                    p.w = 0.90f; p.h = 1.0f;
                    p.skin = new Color(0.96f, 0.80f, 0.68f);
                    p.hasTop = true; p.vneck = true; p.top = new Color(0.82f, 0.10f, 0.20f);
                    p.longPants = true; p.shorts = new Color(0.62f, 0.07f, 0.15f); p.belt = new Color(1f, 0.80f, 0.15f);
                    p.glove = new Color(0.15f, 0.15f, 0.20f); p.boot = new Color(0.45f, 0.05f, 0.10f);
                    break;
                case FighterBodyType.Playero:
                    p.w = 1.05f; p.h = 1.0f;
                    p.skin = new Color(0.85f, 0.62f, 0.44f);
                    p.hasTop = false;
                    p.shorts = new Color(0.12f, 0.78f, 0.88f);
                    p.belt = new Color(1.0f, 0.90f, 0.20f);
                    p.glove = new Color(1.0f, 0.55f, 0.15f);
                    p.boot = new Color(0.12f, 0.78f, 0.88f);
                    break;
                case FighterBodyType.Oficinista:
                    p.w = 0.95f; p.h = 1.02f;
                    p.skin = new Color(0.96f, 0.82f, 0.72f);
                    p.hasTop = true; p.top = new Color(0.95f, 0.95f, 0.95f);
                    p.longPants = true; p.shorts = new Color(0.18f, 0.20f, 0.24f);
                    p.belt = new Color(0.10f, 0.10f, 0.10f);
                    p.glove = new Color(0.96f, 0.82f, 0.72f);
                    p.boot = new Color(0.08f, 0.08f, 0.10f);
                    break;
                case FighterBodyType.Pijamero:
                    p.w = 1.0f; p.h = 0.98f;
                    p.skin = new Color(0.95f, 0.80f, 0.68f);
                    p.hasTop = true; p.top = new Color(0.35f, 0.55f, 0.90f);
                    p.longPants = true; p.shorts = new Color(0.35f, 0.55f, 0.90f);
                    p.belt = new Color(0.95f, 0.95f, 0.40f);
                    p.glove = new Color(0.95f, 0.95f, 0.95f);
                    p.boot = new Color(0.98f, 0.98f, 0.98f);
                    break;
                default: // DosCabezas / Hombre-Máquina
                    p.w = 1.3f; p.h = 0.98f;
                    p.skin = new Color(0.62f, 0.70f, 0.76f);
                    p.hasTop = true; p.top = new Color(0.62f, 0.35f, 0.17f);
                    p.shorts = new Color(0.43f, 0.24f, 0.12f); p.belt = new Color(0.9f, 0.5f, 0.15f);
                    p.glove = new Color(0.45f, 0.25f, 0.12f); p.boot = new Color(0.28f, 0.16f, 0.08f);
                    break;
            }

            // Overrides del torneo / selección de color
            if (suitColor != Color.white)
            {
                if (p.hasTop) p.top = suitColor;
                p.shorts = suitColor * 0.8f; p.shorts.a = 1f;
            }
            if (gloveColor != new Color(0.95f, 0.25f, 0.25f)) p.glove = gloveColor;
            if (skinColor != Color.white) p.skin = skinColor;
            if (bootColor != new Color(0.18f, 0.18f, 0.22f)) p.boot = bootColor;
            return p;
        }

        // ---------- Construcción del esqueleto ----------
        private void BuildRig(FighterBodyType type)
        {
            if (bodyArtwork == null) return;

            // Proteger los cuellos (y las caras) antes de limpiar
            if (neckPoint != null) neckPoint.SetParent(bodyArtwork, false);
            if (neckPointRight != null) neckPointRight.SetParent(bodyArtwork, false);
            for (int i = bodyArtwork.childCount - 1; i >= 0; i--)
            {
                Transform c = bodyArtwork.GetChild(i);
                if (c == neckPoint || c == neckPointRight) continue;
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
            for (int i = 0; i < ownedAssets.Count; i++) if (ownedAssets[i] != null) Destroy(ownedAssets[i]);
            ownedAssets.Clear();

            Palette pal = GetPalette(type);
            appliedSuit = suitColor; appliedGlove = gloveColor;

            float w = pal.w, h = pal.h;
            thighLen = 0.50f * h; shinLen = 0.48f * h; torsoLen = 0.58f * h;
            float upperLen = 0.34f * h, foreLen = 0.32f * h;
            Color outline = new Color(0.06f, 0.05f, 0.09f);
            Color skin = pal.skin;
            Color skinDark = skin * 0.78f; skinDark.a = 1f;

            // ---- Sprites ----
            float thighW = Mathf.Clamp(0.22f * w, 0.16f, 0.34f);
            float shinW = Mathf.Clamp(0.18f * w, 0.14f, 0.28f);
            float armW = Mathf.Clamp(0.14f * w, 0.11f, 0.22f);
            float foreW = Mathf.Clamp(0.12f * w, 0.10f, 0.19f);

            Color thighTop = pal.longPants ? pal.shorts : pal.shorts;
            Color thighBot = pal.longPants ? pal.shorts : skin;
            Sprite thighSpr = MakeLimb(thighW, thighLen, thighTop, thighBot, pal.longPants ? 1f : 0.42f, outline, Color.clear);
            Sprite shinSpr = MakeLimb(shinW, shinLen, pal.longPants ? pal.shorts : skin, pal.longPants ? pal.shorts : skin, 1f, outline, pal.boot);
            Sprite shoeSpr = MakeShoe(pal.boot, outline, w);
            Sprite upperSpr = MakeLimb(armW, upperLen, pal.hasTop ? pal.top : skin, skin, pal.hasTop ? 0.45f : 0f, outline, Color.clear);
            Sprite foreSpr = MakeLimb(foreW, foreLen, skin, skin, 0f, outline, pal.glove);
            Sprite gloveSpr = MakeGlove(0.115f + 0.02f * w, pal.glove, outline);
            Sprite torsoSpr = MakeTorso(pal, torsoLen, outline);
            Sprite pelvisSpr = MakePelvis(pal, outline);
            Sprite neckSpr = MakeLimb(Mathf.Clamp(0.15f * w, 0.12f, 0.22f), 0.22f, skin, skin, 0f, outline, Color.clear);

            // ---- Jerarquía ----
            pelvisT = NewNode("Pelvis", bodyArtwork, Vector3.zero);
            AddSprite(pelvisT.gameObject, pelvisSpr, 8);

            // Piernas
            bThigh = BuildLeg("BackLeg", pelvisT, new Vector3(-0.05f * w, 0f, 0f), thighSpr, shinSpr, shoeSpr, 5, out bShin, out bFoot);
            fThigh = BuildLeg("FrontLeg", pelvisT, new Vector3(0.05f * w, 0f, 0f), thighSpr, shinSpr, shoeSpr, 9, out fShin, out fFoot);

            // Torso
            torsoT = NewNode("Torso", pelvisT, new Vector3(0f, 0.02f, 0f));
            AddSprite(torsoT.gameObject, torsoSpr, 7);

            if (type == FighterBodyType.DosCabezas)
            {
                Transform neckLeftVis = NewNode("NeckLeft", torsoT, new Vector3(-0.25f * w, torsoLen - 0.02f, 0f));
                neckLeftVis.localRotation = Quaternion.Euler(0f, 0f, 188f);
                AddSprite(neckLeftVis.gameObject, neckSpr, 6);

                Transform neckRightVis = NewNode("NeckRight", torsoT, new Vector3(0.25f * w, torsoLen - 0.02f, 0f));
                neckRightVis.localRotation = Quaternion.Euler(0f, 0f, 172f);
                AddSprite(neckRightVis.gameObject, neckSpr, 6);
            }
            else
            {
                Transform neckVis = NewNode("Neck", torsoT, new Vector3(0f, torsoLen - 0.06f, 0f));
                // Cuello apunta hacia arriba: usamos un nodo rotado 180° para reutilizar el sprite colgante
                neckVis.localRotation = Quaternion.Euler(0f, 0f, 180f);
                neckVis.localPosition = new Vector3(0f, torsoLen - 0.02f, 0f);
                AddSprite(neckVis.gameObject, neckSpr, 6);
            }

            // Brazos
            bArm = BuildArm("BackArm", torsoT, new Vector3(-0.03f * w, torsoLen - 0.09f, 0f), upperSpr, foreSpr, gloveSpr, upperLen, foreLen, 4, out bFore, out bFist);
            fArm = BuildArm("FrontArm", torsoT, new Vector3(0.03f * w, torsoLen - 0.09f, 0f), upperSpr, foreSpr, gloveSpr, upperLen, foreLen, 11, out fFore, out fFist);

            // Cuellos/cabezas del usuario: hijos del torso => las caras se inclinan con el cuerpo
            if (type == FighterBodyType.DosCabezas)
            {
                if (neckPoint != null)
                {
                    neckPoint.SetParent(torsoT, false);
                    neckPoint.localPosition = new Vector3(-0.25f * w, torsoLen + 0.28f * h, 0f);
                    neckPoint.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    neckPoint.localScale = new Vector3(0.88f, 0.88f, 1f);
                }
                if (neckPointRight != null)
                {
                    neckPointRight.SetParent(torsoT, false);
                    neckPointRight.localPosition = new Vector3(0.25f * w, torsoLen + 0.28f * h, 0f);
                    neckPointRight.localRotation = Quaternion.Euler(0f, 0f, -8f);
                    neckPointRight.localScale = new Vector3(0.88f, 0.88f, 1f);
                    neckPointRight.gameObject.SetActive(true);
                }
            }
            else
            {
                if (neckPoint != null)
                {
                    neckPoint.SetParent(torsoT, false);
                    neckPoint.localPosition = new Vector3(0.02f, torsoLen + 0.27f * h, 0f);
                    neckPoint.localRotation = Quaternion.identity;
                    neckPoint.localScale = Vector3.one;
                }
                if (neckPointRight != null)
                {
                    neckPointRight.gameObject.SetActive(false);
                }
            }

            // Accesorios y trajes cómicos según el arquetipo
            if (type == FighterBodyType.Playero)
            {
                AttachDuckFloatie(pelvisT, w);
            }
            else if (type == FighterBodyType.Oficinista)
            {
                AttachOfficeSuitProps(torsoT, fFist, w, torsoLen);
            }
            else if (type == FighterBodyType.Pijamero)
            {
                AttachPajamaProps(fFoot, bFoot, w);
            }

            // Limpiar cualquier cutout antiguo si existiera
            if (pelvisT != null)
            {
                Transform existing = pelvisT.Find("PhotoBodyCutout");
                if (existing != null) Destroy(existing.gameObject);
            }

            // Compatibilidad
            pelvis = pelvisT; torso = torsoT;
            leftArm = fArm; leftForearm = fFore; leftFist = fFist;
            rightArm = bArm; rightForearm = bFore; rightFist = bFist;
            leftLeg = fThigh; leftCalf = fShin; leftFoot = fFoot;
            rightLeg = bThigh; rightCalf = bShin; rightFoot = bFoot;

            shown = loco = over = Stance();
            useOver = false;
            ApplyPose(shown);
            CacheRenderers();
        }

        public int GetPlayerId()
        {
            FighterController fc = GetComponent<FighterController>();
            if (fc != null && fc.playerId > 0) return fc.playerId;
            if (gameObject.name.Contains("1")) return 1;
            if (gameObject.name.Contains("2")) return 2;
            return ((int)currentBodyType % 2) + 1;
        }

        public void ReloadCustomBodyPhoto(int fighterId)
        {
            if (pelvisT != null)
            {
                Transform existing = pelvisT.Find("PhotoBodyCutout");
                if (existing != null) Destroy(existing.gameObject);
            }
        }

        private void AttachDuckFloatie(Transform parentPelvis, float w)
        {
            Sprite floatieSpr = MakeDuckFloatieSprite(w);
            Transform floatieNode = NewNode("DuckFloatie", parentPelvis, new Vector3(0.04f, 0.02f, 0f));
            AddSprite(floatieNode.gameObject, floatieSpr, 13);
        }

        private void AttachOfficeSuitProps(Transform parentTorso, Transform parentFist, float w, float tLen)
        {
            Sprite tieSpr = MakeRedTieSprite();
            Transform tieNode = NewNode("RedTie", parentTorso, new Vector3(0.02f, tLen - 0.04f, 0f));
            AddSprite(tieNode.gameObject, tieSpr, 10);

            if (parentFist != null)
            {
                Sprite caseSpr = MakeBriefcaseSprite();
                Transform caseNode = NewNode("ExecutiveBriefcase", parentFist, new Vector3(0.06f, -0.06f, 0f));
                AddSprite(caseNode.gameObject, caseSpr, 14);
            }
        }

        private void AttachPajamaProps(Transform fFootT, Transform bFootT, float w)
        {
            Sprite bunnySpr = MakeBunnySlipperSprite(w);
            if (fFootT != null)
            {
                Transform bunnyF = NewNode("BunnySlipper_Front", fFootT, new Vector3(0.08f, 0.04f, 0f));
                AddSprite(bunnyF.gameObject, bunnySpr, 12);
            }
            if (bFootT != null)
            {
                Transform bunnyB = NewNode("BunnySlipper_Back", bFootT, new Vector3(0.08f, 0.04f, 0f));
                AddSprite(bunnyB.gameObject, bunnySpr, 7);
            }
        }

        private Sprite MakeDuckFloatieSprite(float w)
        {
            int W = 64, H = 34;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[W * H];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 yellow = new Color32(255, 224, 25, 255);
            Color32 yellowShade = new Color32(230, 180, 15, 255);
            Color32 orangeBeak = new Color32(255, 125, 15, 255);
            Color32 outline = new Color32(25, 20, 30, 255);
            Color32 white = new Color32(255, 255, 255, 255);
            Color32 black = new Color32(20, 20, 20, 255);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float nx = (float)x / W;
                    float ny = (float)y / H;

                    // Flotador (anillo elíptico)
                    float ringDx = (nx - 0.44f) / 0.38f;
                    float ringDy = (ny - 0.40f) / 0.32f;
                    float ringDist = Mathf.Sqrt(ringDx * ringDx + ringDy * ringDy);
                    bool inRing = (ringDist >= 0.48f && ringDist <= 1.0f);
                    bool inRingOutline = (ringDist >= 0.42f && ringDist <= 1.08f);

                    // Cabeza del pato en la parte frontal derecha
                    float headDx = (nx - 0.80f) / 0.14f;
                    float headDy = (ny - 0.65f) / 0.26f;
                    float headDist = Mathf.Sqrt(headDx * headDx + headDy * headDy);
                    bool inHead = headDist <= 0.88f;
                    bool inHeadOutline = headDist <= 1.0f;

                    // Pico naranja del pato
                    bool inBeak = (nx >= 0.88f && nx <= 0.99f && ny >= 0.58f && ny <= 0.72f);

                    // Ojo negro con brillo blanco
                    bool inEye = (nx >= 0.77f && nx <= 0.82f && ny >= 0.70f && ny <= 0.78f);
                    bool inEyeSpark = (nx >= 0.78f && nx <= 0.80f && ny >= 0.75f && ny <= 0.78f);

                    if (inEyeSpark) px[y * W + x] = white;
                    else if (inEye) px[y * W + x] = black;
                    else if (inBeak) px[y * W + x] = orangeBeak;
                    else if (inHead) px[y * W + x] = ny < 0.6f ? yellowShade : yellow;
                    else if (inHeadOutline) px[y * W + x] = outline;
                    else if (inRing) px[y * W + x] = ny < 0.35f ? yellowShade : yellow;
                    else if (inRingOutline) px[y * W + x] = outline;
                    else px[y * W + x] = clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.44f, 0.40f), PPU);
            ownedAssets.Add(tex);
            ownedAssets.Add(s);
            return s;
        }

        private Sprite MakeRedTieSprite()
        {
            int W = 16, H = 36;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[W * H];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 red = new Color32(225, 35, 45, 255);
            Color32 redDark = new Color32(165, 18, 25, 255);
            Color32 outline = new Color32(30, 10, 15, 255);

            for (int y = 0; y < H; y++)
            {
                float ny = (float)y / H; // 0 abajo, 1 arriba
                float halfW = (ny > 0.82f) ? 0.35f : Mathf.Lerp(0.45f, 0.22f, (ny - 0.15f) / 0.67f);
                if (ny < 0.15f) halfW = Mathf.Lerp(0.05f, 0.45f, ny / 0.15f); // punta inferior

                for (int x = 0; x < W; x++)
                {
                    float nx = Mathf.Abs((float)x / W - 0.5f);
                    if (nx <= halfW)
                    {
                        if (nx > halfW - 0.08f || y == 0 || y == H - 1) px[y * W + x] = outline;
                        else px[y * W + x] = ((float)x / W > 0.5f) ? redDark : red;
                    }
                    else px[y * W + x] = clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.95f), PPU);
            ownedAssets.Add(tex);
            ownedAssets.Add(s);
            return s;
        }

        private Sprite MakeBriefcaseSprite()
        {
            int W = 36, H = 26;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[W * H];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 leather = new Color32(75, 42, 22, 255);
            Color32 leatherDark = new Color32(50, 26, 12, 255);
            Color32 gold = new Color32(245, 205, 45, 255);
            Color32 outline = new Color32(20, 15, 10, 255);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float nx = (float)x / W;
                    float ny = (float)y / H;

                    // Asa superior del maletín
                    bool inHandle = (nx >= 0.38f && nx <= 0.62f && ny >= 0.78f && ny <= 0.96f);
                    bool inHandleHole = (nx >= 0.44f && nx <= 0.56f && ny >= 0.78f && ny <= 0.88f);

                    // Cuerpo del maletín
                    bool inBody = (nx >= 0.06f && nx <= 0.94f && ny >= 0.06f && ny <= 0.78f);
                    bool inBodyOutline = (nx >= 0.03f && nx <= 0.97f && ny >= 0.03f && ny <= 0.81f);

                    // Broches dorados
                    bool inLock = inBody && (ny >= 0.42f && ny <= 0.54f) && ((nx >= 0.22f && nx <= 0.32f) || (nx >= 0.68f && nx <= 0.78f));

                    if (inHandle && inHandleHole) px[y * W + x] = clear;
                    else if (inHandle) px[y * W + x] = outline;
                    else if (inLock) px[y * W + x] = gold;
                    else if (inBody) px[y * W + x] = ny < 0.35f ? leatherDark : leather;
                    else if (inBodyOutline) px[y * W + x] = outline;
                    else px[y * W + x] = clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.85f), PPU);
            ownedAssets.Add(tex);
            ownedAssets.Add(s);
            return s;
        }

        private Sprite MakeBunnySlipperSprite(float w)
        {
            int W = 32, H = 24;
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[W * H];
            Color32 clear = new Color32(0, 0, 0, 0);
            Color32 white = new Color32(250, 250, 252, 255);
            Color32 whiteDark = new Color32(215, 215, 225, 255);
            Color32 pink = new Color32(255, 160, 185, 255);
            Color32 outline = new Color32(30, 25, 35, 255);
            Color32 black = new Color32(25, 25, 25, 255);

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float nx = (float)x / W;
                    float ny = (float)y / H;

                    // Orejas de conejo
                    bool inEar1 = (nx >= 0.25f && nx <= 0.42f && ny >= 0.48f && ny <= 0.95f);
                    bool inEar1Inner = (nx >= 0.29f && nx <= 0.38f && ny >= 0.55f && ny <= 0.88f);
                    bool inEar2 = (nx >= 0.58f && nx <= 0.75f && ny >= 0.48f && ny <= 0.95f);
                    bool inEar2Inner = (nx >= 0.62f && nx <= 0.71f && ny >= 0.55f && ny <= 0.88f);

                    // Cabeza / cuerpo de la pantufla
                    float dx = (nx - 0.50f) / 0.40f;
                    float dy = (ny - 0.32f) / 0.28f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    bool inHead = dist <= 0.92f;
                    bool inHeadOutline = dist <= 1.04f;

                    // Naricita y ojitos
                    bool inNose = (nx >= 0.45f && nx <= 0.55f && ny >= 0.22f && ny <= 0.32f);
                    bool inEye = (ny >= 0.32f && ny <= 0.42f) && ((nx >= 0.32f && nx <= 0.38f) || (nx >= 0.62f && nx <= 0.68f));

                    if (inEar1Inner || inEar2Inner) px[y * W + x] = pink;
                    else if (inEar1 || inEar2) px[y * W + x] = white;
                    else if (inNose) px[y * W + x] = pink;
                    else if (inEye) px[y * W + x] = black;
                    else if (inHead) px[y * W + x] = ny < 0.22f ? whiteDark : white;
                    else if (inHeadOutline) px[y * W + x] = outline;
                    else px[y * W + x] = clear;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.30f, 0.40f), PPU);
            ownedAssets.Add(tex);
            ownedAssets.Add(s);
            return s;
        }

        private Transform NewNode(string name, Transform parent, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        private SpriteRenderer AddSprite(GameObject go, Sprite spr, int order)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = order;
            return sr;
        }

        private Transform BuildLeg(string name, Transform parent, Vector3 pos, Sprite thigh, Sprite shin, Sprite shoe, int order, out Transform shinT, out Transform footT)
        {
            Transform t = NewNode(name, parent, pos);
            AddSprite(t.gameObject, thigh, order);
            shinT = NewNode(name + "_Shin", t, new Vector3(0f, -thighLen, 0f));
            AddSprite(shinT.gameObject, shin, order);
            footT = NewNode(name + "_Foot", shinT, new Vector3(0f, -shinLen, 0f));
            AddSprite(footT.gameObject, shoe, order + 1);
            return t;
        }

        private Transform BuildArm(string name, Transform parent, Vector3 pos, Sprite upper, Sprite fore, Sprite glove, float upperLen, float foreLen, int order, out Transform foreT, out Transform fistT)
        {
            Transform t = NewNode(name, parent, pos);
            AddSprite(t.gameObject, upper, order);
            foreT = NewNode(name + "_Fore", t, new Vector3(0f, -upperLen, 0f));
            AddSprite(foreT.gameObject, fore, order);
            fistT = NewNode(name + "_Fist", foreT, new Vector3(0f, -foreLen, 0f));
            AddSprite(fistT.gameObject, glove, order + 1);
            return t;
        }

        // ---------- Generación procedural de sprites ----------
        private Sprite MakeLimb(float widthU, float lenU, Color top, Color bot, float split, Color outline, Color accent)
        {
            int wPx = Mathf.Max(8, Mathf.RoundToInt(widthU * PPU));
            float r = wPx * 0.5f;
            int hPx = Mathf.RoundToInt(lenU * PPU + 2f * r);
            float splitV = split;
            System.Func<float, float, Color> col = (vf, uf) =>
            {
                Color c = vf < splitV ? top : bot;
                if (accent.a > 0.01f)
                {
                    // Puño/borde de manga o cinta de tobillo cerca del extremo inferior
                    if (vf > 0.80f && vf < 0.93f) c = accent;
                }
                if (splitV > 0.01f && splitV < 0.99f && Mathf.Abs(vf - splitV) < 0.018f) c = c * 0.65f;
                c.a = 1f;
                return c;
            };
            return MakeShape(wPx, hPx, wPx, wPx, r, col, new Vector2(0.5f, 1f - r / hPx), outline);
        }

        private Sprite MakeGlove(float radiusU, Color glove, Color outline)
        {
            int d = Mathf.RoundToInt(radiusU * 2f * PPU);
            System.Func<float, float, Color> col = (vf, uf) =>
            {
                Color c = glove;
                float hl = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(uf, vf), new Vector2(0.33f, 0.30f)) * 3.2f);
                c = Color.Lerp(c, Color.white, hl * 0.45f);
                if (vf > 0.78f) c = c * 0.78f;
                c.a = 1f;
                return c;
            };
            return MakeShape(d, d, d, d, d * 0.5f, col, new Vector2(0.5f, 0.5f), outline);
        }

        private Sprite MakeShoe(Color boot, Color outline, float w)
        {
            int wPx = Mathf.RoundToInt((0.34f + 0.03f * w) * PPU);
            int hPx = Mathf.RoundToInt(0.14f * PPU);
            System.Func<float, float, Color> col = (vf, uf) =>
            {
                Color c = boot;
                if (vf > 0.78f) c = new Color(0.92f, 0.92f, 0.92f); // suela clara
                if (uf > 0.82f && vf < 0.6f) c = Color.Lerp(c, Color.white, 0.18f); // puntera
                c.a = 1f;
                return c;
            };
            // El tobillo está en ~28% desde el talón y a ~80% de altura
            return MakeShape(wPx, hPx, wPx, wPx, hPx * 0.42f, col, new Vector2(0.27f, 0.80f), outline);
        }

        private Sprite MakeTorso(Palette pal, float lenU, Color outline)
        {
            int wTop = Mathf.RoundToInt(0.50f * pal.w * PPU);
            int wBot = Mathf.RoundToInt(0.36f * pal.w * PPU * (pal.w > 1.1f ? 1.15f : 1f));
            int hPx = Mathf.RoundToInt(lenU * PPU);
            Color skin = pal.skin;
            Color skinDark = skin * 0.72f; skinDark.a = 1f;
            Color skinLight = Color.Lerp(skin, Color.white, 0.12f);
            System.Func<float, float, Color> col = (vf, uf) =>
            {
                float dx = Mathf.Abs(uf - 0.5f);
                if (vf > 0.90f) return pal.belt;
                Color c = pal.hasTop ? pal.top : skin;
                if (pal.hasTop)
                {
                    if (pal.stripes && (dx > 0.08f && dx < 0.16f || dx > 0.30f && dx < 0.38f)) c = Color.white;
                    if (pal.vneck && vf < 0.30f && dx < 0.16f * (1f - vf / 0.30f)) c = skin;
                    if (pal.vneck && Mathf.Abs(dx - 0.16f * (1f - vf / 0.30f)) < 0.012f && vf < 0.30f) c = pal.belt;
                }
                else
                {
                    // Pectorales y abdominales dibujados
                    if (dx < 0.012f && vf > 0.20f && vf < 0.88f) c = skinDark;
                    if (vf > 0.22f && vf < 0.26f && dx < 0.34f && dx > 0.02f) c = skinDark;
                    if ((Mathf.Abs(vf - 0.50f) < 0.012f || Mathf.Abs(vf - 0.64f) < 0.012f || Mathf.Abs(vf - 0.78f) < 0.012f) && dx < 0.17f) c = skinDark;
                    if (vf < 0.20f) c = skinLight;
                }
                c.a = 1f;
                return c;
            };
            int W = Mathf.Max(wTop, wBot);
            return MakeShape(W, hPx, wTop, wBot, 0.08f * PPU, col, new Vector2(0.5f, 0f), outline);
        }

        private Sprite MakePelvis(Palette pal, Color outline)
        {
            int wTop = Mathf.RoundToInt(0.40f * pal.w * PPU);
            int wBot = Mathf.RoundToInt(0.42f * pal.w * PPU);
            int hPx = Mathf.RoundToInt(0.30f * PPU);
            System.Func<float, float, Color> col = (vf, uf) =>
            {
                Color c = vf < 0.26f ? pal.belt : pal.shorts;
                c.a = 1f;
                return c;
            };
            int W = Mathf.Max(wTop, wBot);
            return MakeShape(W, hPx, wTop, wBot, 0.07f * PPU, col, new Vector2(0.5f, 0.62f), outline);
        }

        /// <summary>Caja redondeada con ancho variable (arriba/abajo), borde oscuro y sombreado.</summary>
        private Sprite MakeShape(int W, int H, float wTopPx, float wBotPx, float rPx, System.Func<float, float, Color> colorAt, Vector2 pivot, Color outline)
        {
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[W * H];
            const float OUT = 3f;
            for (int y = 0; y < H; y++)
            {
                float v = H - (y + 0.5f);
                float vf = v / H;
                float hw = Mathf.Lerp(wTopPx, wBotPx, vf) * 0.5f;
                float rr = Mathf.Min(rPx, hw);
                for (int x = 0; x < W; x++)
                {
                    float u = x + 0.5f;
                    float dx = Mathf.Abs(u - W * 0.5f);
                    float dxp = Mathf.Max(dx - (hw - rr), 0f);
                    float dyp = Mathf.Max(rr - Mathf.Min(v, H - v), 0f);
                    float d = Mathf.Sqrt(dxp * dxp + dyp * dyp) - rr;
                    if (dx > hw) d = Mathf.Max(d, dx - hw);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * W + x] = new Color32(0, 0, 0, 0); continue; }

                    Color baseC = colorAt(vf, u / W);
                    float shade = 1.12f - 0.34f * (u / W);
                    Color c = new Color(Mathf.Clamp01(baseC.r * shade), Mathf.Clamp01(baseC.g * shade), Mathf.Clamp01(baseC.b * shade), 1f);
                    float e = Mathf.Clamp01(d + OUT + 0.5f);
                    c = Color.Lerp(c, outline, e);
                    c.a = a;
                    px[y * W + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            Sprite s = Sprite.Create(tex, new Rect(0, 0, W, H), pivot, PPU);
            ownedAssets.Add(tex);
            ownedAssets.Add(s);
            return s;
        }

        // ---------- Aplicar pose al esqueleto ----------
        private void ApplyPose(Pose p)
        {
            if (pelvisT == null) return;

            float dropF = thighLen * Mathf.Cos(p.FT * Mathf.Deg2Rad) + shinLen * Mathf.Cos((p.FT + p.FK) * Mathf.Deg2Rad);
            float dropB = thighLen * Mathf.Cos(p.BT * Mathf.Deg2Rad) + shinLen * Mathf.Cos((p.BT + p.BK) * Mathf.Deg2Rad);
            float grounded = Mathf.Max(dropF, dropB) + ANKLE;
            float k = Mathf.Clamp01(Mathf.Abs(p.Root) / 20f);
            float hipY = Mathf.Lerp(grounded, 0f, k) + p.Lift;

            pelvisT.localPosition = new Vector3(p.HipX, hipY, 0f);
            pelvisT.localRotation = Quaternion.Euler(0f, 0f, p.Root);
            torsoT.localRotation = Quaternion.Euler(0f, 0f, -p.Torso);
            if (neckPoint != null) neckPoint.localRotation = Quaternion.Euler(0f, 0f, (currentBodyType == FighterBodyType.DosCabezas ? 8f : 0f) - p.Head);
            if (neckPointRight != null && currentBodyType == FighterBodyType.DosCabezas) neckPointRight.localRotation = Quaternion.Euler(0f, 0f, -8f - p.Head);

            bThigh.localRotation = Quaternion.Euler(0f, 0f, p.BT);
            bShin.localRotation = Quaternion.Euler(0f, 0f, p.BK);
            bFoot.localRotation = Quaternion.Euler(0f, 0f, FootRot(p.BT, p.BK));
            fThigh.localRotation = Quaternion.Euler(0f, 0f, p.FT);
            fShin.localRotation = Quaternion.Euler(0f, 0f, p.FK);
            fFoot.localRotation = Quaternion.Euler(0f, 0f, FootRot(p.FT, p.FK));

            bArm.localRotation = Quaternion.Euler(0f, 0f, p.BU);
            bFore.localRotation = Quaternion.Euler(0f, 0f, p.BF);
            fArm.localRotation = Quaternion.Euler(0f, 0f, p.FU);
            fFore.localRotation = Quaternion.Euler(0f, 0f, p.FF);

            if (groundShadow != null && Mathf.Abs(p.Root) < 1f)
            {
                float baseSW = (currentBodyType == FighterBodyType.Gordo || currentBodyType == FighterBodyType.DosCabezas) ? 1.6f : 1.35f;
                groundShadow.localScale = new Vector3(baseSW * (1f - Mathf.Clamp(p.Lift, 0f, 0.4f) * 0.5f), 0.40f, 1f);
            }
        }

        private static float FootRot(float thigh, float knee)
        {
            float abs = thigh + knee;
            // Pie plano en apoyo; en patadas el pie se mantiene en flexión (punta arriba)
            float f = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01((Mathf.Abs(abs) - 60f) / 40f));
            return -abs * f;
        }

        public void CacheRenderers()
        {
            allRenderers = GetComponentsInChildren<SpriteRenderer>(false);
            originalColors = new Color[allRenderers.Length];
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null) originalColors[i] = allRenderers[i].color;
            }
        }

        private static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        // ==================== ANIMACIÓN ====================

        private void LateUpdate()
        {
            if (pelvisT == null) return;
            if (useOver) shown = over;
            else shown = Pose.Lerp(shown, loco, 1f - Mathf.Exp(-16f * Time.deltaTime));
            ApplyPose(shown);
        }

        public void UpdateAnimation(float moveX, bool isGrounded)
        {
            UpdateAnimation(moveX, isGrounded, false);
        }

        public void UpdateAnimation(float moveX, bool isGrounded, bool guardHeld)
        {
            if (suitColor != appliedSuit || gloveColor != appliedGlove) BuildRig(currentBodyType);
            if (isKO || isAttacking || isHurt || isVictory || pelvisT == null) return;

            isGuarding = guardHeld;
            float sgn = transform.lossyScale.x < 0f ? -1f : 1f;
            float fwd = moveX * sgn; // + = avanzando hacia el rival

            if (isGuarding && isGrounded)
            {
                loco = GuardPose();
            }
            else if (!isGrounded)
            {
                loco = AirPose();
                loco.Torso += Mathf.Sign(fwd) * 4f * Mathf.Clamp01(Mathf.Abs(fwd));
            }
            else if (Mathf.Abs(moveX) > 0.15f)
            {
                float dir = fwd >= 0f ? 1f : -1f;
                walkCycle += Time.deltaTime * 9.5f * dir;
                float a = Mathf.Sin(walkCycle);
                float b = Mathf.Sin(walkCycle + Mathf.PI);
                float cf = Mathf.Max(0f, Mathf.Cos(walkCycle) * dir);
                float cb = Mathf.Max(0f, Mathf.Cos(walkCycle + Mathf.PI) * dir);

                Pose p = Stance();
                p.HipX = 0.10f;
                p.FT = 26f + 24f * a; p.FK = -(22f + 42f * cf);
                p.BT = -12f + 24f * b; p.BK = -(22f + 42f * cb);
                p.Torso = dir > 0f ? 10f : 2f;
                p.FU += a * 5f; p.FF += a * 6f; p.BU -= a * 5f; p.BF -= a * 4f;
                p.Lift = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.012f;
                loco = p;
            }
            else
            {
                breatheCycle += Time.deltaTime * 3.6f;
                float s = Mathf.Sin(breatheCycle);
                Pose p = Stance();
                p.Lift = s * 0.014f;
                p.Torso += s * 1.5f;
                p.FU += s * 3f; p.FF += s * 4f;
                p.BU -= s * 3f; p.BF -= s * 3f;
                p.FT += s * 2f; p.BT -= s * 1.5f;
                loco = p;
            }
        }

        // ==================== ATAQUES DE PUÑO Y PATADA ====================

        public void PlayPunchAnimation(float startup, float active, float recovery)
        {
            PlayPunchAnimation(startup, active, recovery, false);
        }

        public void PlayPunchAnimation(float startup, float active, float recovery, bool isHeavy)
        {
            if (isKO || pelvisT == null) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);

            Pose windup, strike;
            if (isHeavy || lightPunchAlt) { windup = CrossWindup(); strike = CrossStrike(); }
            else { windup = JabWindup(); strike = JabStrike(); }
            if (!isHeavy) lightPunchAlt = !lightPunchAlt;

            activeAttackRoutine = StartCoroutine(AttackRoutine(windup, strike, startup, active, recovery, isHeavy));
        }

        public void PlayHadoukenAnimation(float startup, float active, float recovery)
        {
            if (isKO || pelvisT == null) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);

            // Manos recogidas a la cadera -> ambas palmas lanzadas al frente
            Pose windup = Stance();
            windup.HipX = -0.02f; windup.Torso = -8f; windup.Head = -4f;
            windup.FU = -25f; windup.FF = 105f; windup.BU = -30f; windup.BF = 110f;
            windup.FT = 46f; windup.FK = -70f; windup.BT = -18f; windup.BK = -44f;

            Pose strike = Stance();
            strike.HipX = 0.22f; strike.Torso = 16f; strike.Head = 8f;
            strike.FU = 86f; strike.FF = 2f; strike.BU = 90f; strike.BF = 4f;
            strike.FT = 48f; strike.FK = -72f; strike.BT = -22f; strike.BK = -30f;

            activeAttackRoutine = StartCoroutine(AttackRoutine(windup, strike, startup, active, recovery, false));
        }

        public void PlayKickAnimation(float startup, float active, float recovery)
        {
            PlayKickAnimation(startup, active, recovery, false);
        }

        public void PlayKickAnimation(float startup, float active, float recovery, bool isHeavy)
        {
            if (isKO || pelvisT == null) return;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(AttackRoutine(KickWindup(isHeavy), KickStrike(isHeavy), startup, active, recovery, isHeavy));
        }

        private IEnumerator AttackRoutine(Pose windup, Pose strike, float startup, float active, float recovery, bool isHeavy)
        {
            isAttacking = true;
            useOver = true;
            Pose from = shown;
            startup = Mathf.Max(0.001f, startup);
            active = Mathf.Max(0.001f, active);
            recovery = Mathf.Max(0.001f, recovery);

            // 1. STARTUP: anticipación (preparación del golpe)
            float t = 0f;
            while (t < startup)
            {
                t += Time.deltaTime;
                over = Pose.Lerp(from, windup, Ease(t / startup));
                yield return null;
            }

            // 2. ACTIVE: extensión rápida y mantenida en el punto de impacto
            t = 0f;
            bool confetti = false;
            while (t < active)
            {
                t += Time.deltaTime;
                float f = Ease(t / (active * 0.4f));
                over = Pose.Lerp(windup, strike, f);
                if (!confetti && f > 0.6f && isHeavy && currentBodyType == FighterBodyType.DosCabezas && CombatEffectsManager.Instance != null)
                {
                    confetti = true;
                    CombatEffectsManager.Instance.SpawnConfettiBurst(transform.position + Vector3.up * 1.8f, 14);
                }
                yield return null;
            }

            // 3. RECOVERY: regreso a la guardia
            t = 0f;
            Pose end = Stance();
            while (t < recovery)
            {
                t += Time.deltaTime;
                over = Pose.Lerp(strike, end, Ease(t / recovery));
                yield return null;
            }

            shown = end;
            useOver = false;
            isAttacking = false;
            activeAttackRoutine = null;
        }

        // ==================== HITSTUN, DERROTA (K.O.) Y VICTORIA ====================

        public void PlayHurtAnimation(float duration)
        {
            if (isKO || pelvisT == null) return;
            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; isAttacking = false; }
            if (activeHurtRoutine != null) StopCoroutine(activeHurtRoutine);
            activeHurtRoutine = StartCoroutine(HurtMotionRoutine(duration));
        }

        private IEnumerator HurtMotionRoutine(float duration)
        {
            isHurt = true;
            useOver = true;
            FlashRenderers(new Color(1f, 0.45f, 0.45f, 1f));

            Pose from = shown;
            Pose hit = HurtPose();
            float elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float f = Ease(elapsed / 0.07f);
                Pose p = Pose.Lerp(from, hit, f);
                // Vibración del impacto que se disipa
                float decay = 1f - Mathf.Clamp01(elapsed / duration);
                p.HipX += Random.Range(-0.03f, 0.03f) * decay;
                p.Torso += Random.Range(-2f, 2f) * decay;
                over = p;
                yield return null;
            }

            ResetLimbColors();
            shown = hit;
            useOver = false;
            isHurt = false;
            activeHurtRoutine = null;
        }

        public void PlayKODefeatedAnimation()
        {
            isKO = true;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            if (activeHurtRoutine != null) StopCoroutine(activeHurtRoutine);
            if (activeKORoutine != null) StopCoroutine(activeKORoutine);
            activeKORoutine = StartCoroutine(KOMotionRoutine());
        }

        private IEnumerator KOMotionRoutine()
        {
            if (pelvisT == null) yield break;
            useOver = true;
            Pose from = shown;

            Pose air = HurtPose();
            air.Root = 45f; air.Lift = 0.95f; air.HipX = -0.35f; air.Head = -38f; air.Torso = -25f;
            air.BU = -60f; air.BF = 0f; air.FU = -50f; air.FF = -10f;
            air.FT = 40f; air.FK = -30f; air.BT = 30f; air.BK = -20f;

            Pose floor = air;
            floor.Root = 88f; floor.Lift = 0.22f; floor.HipX = -0.95f; floor.Head = -10f; floor.Torso = 0f;
            floor.BU = -70f; floor.BF = -10f; floor.FU = -40f; floor.FF = 10f;
            floor.FT = 25f; floor.FK = -10f; floor.BT = 10f; floor.BK = -8f;

            // Fase 1: salida volando por el golpe final
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                over = Pose.Lerp(from, air, Ease(t / 0.25f));
                yield return null;
            }

            // Fase 2: caída pesada a la lona
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                over = Pose.Lerp(air, floor, Ease(t / 0.35f));
                yield return null;
            }

            // Pequeño rebote al impactar
            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                Pose p = floor;
                p.Lift += Mathf.Sin(t / 0.18f * Mathf.PI) * 0.12f;
                over = p;
                yield return null;
            }
            over = floor;
        }

        public void PlayVictoryAnimation()
        {
            if (isKO || pelvisT == null) return;
            isVictory = true;
            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeVictoryRoutine != null) StopCoroutine(activeVictoryRoutine);
            activeVictoryRoutine = StartCoroutine(VictoryMotionRoutine());
        }

        private IEnumerator VictoryMotionRoutine()
        {
            if (CombatEffectsManager.Instance != null)
            {
                CombatEffectsManager.Instance.SpawnConfettiBurst(transform.position + Vector3.up * 1.8f, 22);
            }

            useOver = true;
            float t = 0f;
            float confettiTimer = 0f;
            while (true)
            {
                t += Time.deltaTime * 5f;
                confettiTimer += Time.deltaTime;

                if (confettiTimer > 1.3f)
                {
                    confettiTimer = 0f;
                    if (CombatEffectsManager.Instance != null)
                    {
                        CombatEffectsManager.Instance.SpawnConfettiBurst(transform.position + Vector3.up * 1.8f, 15);
                    }
                }

                float b = Mathf.Sin(t);
                Pose p = Stance();
                p.HipX = 0f; p.Torso = -4f; p.Head = -8f;
                p.FU = 160f + b * 14f; p.FF = -12f;           // puño al cielo
                p.BU = -20f; p.BF = 95f + b * 10f;
                p.FT = 22f; p.FK = -30f; p.BT = -22f; p.BK = -30f;
                p.Lift = Mathf.Abs(b) * 0.12f;
                over = p;
                yield return null;
            }
        }

        public void ResetBody()
        {
            isKO = false;
            isAttacking = false;
            isHurt = false;
            isVictory = false;

            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeHurtRoutine != null) { StopCoroutine(activeHurtRoutine); activeHurtRoutine = null; }
            if (activeKORoutine != null) { StopCoroutine(activeKORoutine); activeKORoutine = null; }
            if (activeVictoryRoutine != null) { StopCoroutine(activeVictoryRoutine); activeVictoryRoutine = null; }

            ResetLimbColors();
            useOver = false;
            shown = loco = Stance();
            ApplyPose(shown);
        }

        private void FlashRenderers(Color col)
        {
            if (allRenderers == null) return;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].transform != groundShadow) allRenderers[i].color = col;
            }
        }

        private void ResetLimbColors()
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

        private static Sprite GetOrCreateShadowSprite()
        {
            if (s_ShadowSprite != null) return s_ShadowSprite;

            int w = 128;
            int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / (w * 0.48f);
                    float dy = (y - c.y) / (h * 0.45f);
                    float d2 = (dx * dx) + (dy * dy);
                    if (d2 <= 1f)
                    {
                        float alpha = Mathf.Lerp(0.55f, 0f, d2);
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, clear);
                    }
                }
            }
            tex.Apply();
            s_ShadowSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return s_ShadowSprite;
        }
    }
}
