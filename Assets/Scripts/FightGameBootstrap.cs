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
    /// - Escenario Ring con colisionadores de suelo y paredes
    /// - Luchador 1 y Luchador 2 (con cuerpos animados y las 4 caras)
    /// - Sistema de Efectos de Combate estilo cómic
    /// - Interfaz de Usuario completa (barras de vida, retratos dinámicos, temporizador)
    /// - Sistema de Webcam para capturar las 4 caras en vivo
    /// 
    /// ¡Permite darle a PLAY en Unity y tener el juego funcionando al instante!
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

            Debug.Log("<color=green><b>[FightFace] ¡Juego de Pelea 2D Inicializado Exitosamente!</b></color>");
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

            // Suelo (Plataforma del Ring)
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(arena.transform);
            floor.transform.position = new Vector3(0, -2.5f, 0);
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(24f, 1f);

            var floorSr = floor.AddComponent<SpriteRenderer>();
            floorSr.sprite = CreateBoxSprite(2400, 100, new Color(0.22f, 0.22f, 0.28f));
            floorSr.sortingOrder = -1;

            // Lona del Ring
            GameObject mat = new GameObject("RingMat");
            mat.transform.SetParent(arena.transform);
            mat.transform.position = new Vector3(0, -2.05f, 0);
            var matSr = mat.AddComponent<SpriteRenderer>();
            matSr.sprite = CreateBoxSprite(1800, 20, new Color(0.85f, 0.25f, 0.25f));
            matSr.sortingOrder = 0;

            // Paredes invisibles
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

            // Fondo estilo Dojo / Ring Urbano
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(arena.transform);
            backdrop.transform.position = new Vector3(0, 1.5f, 5f);
            var bgSr = backdrop.AddComponent<SpriteRenderer>();
            bgSr.sprite = CreateGradientBackdrop(1920, 1080);
            bgSr.sortingOrder = -10;

            // Cuerdas del ring
            for (int i = 0; i < 3; i++)
            {
                GameObject rope = new GameObject($"Rope_{i}");
                rope.transform.SetParent(arena.transform);
                rope.transform.position = new Vector3(0, -1.6f + (i * 0.5f), 0);
                var ropeSr = rope.AddComponent<SpriteRenderer>();
                Color ropeColor = i == 0 ? Color.red : (i == 1 ? Color.white : Color.blue);
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
            p1.fighterName = "Panchito 'El Bravo'";
            p1.isAI = false;

            // Colisionador del cuerpo de P1
            var p1Col = p1Obj.GetComponent<CapsuleCollider2D>();
            if (p1Col == null) p1Col = p1Obj.AddComponent<CapsuleCollider2D>();
            p1Col.size = new Vector2(0.9f, 2.1f);
            p1Col.offset = new Vector2(0, 0.9f);

            // Cuerpo de P1
            var p1Body = p1Obj.GetComponentInChildren<FighterBodyController>();
            if (p1Body == null)
            {
                GameObject p1BodyObj = new GameObject("Body");
                p1BodyObj.transform.SetParent(p1Obj.transform, false);
                p1Body = p1BodyObj.AddComponent<FighterBodyController>();
                p1Body.suitColor = new Color(0.9f, 0.2f, 0.2f); // Rojo
                p1Body.gloveColor = new Color(1f, 0.85f, 0.1f); // Dorado
            }
            p1.bodyController = p1Body;

            // Cabeza y Caras de P1
            var p1Face = p1Obj.GetComponentInChildren<DynamicFaceController>();
            if (p1Face == null)
            {
                GameObject p1HeadObj = new GameObject("Head_Face");
                p1HeadObj.transform.SetParent(p1Body.neckPoint != null ? p1Body.neckPoint : p1Obj.transform, false);
                p1HeadObj.transform.localPosition = new Vector3(0, 0.45f, 0);
                p1HeadObj.transform.localScale = Vector3.one * 1.35f;

                p1Face = p1HeadObj.AddComponent<DynamicFaceController>();
                var sr = p1HeadObj.GetComponent<SpriteRenderer>();
                sr.sortingOrder = 5;
            }
            p1.faceController = p1Face;

            // Asignar perfil con las 4 caras a P1
            FaceProfile p1Profile = FaceLoader.CreateDefaultProceduralProfile(1);
            string p1SaveDir = Path.Combine(Application.persistentDataPath, "Luchadores", "Jugador_1");
            if (Directory.Exists(p1SaveDir))
            {
                p1Profile.LoadFromDirectory(p1SaveDir);
            }
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
            p2.fighterName = "Rocky 'El Furioso'";
            p2.isAI = true; // Por defecto modo bot/IA para que se pueda jugar de inmediato

            var p2Col = p2Obj.GetComponent<CapsuleCollider2D>();
            if (p2Col == null) p2Col = p2Obj.AddComponent<CapsuleCollider2D>();
            p2Col.size = new Vector2(0.9f, 2.1f);
            p2Col.offset = new Vector2(0, 0.9f);

            // Cuerpo de P2
            var p2Body = p2Obj.GetComponentInChildren<FighterBodyController>();
            if (p2Body == null)
            {
                GameObject p2BodyObj = new GameObject("Body");
                p2BodyObj.transform.SetParent(p2Obj.transform, false);
                p2Body = p2BodyObj.AddComponent<FighterBodyController>();
                p2Body.suitColor = new Color(0.15f, 0.45f, 0.95f); // Azul
                p2Body.gloveColor = new Color(0.2f, 0.9f, 0.3f);   // Verde neón
            }
            p2.bodyController = p2Body;

            // Cabeza y Caras de P2
            var p2Face = p2Obj.GetComponentInChildren<DynamicFaceController>();
            if (p2Face == null)
            {
                GameObject p2HeadObj = new GameObject("Head_Face");
                p2HeadObj.transform.SetParent(p2Body.neckPoint != null ? p2Body.neckPoint : p2Obj.transform, false);
                p2HeadObj.transform.localPosition = new Vector3(0, 0.45f, 0);
                p2HeadObj.transform.localScale = Vector3.one * 1.35f;

                p2Face = p2HeadObj.AddComponent<DynamicFaceController>();
                var sr = p2HeadObj.GetComponent<SpriteRenderer>();
                sr.sortingOrder = 5;
            }
            p2.faceController = p2Face;

            // Asignar perfil con las 4 caras a P2
            FaceProfile p2Profile = FaceLoader.CreateDefaultProceduralProfile(2);
            string p2SaveDir = Path.Combine(Application.persistentDataPath, "Luchadores", "Jugador_2");
            if (Directory.Exists(p2SaveDir))
            {
                p2Profile.LoadFromDirectory(p2SaveDir);
            }
            p2Face.SetProfile(p2Profile);

            // Enlazar oponentes
            p1.opponent = p2.transform;
            p2.opponent = p1.transform;
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

            // Construir UI de barras de salud y retratos
            BuildHealthBarUI(canvasObj.transform, battleUI, p1, p2);

            // Construir Banner Central
            BuildCenterBannerUI(canvasObj.transform, battleUI);

            // Construir Barra de Controles Inferior
            BuildControlsBottomBar(canvasObj.transform, battleUI);

            // Construir Panel de Captura Webcam / Creador de Caras
            BuildWebcamModal(canvasObj.transform, battleUI, out webcamMgr);
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

            // --- JUGADOR 1 (Izquierda) ---
            // Retrato P1
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

            // Barra de vida P1 Fondo
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

            // Barra Ghost (Daño pendiente) P1
            GameObject p1GhostObj = new GameObject("P1_Hp_Ghost");
            p1GhostObj.transform.SetParent(p1HpBg.transform, false);
            var p1GhostRect = p1GhostObj.AddComponent<RectTransform>();
            p1GhostRect.anchorMin = Vector2.zero;
            p1GhostRect.anchorMax = Vector2.one;
            p1GhostRect.sizeDelta = Vector2.zero;
            ui.p1HealthGhost = p1GhostObj.AddComponent<Image>();
            ui.p1HealthGhost.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            ui.p1HealthGhost.type = Image.Type.Filled;
            ui.p1HealthGhost.fillMethod = Image.FillMethod.Horizontal;
            ui.p1HealthGhost.fillOrigin = 1; // De derecha a izquierda

            // Barra Fill P1
            GameObject p1FillObj = new GameObject("P1_Hp_Fill");
            p1FillObj.transform.SetParent(p1HpBg.transform, false);
            var p1FillRect = p1FillObj.AddComponent<RectTransform>();
            p1FillRect.anchorMin = Vector2.zero;
            p1FillRect.anchorMax = Vector2.one;
            p1FillRect.sizeDelta = Vector2.zero;
            ui.p1HealthFill = p1FillObj.AddComponent<Image>();
            ui.p1HealthFill.color = new Color(0.2f, 0.9f, 0.3f);
            ui.p1HealthFill.type = Image.Type.Filled;
            ui.p1HealthFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p1HealthFill.fillOrigin = 1;

            // Nombre P1
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

            // --- JUGADOR 2 (Derecha) ---
            // Retrato P2
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

            // Barra de vida P2 Fondo
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

            // Barra Ghost P2
            GameObject p2GhostObj = new GameObject("P2_Hp_Ghost");
            p2GhostObj.transform.SetParent(p2HpBg.transform, false);
            var p2GhostRect = p2GhostObj.AddComponent<RectTransform>();
            p2GhostRect.anchorMin = Vector2.zero;
            p2GhostRect.anchorMax = Vector2.one;
            p2GhostRect.sizeDelta = Vector2.zero;
            ui.p2HealthGhost = p2GhostObj.AddComponent<Image>();
            ui.p2HealthGhost.color = new Color(1f, 0.3f, 0.1f, 0.8f);
            ui.p2HealthGhost.type = Image.Type.Filled;
            ui.p2HealthGhost.fillMethod = Image.FillMethod.Horizontal;
            ui.p2HealthGhost.fillOrigin = 0; // De izquierda a derecha

            // Barra Fill P2
            GameObject p2FillObj = new GameObject("P2_Hp_Fill");
            p2FillObj.transform.SetParent(p2HpBg.transform, false);
            var p2FillRect = p2FillObj.AddComponent<RectTransform>();
            p2FillRect.anchorMin = Vector2.zero;
            p2FillRect.anchorMax = Vector2.one;
            p2FillRect.sizeDelta = Vector2.zero;
            ui.p2HealthFill = p2FillObj.AddComponent<Image>();
            ui.p2HealthFill.color = new Color(0.2f, 0.9f, 0.3f);
            ui.p2HealthFill.type = Image.Type.Filled;
            ui.p2HealthFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p2HealthFill.fillOrigin = 0;

            // Nombre P2
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

            // --- TEMPORIZADOR CENTRAL ---
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
            bannerRect.sizeDelta = new Vector2(800, 180);
            bannerRect.anchoredPosition = new Vector2(0, 50);

            var bg = bannerPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            GameObject textObj = new GameObject("BannerText");
            textObj.transform.SetParent(bannerPanel.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            ui.centerBannerText = textObj.AddComponent<Text>();
            ui.centerBannerText.text = "¡A PELEAR!";
            ui.centerBannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.centerBannerText.fontSize = 56;
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
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

            // Texto de controles
            GameObject infoObj = new GameObject("ControlsText");
            infoObj.transform.SetParent(bottomBar.transform, false);
            var infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(0.72f, 1);
            infoRect.offsetMin = new Vector2(25, 5);
            infoRect.offsetMax = new Vector2(0, -5);

            var text = infoObj.AddComponent<Text>();
            text.text = "<b>P1:</b> [A/D] Mover | [W] Salto | [F/Espacio] Puño (¡Enojo!) | [G] Patada\n<b>P2:</b> [←/→] Mover | [↑] Salto | [L] Puño (¡Enojo!) | [K] Patada   <i>(P2 usa IA por defecto)</i>";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 17;
            text.color = new Color(0.9f, 0.95f, 1f);
            text.alignment = TextAnchor.MiddleLeft;

            // Botón Personalizar Caras (Webcam)
            GameObject btnCustomObj = new GameObject("Btn_CustomFaces");
            btnCustomObj.transform.SetParent(bottomBar.transform, false);
            var btnRect = btnCustomObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.74f, 0.15f);
            btnRect.anchorMax = new Vector2(0.88f, 0.85f);
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
            btnTxt.text = "📸 Caras / Webcam [C]";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 16;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;

            // Botón Reiniciar
            GameObject btnRestartObj = new GameObject("Btn_Restart");
            btnRestartObj.transform.SetParent(bottomBar.transform, false);
            var rRect = btnRestartObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.89f, 0.15f);
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
            rTxt.fontSize = 16;
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
            mRect.sizeDelta = new Vector2(960, 680);
            mRect.anchoredPosition = Vector2.zero;

            var modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.08f, 0.09f, 0.14f, 0.96f);

            webcamMgr = modalObj.AddComponent<WebcamCaptureManager>();

            // Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 1);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.pivot = new Vector2(0.5f, 1);
            tRect.sizeDelta = new Vector2(0, 60);
            tRect.anchoredPosition = new Vector2(0, -15);
            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "📸 CREADOR DE LUCHADOR - CAPTURA DE ROSTROS";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 26;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.85f, 0.2f);

            // Subtítulo con las 4 emociones
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(modalObj.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 1);
            subRect.anchorMax = new Vector2(1, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.sizeDelta = new Vector2(0, 35);
            subRect.anchoredPosition = new Vector2(0, -70);
            var subTxt = subObj.AddComponent<Text>();
            subTxt.text = "Posa frente a la cámara y captura tus 4 expresiones: Base, Enojo/Odio, Dolor y KO";
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 17;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.8f, 0.85f, 0.95f);

            // Visor de Cámara en Vivo (RawImage)
            GameObject camViewObj = new GameObject("CamView");
            camViewObj.transform.SetParent(modalObj.transform, false);
            var cRect = camViewObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.06f, 0.32f);
            cRect.anchorMax = new Vector2(0.52f, 0.84f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var camRaw = camViewObj.AddComponent<RawImage>();
            camRaw.color = Color.white;
            webcamMgr.cameraPreviewUI = camRaw;

            // Marco del visor
            var outline = camViewObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.6f, 1f);
            outline.effectDistance = new Vector2(4, 4);

            // Botones de Selección de Jugador (P1 / P2)
            CreatePlayerToggleButtons(modalObj.transform, webcamMgr);

            // Botones para capturar las 4 fotos
            CreateCaptureButton(modalObj.transform, webcamMgr, "1. Foto BASE (Pose Normal)", FaceType.Base, new Vector2(530, -220), out Image thumbBase);
            CreateCaptureButton(modalObj.transform, webcamMgr, "2. Foto ENOJO (¡Furia y Odio!)", FaceType.Enojo, new Vector2(530, -300), out Image thumbAngry);
            CreateCaptureButton(modalObj.transform, webcamMgr, "3. Foto DOLOR (¡Mueca de Golpe!)", FaceType.Dolor, new Vector2(530, -380), out Image thumbHurt);
            CreateCaptureButton(modalObj.transform, webcamMgr, "4. Foto K.O. (Ojos Cerrados)", FaceType.KO, new Vector2(530, -460), out Image thumbKO);

            webcamMgr.previewThumbBase = thumbBase;
            webcamMgr.previewThumbAngry = thumbAngry;
            webcamMgr.previewThumbHurt = thumbHurt;
            webcamMgr.previewThumbKO = thumbKO;

            // Botón Cerrar / Volver a la Pelea
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
            clTxt.text = "⚔️ ¡LISTO! IR A PELEAR";
            clTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            clTxt.fontSize = 20;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;

            ui.faceCustomizerPanel = modalObj;
            modalObj.SetActive(false); // Iniciar cerrado para ver la pelea de inmediato
        }

        private void CreatePlayerToggleButtons(Transform parent, WebcamCaptureManager mgr)
        {
            GameObject container = new GameObject("PlayerToggles");
            container.transform.SetParent(parent, false);
            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.22f);
            rect.anchorMax = new Vector2(0.52f, 0.30f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Botón P1
            GameObject btnP1 = new GameObject("Btn_P1");
            btnP1.transform.SetParent(container.transform, false);
            var p1Rect = btnP1.AddComponent<RectTransform>();
            p1Rect.anchorMin = new Vector2(0, 0);
            p1Rect.anchorMax = new Vector2(0.48f, 1);
            p1Rect.offsetMin = Vector2.zero;
            p1Rect.offsetMax = Vector2.zero;
            var p1Img = btnP1.AddComponent<Image>();
            p1Img.color = new Color(0.85f, 0.25f, 0.25f);
            var p1Btn = btnP1.AddComponent<Button>();
            p1Btn.onClick.AddListener(() => mgr.SetTargetPlayer(1));

            GameObject p1TxtObj = new GameObject("Text");
            p1TxtObj.transform.SetParent(btnP1.transform, false);
            var t1Rect = p1TxtObj.AddComponent<RectTransform>();
            t1Rect.anchorMin = Vector2.zero;
            t1Rect.anchorMax = Vector2.one;
            t1Rect.sizeDelta = Vector2.zero;
            var t1 = p1TxtObj.AddComponent<Text>();
            t1.text = "Modificar Jugador 1";
            t1.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t1.fontSize = 15;
            t1.fontStyle = FontStyle.Bold;
            t1.alignment = TextAnchor.MiddleCenter;
            t1.color = Color.white;

            // Botón P2
            GameObject btnP2 = new GameObject("Btn_P2");
            btnP2.transform.SetParent(container.transform, false);
            var p2Rect = btnP2.AddComponent<RectTransform>();
            p2Rect.anchorMin = new Vector2(0.52f, 0);
            p2Rect.anchorMax = new Vector2(1f, 1);
            p2Rect.offsetMin = Vector2.zero;
            p2Rect.offsetMax = Vector2.zero;
            var p2Img = btnP2.AddComponent<Image>();
            p2Img.color = new Color(0.25f, 0.45f, 0.9f);
            var p2Btn = btnP2.AddComponent<Button>();
            p2Btn.onClick.AddListener(() => mgr.SetTargetPlayer(2));

            GameObject p2TxtObj = new GameObject("Text");
            p2TxtObj.transform.SetParent(btnP2.transform, false);
            var t2Rect = p2TxtObj.AddComponent<RectTransform>();
            t2Rect.anchorMin = Vector2.zero;
            t2Rect.anchorMax = Vector2.one;
            t2Rect.sizeDelta = Vector2.zero;
            var t2 = p2TxtObj.AddComponent<Text>();
            t2.text = "Modificar Jugador 2";
            t2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t2.fontSize = 15;
            t2.fontStyle = FontStyle.Bold;
            t2.alignment = TextAnchor.MiddleCenter;
            t2.color = Color.white;
        }

        private void CreateCaptureButton(Transform parent, WebcamCaptureManager mgr, string label, FaceType emotion, Vector2 pos, out Image thumb)
        {
            GameObject container = new GameObject("CaptureRow_" + emotion);
            container.transform.SetParent(parent, false);
            var cRect = container.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(0, 1);
            cRect.pivot = new Vector2(0, 1);
            cRect.sizeDelta = new Vector2(380, 65);
            cRect.anchoredPosition = pos;

            // Miniatura
            GameObject thumbObj = new GameObject("Thumbnail");
            thumbObj.transform.SetParent(container.transform, false);
            var tRect = thumbObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.5f);
            tRect.anchorMax = new Vector2(0, 0.5f);
            tRect.pivot = new Vector2(0, 0.5f);
            tRect.sizeDelta = new Vector2(55, 55);
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
            bRect.sizeDelta = new Vector2(300, 55);
            bRect.anchoredPosition = new Vector2(70, 0);

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
            if (mgrObj == null)
            {
                mgrObj = new GameObject("BattleManager");
            }

            var bm = mgrObj.GetComponent<BattleManager>();
            if (bm == null) bm = mgrObj.AddComponent<BattleManager>();

            bm.player1 = p1;
            bm.player2 = p2;
            bm.isP2ControlledByAI = true;
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
            Color top = new Color(0.12f, 0.08f, 0.22f);
            Color bot = new Color(0.22f, 0.14f, 0.32f);

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
    }
}
