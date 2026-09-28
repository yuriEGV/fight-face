using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Gestiona y anima el cuerpo 2D estilizado del luchador:
    /// - Torso musculoso estilo cómic con cinturón de campeón y sombras de pectorales.
    /// - Guantes de boxeo gruesos con brillos y muñequeras.
    /// - Pantalones cortos de pelea con franjas laterales y botas de lucha libre.
    /// - Estela de velocidad (whoosh trail) al golpear.
    /// - Animaciones mejoradas de puñetazo, patada voladora, respiración idle y KO.
    /// </summary>
    public class FighterBodyController : MonoBehaviour
    {
        [Header("Extremidades")]
        public Transform neckPoint;
        public Transform torso;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        [Header("Guantes / Puños")]
        public Transform rightFist;
        public Transform leftFist;

        [Header("Pies / Botas")]
        public Transform rightFoot;
        public Transform leftFoot;

        [Header("Efectos")]
        public Transform groundShadow;
        public GameObject punchTrailObj;
        public GameObject kickTrailObj;

        [Header("Paleta de Colores")]
        public Color suitColor = new Color(0.9f, 0.2f, 0.2f); // Traje / Pantalón
        public Color gloveColor = new Color(1f, 0.85f, 0.1f); // Guantes
        public Color skinColor = new Color(1f, 0.85f, 0.72f); // Piel
        public Color bootColor = new Color(0.12f, 0.12f, 0.15f); // Botas

        [Header("Cuerpo Clásico (5 Personajes)")]
        public FighterBodyType currentBodyType = FighterBodyType.Musculoso;
        public SpriteRenderer bodySpriteRenderer;
        public Transform neckPointLeft;
        public Transform neckPointRight;
        public bool isUsingClassicSpriteBody = true;

        [Header("Extremidades Articuladas Dinámicas")]
        public Transform articulatedPunchFist;
        public SpriteRenderer articulatedPunchSr;
        public Transform articulatedKickFoot;
        public SpriteRenderer articulatedKickSr;

        private SpriteRenderer[] allRenderers;
        private Color[] originalColors;
        private float walkCycle = 0f;
        private bool isAttacking = false;
        private bool isKO = false;
        private Vector3 initialTorsoPos;

        private void Awake()
        {
            if (bodySpriteRenderer == null && neckPoint == null)
            {
                SetClassicBody(currentBodyType);
            }
            else if (neckPoint == null)
            {
                BuildDetailedBrawlerBody();
            }

            if (torso != null)
            {
                initialTorsoPos = torso.localPosition;
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
        /// Aplica uno de los 5 cuerpos clásicos (El Gordo, El Flaco, El Musculoso, La Mujer, El Dos Cabezas)
        /// y calibra la posición del cuello y el renderizador.
        /// </summary>
        public void SetClassicBody(FighterBodyType bodyType)
        {
            currentBodyType = bodyType;
            isUsingClassicSpriteBody = true;

            // Cargar sprite del cuerpo headless
            Sprite bodySprite = FaceLoader.LoadFighterBodySprite(bodyType);

            if (bodySpriteRenderer == null)
            {
                Transform existing = transform.Find("ClassicBodySprite");
                if (existing != null)
                {
                    bodySpriteRenderer = existing.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject bodyObj = new GameObject("ClassicBodySprite");
                    bodyObj.transform.SetParent(transform, false);
                    bodyObj.transform.localPosition = Vector3.zero;
                    bodyObj.transform.localScale = Vector3.one;
                    bodySpriteRenderer = bodyObj.AddComponent<SpriteRenderer>();
                }
            }

            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sortingOrder = 2;
                if (bodySprite != null)
                {
                    bodySpriteRenderer.sprite = bodySprite;
                }
                bodySpriteRenderer.gameObject.SetActive(true);
            }

            // Ocultar miembros procedurales si existen
            if (torso != null) torso.gameObject.SetActive(false);
            if (leftArm != null) leftArm.gameObject.SetActive(false);
            if (rightArm != null) rightArm.gameObject.SetActive(false);
            if (leftLeg != null) leftLeg.gameObject.SetActive(false);
            if (rightLeg != null) rightLeg.gameObject.SetActive(false);

            // Crear o posicionar NeckPoint
            if (neckPoint == null)
            {
                Transform existingNeck = transform.Find("NeckPoint");
                if (existingNeck != null)
                {
                    neckPoint = existingNeck;
                }
                else
                {
                    GameObject neckObj = new GameObject("NeckPoint");
                    neckObj.transform.SetParent(transform, false);
                    neckPoint = neckObj.transform;
                }
            }

            // Calibrar la posición del cuello exacta para este cuerpo
            Vector3 neckOffset = FaceLoader.GetNeckLocalOffset(bodyType);
            neckPoint.localPosition = neckOffset;

            // Dos Cabezas: cuello izquierdo y cuello derecho
            if (bodyType == FighterBodyType.DosCabezas)
            {
                neckPoint.localPosition = new Vector3(-0.23f, 0.96f, 0);

                if (neckPointRight == null)
                {
                    Transform existingR = transform.Find("NeckPointRight");
                    if (existingR != null)
                    {
                        neckPointRight = existingR;
                    }
                    else
                    {
                        GameObject rNeck = new GameObject("NeckPointRight");
                        rNeck.transform.SetParent(transform, false);
                        neckPointRight = rNeck.transform;
                    }
                }
                neckPointRight.localPosition = new Vector3(0.23f, 0.92f, 0);
            }

            // Sombra en el suelo
            if (groundShadow == null)
            {
                Transform existingShadow = transform.Find("Shadow");
                if (existingShadow != null)
                {
                    groundShadow = existingShadow;
                }
                else
                {
                    GameObject shadowObj = new GameObject("Shadow");
                    shadowObj.transform.SetParent(transform, false);
                    shadowObj.transform.localPosition = new Vector3(0, -0.05f, 0);
                    groundShadow = shadowObj.transform;
                    var shadowSr = shadowObj.AddComponent<SpriteRenderer>();
                    shadowSr.sprite = CreateShadowSprite();
                    shadowSr.sortingOrder = -5;
                }
            }

            CacheRenderers();
            EnsureArticulatedLimbs();
        }

        public void EnsureArticulatedLimbs()
        {
            if (articulatedPunchFist == null)
            {
                Transform existingFist = transform.Find("ArticulatedPunchFist");
                if (existingFist != null)
                {
                    articulatedPunchFist = existingFist;
                    articulatedPunchSr = existingFist.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject fistObj = new GameObject("ArticulatedPunchFist");
                    fistObj.transform.SetParent(transform, false);
                    fistObj.transform.localPosition = new Vector3(0.35f, 0.75f, 0);
                    articulatedPunchFist = fistObj.transform;
                    articulatedPunchSr = fistObj.AddComponent<SpriteRenderer>();
                    articulatedPunchSr.sprite = CreateProBoxingGlove(56, gloveColor, false);
                    articulatedPunchSr.sortingOrder = 16;
                }
            }

            if (punchTrailObj == null && articulatedPunchFist != null)
            {
                Transform pt = articulatedPunchFist.Find("PunchTrail");
                if (pt != null) punchTrailObj = pt.gameObject;
                else
                {
                    GameObject ptObj = new GameObject("PunchTrail");
                    ptObj.transform.SetParent(articulatedPunchFist, false);
                    ptObj.transform.localPosition = new Vector3(-0.35f, 0, 0);
                    var sr = ptObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateSpeedTrailSprite(80, 40);
                    sr.sortingOrder = 15;
                    punchTrailObj = ptObj;
                }
            }
            if (punchTrailObj != null) punchTrailObj.SetActive(false);
            if (articulatedPunchFist != null) articulatedPunchFist.gameObject.SetActive(false);

            if (articulatedKickFoot == null)
            {
                Transform existingFoot = transform.Find("ArticulatedKickFoot");
                if (existingFoot != null)
                {
                    articulatedKickFoot = existingFoot;
                    articulatedKickSr = existingFoot.GetComponent<SpriteRenderer>();
                }
                else
                {
                    GameObject footObj = new GameObject("ArticulatedKickFoot");
                    footObj.transform.SetParent(transform, false);
                    footObj.transform.localPosition = new Vector3(0.35f, 0.35f, 0);
                    articulatedKickFoot = footObj.transform;
                    articulatedKickSr = footObj.AddComponent<SpriteRenderer>();
                    articulatedKickSr.sprite = CreateProWrestlingBoot(54, 32, bootColor);
                    articulatedKickSr.sortingOrder = 16;
                }
            }

            if (kickTrailObj == null && articulatedKickFoot != null)
            {
                Transform kt = articulatedKickFoot.Find("KickTrail");
                if (kt != null) kickTrailObj = kt.gameObject;
                else
                {
                    GameObject ktObj = new GameObject("KickTrail");
                    ktObj.transform.SetParent(articulatedKickFoot, false);
                    ktObj.transform.localPosition = new Vector3(-0.35f, 0, 0);
                    var sr = ktObj.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateSpeedTrailSprite(90, 45);
                    sr.sortingOrder = 15;
                    kickTrailObj = ktObj;
                }
            }
            if (kickTrailObj != null) kickTrailObj.SetActive(false);
            if (articulatedKickFoot != null) articulatedKickFoot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Construye un cuerpo de luchador brawler mucho más prolijo, estilizado y musculoso.
        /// </summary>
        public void BuildDetailedBrawlerBody()
        {
            // Sombra en el suelo
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(transform, false);
            shadowObj.transform.localPosition = new Vector3(0, -0.05f, 0);
            groundShadow = shadowObj.transform;
            var shadowSr = shadowObj.AddComponent<SpriteRenderer>();
            shadowSr.sprite = CreateShadowSprite();
            shadowSr.sortingOrder = -5;

            // Cuello
            GameObject neckObj = new GameObject("NeckPoint");
            neckObj.transform.SetParent(transform, false);
            neckObj.transform.localPosition = new Vector3(0, 1.25f, 0);
            neckPoint = neckObj.transform;

            // Torso musculoso con cinturón de campeonato
            GameObject torsoObj = new GameObject("Torso");
            torsoObj.transform.SetParent(transform, false);
            torsoObj.transform.localPosition = new Vector3(0, 0.68f, 0);
            torso = torsoObj.transform;
            var torsoSr = torsoObj.AddComponent<SpriteRenderer>();
            torsoSr.sprite = CreateMuscularTorsoSprite(64, 80, suitColor, skinColor);
            torsoSr.sortingOrder = 2;

            // Brazo Izquierdo (trasero)
            GameObject lArmObj = new GameObject("LeftArm");
            lArmObj.transform.SetParent(transform, false);
            lArmObj.transform.localPosition = new Vector3(-0.42f, 0.90f, 0);
            leftArm = lArmObj.transform;
            var lArmSr = lArmObj.AddComponent<SpriteRenderer>();
            lArmSr.sprite = CreateMuscularArmSprite(28, 48, skinColor, true);
            lArmSr.sortingOrder = 0;

            GameObject lFistObj = new GameObject("LeftFist");
            lFistObj.transform.SetParent(leftArm, false);
            lFistObj.transform.localPosition = new Vector3(-0.1f, -0.35f, 0);
            leftFist = lFistObj.transform;
            var lFistSr = lFistObj.AddComponent<SpriteRenderer>();
            lFistSr.sprite = CreateProBoxingGlove(44, gloveColor, false);
            lFistSr.sortingOrder = 1;

            // Brazo Derecho (delantero / puño principal)
            GameObject rArmObj = new GameObject("RightArm");
            rArmObj.transform.SetParent(transform, false);
            rArmObj.transform.localPosition = new Vector3(0.42f, 0.90f, 0);
            rightArm = rArmObj.transform;
            var rArmSr = rArmObj.AddComponent<SpriteRenderer>();
            rArmSr.sprite = CreateMuscularArmSprite(28, 48, skinColor, false);
            rArmSr.sortingOrder = 3;

            GameObject rFistObj = new GameObject("RightFist");
            rFistObj.transform.SetParent(rightArm, false);
            rFistObj.transform.localPosition = new Vector3(0.1f, -0.35f, 0);
            rightFist = rFistObj.transform;
            var rFistSr = rFistObj.AddComponent<SpriteRenderer>();
            rFistSr.sprite = CreateProBoxingGlove(44, gloveColor, true);
            rFistSr.sortingOrder = 5;

            // Estela visual de puñetazo (Speed trail)
            GameObject punchTrail = new GameObject("PunchSpeedTrail");
            punchTrail.transform.SetParent(rightFist, false);
            punchTrail.transform.localPosition = new Vector3(-0.3f, 0, 0);
            var ptSr = punchTrail.AddComponent<SpriteRenderer>();
            ptSr.sprite = CreateSpeedTrailSprite(60, 30);
            ptSr.sortingOrder = 4;
            punchTrailObj = punchTrail;
            punchTrailObj.SetActive(false);

            // Piernas con pantalones de boxeo / lucha
            GameObject lLegObj = new GameObject("LeftLeg");
            lLegObj.transform.SetParent(transform, false);
            lLegObj.transform.localPosition = new Vector3(-0.25f, 0.28f, 0);
            leftLeg = lLegObj.transform;
            var lLegSr = lLegObj.AddComponent<SpriteRenderer>();
            lLegSr.sprite = CreateMuscularLegSprite(28, 52, suitColor, skinColor);
            lLegSr.sortingOrder = 1;

            GameObject lFootObj = new GameObject("LeftFoot");
            lFootObj.transform.SetParent(leftLeg, false);
            lFootObj.transform.localPosition = new Vector3(0, -0.38f, 0);
            leftFoot = lFootObj.transform;
            var lFootSr = lFootObj.AddComponent<SpriteRenderer>();
            lFootSr.sprite = CreateProWrestlingBoot(38, 24, bootColor);
            lFootSr.sortingOrder = 2;

            GameObject rLegObj = new GameObject("RightLeg");
            rLegObj.transform.SetParent(transform, false);
            rLegObj.transform.localPosition = new Vector3(0.25f, 0.28f, 0);
            rightLeg = rLegObj.transform;
            var rLegSr = rLegObj.AddComponent<SpriteRenderer>();
            rLegSr.sprite = CreateMuscularLegSprite(28, 52, suitColor, skinColor);
            rLegSr.sortingOrder = 3;

            GameObject rFootObj = new GameObject("RightFoot");
            rFootObj.transform.SetParent(rightLeg, false);
            rFootObj.transform.localPosition = new Vector3(0, -0.38f, 0);
            rightFoot = rFootObj.transform;
            var rFootSr = rFootObj.AddComponent<SpriteRenderer>();
            rFootSr.sprite = CreateProWrestlingBoot(38, 24, bootColor);
            rFootSr.sortingOrder = 4;

            // Estela visual de patada
            GameObject kickTrail = new GameObject("KickSpeedTrail");
            kickTrail.transform.SetParent(rightFoot, false);
            kickTrail.transform.localPosition = new Vector3(-0.2f, 0, 0);
            var ktSr = kickTrail.AddComponent<SpriteRenderer>();
            ktSr.sprite = CreateSpeedTrailSprite(70, 35);
            ktSr.sortingOrder = 5;
            kickTrailObj = kickTrail;
            kickTrailObj.SetActive(false);
        }

        // ----------------- GENERADORES DE SPRITES ESTILIZADOS -----------------

        private Sprite CreateMuscularTorsoSprite(int w, int h, Color shortsColor, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color pecShadow = Color.Lerp(skin, Color.black, 0.18f);
            Color beltGold = new Color(1f, 0.85f, 0.1f);
            Color beltBlack = new Color(0.15f, 0.15f, 0.18f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, clear);

                    // Forma en V de torso atlético (ancho arriba, estrecho en la cintura)
                    float tY = (float)y / h;
                    float widthAtY = Mathf.Lerp(w * 0.65f, w * 0.95f, Mathf.SmoothStep(0.2f, 1f, tY));
                    float xDist = Mathf.Abs(x - (w / 2f));

                    if (xDist <= widthAtY / 2f)
                    {
                        if (xDist >= (widthAtY / 2f) - 2.5f || y <= 2 || y >= h - 3)
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else if (y < h * 0.32f)
                        {
                            // Pantalones de boxeo
                            tex.SetPixel(x, y, shortsColor);
                        }
                        else if (y < h * 0.44f)
                        {
                            // Cinturón de campeón con hebilla dorada
                            if (Mathf.Abs(x - (w / 2f)) < 8)
                                tex.SetPixel(x, y, beltGold);
                            else
                                tex.SetPixel(x, y, beltBlack);
                        }
                        else
                        {
                            // Piel con sombreado de pectorales y abdominales
                            bool isPecDivider = (x == w / 2 && y > h * 0.5f);
                            bool isPecLine = (y == (int)(h * 0.68f) && Mathf.Abs(x - (w / 2)) < w * 0.35f);
                            bool isAbsDivider = (x == w / 2 && y < h * 0.5f);
                            bool isAbsLine = (y == (int)(h * 0.52f) && Mathf.Abs(x - (w / 2)) < w * 0.22f);

                            if (isPecDivider || isPecLine || isAbsDivider || isAbsLine)
                                tex.SetPixel(x, y, pecShadow);
                            else
                                tex.SetPixel(x, y, skin);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateMuscularArmSprite(int w, int h, Color skin, bool isLeft)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color muscleShadow = Color.Lerp(skin, Color.black, 0.15f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    float r = w * 0.45f;
                    float cy = Mathf.Clamp(y, r, h - r);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(w / 2f, cy));

                    if (d <= r)
                    {
                        if (d >= r - 2.5f || y <= 1 || y >= h - 2)
                            tex.SetPixel(x, y, outline);
                        else if (x < w * 0.35f)
                            tex.SetPixel(x, y, muscleShadow); // Sombra de bíceps
                        else
                            tex.SetPixel(x, y, skin);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateProBoxingGlove(int size, Color gloveCol, bool isRight)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color highlight = Color.Lerp(gloveCol, Color.white, 0.45f);
            Color shadow = Color.Lerp(gloveCol, Color.black, 0.35f);
            Color wristBand = Color.white;

            Vector2 center = new Vector2(size / 2f, size * 0.55f);
            float radius = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    float d = Vector2.Distance(new Vector2(x, y), center);

                    if (y < size * 0.22f && Mathf.Abs(x - size / 2f) < size * 0.38f)
                    {
                        // Muñequera blanca con cordones
                        if (x <= (size / 2f - size * 0.36f) || x >= (size / 2f + size * 0.36f) || y <= 1)
                            tex.SetPixel(x, y, outline);
                        else
                            tex.SetPixel(x, y, wristBand);
                    }
                    else if (d <= radius)
                    {
                        if (d >= radius - 2.8f)
                            tex.SetPixel(x, y, outline);
                        else if (Vector2.Distance(new Vector2(x, y), center + new Vector2(-4, 6)) < radius * 0.35f)
                            tex.SetPixel(x, y, highlight); // Brillo esférico
                        else if (y < size * 0.45f)
                            tex.SetPixel(x, y, shadow);
                        else
                            tex.SetPixel(x, y, gloveCol);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateMuscularLegSprite(int w, int h, Color shortColor, Color skin)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color stripe = Color.white;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    float r = w * 0.44f;
                    float cy = Mathf.Clamp(y, r, h - r);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(w / 2f, cy));

                    if (d <= r)
                    {
                        if (d >= r - 2.5f || y <= 1 || y >= h - 2)
                        {
                            tex.SetPixel(x, y, outline);
                        }
                        else if (y > h * 0.45f)
                        {
                            // Pantalón con franja lateral
                            if (x >= w * 0.42f && x <= w * 0.58f)
                                tex.SetPixel(x, y, stripe);
                            else
                                tex.SetPixel(x, y, shortColor);
                        }
                        else
                        {
                            // Muslo / pantorrilla
                            tex.SetPixel(x, y, skin);
                        }
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateProWrestlingBoot(int w, int h, Color bootColor)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color outline = new Color(0.12f, 0.12f, 0.15f, 1f);
            Color sole = new Color(0.85f, 0.85f, 0.9f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));

                    if (x >= 2 && x < w - 2 && y >= 1 && y < h - 1)
                    {
                        if (x <= 3 || x >= w - 4 || y <= 2 || y >= h - 2)
                            tex.SetPixel(x, y, outline);
                        else if (y <= 5)
                            tex.SetPixel(x, y, sole); // Suela gruesa
                        else
                            tex.SetPixel(x, y, bootColor);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateShadowSprite()
        {
            int w = 64;
            int h = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color shadowColor = new Color(0, 0, 0, 0.35f);

            Vector2 c = new Vector2(w / 2f, h / 2f);
            float rx = w * 0.45f;
            float ry = h * 0.42f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - c.x) / rx;
                    float dy = (y - c.y) / ry;
                    float dSq = (dx * dx) + (dy * dy);

                    if (dSq <= 1f)
                    {
                        Color sc = shadowColor;
                        sc.a = Mathf.Lerp(0.4f, 0f, dSq);
                        tex.SetPixel(x, y, sc);
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
            Color yellow = new Color(1f, 0.9f, 0.2f, 0.8f);
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
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.8f, 0.5f), 100f);
        }

        // ----------------- ANIMACIONES DE COMBATE MEJORADAS -----------------

        public void UpdateAnimation(float moveInput, bool isGrounded)
        {
            if (isKO || isAttacking) return;

            // Soporte para los 5 Cuerpos de Sprite Clásicos
            if (isUsingClassicSpriteBody && bodySpriteRenderer != null)
            {
                if (Mathf.Abs(moveInput) > 0.1f && isGrounded)
                {
                    walkCycle += Time.deltaTime * 12f;
                    float walkTilt = Mathf.Sin(walkCycle) * 3.5f;
                    float walkBob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.04f;
                    bodySpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, walkTilt);
                    bodySpriteRenderer.transform.localPosition = new Vector3(0, walkBob, 0);
                }
                else
                {
                    walkCycle = 0f;
                    float breathBob = Mathf.Sin(Time.time * 5.5f) * 0.025f;
                    bodySpriteRenderer.transform.localRotation = Quaternion.identity;
                    bodySpriteRenderer.transform.localPosition = new Vector3(0, breathBob, 0);
                }
                return;
            }

            // Idle respiración atlética y rebote procedural
            float idleBob = Mathf.Sin(Time.time * 6.5f) * 0.05f;
            if (torso != null)
            {
                torso.localPosition = initialTorsoPos + new Vector3(0, idleBob, 0);
            }

            if (Mathf.Abs(moveInput) > 0.1f && isGrounded)
            {
                // Caminar: zancada con balanceo de brazos y hombros
                walkCycle += Time.deltaTime * 14f;
                float legAngle = Mathf.Sin(walkCycle) * 32f;
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, legAngle);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, -legAngle);

                float armAngle = Mathf.Sin(walkCycle) * 26f;
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, -armAngle);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, armAngle);
            }
            else
            {
                // Pose de guardia de pelea
                walkCycle = 0f;
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, -5f);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, 8f);

                float guardBob = Mathf.Sin(Time.time * 4f) * 6f;
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, 30f + guardBob);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -20f - guardBob);
            }
        }

        /// <summary>
        /// Puñetazo demoledor: retroceso de anticipación, estiramiento rápido con estela y retorno.
        /// </summary>
        public void PlayPunchAnimation(float duration)
        {
            PlayPunchAnimation(duration * 0.22f, duration * 0.38f, duration * 0.40f);
        }

        public void PlayPunchAnimation(float startup, float active, float recovery)
        {
            if (isKO) return;
            EnsureArticulatedLimbs();
            StartCoroutine(PunchRoutine(startup, active, recovery));
        }

        private IEnumerator PunchRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;
            EnsureArticulatedLimbs();

            Vector3 bodyOrigPos = Vector3.zero;
            Vector3 fistPrepPos = new Vector3(0.12f, 0.70f, 0);
            Vector3 fistStrikePos = new Vector3(0.95f, 0.85f, 0);

            if (articulatedPunchFist != null)
            {
                articulatedPunchFist.gameObject.SetActive(true);
                articulatedPunchFist.localPosition = fistPrepPos;
                articulatedPunchFist.localRotation = Quaternion.Euler(0, 0, 15f);
            }

            // 1. STARTUP (Preparación)
            float t = 0f;
            while (t < startup)
            {
                float frac = t / startup;
                if (bodySpriteRenderer != null)
                    bodySpriteRenderer.transform.localPosition = Vector3.Lerp(bodyOrigPos, bodyOrigPos + new Vector3(-0.12f, 0.02f, 0), frac);
                if (articulatedPunchFist != null)
                    articulatedPunchFist.localPosition = Vector3.Lerp(fistPrepPos, fistPrepPos + new Vector3(-0.10f, -0.05f, 0), frac);
                t += Time.deltaTime;
                yield return null;
            }

            // 2. ACTIVE (Impacto con estela de velocidad)
            if (punchTrailObj != null) punchTrailObj.SetActive(true);
            t = 0f;
            while (t < active)
            {
                float frac = t / active;
                if (bodySpriteRenderer != null)
                    bodySpriteRenderer.transform.localPosition = Vector3.Lerp(bodyOrigPos + new Vector3(-0.12f, 0.02f, 0), bodyOrigPos + new Vector3(0.38f, 0.05f, 0), frac);
                if (articulatedPunchFist != null)
                {
                    articulatedPunchFist.localPosition = Vector3.Lerp(fistPrepPos + new Vector3(-0.10f, -0.05f, 0), fistStrikePos, frac);
                    articulatedPunchFist.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 15f), Quaternion.Euler(0, 0, -10f), frac);
                }
                t += Time.deltaTime;
                yield return null;
            }

            // 3. RECOVERY (Retorno a guardia)
            if (punchTrailObj != null) punchTrailObj.SetActive(false);
            t = 0f;
            while (t < recovery)
            {
                float frac = t / recovery;
                if (bodySpriteRenderer != null)
                    bodySpriteRenderer.transform.localPosition = Vector3.Lerp(bodyOrigPos + new Vector3(0.38f, 0.05f, 0), bodyOrigPos, frac);
                if (articulatedPunchFist != null)
                {
                    articulatedPunchFist.localPosition = Vector3.Lerp(fistStrikePos, fistPrepPos, frac);
                    articulatedPunchFist.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -10f), Quaternion.identity, frac);
                }
                t += Time.deltaTime;
                yield return null;
            }

            if (bodySpriteRenderer != null)
                bodySpriteRenderer.transform.localPosition = bodyOrigPos;
            if (articulatedPunchFist != null)
                articulatedPunchFist.gameObject.SetActive(false);

            isAttacking = false;
        }

        /// <summary>
        /// Patada voladora potente con arco de velocidad.
        /// </summary>
        public void PlayKickAnimation(float duration)
        {
            PlayKickAnimation(duration * 0.25f, duration * 0.35f, duration * 0.40f);
        }

        public void PlayKickAnimation(float startup, float active, float recovery)
        {
            if (isKO) return;
            EnsureArticulatedLimbs();
            StartCoroutine(KickRoutine(startup, active, recovery));
        }

        private IEnumerator KickRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;
            EnsureArticulatedLimbs();

            Vector3 bodyOrigPos = Vector3.zero;
            Quaternion bodyOrigRot = Quaternion.identity;
            Vector3 footPrepPos = new Vector3(0.18f, 0.45f, 0);
            Vector3 footStrikePos = new Vector3(1.10f, 0.42f, 0);

            if (articulatedKickFoot != null)
            {
                articulatedKickFoot.gameObject.SetActive(true);
                articulatedKickFoot.localPosition = footPrepPos;
                articulatedKickFoot.localRotation = Quaternion.Euler(0, 0, 35f);
            }

            // 1. STARTUP
            float t = 0f;
            while (t < startup)
            {
                float frac = t / startup;
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.transform.localPosition = Vector3.Lerp(bodyOrigPos, bodyOrigPos + new Vector3(-0.15f, 0.04f, 0), frac);
                    bodySpriteRenderer.transform.localRotation = Quaternion.Lerp(bodyOrigRot, Quaternion.Euler(0, 0, 10f), frac);
                }
                if (articulatedKickFoot != null)
                {
                    articulatedKickFoot.localPosition = Vector3.Lerp(footPrepPos, footPrepPos + new Vector3(-0.08f, 0.08f, 0), frac);
                }
                t += Time.deltaTime;
                yield return null;
            }

            // 2. ACTIVE
            if (kickTrailObj != null) kickTrailObj.SetActive(true);
            t = 0f;
            while (t < active)
            {
                float frac = t / active;
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.transform.localPosition = Vector3.Lerp(bodyOrigPos + new Vector3(-0.15f, 0.04f, 0), bodyOrigPos + new Vector3(0.42f, 0.10f, 0), frac);
                    bodySpriteRenderer.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 10f), Quaternion.Euler(0, 0, -14f), frac);
                }
                if (articulatedKickFoot != null)
                {
                    articulatedKickFoot.localPosition = Vector3.Lerp(footPrepPos + new Vector3(-0.08f, 0.08f, 0), footStrikePos, frac);
                    articulatedKickFoot.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 35f), Quaternion.Euler(0, 0, -20f), frac);
                }
                t += Time.deltaTime;
                yield return null;
            }

            // 3. RECOVERY
            if (kickTrailObj != null) kickTrailObj.SetActive(false);
            t = 0f;
            while (t < recovery)
            {
                float frac = t / recovery;
                if (bodySpriteRenderer != null)
                {
                    bodySpriteRenderer.transform.localPosition = Vector3.Lerp(bodyOrigPos + new Vector3(0.42f, 0.10f, 0), bodyOrigPos, frac);
                    bodySpriteRenderer.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -14f), bodyOrigRot, frac);
                }
                if (articulatedKickFoot != null)
                {
                    articulatedKickFoot.localPosition = Vector3.Lerp(footStrikePos, footPrepPos, frac);
                    articulatedKickFoot.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -20f), Quaternion.identity, frac);
                }
                t += Time.deltaTime;
                yield return null;
            }

            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.transform.localPosition = bodyOrigPos;
                bodySpriteRenderer.transform.localRotation = bodyOrigRot;
            }
            if (articulatedKickFoot != null)
            {
                articulatedKickFoot.gameObject.SetActive(false);
            }

            isAttacking = false;
        }

        /// <summary>
        /// Reacción al daño: flash blanco/rojo en todo el cuerpo y sacudida.
        /// </summary>
        public void PlayHurtAnimation(float duration)
        {
            if (isKO) return;
            StartCoroutine(HurtRoutine(duration));
        }

        private IEnumerator HurtRoutine(float duration)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            // Flash de impacto rojo/blanco
            FlashBodyColor(new Color(1f, 0.3f, 0.3f, 1f));

            while (elapsed < duration)
            {
                float shake = Mathf.Sin(elapsed * 50f) * 0.12f;
                transform.localPosition = originalPos + new Vector3(-0.15f + shake, 0, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
            ResetBodyColors();
        }

        private void FlashBodyColor(Color flashColor)
        {
            if (allRenderers == null) return;
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].transform != groundShadow)
                {
                    allRenderers[i].color = flashColor;
                }
            }
        }

        private void ResetBodyColors()
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

        public void PlayKODefeatedAnimation()
        {
            isKO = true;
            isAttacking = false;
            StopAllCoroutines();
            if (punchTrailObj != null) punchTrailObj.SetActive(false);
            if (kickTrailObj != null) kickTrailObj.SetActive(false);
            StartCoroutine(KORoutine());
        }

        private IEnumerator KORoutine()
        {
            Quaternion startRot = transform.localRotation;
            Quaternion targetRot = Quaternion.Euler(0, 0, -90f);
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = new Vector3(startPos.x, -2.1f, startPos.z);

            float duration = 0.65f;
            float t = 0f;

            while (t < duration)
            {
                transform.localRotation = Quaternion.Lerp(startRot, targetRot, t / duration);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t / duration);
                t += Time.deltaTime;
                yield return null;
            }

            transform.localRotation = targetRot;
            transform.localPosition = targetPos;
        }

        public void ResetBody()
        {
            isKO = false;
            isAttacking = false;
            transform.localRotation = Quaternion.identity;
            if (torso != null) torso.localPosition = initialTorsoPos;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
            ResetBodyColors();
        }
    }
}
