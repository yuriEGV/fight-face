using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace FightFace
{
    /// <summary>
    /// Inicializa y ensambla automáticamente todo el juego en la escena:
    /// - Cámara y Luces 2D
    /// - Escenario Ring con colisionadores y cuerdas
    /// - Luchadores con cuerpos estilizados brawler y cabezas proporcionadas (~1.05 unidades)
    /// - Sistema de Efectos de Combate con estelas y estrellas de impacto
    /// - Interfaz de Usuario completa (salud, retratos dinámicos, temporizador)
    /// - Creador de Caras y Webcam con soporte para 8 luchadores y botón de giro 180°
    /// - Modo Campeonato Street Fighter con avance por rondas
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class FightGameBootstrap : MonoBehaviour
    {
        [Header("Auto-construcción al Iniciar")]
        public bool buildOnStart = true;

        private void Start()
        {
            if (buildOnStart)
            {
                SetupGame();
            }
        }

        [ContextMenu("Construir Juego Completo")]
        public void SetupGame()
        {
            EnsureEventSystem();
            SetupArena();
            SetupCombatEffects();
            SetupFighters(out FighterController p1, out FighterController p2);
            SetupUI(p1, p2, out BattleUI battleUI, out WebcamCaptureManager webcamMgr);
            SetupBattleManager(p1, p2);
            SetupTournamentManager(p1, p2);

            Debug.Log("<color=green><b>[FightFace] ¡Juego de Pelea 2D Inicializado con Éxito!</b></color>");
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }

        private void SetupArena()
        {
            GameObject arena = GameObject.Find("ArenaRing");
            if (arena != null) return;

            arena = new GameObject("ArenaRing");

            // Suelo
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(arena.transform);
            floor.transform.position = new Vector3(0, -2.5f, 0);
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(24f, 1f);

            var floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = CreateBoxSprite(2400, 100, new Color(0.18f, 0.18f, 0.24f));
            floorSr.sortingOrder = -1;

            // Lona del Ring
            GameObject mat = new GameObject("RingMat");
            mat.transform.SetParent(arena.transform);
            mat.transform.position = new Vector3(0, -2.05f, 0);
            var matSr = mat.AddComponent<SpriteRenderer>();
            matSr.sprite = CreateBoxSprite(1800, 22, new Color(0.85f, 0.22f, 0.22f));
            matSr.sortingOrder = 0;

            // Paredes laterales invisibles
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(arena.transform);
            leftWall.transform.position = new Vector3(-9.5f, 1f, 0);
            var leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(1f, 10f);

            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(arena.transform);
            rightWall.transform.position = new Vector3(9.5f, 1f, 0);
            var rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(1f, 10f);

            // Fondo Callejero Tradicional (Street Fighter Stage)
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(arena.transform);
            backdrop.transform.position = new Vector3(0, 1.35f, 5f);
            var bgSr = backdrop.AddComponent<SpriteRenderer>();
            Sprite streetSprite = FaceLoader.LoadStageStreetSprite();
            if (streetSprite != null)
            {
                bgSr.sprite = streetSprite;
                float worldW = streetSprite.rect.width / streetSprite.pixelsPerUnit;
                float scale = 20.5f / worldW;
                backdrop.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                bgSr.sprite = CreateGradientBackdrop(1920, 1080);
            }
            bgSr.sortingOrder = -10;

            // Cuerdas del ring
            for (int i = 0; i < 3; i++)
            {
                GameObject rope = new GameObject($"Rope_{i}");
                rope.transform.SetParent(arena.transform);
                rope.transform.position = new Vector3(0, -1.6f + (i * 0.5f), 0);
                var ropeSr = rope.AddComponent<SpriteRenderer>();
                Color ropeColor = i == 0 ? new Color(0.9f, 0.2f, 0.2f) : (i == 1 ? Color.white : new Color(0.2f, 0.45f, 0.95f));
                ropeSr.sprite = CreateBoxSprite(1800, 8, ropeColor);
                ropeSr.sortingOrder = -2;
            }
        }

        private void SetupCombatEffects()
        {
            if (CombatEffectsManager.Instance == null)
            {
                GameObject fxObj = new GameObject("CombatEffectsManager");
                fxObj.AddComponent<CombatEffectsManager>();
            }

            if (FightImpactManager.Instance == null)
            {
                GameObject impactObj = new GameObject("FightImpactManager");
                impactObj.AddComponent<FightImpactManager>();
            }

            // Configurar Cámara Dinámica con zoom automático cuerpo a cuerpo y K.O.
            if (Camera.main != null && Camera.main.GetComponent<DynamicFightCamera>() == null)
            {
                Camera.main.gameObject.AddComponent<DynamicFightCamera>();
            }
        }

        private void SetupFighters(out FighterController p1, out FighterController p2)
        {
            // --- JUGADOR 1 ---
            GameObject p1Obj = GameObject.Find("Player1");
            if (p1Obj == null)
            {
                p1Obj = new GameObject("Player1");
                p1Obj.transform.position = new Vector3(-3.5f, -1.8f, 0);
            }
            p1 = p1Obj.GetComponent<FighterController>();
            if (p1 == null) p1 = p1Obj.AddComponent<FighterController>();
            p1.playerId = 1;
            p1.fighterName = "El Musculoso";
            p1.isAI = false;

            var p1Col = p1Obj.GetComponent<CapsuleCollider2D>();
            if (p1Col == null) p1Col = p1Obj.AddComponent<CapsuleCollider2D>();
            p1Col.size = new Vector2(1.1f, 2.3f);
            p1Col.offset = new Vector2(0, 1.15f);

            var p1Body = p1Obj.GetComponentInChildren<FighterBodyController>();
            if (p1Body == null)
            {
                GameObject p1BodyObj = new GameObject("Body");
                p1BodyObj.transform.SetParent(p1Obj.transform, false);
                p1Body = p1BodyObj.AddComponent<FighterBodyController>();
            }
            p1Body.SetClassicBody(FighterBodyType.Musculoso);
            p1.bodyController = p1Body;

            // Cabeza y Caras de P1
            var p1Face = p1Obj.GetComponentInChildren<DynamicFaceController>();
            if (p1Face == null)
            {
                GameObject p1HeadObj = new GameObject("Head_Face");
                p1HeadObj.transform.SetParent(p1Body.neckPoint != null ? p1Body.neckPoint : p1Obj.transform, false);
                p1HeadObj.transform.localPosition = Vector3.zero;
                p1HeadObj.transform.localScale = Vector3.one;

                p1Face = p1HeadObj.AddComponent<DynamicFaceController>();
                var sr = p1HeadObj.GetComponent<SpriteRenderer>();
                sr.sortingOrder = 10;
            }
            else
            {
                p1Face.transform.SetParent(p1Body.neckPoint != null ? p1Body.neckPoint : p1Obj.transform, false);
                p1Face.transform.localPosition = Vector3.zero;
            }
            p1.faceController = p1Face;

            // Cargar perfil de P1 (revisando Jugador_1 o Luchador_1)
            FaceProfile p1Profile = LoadProfileForFighter(1, "El Musculoso");
            p1Face.SetProfile(p1Profile);

            // --- JUGADOR 2 ---
            GameObject p2Obj = GameObject.Find("Player2");
            if (p2Obj == null)
            {
                p2Obj = new GameObject("Player2");
                p2Obj.transform.position = new Vector3(3.5f, -1.8f, 0);
            }
            p2 = p2Obj.GetComponent<FighterController>();
            if (p2 == null) p2 = p2Obj.AddComponent<FighterController>();
            p2.playerId = 2;
            p2.fighterName = "El Gordo";
            p2.isAI = true;

            var p2Col = p2Obj.GetComponent<CapsuleCollider2D>();
            if (p2Col == null) p2Col = p2Obj.AddComponent<CapsuleCollider2D>();
            p2Col.size = new Vector2(1.2f, 2.3f);
            p2Col.offset = new Vector2(0, 1.15f);

            var p2Body = p2Obj.GetComponentInChildren<FighterBodyController>();
            if (p2Body == null)
            {
                GameObject p2BodyObj = new GameObject("Body");
                p2BodyObj.transform.SetParent(p2Obj.transform, false);
                p2Body = p2BodyObj.AddComponent<FighterBodyController>();
            }
            p2Body.SetClassicBody(FighterBodyType.Gordo);
            p2.bodyController = p2Body;

            // Cabeza y Caras de P2
            var p2Face = p2Obj.GetComponentInChildren<DynamicFaceController>();
            if (p2Face == null)
            {
                GameObject p2HeadObj = new GameObject("Head_Face");
                p2HeadObj.transform.SetParent(p2Body.neckPoint != null ? p2Body.neckPoint : p2Obj.transform, false);
                p2HeadObj.transform.localPosition = Vector3.zero;
                p2HeadObj.transform.localScale = Vector3.one;

                p2Face = p2HeadObj.AddComponent<DynamicFaceController>();
                var sr = p2HeadObj.GetComponent<SpriteRenderer>();
                sr.sortingOrder = 10;
            }
            else
            {
                p2Face.transform.SetParent(p2Body.neckPoint != null ? p2Body.neckPoint : p2Obj.transform, false);
                p2Face.transform.localPosition = Vector3.zero;
            }
            p2.faceController = p2Face;

            // Cargar perfil de P2
            FaceProfile p2Profile = LoadProfileForFighter(2, "El Gordo");
            p2Face.SetProfile(p2Profile);

            // Enlazar oponentes
            p1.opponent = p2.transform;
            p2.opponent = p1.transform;
        }

        private FaceProfile LoadProfileForFighter(int id, string defaultName)
        {
            FaceProfile profile = new FaceProfile();
            profile.fighterId = id;
            profile.fighterName = defaultName;

            string dir1 = Path.Combine(Application.persistentDataPath, "Luchadores", $"Jugador_{id}");
            string dir2 = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{id}");

            if (Directory.Exists(dir1) && profile.LoadFromDirectory(dir1))
            {
                return profile;
            }
            if (Directory.Exists(dir2) && profile.LoadFromDirectory(dir2))
            {
                return profile;
            }

            profile = FaceLoader.CreateDefaultProceduralProfile(id);
            profile.fighterName = defaultName;
            return profile;
        }

        private void SetupUI(FighterController p1, FighterController p2, out BattleUI battleUI, out WebcamCaptureManager webcamMgr)
        {
            GameObject canvasObj = GameObject.Find("BattleCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("BattleCanvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            battleUI = canvasObj.GetComponent<BattleUI>();
            if (battleUI == null) battleUI = canvasObj.AddComponent<BattleUI>();

            BuildHealthBarUI(canvasObj.transform, battleUI, p1, p2);
            BuildCenterBannerUI(canvasObj.transform, battleUI);
            BuildControlsBottomBar(canvasObj.transform, battleUI);
            BuildWebcamModal(canvasObj.transform, battleUI, out webcamMgr);
            BuildCharacterSelectMenu(canvasObj.transform, battleUI, p1, p2, webcamMgr);
        }

        private void BuildHealthBarUI(Transform canvas, BattleUI ui, FighterController p1, FighterController p2)
        {
            GameObject topBar = new GameObject("TopHealthBar");
            topBar.transform.SetParent(canvas, false);
            var topRect = topBar.AddComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 140);
            topRect.anchoredPosition = new Vector2(0, 0);

            // P1 Retrato
            GameObject p1PortraitObj = new GameObject("P1_Portrait");
            p1PortraitObj.transform.SetParent(topBar.transform, false);
            var p1PRect = p1PortraitObj.AddComponent<RectTransform>();
            p1PRect.anchorMin = new Vector2(0, 1);
            p1PRect.anchorMax = new Vector2(0, 1);
            p1PRect.pivot = new Vector2(0, 1);
            p1PRect.sizeDelta = new Vector2(90, 90);
            p1PRect.anchoredPosition = new Vector2(30, -20);
            ui.p1FacePortrait = p1PortraitObj.AddComponent<Image>();
            ui.p1FacePortrait.sprite = p1.faceController.Profile.GetSprite(FaceType.Base);

            // P1 HP Fondo
            GameObject p1HpBg = new GameObject("P1_Hp_Bg");
            p1HpBg.transform.SetParent(topBar.transform, false);
            var p1HpBgRect = p1HpBg.AddComponent<RectTransform>();
            p1HpBgRect.anchorMin = new Vector2(0, 1);
            p1HpBgRect.anchorMax = new Vector2(0, 1);
            p1HpBgRect.pivot = new Vector2(0, 1);
            p1HpBgRect.sizeDelta = new Vector2(480, 36);
            p1HpBgRect.anchoredPosition = new Vector2(130, -35);
            var p1BgImg = p1HpBg.AddComponent<Image>();
            p1BgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            Sprite whiteSprite = CreateWhiteUiSprite();

            // P1 Ghost Bar
            GameObject p1GhostObj = new GameObject("P1_Hp_Ghost");
            p1GhostObj.transform.SetParent(p1HpBg.transform, false);
            var p1GhostRect = p1GhostObj.AddComponent<RectTransform>();
            p1GhostRect.anchorMin = Vector2.zero;
            p1GhostRect.anchorMax = Vector2.one;
            p1GhostRect.sizeDelta = Vector2.zero;
            ui.p1HealthGhost = p1GhostObj.AddComponent<Image>();
            ui.p1HealthGhost.sprite = whiteSprite;
            ui.p1HealthGhost.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            ui.p1HealthGhost.type = Image.Type.Filled;
            ui.p1HealthGhost.fillMethod = Image.FillMethod.Horizontal;
            ui.p1HealthGhost.fillOrigin = 0; // (int)Image.OriginHorizontal.Left (se vacía hacia el retrato en la izquierda)

            // P1 Fill Bar
            GameObject p1FillObj = new GameObject("P1_Hp_Fill");
            p1FillObj.transform.SetParent(p1HpBg.transform, false);
            var p1FillRect = p1FillObj.AddComponent<RectTransform>();
            p1FillRect.anchorMin = Vector2.zero;
            p1FillRect.anchorMax = Vector2.one;
            p1FillRect.sizeDelta = Vector2.zero;
            ui.p1HealthFill = p1FillObj.AddComponent<Image>();
            ui.p1HealthFill.sprite = whiteSprite;
            ui.p1HealthFill.color = new Color(0.2f, 0.9f, 0.3f);
            ui.p1HealthFill.type = Image.Type.Filled;
            ui.p1HealthFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p1HealthFill.fillOrigin = 0; // (int)Image.OriginHorizontal.Left

            // P1 HP Texto numérico
            GameObject p1HpTxtObj = new GameObject("P1_Hp_Text");
            p1HpTxtObj.transform.SetParent(p1HpBg.transform, false);
            var p1HpTxtRect = p1HpTxtObj.AddComponent<RectTransform>();
            p1HpTxtRect.anchorMin = Vector2.zero;
            p1HpTxtRect.anchorMax = Vector2.one;
            p1HpTxtRect.sizeDelta = Vector2.zero;
            ui.p1HealthText = p1HpTxtObj.AddComponent<Text>();
            ui.p1HealthText.text = "100 / 100";
            ui.p1HealthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.p1HealthText.fontSize = 17;
            ui.p1HealthText.fontStyle = FontStyle.Bold;
            ui.p1HealthText.alignment = TextAnchor.MiddleCenter;
            ui.p1HealthText.color = Color.white;
            var p1Outline = p1HpTxtObj.AddComponent<Outline>();
            p1Outline.effectColor = Color.black;
            p1Outline.effectDistance = new Vector2(1, -1);

            // P1 Nombre
            GameObject p1NameObj = new GameObject("P1_Name");
            p1NameObj.transform.SetParent(topBar.transform, false);
            var p1NameRect = p1NameObj.AddComponent<RectTransform>();
            p1NameRect.anchorMin = new Vector2(0, 1);
            p1NameRect.anchorMax = new Vector2(0, 1);
            p1NameRect.pivot = new Vector2(0, 1);
            p1NameRect.sizeDelta = new Vector2(300, 30);
            p1NameRect.anchoredPosition = new Vector2(130, -75);
            ui.p1NameText = p1NameObj.AddComponent<Text>();
            ui.p1NameText.text = "P1: " + p1.fighterName;
            ui.p1NameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.p1NameText.fontSize = 20;
            ui.p1NameText.fontStyle = FontStyle.Bold;
            ui.p1NameText.color = Color.white;

            // P2 Retrato
            GameObject p2PortraitObj = new GameObject("P2_Portrait");
            p2PortraitObj.transform.SetParent(topBar.transform, false);
            var p2PRect = p2PortraitObj.AddComponent<RectTransform>();
            p2PRect.anchorMin = new Vector2(1, 1);
            p2PRect.anchorMax = new Vector2(1, 1);
            p2PRect.pivot = new Vector2(1, 1);
            p2PRect.sizeDelta = new Vector2(90, 90);
            p2PRect.anchoredPosition = new Vector2(-30, -20);
            ui.p2FacePortrait = p2PortraitObj.AddComponent<Image>();
            ui.p2FacePortrait.sprite = p2.faceController.Profile.GetSprite(FaceType.Base);

            // P2 HP Fondo
            GameObject p2HpBg = new GameObject("P2_Hp_Bg");
            p2HpBg.transform.SetParent(topBar.transform, false);
            var p2HpBgRect = p2HpBg.AddComponent<RectTransform>();
            p2HpBgRect.anchorMin = new Vector2(1, 1);
            p2HpBgRect.anchorMax = new Vector2(1, 1);
            p2HpBgRect.pivot = new Vector2(1, 1);
            p2HpBgRect.sizeDelta = new Vector2(480, 36);
            p2HpBgRect.anchoredPosition = new Vector2(-130, -35);
            var p2BgImg = p2HpBg.AddComponent<Image>();
            p2BgImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            // P2 Ghost Bar
            GameObject p2GhostObj = new GameObject("P2_Hp_Ghost");
            p2GhostObj.transform.SetParent(p2HpBg.transform, false);
            var p2GhostRect = p2GhostObj.AddComponent<RectTransform>();
            p2GhostRect.anchorMin = Vector2.zero;
            p2GhostRect.anchorMax = Vector2.one;
            p2GhostRect.sizeDelta = Vector2.zero;
            ui.p2HealthGhost = p2GhostObj.AddComponent<Image>();
            ui.p2HealthGhost.sprite = whiteSprite;
            ui.p2HealthGhost.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            ui.p2HealthGhost.type = Image.Type.Filled;
            ui.p2HealthGhost.fillMethod = Image.FillMethod.Horizontal;
            ui.p2HealthGhost.fillOrigin = 1; // (int)Image.OriginHorizontal.Right (se vacía hacia el retrato en la derecha)

            // P2 Fill Bar
            GameObject p2FillObj = new GameObject("P2_Hp_Fill");
            p2FillObj.transform.SetParent(p2HpBg.transform, false);
            var p2FillRect = p2FillObj.AddComponent<RectTransform>();
            p2FillRect.anchorMin = Vector2.zero;
            p2FillRect.anchorMax = Vector2.one;
            p2FillRect.sizeDelta = Vector2.zero;
            ui.p2HealthFill = p2FillObj.AddComponent<Image>();
            ui.p2HealthFill.sprite = whiteSprite;
            ui.p2HealthFill.color = new Color(0.2f, 0.9f, 0.3f);
            ui.p2HealthFill.type = Image.Type.Filled;
            ui.p2HealthFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p2HealthFill.fillOrigin = 1; // (int)Image.OriginHorizontal.Right

            // P2 HP Texto numérico
            GameObject p2HpTxtObj = new GameObject("P2_Hp_Text");
            p2HpTxtObj.transform.SetParent(p2HpBg.transform, false);
            var p2HpTxtRect = p2HpTxtObj.AddComponent<RectTransform>();
            p2HpTxtRect.anchorMin = Vector2.zero;
            p2HpTxtRect.anchorMax = Vector2.one;
            p2HpTxtRect.sizeDelta = Vector2.zero;
            ui.p2HealthText = p2HpTxtObj.AddComponent<Text>();
            ui.p2HealthText.text = "100 / 100";
            ui.p2HealthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.p2HealthText.fontSize = 17;
            ui.p2HealthText.fontStyle = FontStyle.Bold;
            ui.p2HealthText.alignment = TextAnchor.MiddleCenter;
            ui.p2HealthText.color = Color.white;
            var p2Outline = p2HpTxtObj.AddComponent<Outline>();
            p2Outline.effectColor = Color.black;
            p2Outline.effectDistance = new Vector2(1, -1);

            // P2 Nombre
            GameObject p2NameObj = new GameObject("P2_Name");
            p2NameObj.transform.SetParent(topBar.transform, false);
            var p2NameRect = p2NameObj.AddComponent<RectTransform>();
            p2NameRect.anchorMin = new Vector2(1, 1);
            p2NameRect.anchorMax = new Vector2(1, 1);
            p2NameRect.pivot = new Vector2(1, 1);
            p2NameRect.sizeDelta = new Vector2(300, 30);
            p2NameRect.anchoredPosition = new Vector2(-130, -75);
            ui.p2NameText = p2NameObj.AddComponent<Text>();
            ui.p2NameText.text = "P2: " + p2.fighterName + " (CPU IA)";
            ui.p2NameText.alignment = TextAnchor.UpperRight;
            ui.p2NameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.p2NameText.fontSize = 20;
            ui.p2NameText.fontStyle = FontStyle.Bold;
            ui.p2NameText.color = Color.white;

            // Timer
            GameObject timerObj = new GameObject("Timer_Text");
            timerObj.transform.SetParent(topBar.transform, false);
            var timerRect = timerObj.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1);
            timerRect.anchorMax = new Vector2(0.5f, 1);
            timerRect.pivot = new Vector2(0.5f, 1);
            timerRect.sizeDelta = new Vector2(160, 80);
            timerRect.anchoredPosition = new Vector2(0, -20);
            ui.timerText = timerObj.AddComponent<Text>();
            ui.timerText.text = "99";
            ui.timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.timerText.fontSize = 54;
            ui.timerText.fontStyle = FontStyle.Bold;
            ui.timerText.alignment = TextAnchor.MiddleCenter;
            ui.timerText.color = new Color(1f, 0.9f, 0.1f);
        }

        private void BuildCenterBannerUI(Transform canvas, BattleUI ui)
        {
            GameObject bannerPanel = new GameObject("CenterBanner");
            bannerPanel.transform.SetParent(canvas, false);
            var bannerRect = bannerPanel.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(850, 190);
            bannerRect.anchoredPosition = new Vector2(0, 50);

            var bg = bannerPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.12f, 0.92f);

            GameObject textObj = new GameObject("BannerText");
            textObj.transform.SetParent(bannerPanel.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            ui.centerBannerText = textObj.AddComponent<Text>();
            ui.centerBannerText.text = "¡A PELEAR!";
            ui.centerBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.centerBannerText.fontSize = 52;
            ui.centerBannerText.fontStyle = FontStyle.Bold;
            ui.centerBannerText.alignment = TextAnchor.MiddleCenter;
            ui.centerBannerText.color = new Color(1f, 0.25f, 0.2f);

            ui.centerBannerPanel = bannerPanel;
            bannerPanel.SetActive(false);
        }

        private void BuildControlsBottomBar(Transform canvas, BattleUI ui)
        {
            GameObject bottomBar = new GameObject("BottomControlsBar");
            bottomBar.transform.SetParent(canvas, false);
            var bRect = bottomBar.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0);
            bRect.anchorMax = new Vector2(1, 0);
            bRect.pivot = new Vector2(0.5f, 0);
            bRect.sizeDelta = new Vector2(0, 75);
            bRect.anchoredPosition = new Vector2(0, 0);

            var bg = bottomBar.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.90f);

            // Texto de controles
            GameObject infoObj = new GameObject("ControlsText");
            infoObj.transform.SetParent(bottomBar.transform, false);
            var infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(0.48f, 1);
            infoRect.offsetMin = new Vector2(20, 5);
            infoRect.offsetMax = new Vector2(0, -5);

            var text = infoObj.AddComponent<Text>();
            text.text = "<b>P1:</b> [A/D] Mover | [W] Salto | [F/Espacio] Puño | [G] Patada\n<b>P2:</b> [←/→] Mover | [↑] Salto | [L] Puño | [K] Patada";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 15;
            text.color = new Color(0.9f, 0.95f, 1f);
            text.alignment = TextAnchor.MiddleLeft;

            // Botón 0: 🥊 Personajes [M / ESC]
            GameObject btnMenuObj = new GameObject("Btn_SelectMenu");
            btnMenuObj.transform.SetParent(bottomBar.transform, false);
            var mRect = btnMenuObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.49f, 0.15f);
            mRect.anchorMax = new Vector2(0.61f, 0.85f);
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;
            var mImg = btnMenuObj.AddComponent<Image>();
            mImg.color = new Color(0.95f, 0.25f, 0.35f);
            ui.openSelectMenuButton = btnMenuObj.AddComponent<Button>();
            ui.openSelectMenuButton.onClick.AddListener(() =>
            {
                if (BattleUI.Instance != null)
                    BattleUI.Instance.ToggleCharacterSelectMenu();
            });

            GameObject mTxtObj = new GameObject("Text");
            mTxtObj.transform.SetParent(btnMenuObj.transform, false);
            var mTRect = mTxtObj.AddComponent<RectTransform>();
            mTRect.anchorMin = Vector2.zero;
            mTRect.anchorMax = Vector2.one;
            mTRect.sizeDelta = Vector2.zero;
            var mTxt = mTxtObj.AddComponent<Text>();
            mTxt.text = "🥊 Menú [M]";
            mTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mTxt.fontSize = 15;
            mTxt.fontStyle = FontStyle.Bold;
            mTxt.alignment = TextAnchor.MiddleCenter;
            mTxt.color = Color.white;

            // Botón 1: 🏆 Torneo Street Fighter [T]
            GameObject btnTourneyObj = new GameObject("Btn_Tournament");
            btnTourneyObj.transform.SetParent(bottomBar.transform, false);
            var tRect = btnTourneyObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.62f, 0.15f);
            tRect.anchorMax = new Vector2(0.73f, 0.85f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tImg = btnTourneyObj.AddComponent<Image>();
            tImg.color = new Color(1f, 0.65f, 0.1f);
            ui.tournamentButton = btnTourneyObj.AddComponent<Button>();

            GameObject tTxtObj = new GameObject("Text");
            tTxtObj.transform.SetParent(btnTourneyObj.transform, false);
            var tTRect = tTxtObj.AddComponent<RectTransform>();
            tTRect.anchorMin = Vector2.zero;
            tTRect.anchorMax = Vector2.one;
            tTRect.sizeDelta = Vector2.zero;
            var tTxt = tTxtObj.AddComponent<Text>();
            tTxt.text = "🏆 Torneo [T]";
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 15;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = Color.black;

            // Botón 2: 📸 Caras / Webcam [C]
            GameObject btnCustomObj = new GameObject("Btn_CustomFaces");
            btnCustomObj.transform.SetParent(bottomBar.transform, false);
            var btnRect = btnCustomObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.74f, 0.15f);
            btnRect.anchorMax = new Vector2(0.87f, 0.85f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            var btnImg = btnCustomObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f);
            ui.openCustomizerButton = btnCustomObj.AddComponent<Button>();

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnCustomObj.transform, false);
            var btnTRect = btnTextObj.AddComponent<RectTransform>();
            btnTRect.anchorMin = Vector2.zero;
            btnTRect.anchorMax = Vector2.one;
            btnTRect.sizeDelta = Vector2.zero;
            var btnTxt = btnTextObj.AddComponent<Text>();
            btnTxt.text = "📸 Caras [C]";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 15;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;

            // Botón 3: 🔄 Reiniciar [R]
            GameObject btnRestartObj = new GameObject("Btn_Restart");
            btnRestartObj.transform.SetParent(bottomBar.transform, false);
            var rRect = btnRestartObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.88f, 0.15f);
            rRect.anchorMax = new Vector2(0.98f, 0.85f);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;
            var rImg = btnRestartObj.AddComponent<Image>();
            rImg.color = new Color(0.9f, 0.3f, 0.2f);
            ui.rematchButton = btnRestartObj.AddComponent<Button>();

            GameObject rTextObj = new GameObject("Text");
            rTextObj.transform.SetParent(btnRestartObj.transform, false);
            var rTRect = rTextObj.AddComponent<RectTransform>();
            rTRect.anchorMin = Vector2.zero;
            rTRect.anchorMax = Vector2.one;
            rTRect.sizeDelta = Vector2.zero;
            var rTxt = rTextObj.AddComponent<Text>();
            rTxt.text = "🔄 Reiniciar [R]";
            rTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rTxt.fontSize = 15;
            rTxt.fontStyle = FontStyle.Bold;
            rTxt.alignment = TextAnchor.MiddleCenter;
            rTxt.color = Color.white;
        }

        private void BuildWebcamModal(Transform canvas, BattleUI ui, out WebcamCaptureManager webcamMgr)
        {
            GameObject modalObj = new GameObject("Modal_FaceCustomizer");
            modalObj.transform.SetParent(canvas, false);
            var mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.sizeDelta = new Vector2(1060, 720);
            mRect.anchoredPosition = Vector2.zero;

            var modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);

            webcamMgr = modalObj.AddComponent<WebcamCaptureManager>();

            // Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 1);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.pivot = new Vector2(0.5f, 1);
            tRect.sizeDelta = new Vector2(0, 50);
            tRect.anchoredPosition = new Vector2(0, -12);
            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "📸 CREADOR DE LUCHADORES - CAPTURA DE ROSTROS";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.85f, 0.2f);

            // Selector de los 8 Luchadores del Torneo
            CreateTournamentRosterButtons(modalObj.transform, webcamMgr);

            // Visor de Cámara en Vivo
            GameObject camViewObj = new GameObject("CamView");
            camViewObj.transform.SetParent(modalObj.transform, false);
            var cRect = camViewObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.04f, 0.26f);
            cRect.anchorMax = new Vector2(0.50f, 0.74f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var camRaw = camViewObj.AddComponent<RawImage>();
            camRaw.color = Color.white;
            webcamMgr.cameraPreviewUI = camRaw;

            var outline = camViewObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.6f, 1f);
            outline.effectDistance = new Vector2(4, 4);

            // Guía de objetivo ovalada transparente (Sticker Target Guide)
            GameObject guideObj = new GameObject("OvalTargetGuide");
            guideObj.transform.SetParent(camViewObj.transform, false);
            var gRect = guideObj.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.20f, 0.06f);
            gRect.anchorMax = new Vector2(0.80f, 0.94f);
            gRect.offsetMin = Vector2.zero;
            gRect.offsetMax = Vector2.zero;
            var gImg = guideObj.AddComponent<Image>();
            gImg.sprite = CreateOvalGuideSprite(200, 260);
            gImg.color = new Color(0.2f, 1f, 0.5f, 0.85f);
            gImg.raycastTarget = false;

            // Botones de orientación (Girar 180° y Espejo)
            CreateCameraOrientationControls(modalObj.transform, webcamMgr);

            // Botones para capturar las 4 fotos
            CreateCaptureButton(modalObj.transform, webcamMgr, "1. Foto BASE (Pose Normal)", FaceType.Base, new Vector2(560, -190), out Image thumbBase);
            CreateCaptureButton(modalObj.transform, webcamMgr, "2. Foto ENOJO (¡Furia y Odio!)", FaceType.Enojo, new Vector2(560, -270), out Image thumbAngry);
            CreateCaptureButton(modalObj.transform, webcamMgr, "3. Foto DOLOR (¡Mueca de Golpe!)", FaceType.Dolor, new Vector2(560, -350), out Image thumbHurt);
            CreateCaptureButton(modalObj.transform, webcamMgr, "4. Foto K.O. (Ojos Cerrados)", FaceType.KO, new Vector2(560, -430), out Image thumbKO);

            webcamMgr.previewThumbBase = thumbBase;
            webcamMgr.previewThumbAngry = thumbAngry;
            webcamMgr.previewThumbHurt = thumbHurt;
            webcamMgr.previewThumbKO = thumbKO;

            // Botón Cerrar / Pelear
            GameObject closeBtnObj = new GameObject("Btn_CloseModal");
            closeBtnObj.transform.SetParent(modalObj.transform, false);
            var clRect = closeBtnObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.5f, 0.05f);
            clRect.anchorMax = new Vector2(0.5f, 0.05f);
            clRect.pivot = new Vector2(0.5f, 0.5f);
            clRect.sizeDelta = new Vector2(280, 50);
            clRect.anchoredPosition = Vector2.zero;

            var clImg = closeBtnObj.AddComponent<Image>();
            clImg.color = new Color(0.2f, 0.8f, 0.35f);
            ui.closeCustomizerButton = closeBtnObj.AddComponent<Button>();

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var clTRect = clTxtObj.AddComponent<RectTransform>();
            clTRect.anchorMin = Vector2.zero;
            clTRect.anchorMax = Vector2.one;
            clTRect.sizeDelta = Vector2.zero;
            var clTxt = clTxtObj.AddComponent<Text>();
            clTxt.text = "✅ ¡LISTO! VOLVER";
            clTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            clTxt.fontSize = 20;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;

            ui.faceCustomizerPanel = modalObj;
            modalObj.SetActive(false);
        }

        private void CreateTournamentRosterButtons(Transform parent, WebcamCaptureManager mgr)
        {
            GameObject container = new GameObject("RosterButtons");
            container.transform.SetParent(parent, false);
            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.77f);
            rect.anchorMax = new Vector2(0.96f, 0.89f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            string[] names = { "1. Panchito", "2. Rocky", "3. Ramón", "4. Titán", "5. Furia", "6. Míster KO", "7. Fantasma", "8. Jefe Final" };
            Color[] colors = {
                new Color(0.9f, 0.2f, 0.2f),
                new Color(0.2f, 0.45f, 0.95f),
                new Color(0.95f, 0.8f, 0.1f),
                new Color(0.2f, 0.85f, 0.35f),
                new Color(0.75f, 0.2f, 0.85f),
                new Color(0.95f, 0.5f, 0.1f),
                new Color(0.1f, 0.85f, 0.85f),
                new Color(0.3f, 0.3f, 0.35f)
            };

            float btnWidth = 1f / 8f;
            for (int i = 0; i < 8; i++)
            {
                int fighterId = i + 1;
                GameObject btnObj = new GameObject($"Btn_Fighter_{fighterId}");
                btnObj.transform.SetParent(container.transform, false);
                var bRect = btnObj.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(i * btnWidth + 0.005f, 0);
                bRect.anchorMax = new Vector2((i + 1) * btnWidth - 0.005f, 1);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                var img = btnObj.AddComponent<Image>();
                img.color = colors[i];
                var btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => mgr.SetTargetFighter(fighterId));

                GameObject txtObj = new GameObject("Text");
                txtObj.transform.SetParent(btnObj.transform, false);
                var tRect = txtObj.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.sizeDelta = Vector2.zero;
                var txt = txtObj.AddComponent<Text>();
                txt.text = names[i];
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 13;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = (i == 2 || i == 6) ? Color.black : Color.white;
            }

            // Etiqueta del luchador activo
            GameObject labelObj = new GameObject("Label_CurrentFighter");
            labelObj.transform.SetParent(parent, false);
            var lRect = labelObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.04f, 0.72f);
            lRect.anchorMax = new Vector2(0.96f, 0.76f);
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;

            var lTxt = labelObj.AddComponent<Text>();
            lTxt.text = "Editando: <b>Luchador 1 (Panchito 'El Bravo')</b>";
            lTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lTxt.fontSize = 17;
            lTxt.alignment = TextAnchor.MiddleLeft;
            lTxt.color = new Color(1f, 0.9f, 0.2f);
            mgr.targetFighterLabel = lTxt;
        }

        private void CreateCameraOrientationControls(Transform parent, WebcamCaptureManager mgr)
        {
            GameObject container = new GameObject("CameraControls");
            container.transform.SetParent(parent, false);
            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.17f);
            rect.anchorMax = new Vector2(0.50f, 0.24f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Botón Girar 180° (Invertir Vertical)
            GameObject btnFlipV = new GameObject("Btn_FlipV");
            btnFlipV.transform.SetParent(container.transform, false);
            var vRect = btnFlipV.AddComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0, 0);
            vRect.anchorMax = new Vector2(0.23f, 1);
            vRect.offsetMin = Vector2.zero;
            vRect.offsetMax = Vector2.zero;
            var vImg = btnFlipV.AddComponent<Image>();
            vImg.color = new Color(0.35f, 0.45f, 0.6f);
            var vBtn = btnFlipV.AddComponent<Button>();
            vBtn.onClick.AddListener(mgr.ToggleFlipVertical);

            GameObject vTextObj = new GameObject("Text");
            vTextObj.transform.SetParent(btnFlipV.transform, false);
            var vTRect = vTextObj.AddComponent<RectTransform>();
            vTRect.anchorMin = Vector2.zero;
            vTRect.anchorMax = Vector2.one;
            vTRect.sizeDelta = Vector2.zero;
            var vTxt = vTextObj.AddComponent<Text>();
            vTxt.text = "🔄 Girar";
            vTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vTxt.fontSize = 13;
            vTxt.fontStyle = FontStyle.Bold;
            vTxt.alignment = TextAnchor.MiddleCenter;
            vTxt.color = Color.white;

            // Botón Espejo Horizontal
            GameObject btnFlipH = new GameObject("Btn_FlipH");
            btnFlipH.transform.SetParent(container.transform, false);
            var hRect = btnFlipH.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.25f, 0);
            hRect.anchorMax = new Vector2(0.48f, 1);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;
            var hImg = btnFlipH.AddComponent<Image>();
            hImg.color = new Color(0.35f, 0.45f, 0.6f);
            var hBtn = btnFlipH.AddComponent<Button>();
            hBtn.onClick.AddListener(mgr.ToggleFlipHorizontal);

            GameObject hTextObj = new GameObject("Text");
            hTextObj.transform.SetParent(btnFlipH.transform, false);
            var hTRect = hTextObj.AddComponent<RectTransform>();
            hTRect.anchorMin = Vector2.zero;
            hTRect.anchorMax = Vector2.one;
            hTRect.sizeDelta = Vector2.zero;
            var hTxt = hTextObj.AddComponent<Text>();
            hTxt.text = "↔️ Espejo";
            hTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hTxt.fontSize = 13;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = Color.white;

            // Botón Zoom + (Acercar más la foto al rostro)
            GameObject btnZoomIn = new GameObject("Btn_ZoomIn");
            btnZoomIn.transform.SetParent(container.transform, false);
            var zInRect = btnZoomIn.AddComponent<RectTransform>();
            zInRect.anchorMin = new Vector2(0.52f, 0);
            zInRect.anchorMax = new Vector2(0.74f, 1);
            zInRect.offsetMin = Vector2.zero;
            zInRect.offsetMax = Vector2.zero;
            var zInImg = btnZoomIn.AddComponent<Image>();
            zInImg.color = new Color(0.25f, 0.65f, 0.35f);
            var zInBtn = btnZoomIn.AddComponent<Button>();
            zInBtn.onClick.AddListener(mgr.ZoomIn);

            GameObject zInTxtObj = new GameObject("Text");
            zInTxtObj.transform.SetParent(btnZoomIn.transform, false);
            var zInTRect = zInTxtObj.AddComponent<RectTransform>();
            zInTRect.anchorMin = Vector2.zero;
            zInTRect.anchorMax = Vector2.one;
            zInTRect.sizeDelta = Vector2.zero;
            var zInTxt = zInTxtObj.AddComponent<Text>();
            zInTxt.text = "🔍 Zoom +";
            zInTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            zInTxt.fontSize = 13;
            zInTxt.fontStyle = FontStyle.Bold;
            zInTxt.alignment = TextAnchor.MiddleCenter;
            zInTxt.color = Color.white;

            // Botón Zoom - (Alejar foto)
            GameObject btnZoomOut = new GameObject("Btn_ZoomOut");
            btnZoomOut.transform.SetParent(container.transform, false);
            var zOutRect = btnZoomOut.AddComponent<RectTransform>();
            zOutRect.anchorMin = new Vector2(0.76f, 0);
            zOutRect.anchorMax = new Vector2(0.99f, 1);
            zOutRect.offsetMin = Vector2.zero;
            zOutRect.offsetMax = Vector2.zero;
            var zOutImg = btnZoomOut.AddComponent<Image>();
            zOutImg.color = new Color(0.55f, 0.45f, 0.25f);
            var zOutBtn = btnZoomOut.AddComponent<Button>();
            zOutBtn.onClick.AddListener(mgr.ZoomOut);

            GameObject zOutTxtObj = new GameObject("Text");
            zOutTxtObj.transform.SetParent(btnZoomOut.transform, false);
            var zOutTRect = zOutTxtObj.AddComponent<RectTransform>();
            zOutTRect.anchorMin = Vector2.zero;
            zOutTRect.anchorMax = Vector2.one;
            zOutTRect.sizeDelta = Vector2.zero;
            var zOutTxt = zOutTxtObj.AddComponent<Text>();
            zOutTxt.text = "🔍 Zoom -";
            zOutTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            zOutTxt.fontSize = 13;
            zOutTxt.fontStyle = FontStyle.Bold;
            zOutTxt.alignment = TextAnchor.MiddleCenter;
            zOutTxt.color = Color.white;

            // Estado de Giro y Zoom
            GameObject statusObj = new GameObject("Status_OrientationZoom");
            statusObj.transform.SetParent(parent, false);
            var sRect = statusObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.04f, 0.11f);
            sRect.anchorMax = new Vector2(0.50f, 0.16f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            var sTxt = statusObj.AddComponent<Text>();
            sTxt.text = $"Giro: ON | Espejo: OFF | Zoom: {mgr.faceZoom:F1}x";
            sTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sTxt.fontSize = 14;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = new Color(0.9f, 0.9f, 0.95f);
            mgr.flipStatusText = sTxt;
        }

        private void CreateCaptureButton(Transform parent, WebcamCaptureManager mgr, string label, FaceType emotion, Vector2 pos, out Image thumb)
        {
            GameObject container = new GameObject("CaptureRow_" + emotion);
            container.transform.SetParent(parent, false);
            var cRect = container.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(0, 1);
            cRect.pivot = new Vector2(0, 1);
            cRect.sizeDelta = new Vector2(460, 68);
            cRect.anchoredPosition = pos;

            // Miniatura
            GameObject thumbObj = new GameObject("Thumbnail");
            thumbObj.transform.SetParent(container.transform, false);
            var tRect = thumbObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.5f);
            tRect.anchorMax = new Vector2(0, 0.5f);
            tRect.pivot = new Vector2(0, 0.5f);
            tRect.sizeDelta = new Vector2(60, 60);
            tRect.anchoredPosition = new Vector2(5, 0);
            thumb = thumbObj.AddComponent<Image>();
            thumb.color = Color.white;

            // Botón
            GameObject btnObj = new GameObject("Button");
            btnObj.transform.SetParent(container.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0.5f);
            bRect.anchorMax = new Vector2(0, 0.5f);
            bRect.pivot = new Vector2(0, 0.5f);
            bRect.sizeDelta = new Vector2(370, 58);
            bRect.anchoredPosition = new Vector2(80, 0);

            var bImg = btnObj.AddComponent<Image>();
            bImg.color = emotion == FaceType.Enojo 
                ? new Color(0.9f, 0.2f, 0.2f) 
                : (emotion == FaceType.Dolor ? new Color(0.85f, 0.55f, 0.15f) : (emotion == FaceType.KO ? new Color(0.4f, 0.4f, 0.5f) : new Color(0.2f, 0.5f, 0.85f)));

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => mgr.CaptureCurrentFrameAs(emotion));

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private void SetupBattleManager(FighterController p1, FighterController p2)
        {
            GameObject mgrObj = GameObject.Find("BattleManager");
            if (mgrObj == null) mgrObj = new GameObject("BattleManager");

            var bm = mgrObj.GetComponent<BattleManager>();
            if (bm == null) bm = mgrObj.AddComponent<BattleManager>();

            bm.player1 = p1;
            bm.player2 = p2;
            bm.isP2ControlledByAI = true;
        }

        private void SetupTournamentManager(FighterController p1, FighterController p2)
        {
            GameObject tmObj = GameObject.Find("TournamentManager");
            if (tmObj == null) tmObj = new GameObject("TournamentManager");

            var tm = tmObj.GetComponent<TournamentManager>();
            if (tm == null) tm = tmObj.AddComponent<TournamentManager>();
        }

        private Sprite CreateBoxSprite(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.SetPixel(1, 0, color);
            tex.SetPixel(0, 1, color);
            tex.SetPixel(1, 1, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite CreateGradientBackdrop(int width, int height)
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color top = new Color(0.10f, 0.06f, 0.18f);
            Color bot = new Color(0.20f, 0.12f, 0.28f);

            for (int y = 0; y < 32; y++)
            {
                Color c = Color.Lerp(bot, top, (float)y / 31f);
                for (int x = 0; x < 32; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 2f);
        }

        // ----------------- PANTALLA DE SELECCIÓN DE LUCHADORES -----------------

        private void BuildCharacterSelectMenu(Transform canvas, BattleUI ui, FighterController p1, FighterController p2, WebcamCaptureManager webcamMgr)
        {
            GameObject menuObj = new GameObject("Panel_CharacterSelect");
            menuObj.transform.SetParent(canvas, false);
            var mRect = menuObj.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;

            var bgImg = menuObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.07f, 0.12f, 0.98f);

            var selectUI = menuObj.AddComponent<CharacterSelectUI>();
            ui.characterSelectPanel = menuObj;

            // Encabezado
            GameObject titleObj = new GameObject("HeaderTitle");
            titleObj.transform.SetParent(menuObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.89f);
            tRect.anchorMax = new Vector2(1, 0.99f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "💥 FIGHT FACE - SELECCIÓN DE LUCHADORES 💥";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.85f, 0.15f);

            GameObject subObj = new GameObject("HeaderSubtitle");
            subObj.transform.SetParent(menuObj.transform, false);
            var sRect = subObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0.85f);
            sRect.anchorMax = new Vector2(1, 0.89f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            var subTxt = subObj.AddComponent<Text>();
            subTxt.text = "Elige el cuerpo de tu peleador y personaliza las caras con tu cámara web";
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 16;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.75f, 0.88f, 1f);

            // Columna Jugador 1
            BuildPlayerSelectionColumn(menuObj.transform, selectUI, 1, new Vector2(0.03f, 0.13f), new Vector2(0.48f, 0.85f));

            // Columna Jugador 2
            BuildPlayerSelectionColumn(menuObj.transform, selectUI, 2, new Vector2(0.52f, 0.13f), new Vector2(0.97f, 0.85f));

            // Botón Inferior: ¡A PELEAR!
            GameObject fightBtnObj = new GameObject("Btn_StartFight");
            fightBtnObj.transform.SetParent(menuObj.transform, false);
            var fbRect = fightBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.5f, 0.03f);
            fbRect.anchorMax = new Vector2(0.5f, 0.03f);
            fbRect.pivot = new Vector2(0.5f, 0);
            fbRect.sizeDelta = new Vector2(420, 60);
            fbRect.anchoredPosition = Vector2.zero;

            var fbImg = fightBtnObj.AddComponent<Image>();
            fbImg.color = new Color(1f, 0.40f, 0.08f);
            var fbOutline = fightBtnObj.AddComponent<Outline>();
            fbOutline.effectColor = new Color(1f, 0.88f, 0.2f);
            fbOutline.effectDistance = new Vector2(3, 3);

            selectUI.fightButton = fightBtnObj.AddComponent<Button>();

            GameObject fbTxtObj = new GameObject("Text");
            fbTxtObj.transform.SetParent(fightBtnObj.transform, false);
            var fbTRect = fbTxtObj.AddComponent<RectTransform>();
            fbTRect.anchorMin = Vector2.zero;
            fbTRect.anchorMax = Vector2.one;
            fbTRect.sizeDelta = Vector2.zero;
            var fbTxt = fbTxtObj.AddComponent<Text>();
            fbTxt.text = "🔥 ¡A PELEAR! [FIGHT / ENTER] 🔥";
            fbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fbTxt.fontSize = 22;
            fbTxt.fontStyle = FontStyle.Bold;
            fbTxt.alignment = TextAnchor.MiddleCenter;
            fbTxt.color = Color.white;
        }

        private void BuildPlayerSelectionColumn(Transform parent, CharacterSelectUI selectUI, int playerNum, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject colObj = new GameObject($"Col_Player{playerNum}");
            colObj.transform.SetParent(parent, false);
            var cRect = colObj.AddComponent<RectTransform>();
            cRect.anchorMin = anchorMin;
            cRect.anchorMax = anchorMax;
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var colBg = colObj.AddComponent<Image>();
            colBg.color = new Color(0.10f, 0.12f, 0.18f, 0.95f);
            var colOutline = colObj.AddComponent<Outline>();
            colOutline.effectColor = playerNum == 1 ? new Color(0.95f, 0.3f, 0.2f) : new Color(0.2f, 0.6f, 1f);
            colOutline.effectDistance = new Vector2(2, 2);

            // Banner del jugador
            GameObject pBanner = new GameObject("Header");
            pBanner.transform.SetParent(colObj.transform, false);
            var pbRect = pBanner.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0, 0.90f);
            pbRect.anchorMax = new Vector2(1, 1f);
            pbRect.offsetMin = Vector2.zero;
            pbRect.offsetMax = Vector2.zero;

            var pText = pBanner.AddComponent<Text>();
            pText.text = playerNum == 1 ? "🔴 JUGADOR 1" : "🔵 JUGADOR 2 / OPONENTE";
            pText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pText.fontSize = 20;
            pText.fontStyle = FontStyle.Bold;
            pText.alignment = TextAnchor.MiddleCenter;
            pText.color = playerNum == 1 ? new Color(1f, 0.4f, 0.3f) : new Color(0.35f, 0.8f, 1f);

            // Si es P2, botón para alternar CPU vs Humano
            if (playerNum == 2)
            {
                GameObject aiBtnObj = new GameObject("Btn_ToggleAI");
                aiBtnObj.transform.SetParent(colObj.transform, false);
                var aiRect = aiBtnObj.AddComponent<RectTransform>();
                aiRect.anchorMin = new Vector2(0.65f, 0.91f);
                aiRect.anchorMax = new Vector2(0.97f, 0.99f);
                aiRect.offsetMin = Vector2.zero;
                aiRect.offsetMax = Vector2.zero;

                var aiImg = aiBtnObj.AddComponent<Image>();
                aiImg.color = new Color(0.2f, 0.45f, 0.85f);
                selectUI.p2AIToggleButton = aiBtnObj.AddComponent<Button>();

                GameObject aiTxtObj = new GameObject("Text");
                aiTxtObj.transform.SetParent(aiBtnObj.transform, false);
                var aiTRect = aiTxtObj.AddComponent<RectTransform>();
                aiTRect.anchorMin = Vector2.zero;
                aiTRect.anchorMax = Vector2.one;
                aiTRect.sizeDelta = Vector2.zero;
                selectUI.p2AILabelText = aiTxtObj.AddComponent<Text>();
                selectUI.p2AILabelText.text = "🤖 CPU";
                selectUI.p2AILabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                selectUI.p2AILabelText.fontSize = 13;
                selectUI.p2AILabelText.fontStyle = FontStyle.Bold;
                selectUI.p2AILabelText.alignment = TextAnchor.MiddleCenter;
                selectUI.p2AILabelText.color = Color.white;
            }

            // Fila de 5 botones de selección de cuerpo
            GameObject bodiesRow = new GameObject("BodiesRow");
            bodiesRow.transform.SetParent(colObj.transform, false);
            var brRect = bodiesRow.AddComponent<RectTransform>();
            brRect.anchorMin = new Vector2(0.02f, 0.80f);
            brRect.anchorMax = new Vector2(0.98f, 0.89f);
            brRect.offsetMin = Vector2.zero;
            brRect.offsetMax = Vector2.zero;

            FighterBodyType[] allBodies = {
                FighterBodyType.Gordo,
                FighterBodyType.Flaco,
                FighterBodyType.Musculoso,
                FighterBodyType.Mujer,
                FighterBodyType.DosCabezas
            };
            string[] shortLabels = { "Gordo", "Flaco", "Musculoso", "Mujer", "2 Cabezas" };

            Button[] buttons = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                FighterBodyType bType = allBodies[i];

                GameObject bObj = new GameObject($"Btn_{bType}");
                bObj.transform.SetParent(bodiesRow.transform, false);
                var bRect = bObj.AddComponent<RectTransform>();
                float startX = idx * 0.20f;
                float endX = (idx + 1) * 0.20f;
                bRect.anchorMin = new Vector2(startX + 0.01f, 0.05f);
                bRect.anchorMax = new Vector2(endX - 0.01f, 0.95f);
                bRect.offsetMin = Vector2.zero;
                bRect.offsetMax = Vector2.zero;

                var bImg = bObj.AddComponent<Image>();
                bImg.color = new Color(0.18f, 0.22f, 0.32f);
                var btn = bObj.AddComponent<Button>();
                buttons[i] = btn;

                btn.onClick.AddListener(() =>
                {
                    if (playerNum == 1) selectUI.SelectP1Body(bType);
                    else selectUI.SelectP2Body(bType);
                });

                GameObject bTxtObj = new GameObject("Text");
                bTxtObj.transform.SetParent(bObj.transform, false);
                var bTRect = bTxtObj.AddComponent<RectTransform>();
                bTRect.anchorMin = Vector2.zero;
                bTRect.anchorMax = Vector2.one;
                bTRect.sizeDelta = Vector2.zero;
                var bTxt = bTxtObj.AddComponent<Text>();
                bTxt.text = shortLabels[i];
                bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bTxt.fontSize = 13;
                bTxt.fontStyle = FontStyle.Bold;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.color = Color.white;
            }

            if (playerNum == 1) selectUI.p1BodyButtons = buttons;
            else selectUI.p2BodyButtons = buttons;

            // Tarjeta de Imagen del Cuerpo Completo
            GameObject cardImgObj = new GameObject("FighterCardImage");
            cardImgObj.transform.SetParent(colObj.transform, false);
            var cardRect = cardImgObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.06f, 0.28f);
            cardRect.anchorMax = new Vector2(0.68f, 0.78f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            var cardImg = cardImgObj.AddComponent<Image>();
            cardImg.preserveAspect = true;
            if (playerNum == 1) selectUI.p1FighterCardImage = cardImg;
            else selectUI.p2FighterCardImage = cardImg;

            // Miniatura del rostro ovalado actual
            GameObject facePreviewObj = new GameObject("HeadStickerPreview");
            facePreviewObj.transform.SetParent(colObj.transform, false);
            var fpRect = facePreviewObj.AddComponent<RectTransform>();
            fpRect.anchorMin = new Vector2(0.72f, 0.50f);
            fpRect.anchorMax = new Vector2(0.95f, 0.78f);
            fpRect.offsetMin = Vector2.zero;
            fpRect.offsetMax = Vector2.zero;

            var faceImg = facePreviewObj.AddComponent<Image>();
            faceImg.preserveAspect = true;
            var fpOutline = facePreviewObj.AddComponent<Outline>();
            fpOutline.effectColor = Color.white;
            fpOutline.effectDistance = new Vector2(2, 2);

            if (playerNum == 1) selectUI.p1HeadPreviewImage = faceImg;
            else selectUI.p2HeadPreviewImage = faceImg;

            // Nombre y Descripción
            GameObject nameObj = new GameObject("FighterName");
            nameObj.transform.SetParent(colObj.transform, false);
            var nRect = nameObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.06f, 0.21f);
            nRect.anchorMax = new Vector2(0.94f, 0.27f);
            nRect.offsetMin = Vector2.zero;
            nRect.offsetMax = Vector2.zero;

            var nTxt = nameObj.AddComponent<Text>();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 20;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.alignment = TextAnchor.MiddleLeft;
            nTxt.color = new Color(1f, 0.9f, 0.2f);
            if (playerNum == 1) selectUI.p1NameText = nTxt;
            else selectUI.p2NameText = nTxt;

            GameObject descObj = new GameObject("FighterDesc");
            descObj.transform.SetParent(colObj.transform, false);
            var dRect = descObj.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.06f, 0.12f);
            dRect.anchorMax = new Vector2(0.94f, 0.20f);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;

            var dTxt = descObj.AddComponent<Text>();
            dTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dTxt.fontSize = 14;
            dTxt.alignment = TextAnchor.UpperLeft;
            dTxt.color = new Color(0.85f, 0.88f, 0.95f);
            if (playerNum == 1) selectUI.p1DescText = dTxt;
            else selectUI.p2DescText = dTxt;

            // Botón: 📷 Personalizar Rostro con Webcam
            GameObject camBtnObj = new GameObject($"Btn_Webcam_P{playerNum}");
            camBtnObj.transform.SetParent(colObj.transform, false);
            var cbRect = camBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.06f, 0.02f);
            cbRect.anchorMax = new Vector2(0.94f, 0.10f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            var cbImg = camBtnObj.AddComponent<Image>();
            cbImg.color = playerNum == 1 ? new Color(0.18f, 0.65f, 0.95f) : new Color(0.70f, 0.25f, 0.95f);
            var cbBtn = camBtnObj.AddComponent<Button>();

            if (playerNum == 1) selectUI.p1WebcamButton = cbBtn;
            else selectUI.p2WebcamButton = cbBtn;

            GameObject cbTxtObj = new GameObject("Text");
            cbTxtObj.transform.SetParent(camBtnObj.transform, false);
            var cbTRect = cbTxtObj.AddComponent<RectTransform>();
            cbTRect.anchorMin = Vector2.zero;
            cbTRect.anchorMax = Vector2.one;
            cbTRect.sizeDelta = Vector2.zero;
            var cbTxt = cbTxtObj.AddComponent<Text>();
            cbTxt.text = $"📸 Personalizar Rostro P{playerNum} (Webcam)";
            cbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cbTxt.fontSize = 17;
            cbTxt.fontStyle = FontStyle.Bold;
            cbTxt.alignment = TextAnchor.MiddleCenter;
            cbTxt.color = Color.white;
        }

        private Sprite CreateOvalGuideSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color lineCol = new Color(0.2f, 1f, 0.5f, 0.85f);
            Color fillCenter = new Color(0.2f, 1f, 0.5f, 0.05f);

            float cx = width / 2f;
            float cy = height / 2f;
            float rx = width * 0.45f;
            float ry = height * 0.47f;

            for (int y = 0; y < height; y++)
            {
                float ny = (y - cy) / ry;
                if (Mathf.Abs(ny) > 1f)
                {
                    for (int x = 0; x < width; x++) tex.SetPixel(x, y, clear);
                    continue;
                }

                float taper = 1f + (0.15f * ny);
                float currRx = rx * taper;

                for (int x = 0; x < width; x++)
                {
                    float nx = (x - cx) / currRx;
                    float distSq = (nx * nx) + (ny * ny);

                    if (distSq > 1.0f)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else if (distSq > 0.86f)
                    {
                        tex.SetPixel(x, y, lineCol);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fillCenter);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateWhiteUiSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 100f);
        }
    }
}
