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
            SetupAudio();
            SetupArena();
            SetupCombatEffects();
            SetupFighters(out FighterController p1, out FighterController p2);
            SetupUI(p1, p2, out BattleUI battleUI, out WebcamCaptureManager webcamMgr);
            SetupBattleManager(p1, p2);
            SetupTournamentManager(p1, p2);

            Debug.Log("<color=green><b>[FightFace] ¡Juego de Pelea 2D Inicializado con Éxito!</b></color>");
        }

        private void SetupAudio()
        {
            if (FindFirstObjectByType<FightAudioManager>() == null)
            {
                GameObject audioObj = new GameObject("FightAudioManager");
                audioObj.AddComponent<FightAudioManager>();
            }
        }

        private void EnsureEventSystem()
        {
            var es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            var inputModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
            {
                var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (oldModule != null) Destroy(oldModule);
                inputModule = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            inputModule.AssignDefaultActions();
#else
            if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
#endif
        }

        private void SetupArena()
        {
            GameObject arena = GameObject.Find("ArenaRing");
            if (arena != null) return;

            arena = new GameObject("ArenaRing");

            // Suelo del ring (colisionador invisible para que los luchadores pisen la lona de la foto real)
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(arena.transform);
            floor.transform.position = new Vector3(0, -3.40f, 0);
            var floorCol = floor.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(24f, 1f);

            // Paredes laterales invisibles dentro del cuadrilátero
            GameObject leftWall = new GameObject("LeftWall");
            leftWall.transform.SetParent(arena.transform);
            leftWall.transform.position = new Vector3(-9.2f, 1f, 0);
            var leftCol = leftWall.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(1f, 10f);

            GameObject rightWall = new GameObject("RightWall");
            rightWall.transform.SetParent(arena.transform);
            rightWall.transform.position = new Vector3(9.2f, 1f, 0);
            var rightCol = rightWall.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(1f, 10f);

            // Fondo: Cuadrilátero Profesional / Escenario Oficial
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(arena.transform);
            backdrop.transform.position = new Vector3(0, 0.65f, 5f);
            var bgSr = backdrop.AddComponent<SpriteRenderer>();
            Sprite defaultStageSprite = FaceLoader.LoadStageSprite(StageType.Auto, FighterBodyType.Gordo);
            if (defaultStageSprite != null)
            {
                bgSr.sprite = defaultStageSprite;
                float worldW = defaultStageSprite.rect.width / defaultStageSprite.pixelsPerUnit;
                float scale = 22.0f / worldW;
                backdrop.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                bgSr.sprite = CreateGradientBackdrop(1920, 1080);
            }
            bgSr.sortingOrder = -20;

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.stageBackdropRenderer = bgSr;
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

            if (ComicFinisherManager.Instance == null)
            {
                GameObject comicObj = new GameObject("ComicFinisherManager");
                comicObj.AddComponent<ComicFinisherManager>();
            }

            if (CrowdCheerManager.Instance == null)
            {
                GameObject crowdObj = new GameObject("CrowdCheerManager");
                crowdObj.AddComponent<CrowdCheerManager>();
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
                p1Obj.transform.position = new Vector3(-3.2f, -2.90f, 0);
            }
            p1 = p1Obj.GetComponent<FighterController>();
            if (p1 == null) p1 = p1Obj.AddComponent<FighterController>();
            p1.playerId = 1;
            p1.fighterName = "P1: El Gordo";
            p1.isAI = false;

            var p1Col = p1Obj.GetComponent<CapsuleCollider2D>();
            if (p1Col == null) p1Col = p1Obj.AddComponent<CapsuleCollider2D>();
            p1Col.size = new Vector2(1.1f, 2.2f);
            p1Col.offset = new Vector2(0, 1.10f);

            var p1Body = p1Obj.GetComponentInChildren<FighterBodyController>();
            if (p1Body == null)
            {
                GameObject p1BodyObj = new GameObject("Body");
                p1BodyObj.transform.SetParent(p1Obj.transform, false);
                p1Body = p1BodyObj.AddComponent<FighterBodyController>();
            }
            p1Body.SetClassicBody(FighterBodyType.Gordo);
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
                sr.sortingOrder = 12;
            }
            else
            {
                p1Face.transform.SetParent(p1Body.neckPoint != null ? p1Body.neckPoint : p1Obj.transform, false);
                p1Face.transform.localPosition = Vector3.zero;
            }
            p1.faceController = p1Face;

            // Cargar perfil de P1 (revisando Jugador_1 o Luchador_1)
            FaceProfile p1Profile = LoadProfileForFighter(1, "El Gordo");
            p1Face.SetProfile(p1Profile);
            p1.voiceProfile = VoiceLoader.LoadProfileForFighter(1, "El Gordo", 0);

            // --- JUGADOR 2 ---
            GameObject p2Obj = GameObject.Find("Player2");
            if (p2Obj == null)
            {
                p2Obj = new GameObject("Player2");
                p2Obj.transform.position = new Vector3(3.2f, -2.90f, 0);
            }
            p2 = p2Obj.GetComponent<FighterController>();
            if (p2 == null) p2 = p2Obj.AddComponent<FighterController>();
            p2.playerId = 2;
            p2.fighterName = "P2: El Flaco";
            p2.isAI = true;

            var p2Col = p2Obj.GetComponent<CapsuleCollider2D>();
            if (p2Col == null) p2Col = p2Obj.AddComponent<CapsuleCollider2D>();
            p2Col.size = new Vector2(1.0f, 2.2f);
            p2Col.offset = new Vector2(0, 1.10f);

            var p2Body = p2Obj.GetComponentInChildren<FighterBodyController>();
            if (p2Body == null)
            {
                GameObject p2BodyObj = new GameObject("Body");
                p2BodyObj.transform.SetParent(p2Obj.transform, false);
                p2Body = p2BodyObj.AddComponent<FighterBodyController>();
            }
            p2Body.SetClassicBody(FighterBodyType.Flaco);
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
                sr.sortingOrder = 12;
            }
            else
            {
                p2Face.transform.SetParent(p2Body.neckPoint != null ? p2Body.neckPoint : p2Obj.transform, false);
                p2Face.transform.localPosition = Vector3.zero;
            }
            p2.faceController = p2Face;

            // Cargar perfil de P2
            FaceProfile p2Profile = LoadProfileForFighter(2, "El Flaco");
            p2Face.SetProfile(p2Profile);
            p2.voiceProfile = VoiceLoader.LoadProfileForFighter(2, "El Flaco", 0);

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
            }

            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            battleUI = canvasObj.GetComponent<BattleUI>();
            if (battleUI == null) battleUI = canvasObj.AddComponent<BattleUI>();

            BuildHealthBarUI(canvasObj.transform, battleUI, p1, p2);
            BuildCenterBannerUI(canvasObj.transform, battleUI);
            BuildComboUI(canvasObj.transform, battleUI);
            BuildWinnerModalUI(canvasObj.transform, battleUI);
            BuildControlsBottomBar(canvasObj.transform, battleUI);
            BuildWebcamModal(canvasObj.transform, battleUI, out webcamMgr);
            BuildCharacterSelectMenu(canvasObj.transform, battleUI, p1, p2, webcamMgr);

            var celebrationUI = canvasObj.AddComponent<TournamentCelebrationUI>();
            celebrationUI.BuildCelebrationUI(canvasObj.transform);

            var netMgr = canvasObj.AddComponent<NetworkFightManager>();
            var netLobby = canvasObj.AddComponent<NetworkLobbyUI>();
            netLobby.BuildLobbyUI(canvasObj.transform);

            // Controles Táctiles en Pantalla para Celulares / Android (y testeable en PC con tecla T)
            BuildVirtualTouchControlsUI(canvasObj.transform);

            // Aplicar estilo Street Fighter II Arcade a todos los paneles y botones
            ArcadeSkin.Apply(canvasObj.transform);
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

            Sprite whiteSprite = CreateWhiteUiSprite();

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
            ui.p1FacePortrait.preserveAspect = true;

            var p1POutline = p1PortraitObj.AddComponent<Outline>();
            p1POutline.effectColor = new Color(1f, 0.85f, 0.2f);
            p1POutline.effectDistance = new Vector2(2, 2);

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
            p1BgImg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            var p1BgOutline = p1HpBg.AddComponent<Outline>();
            p1BgOutline.effectColor = new Color(0.9f, 0.75f, 0.15f);
            p1BgOutline.effectDistance = new Vector2(2, 2);

            // P1 Ghost Bar
            GameObject p1GhostObj = new GameObject("P1_Hp_Ghost");
            p1GhostObj.transform.SetParent(p1HpBg.transform, false);
            var p1GhostRect = p1GhostObj.AddComponent<RectTransform>();
            p1GhostRect.anchorMin = Vector2.zero;
            p1GhostRect.anchorMax = Vector2.one;
            p1GhostRect.sizeDelta = Vector2.zero;
            ui.p1HealthGhost = p1GhostObj.AddComponent<Image>();
            ui.p1HealthGhost.sprite = whiteSprite;
            ui.p1HealthGhost.color = new Color(1f, 0.25f, 0.1f, 0.85f);
            ui.p1HealthGhost.type = Image.Type.Filled;
            ui.p1HealthGhost.fillMethod = Image.FillMethod.Horizontal;
            ui.p1HealthGhost.fillOrigin = 0;

            // P1 Fill Bar (Amarillo clásico Street Fighter)
            GameObject p1FillObj = new GameObject("P1_Hp_Fill");
            p1FillObj.transform.SetParent(p1HpBg.transform, false);
            var p1FillRect = p1FillObj.AddComponent<RectTransform>();
            p1FillRect.anchorMin = Vector2.zero;
            p1FillRect.anchorMax = Vector2.one;
            p1FillRect.sizeDelta = Vector2.zero;
            ui.p1HealthFill = p1FillObj.AddComponent<Image>();
            ui.p1HealthFill.sprite = whiteSprite;
            ui.p1HealthFill.color = new Color(1f, 0.88f, 0.15f);
            ui.p1HealthFill.type = Image.Type.Filled;
            ui.p1HealthFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p1HealthFill.fillOrigin = 0;

            // P1 HP Texto
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

            // P1 Stun Bar
            GameObject p1StunBg = new GameObject("P1_Stun_Bg");
            p1StunBg.transform.SetParent(topBar.transform, false);
            var p1StunBgRect = p1StunBg.AddComponent<RectTransform>();
            p1StunBgRect.anchorMin = new Vector2(0, 1);
            p1StunBgRect.anchorMax = new Vector2(0, 1);
            p1StunBgRect.pivot = new Vector2(0, 1);
            p1StunBgRect.sizeDelta = new Vector2(480, 12);
            p1StunBgRect.anchoredPosition = new Vector2(130, -74);
            var p1StunBgImg = p1StunBg.AddComponent<Image>();
            p1StunBgImg.color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

            GameObject p1StunFillObj = new GameObject("P1_Stun_Fill");
            p1StunFillObj.transform.SetParent(p1StunBg.transform, false);
            var p1StunFillRect = p1StunFillObj.AddComponent<RectTransform>();
            p1StunFillRect.anchorMin = Vector2.zero;
            p1StunFillRect.anchorMax = Vector2.one;
            p1StunFillRect.sizeDelta = Vector2.zero;
            ui.p1StunFill = p1StunFillObj.AddComponent<Image>();
            ui.p1StunFill.sprite = whiteSprite;
            ui.p1StunFill.color = new Color(1f, 0.60f, 0.05f);
            ui.p1StunFill.type = Image.Type.Filled;
            ui.p1StunFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p1StunFill.fillOrigin = 0;
            ui.p1StunFill.fillAmount = 0f;

            // P1 Badges (Rage y Stun)
            GameObject p1RageObj = new GameObject("P1_Rage_Badge");
            p1RageObj.transform.SetParent(topBar.transform, false);
            var p1RageRect = p1RageObj.AddComponent<RectTransform>();
            p1RageRect.anchorMin = new Vector2(0, 1);
            p1RageRect.anchorMax = new Vector2(0, 1);
            p1RageRect.pivot = new Vector2(0, 1);
            p1RageRect.sizeDelta = new Vector2(140, 24);
            p1RageRect.anchoredPosition = new Vector2(130, -90);
            var p1RageTxt = p1RageObj.AddComponent<Text>();
            p1RageTxt.text = "🔥 RAGE READY";
            p1RageTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            p1RageTxt.fontSize = 15;
            p1RageTxt.fontStyle = FontStyle.Bold;
            p1RageTxt.color = new Color(1f, 0.2f, 0.1f);
            ui.p1RageBadge = p1RageObj;
            p1RageObj.SetActive(false);

            GameObject p1StunBadgeObj = new GameObject("P1_Stun_Badge");
            p1StunBadgeObj.transform.SetParent(topBar.transform, false);
            var p1SB頎Rect = p1StunBadgeObj.AddComponent<RectTransform>();
            p1SB頎Rect.anchorMin = new Vector2(0, 1);
            p1SB頎Rect.anchorMax = new Vector2(0, 1);
            p1SB頎Rect.pivot = new Vector2(0, 1);
            p1SB頎Rect.sizeDelta = new Vector2(120, 24);
            p1SB頎Rect.anchoredPosition = new Vector2(300, -90);
            var p1SBTxt = p1StunBadgeObj.AddComponent<Text>();
            p1SBTxt.text = "💫 ¡STUN!";
            p1SBTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            p1SBTxt.fontSize = 16;
            p1SBTxt.fontStyle = FontStyle.Bold;
            p1SBTxt.color = new Color(1f, 0.95f, 0.2f);
            ui.p1StunBadge = p1StunBadgeObj;
            p1StunBadgeObj.SetActive(false);

            // P1 Nombre
            GameObject p1NameObj = new GameObject("P1_Name");
            p1NameObj.transform.SetParent(topBar.transform, false);
            var p1NameRect = p1NameObj.AddComponent<RectTransform>();
            p1NameRect.anchorMin = new Vector2(0, 1);
            p1NameRect.anchorMax = new Vector2(0, 1);
            p1NameRect.pivot = new Vector2(0, 1);
            p1NameRect.sizeDelta = new Vector2(300, 30);
            p1NameRect.anchoredPosition = new Vector2(130, -90);
            ui.p1NameText = p1NameObj.AddComponent<Text>();
            ui.p1NameText.text = "P1: " + p1.fighterName;
            ui.p1NameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.p1NameText.fontSize = 19;
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
            ui.p2FacePortrait.preserveAspect = true;

            var p2POutline = p2PortraitObj.AddComponent<Outline>();
            p2POutline.effectColor = new Color(0.2f, 0.65f, 1f);
            p2POutline.effectDistance = new Vector2(2, 2);

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
            p2BgImg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            var p2BgOutline = p2HpBg.AddComponent<Outline>();
            p2BgOutline.effectColor = new Color(0.9f, 0.75f, 0.15f);
            p2BgOutline.effectDistance = new Vector2(2, 2);

            // P2 Ghost Bar
            GameObject p2GhostObj = new GameObject("P2_Hp_Ghost");
            p2GhostObj.transform.SetParent(p2HpBg.transform, false);
            var p2GhostRect = p2GhostObj.AddComponent<RectTransform>();
            p2GhostRect.anchorMin = Vector2.zero;
            p2GhostRect.anchorMax = Vector2.one;
            p2GhostRect.sizeDelta = Vector2.zero;
            ui.p2HealthGhost = p2GhostObj.AddComponent<Image>();
            ui.p2HealthGhost.sprite = whiteSprite;
            ui.p2HealthGhost.color = new Color(1f, 0.25f, 0.1f, 0.85f);
            ui.p2HealthGhost.type = Image.Type.Filled;
            ui.p2HealthGhost.fillMethod = Image.FillMethod.Horizontal;
            ui.p2HealthGhost.fillOrigin = 1;

            // P2 Fill Bar
            GameObject p2FillObj = new GameObject("P2_Hp_Fill");
            p2FillObj.transform.SetParent(p2HpBg.transform, false);
            var p2FillRect = p2FillObj.AddComponent<RectTransform>();
            p2FillRect.anchorMin = Vector2.zero;
            p2FillRect.anchorMax = Vector2.one;
            p2FillRect.sizeDelta = Vector2.zero;
            ui.p2HealthFill = p2FillObj.AddComponent<Image>();
            ui.p2HealthFill.sprite = whiteSprite;
            ui.p2HealthFill.color = new Color(1f, 0.88f, 0.15f);
            ui.p2HealthFill.type = Image.Type.Filled;
            ui.p2HealthFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p2HealthFill.fillOrigin = 1;

            // P2 HP Texto
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

            // P2 Stun Bar
            GameObject p2StunBg = new GameObject("P2_Stun_Bg");
            p2StunBg.transform.SetParent(topBar.transform, false);
            var p2StunBgRect = p2StunBg.AddComponent<RectTransform>();
            p2StunBgRect.anchorMin = new Vector2(1, 1);
            p2StunBgRect.anchorMax = new Vector2(1, 1);
            p2StunBgRect.pivot = new Vector2(1, 1);
            p2StunBgRect.sizeDelta = new Vector2(480, 12);
            p2StunBgRect.anchoredPosition = new Vector2(-130, -74);
            var p2StunBgImg = p2StunBg.AddComponent<Image>();
            p2StunBgImg.color = new Color(0.1f, 0.1f, 0.14f, 0.9f);

            GameObject p2StunFillObj = new GameObject("P2_Stun_Fill");
            p2StunFillObj.transform.SetParent(p2StunBg.transform, false);
            var p2StunFillRect = p2StunFillObj.AddComponent<RectTransform>();
            p2StunFillRect.anchorMin = Vector2.zero;
            p2StunFillRect.anchorMax = Vector2.one;
            p2StunFillRect.sizeDelta = Vector2.zero;
            ui.p2StunFill = p2StunFillObj.AddComponent<Image>();
            ui.p2StunFill.sprite = whiteSprite;
            ui.p2StunFill.color = new Color(1f, 0.60f, 0.05f);
            ui.p2StunFill.type = Image.Type.Filled;
            ui.p2StunFill.fillMethod = Image.FillMethod.Horizontal;
            ui.p2StunFill.fillOrigin = 1;
            ui.p2StunFill.fillAmount = 0f;

            // P2 Badges (Rage y Stun)
            GameObject p2RageObj = new GameObject("P2_Rage_Badge");
            p2RageObj.transform.SetParent(topBar.transform, false);
            var p2RageRect = p2RageObj.AddComponent<RectTransform>();
            p2RageRect.anchorMin = new Vector2(1, 1);
            p2RageRect.anchorMax = new Vector2(1, 1);
            p2RageRect.pivot = new Vector2(1, 1);
            p2RageRect.sizeDelta = new Vector2(140, 24);
            p2RageRect.anchoredPosition = new Vector2(-130, -90);
            var p2RageTxt = p2RageObj.AddComponent<Text>();
            p2RageTxt.text = "🔥 RAGE READY";
            p2RageTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            p2RageTxt.fontSize = 15;
            p2RageTxt.fontStyle = FontStyle.Bold;
            p2RageTxt.alignment = TextAnchor.UpperRight;
            p2RageTxt.color = new Color(1f, 0.2f, 0.1f);
            ui.p2RageBadge = p2RageObj;
            p2RageObj.SetActive(false);

            GameObject p2StunBadgeObj = new GameObject("P2_Stun_Badge");
            p2StunBadgeObj.transform.SetParent(topBar.transform, false);
            var p2SB頎Rect = p2StunBadgeObj.AddComponent<RectTransform>();
            p2SB頎Rect.anchorMin = new Vector2(1, 1);
            p2SB頎Rect.anchorMax = new Vector2(1, 1);
            p2SB頎Rect.pivot = new Vector2(1, 1);
            p2SB頎Rect.sizeDelta = new Vector2(120, 24);
            p2SB頎Rect.anchoredPosition = new Vector2(-300, -90);
            var p2SBTxt = p2StunBadgeObj.AddComponent<Text>();
            p2SBTxt.text = "💫 ¡STUN!";
            p2SBTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            p2SBTxt.fontSize = 16;
            p2SBTxt.fontStyle = FontStyle.Bold;
            p2SBTxt.color = new Color(1f, 0.95f, 0.2f);
            ui.p2StunBadge = p2StunBadgeObj;
            p2StunBadgeObj.SetActive(false);

            // P2 Nombre
            GameObject p2NameObj = new GameObject("P2_Name");
            p2NameObj.transform.SetParent(topBar.transform, false);
            var p2NameRect = p2NameObj.AddComponent<RectTransform>();
            p2NameRect.anchorMin = new Vector2(1, 1);
            p2NameRect.anchorMax = new Vector2(1, 1);
            p2NameRect.pivot = new Vector2(1, 1);
            p2NameRect.sizeDelta = new Vector2(300, 30);
            p2NameRect.anchoredPosition = new Vector2(-130, -90);
            ui.p2NameText = p2NameObj.AddComponent<Text>();
            ui.p2NameText.text = "P2: " + p2.fighterName + " (CPU IA)";
            ui.p2NameText.alignment = TextAnchor.UpperRight;
            ui.p2NameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.p2NameText.fontSize = 19;
            ui.p2NameText.fontStyle = FontStyle.Bold;
            ui.p2NameText.color = Color.white;

            // Insignia Arcade "KO" Central
            GameObject koObj = new GameObject("Arcade_KO_Icon");
            koObj.transform.SetParent(topBar.transform, false);
            var koRect = koObj.AddComponent<RectTransform>();
            koRect.anchorMin = new Vector2(0.5f, 1);
            koRect.anchorMax = new Vector2(0.5f, 1);
            koRect.pivot = new Vector2(0.5f, 1);
            koRect.sizeDelta = new Vector2(100, 40);
            koRect.anchoredPosition = new Vector2(0, -18);
            var koTxt = koObj.AddComponent<Text>();
            koTxt.text = "KO";
            koTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            koTxt.fontSize = 38;
            koTxt.fontStyle = FontStyle.Bold;
            koTxt.alignment = TextAnchor.MiddleCenter;
            koTxt.color = new Color(0.95f, 0.15f, 0.15f);
            var koOutline = koObj.AddComponent<Outline>();
            koOutline.effectColor = new Color(1f, 0.9f, 0.2f);
            koOutline.effectDistance = new Vector2(2, -2);

            // Timer Retro 99
            GameObject timerObj = new GameObject("Timer_Text");
            timerObj.transform.SetParent(topBar.transform, false);
            var timerRect = timerObj.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1);
            timerRect.anchorMax = new Vector2(0.5f, 1);
            timerRect.pivot = new Vector2(0.5f, 1);
            timerRect.sizeDelta = new Vector2(120, 60);
            timerRect.anchoredPosition = new Vector2(0, -56);
            ui.timerText = timerObj.AddComponent<Text>();
            ui.timerText.text = "99";
            ui.timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.timerText.fontSize = 50;
            ui.timerText.fontStyle = FontStyle.Bold;
            ui.timerText.alignment = TextAnchor.MiddleCenter;
            ui.timerText.color = new Color(1f, 0.92f, 0.25f);

            // Marcadores de Rondas Street Fighter II (V-Markers P1)
            ui.p1RoundMarkers = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject mObj = new GameObject($"P1_RoundMarker_{i+1}");
                mObj.transform.SetParent(topBar.transform, false);
                var mRect = mObj.AddComponent<RectTransform>();
                mRect.anchorMin = new Vector2(0.5f, 1);
                mRect.anchorMax = new Vector2(0.5f, 1);
                mRect.pivot = new Vector2(0.5f, 1);
                mRect.sizeDelta = new Vector2(26, 26);
                mRect.anchoredPosition = new Vector2(-75 - (i * 32), -88);

                var mImg = mObj.AddComponent<Image>();
                mImg.color = new Color(0.2f, 0.22f, 0.3f, 0.6f);
                var mOutline = mObj.AddComponent<Outline>();
                mOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.8f);
                mOutline.effectDistance = new Vector2(1, -1);

                GameObject vObj = new GameObject("Text");
                vObj.transform.SetParent(mObj.transform, false);
                var vRect = vObj.AddComponent<RectTransform>();
                vRect.anchorMin = Vector2.zero;
                vRect.anchorMax = Vector2.one;
                vRect.sizeDelta = Vector2.zero;
                var vTxt = vObj.AddComponent<Text>();
                vTxt.text = "V";
                vTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                vTxt.fontSize = 16;
                vTxt.fontStyle = FontStyle.Bold;
                vTxt.alignment = TextAnchor.MiddleCenter;
                vTxt.color = Color.white;

                ui.p1RoundMarkers[i] = mImg;
            }

            // Marcadores de Rondas Street Fighter II (V-Markers P2)
            ui.p2RoundMarkers = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                GameObject mObj = new GameObject($"P2_RoundMarker_{i+1}");
                mObj.transform.SetParent(topBar.transform, false);
                var mRect = mObj.AddComponent<RectTransform>();
                mRect.anchorMin = new Vector2(0.5f, 1);
                mRect.anchorMax = new Vector2(0.5f, 1);
                mRect.pivot = new Vector2(0.5f, 1);
                mRect.sizeDelta = new Vector2(26, 26);
                mRect.anchoredPosition = new Vector2(75 + (i * 32), -88);

                var mImg = mObj.AddComponent<Image>();
                mImg.color = new Color(0.2f, 0.22f, 0.3f, 0.6f);
                var mOutline = mObj.AddComponent<Outline>();
                mOutline.effectColor = new Color(1f, 0.85f, 0.2f, 0.8f);
                mOutline.effectDistance = new Vector2(1, -1);

                GameObject vObj = new GameObject("Text");
                vObj.transform.SetParent(mObj.transform, false);
                var vRect = vObj.AddComponent<RectTransform>();
                vRect.anchorMin = Vector2.zero;
                vRect.anchorMax = Vector2.one;
                vRect.sizeDelta = Vector2.zero;
                var vTxt = vObj.AddComponent<Text>();
                vTxt.text = "V";
                vTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                vTxt.fontSize = 16;
                vTxt.fontStyle = FontStyle.Bold;
                vTxt.alignment = TextAnchor.MiddleCenter;
                vTxt.color = Color.white;

                ui.p2RoundMarkers[i] = mImg;
            }
        }

        private void BuildComboUI(Transform canvas, BattleUI ui)
        {
            GameObject comboObj = new GameObject("ComboPanel");
            comboObj.transform.SetParent(canvas, false);
            var rect = comboObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.60f);
            rect.anchorMax = new Vector2(0.24f, 0.74f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = comboObj.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.10f, 0.85f);
            var outline = comboObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.45f, 0f);
            outline.effectDistance = new Vector2(3, 3);

            GameObject hitsObj = new GameObject("HitsText");
            hitsObj.transform.SetParent(comboObj.transform, false);
            var hRect = hitsObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 0.48f);
            hRect.anchorMax = new Vector2(1, 1f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;

            ui.comboHitsText = hitsObj.AddComponent<Text>();
            ui.comboHitsText.text = "🔥 3 HITS!";
            ui.comboHitsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.comboHitsText.fontSize = 28;
            ui.comboHitsText.fontStyle = FontStyle.Bold;
            ui.comboHitsText.alignment = TextAnchor.MiddleCenter;
            ui.comboHitsText.color = new Color(1f, 0.85f, 0.1f);

            GameObject dmgObj = new GameObject("DamageText");
            dmgObj.transform.SetParent(comboObj.transform, false);
            var dRect = dmgObj.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0, 0);
            dRect.anchorMax = new Vector2(1, 0.50f);
            dRect.offsetMin = Vector2.zero;
            dRect.offsetMax = Vector2.zero;

            ui.comboDamageText = dmgObj.AddComponent<Text>();
            ui.comboDamageText.text = "💥 120 DAMAGE";
            ui.comboDamageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.comboDamageText.fontSize = 20;
            ui.comboDamageText.fontStyle = FontStyle.Bold;
            ui.comboDamageText.alignment = TextAnchor.MiddleCenter;
            ui.comboDamageText.color = new Color(1f, 0.35f, 0.2f);

            ui.comboPanel = comboObj;
            comboObj.SetActive(false);
        }

        private void BuildWinnerModalUI(Transform canvas, BattleUI ui)
        {
            GameObject modalObj = new GameObject("Panel_WinnerModal");
            modalObj.transform.SetParent(canvas, false);
            var mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = Vector2.zero;
            mRect.anchorMax = Vector2.one;
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;

            var bg = modalObj.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.09f, 0.94f);

            GameObject cardObj = new GameObject("WinnerCard");
            cardObj.transform.SetParent(modalObj.transform, false);
            var cRect = cardObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f);
            cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f);
            cRect.sizeDelta = new Vector2(820, 680);
            cRect.anchoredPosition = Vector2.zero;

            var cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.10f, 0.12f, 0.18f, 0.98f);
            var cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 0.85f, 0.2f);
            cardOutline.effectDistance = new Vector2(4, 4);

            // Título
            GameObject titleObj = new GameObject("Title_Winner");
            titleObj.transform.SetParent(cardObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.88f);
            tRect.anchorMax = new Vector2(1, 0.98f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            ui.winnerTitleText = titleObj.AddComponent<Text>();
            ui.winnerTitleText.text = "🏆 ¡RESULTADO DEL COMBATE! 🏆";
            ui.winnerTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.winnerTitleText.fontSize = 36;
            ui.winnerTitleText.fontStyle = FontStyle.Bold;
            ui.winnerTitleText.alignment = TextAnchor.MiddleCenter;
            ui.winnerTitleText.color = new Color(1f, 0.88f, 0.15f);

            // Foto Ganador (Izquierda)
            GameObject photoFrameObj = new GameObject("Winner_PhotoFrame");
            photoFrameObj.transform.SetParent(cardObj.transform, false);
            var pfRect = photoFrameObj.AddComponent<RectTransform>();
            pfRect.anchorMin = new Vector2(0.5f, 0.5f);
            pfRect.anchorMax = new Vector2(0.5f, 0.5f);
            pfRect.pivot = new Vector2(0.5f, 0.5f);
            pfRect.sizeDelta = new Vector2(230, 230);
            pfRect.anchoredPosition = new Vector2(-180, 75);

            var photoImg = photoFrameObj.AddComponent<Image>();
            photoImg.color = Color.white;
            ui.winnerPortraitImage = photoImg;

            var photoOutline = photoFrameObj.AddComponent<Outline>();
            photoOutline.effectColor = new Color(1f, 0.9f, 0.3f);
            photoOutline.effectDistance = new Vector2(4, 4);

            GameObject winTagObj = new GameObject("Winner_Tag");
            winTagObj.transform.SetParent(photoFrameObj.transform, false);
            var wtRect = winTagObj.AddComponent<RectTransform>();
            wtRect.anchorMin = new Vector2(0, 1);
            wtRect.anchorMax = new Vector2(1, 1);
            wtRect.pivot = new Vector2(0.5f, 0);
            wtRect.sizeDelta = new Vector2(0, 28);
            wtRect.anchoredPosition = new Vector2(0, 6);
            var wtText = winTagObj.AddComponent<Text>();
            wtText.text = "👑 GANADOR";
            wtText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            wtText.fontSize = 18;
            wtText.fontStyle = FontStyle.Bold;
            wtText.alignment = TextAnchor.MiddleCenter;
            wtText.color = new Color(1f, 0.85f, 0.2f);

            // Foto Perdedor / KO (Derecha)
            GameObject loserFrameObj = new GameObject("Loser_PhotoFrame");
            loserFrameObj.transform.SetParent(cardObj.transform, false);
            var lfRect = loserFrameObj.AddComponent<RectTransform>();
            lfRect.anchorMin = new Vector2(0.5f, 0.5f);
            lfRect.anchorMax = new Vector2(0.5f, 0.5f);
            lfRect.pivot = new Vector2(0.5f, 0.5f);
            lfRect.sizeDelta = new Vector2(230, 230);
            lfRect.anchoredPosition = new Vector2(180, 75);

            var loserImg = loserFrameObj.AddComponent<Image>();
            loserImg.color = new Color(0.95f, 0.85f, 0.85f);
            ui.loserPortraitImage = loserImg;

            var loserOutline = loserFrameObj.AddComponent<Outline>();
            loserOutline.effectColor = new Color(0.95f, 0.25f, 0.25f);
            loserOutline.effectDistance = new Vector2(4, 4);

            GameObject loseTagObj = new GameObject("Loser_Tag");
            loseTagObj.transform.SetParent(loserFrameObj.transform, false);
            var ltRect = loseTagObj.AddComponent<RectTransform>();
            ltRect.anchorMin = new Vector2(0, 1);
            ltRect.anchorMax = new Vector2(1, 1);
            ltRect.pivot = new Vector2(0.5f, 0);
            ltRect.sizeDelta = new Vector2(0, 28);
            ltRect.anchoredPosition = new Vector2(0, 6);
            var ltText = loseTagObj.AddComponent<Text>();
            ltText.text = "💀 DERROTADO (K.O.)";
            ltText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ltText.fontSize = 18;
            ltText.fontStyle = FontStyle.Bold;
            ltText.alignment = TextAnchor.MiddleCenter;
            ltText.color = new Color(0.95f, 0.35f, 0.35f);

            // Nombre Ganador
            GameObject nameObj = new GameObject("Winner_Name");
            nameObj.transform.SetParent(cardObj.transform, false);
            var nRect = nameObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.04f, 0.27f);
            nRect.anchorMax = new Vector2(0.50f, 0.36f);
            nRect.offsetMin = Vector2.zero;
            nRect.offsetMax = Vector2.zero;

            ui.winnerNameText = nameObj.AddComponent<Text>();
            ui.winnerNameText.text = "YURI";
            ui.winnerNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.winnerNameText.fontSize = 24;
            ui.winnerNameText.fontStyle = FontStyle.Bold;
            ui.winnerNameText.alignment = TextAnchor.MiddleCenter;
            ui.winnerNameText.color = new Color(1f, 0.95f, 0.7f);

            // Nombre Perdedor
            GameObject loserNameObj = new GameObject("Loser_Name");
            loserNameObj.transform.SetParent(cardObj.transform, false);
            var lnRect = loserNameObj.AddComponent<RectTransform>();
            lnRect.anchorMin = new Vector2(0.50f, 0.27f);
            lnRect.anchorMax = new Vector2(0.96f, 0.36f);
            lnRect.offsetMin = Vector2.zero;
            lnRect.offsetMax = Vector2.zero;

            ui.loserNameText = loserNameObj.AddComponent<Text>();
            ui.loserNameText.text = "RIVAL";
            ui.loserNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.loserNameText.fontSize = 22;
            ui.loserNameText.fontStyle = FontStyle.Bold;
            ui.loserNameText.alignment = TextAnchor.MiddleCenter;
            ui.loserNameText.color = new Color(0.85f, 0.7f, 0.7f);

            // Stats
            GameObject statsObj = new GameObject("Winner_Stats");
            statsObj.transform.SetParent(cardObj.transform, false);
            var sRect = statsObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0.18f);
            sRect.anchorMax = new Vector2(1, 0.28f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            ui.winnerStatsText = statsObj.AddComponent<Text>();
            ui.winnerStatsText.text = "❤️ 87% SALUD  •  🔥 12 HITS";
            ui.winnerStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ui.winnerStatsText.fontSize = 20;
            ui.winnerStatsText.alignment = TextAnchor.MiddleCenter;
            ui.winnerStatsText.color = new Color(0.75f, 0.9f, 1f);

            // Botón Revancha
            GameObject remObj = new GameObject("Btn_Rematch");
            remObj.transform.SetParent(cardObj.transform, false);
            var remRect = remObj.AddComponent<RectTransform>();
            remRect.anchorMin = new Vector2(0.08f, 0.04f);
            remRect.anchorMax = new Vector2(0.48f, 0.14f);
            remRect.offsetMin = Vector2.zero;
            remRect.offsetMax = Vector2.zero;

            var remImg = remObj.AddComponent<Image>();
            remImg.color = new Color(0.2f, 0.75f, 0.3f);
            ui.winnerRematchButton = remObj.AddComponent<Button>();
            ui.winnerRematchButton.onClick.AddListener(() =>
            {
                if (modalObj != null) modalObj.SetActive(false);
                if (BattleManager.Instance != null) BattleManager.Instance.RestartMatch();
            });

            GameObject remTxt = new GameObject("Text");
            remTxt.transform.SetParent(remObj.transform, false);
            var rTRect = remTxt.AddComponent<RectTransform>();
            rTRect.anchorMin = Vector2.zero;
            rTRect.anchorMax = Vector2.one;
            rTRect.sizeDelta = Vector2.zero;
            var rText = remTxt.AddComponent<Text>();
            rText.text = "🔄 Revancha [F5 / R]";
            rText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rText.fontSize = 16;
            rText.fontStyle = FontStyle.Bold;
            rText.alignment = TextAnchor.MiddleCenter;
            rText.color = Color.white;

            // Botón Selección (Principal)
            GameObject selObj = new GameObject("Btn_SelectMenu");
            selObj.transform.SetParent(cardObj.transform, false);
            var selRect = selObj.AddComponent<RectTransform>();
            selRect.anchorMin = new Vector2(0.50f, 0.04f);
            selRect.anchorMax = new Vector2(0.94f, 0.14f);
            selRect.offsetMin = Vector2.zero;
            selRect.offsetMax = Vector2.zero;

            var selImg = selObj.AddComponent<Image>();
            selImg.color = new Color(0.18f, 0.52f, 0.95f);
            ui.winnerSelectButton = selObj.AddComponent<Button>();
            ui.winnerSelectButton.onClick.AddListener(() =>
            {
                if (modalObj != null) modalObj.SetActive(false);
                if (BattleUI.Instance != null) BattleUI.Instance.ShowCharacterSelectMenu();
            });

            GameObject selTxt = new GameObject("Text");
            selTxt.transform.SetParent(selObj.transform, false);
            var sTRect = selTxt.AddComponent<RectTransform>();
            sTRect.anchorMin = Vector2.zero;
            sTRect.anchorMax = Vector2.one;
            sTRect.sizeDelta = Vector2.zero;
            var sText = selTxt.AddComponent<Text>();
            sText.text = "👥 ELEGIR LUCHADOR [ENTER]";
            sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sText.fontSize = 15;
            sText.fontStyle = FontStyle.Bold;
            sText.alignment = TextAnchor.MiddleCenter;
            sText.color = Color.white;

            ui.winnerModalPanel = modalObj;
            modalObj.SetActive(false);
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
            infoRect.anchorMax = new Vector2(0.40f, 1);
            infoRect.offsetMin = new Vector2(15, 5);
            infoRect.offsetMax = new Vector2(0, -5);

            var text = infoObj.AddComponent<Text>();
            text.text = "<b>P1:</b> [A/D] Mover | [W] Salto | [F] LP | [R] HP | [G] LK | [T] HK | [Q] Super | [H] Agarre\n<b>P2:</b> [←/→] Mover | [↑] Salto | [L] LP | [P] HP | [K] LK | [O] HK | [I] Super | [Pad3] Agarre";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = new Color(0.85f, 0.9f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;

            // Botón 0: 🥊 Personajes [M / ESC] / 🛑 SALIR si está en entrenamiento
            GameObject btnMenuObj = new GameObject("Btn_SelectMenu");
            btnMenuObj.transform.SetParent(bottomBar.transform, false);
            var mRect = btnMenuObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.41f, 0.15f);
            mRect.anchorMax = new Vector2(0.52f, 0.85f);
            mRect.offsetMin = Vector2.zero;
            mRect.offsetMax = Vector2.zero;
            var mImg = btnMenuObj.AddComponent<Image>();
            mImg.color = new Color(0.95f, 0.25f, 0.35f);
            ui.openSelectMenuButton = btnMenuObj.AddComponent<Button>();
            ui.openSelectMenuButton.onClick.AddListener(() =>
            {
                if (BattleUI.Instance != null)
                {
                    if (BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
                    {
                        BattleUI.Instance.ExitTrainingToSelectMenu();
                    }
                    else
                    {
                        BattleUI.Instance.ToggleCharacterSelectMenu();
                    }
                }
            });

            GameObject mTxtObj = new GameObject("Text");
            mTxtObj.transform.SetParent(btnMenuObj.transform, false);
            var mTRect = mTxtObj.AddComponent<RectTransform>();
            mTRect.anchorMin = Vector2.zero;
            mTRect.anchorMax = Vector2.one;
            mTRect.sizeDelta = Vector2.zero;
            var mTxt = mTxtObj.AddComponent<Text>();
            mTxt.text = "🥊 Menú [ESC]";
            mTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mTxt.fontSize = 14;
            mTxt.fontStyle = FontStyle.Bold;
            mTxt.alignment = TextAnchor.MiddleCenter;
            mTxt.color = Color.white;
            mTxt.raycastTarget = false;

            // Botón 1: 🏆 Torneo Street Fighter [F3]
            GameObject btnTourneyObj = new GameObject("Btn_Tournament");
            btnTourneyObj.transform.SetParent(bottomBar.transform, false);
            var tRect = btnTourneyObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.53f, 0.15f);
            tRect.anchorMax = new Vector2(0.64f, 0.85f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var tImg = btnTourneyObj.AddComponent<Image>();
            tImg.color = new Color(1f, 0.65f, 0.1f);
            ui.tournamentButton = btnTourneyObj.AddComponent<Button>();
            ui.tournamentButton.onClick.AddListener(() =>
            {
                if (TournamentManager.Instance != null)
                {
                    int p1Id = 1;
                    if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
                    {
                        p1Id = (int)BattleManager.Instance.p1BodyType + 1;
                    }
                    TournamentManager.Instance.StartNewTournament(p1Id);
                }
            });

            GameObject tTxtObj = new GameObject("Text");
            tTxtObj.transform.SetParent(btnTourneyObj.transform, false);
            var tTRect = tTxtObj.AddComponent<RectTransform>();
            tTRect.anchorMin = Vector2.zero;
            tTRect.anchorMax = Vector2.one;
            tTRect.sizeDelta = Vector2.zero;
            var tTxt = tTxtObj.AddComponent<Text>();
            tTxt.text = "🏆 Torneo [F3]";
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 14;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = Color.black;
            tTxt.raycastTarget = false;

            // Botón 2: 📶 Multijugador Wi-Fi [N]
            GameObject btnWifiObj = new GameObject("Btn_Wifi");
            btnWifiObj.transform.SetParent(bottomBar.transform, false);
            var wRect = btnWifiObj.AddComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.65f, 0.15f);
            wRect.anchorMax = new Vector2(0.76f, 0.85f);
            wRect.offsetMin = Vector2.zero;
            wRect.offsetMax = Vector2.zero;
            var wImg = btnWifiObj.AddComponent<Image>();
            wImg.color = new Color(0.12f, 0.65f, 0.85f);
            var wBtn = btnWifiObj.AddComponent<Button>();
            wBtn.onClick.AddListener(() =>
            {
                if (NetworkLobbyUI.Instance != null)
                {
                    NetworkLobbyUI.Instance.ToggleLobby();
                }
            });

            GameObject wTxtObj = new GameObject("Text");
            wTxtObj.transform.SetParent(btnWifiObj.transform, false);
            var wtRect = wTxtObj.AddComponent<RectTransform>();
            wtRect.anchorMin = Vector2.zero;
            wtRect.anchorMax = Vector2.one;
            wtRect.sizeDelta = Vector2.zero;
            var wTxt = wTxtObj.AddComponent<Text>();
            wTxt.text = "📶 WI-FI [N]";
            wTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            wTxt.fontSize = 14;
            wTxt.fontStyle = FontStyle.Bold;
            wTxt.alignment = TextAnchor.MiddleCenter;
            wTxt.color = Color.white;
            wTxt.raycastTarget = false;

            // Botón 3: 📸 Caras / Webcam [C]
            GameObject btnCustomObj = new GameObject("Btn_CustomFaces");
            btnCustomObj.transform.SetParent(bottomBar.transform, false);
            var btnRect = btnCustomObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.77f, 0.15f);
            btnRect.anchorMax = new Vector2(0.88f, 0.85f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            var btnImg = btnCustomObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f);
            ui.openCustomizerButton = btnCustomObj.AddComponent<Button>();
            ui.openCustomizerButton.onClick.AddListener(() =>
            {
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ToggleFaceCustomizer();
                }
            });

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnCustomObj.transform, false);
            var btnTRect = btnTextObj.AddComponent<RectTransform>();
            btnTRect.anchorMin = Vector2.zero;
            btnTRect.anchorMax = Vector2.one;
            btnTRect.sizeDelta = Vector2.zero;
            var btnTxt = btnTextObj.AddComponent<Text>();
            btnTxt.text = "📸 Caras [C]";
            btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnTxt.fontSize = 14;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.color = Color.white;
            btnTxt.raycastTarget = false;

            // Botón 4: 🔄 Reiniciar [F5]
            GameObject btnRestartObj = new GameObject("Btn_Restart");
            btnRestartObj.transform.SetParent(bottomBar.transform, false);
            var rRect = btnRestartObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0.89f, 0.15f);
            rRect.anchorMax = new Vector2(0.99f, 0.85f);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;
            var rImg = btnRestartObj.AddComponent<Image>();
            rImg.color = new Color(0.9f, 0.3f, 0.2f);
            ui.rematchButton = btnRestartObj.AddComponent<Button>();
            ui.rematchButton.onClick.AddListener(() =>
            {
                if (BattleUI.Instance != null && BattleUI.Instance.winnerModalPanel != null)
                {
                    BattleUI.Instance.winnerModalPanel.SetActive(false);
                }
                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.RestartMatch();
                }
            });

            GameObject rTextObj = new GameObject("Text");
            rTextObj.transform.SetParent(btnRestartObj.transform, false);
            var rTRect = rTextObj.AddComponent<RectTransform>();
            rTRect.anchorMin = Vector2.zero;
            rTRect.anchorMax = Vector2.one;
            rTRect.sizeDelta = Vector2.zero;
            var rTxt = rTextObj.AddComponent<Text>();
            rTxt.text = "🔄 Reiniciar [F5]";
            rTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rTxt.fontSize = 14;
            rTxt.fontStyle = FontStyle.Bold;
            rTxt.alignment = TextAnchor.MiddleCenter;
            rTxt.color = Color.white;
            rTxt.raycastTarget = false;
        }

        private void BuildWebcamModal(Transform canvas, BattleUI ui, out WebcamCaptureManager webcamMgr)
        {
            GameObject modalObj = new GameObject("Modal_FaceCustomizer");
            modalObj.transform.SetParent(canvas, false);
            var mRect = modalObj.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.sizeDelta = new Vector2(1080, 750);
            mRect.anchoredPosition = Vector2.zero;

            var modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);

            WebcamCaptureManager localWebcamMgr = modalObj.AddComponent<WebcamCaptureManager>();
            webcamMgr = localWebcamMgr;
            VoiceCaptureManager voiceMgr = modalObj.AddComponent<VoiceCaptureManager>();

            // Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 1);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.pivot = new Vector2(0.5f, 1);
            tRect.sizeDelta = new Vector2(0, 46);
            tRect.anchoredPosition = new Vector2(0, -10);
            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "🎮 PERSONALIZADOR ARCADE: ROSTROS Y VOCES";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.85f, 0.2f);

            // Selector de los 6 Luchadores del Torneo
            CreateTournamentRosterButtons(modalObj.transform, localWebcamMgr);

            // Barra de Pestañas (Tabs)
            GameObject tabsBar = new GameObject("TabsBar");
            tabsBar.transform.SetParent(modalObj.transform, false);
            var tbRect = tabsBar.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.5f, 0.695f);
            tbRect.anchorMax = new Vector2(0.5f, 0.695f);
            tbRect.pivot = new Vector2(0.5f, 0.5f);
            tbRect.sizeDelta = new Vector2(620, 38);
            tbRect.anchoredPosition = Vector2.zero;

            // Tab 1: Caras
            GameObject tabFacesObj = new GameObject("Tab_Faces");
            tabFacesObj.transform.SetParent(tabsBar.transform, false);
            var tfRect = tabFacesObj.AddComponent<RectTransform>();
            tfRect.anchorMin = new Vector2(0f, 0f);
            tfRect.anchorMax = new Vector2(0.485f, 1f);
            tfRect.offsetMin = Vector2.zero; tfRect.offsetMax = Vector2.zero;
            var tabFacesImg = tabFacesObj.AddComponent<Image>();
            tabFacesImg.color = new Color(0.2f, 0.55f, 0.95f);
            var tabFacesBtn = tabFacesObj.AddComponent<Button>();

            GameObject tfTxtObj = new GameObject("Text");
            tfTxtObj.transform.SetParent(tabFacesObj.transform, false);
            var tftRect = tfTxtObj.AddComponent<RectTransform>();
            tftRect.anchorMin = Vector2.zero; tftRect.anchorMax = Vector2.one; tftRect.sizeDelta = Vector2.zero;
            var tfTxt = tfTxtObj.AddComponent<Text>();
            tfTxt.text = "📷 1. CARAS (5 FOTOS)";
            tfTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tfTxt.fontSize = 14;
            tfTxt.fontStyle = FontStyle.Bold;
            tfTxt.alignment = TextAnchor.MiddleCenter;
            tfTxt.color = Color.white;

            // Tab 2: Voces
            GameObject tabVoicesObj = new GameObject("Tab_Voices");
            tabVoicesObj.transform.SetParent(tabsBar.transform, false);
            var tvRect = tabVoicesObj.AddComponent<RectTransform>();
            tvRect.anchorMin = new Vector2(0.515f, 0f);
            tvRect.anchorMax = new Vector2(1f, 1f);
            tvRect.offsetMin = Vector2.zero; tvRect.offsetMax = Vector2.zero;
            var tabVoicesImg = tabVoicesObj.AddComponent<Image>();
            tabVoicesImg.color = new Color(0.20f, 0.22f, 0.30f);
            var tabVoicesBtn = tabVoicesObj.AddComponent<Button>();

            GameObject tvTxtObj = new GameObject("Text");
            tvTxtObj.transform.SetParent(tabVoicesObj.transform, false);
            var tvtRect = tvTxtObj.AddComponent<RectTransform>();
            tvtRect.anchorMin = Vector2.zero; tvtRect.anchorMax = Vector2.one; tvtRect.sizeDelta = Vector2.zero;
            var tvTxt = tvTxtObj.AddComponent<Text>();
            tvTxt.text = "🎙️ 2. VOCES DE BATALLA (5 GRITOS)";
            tvTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tvTxt.fontSize = 14;
            tvTxt.fontStyle = FontStyle.Bold;
            tvTxt.alignment = TextAnchor.MiddleCenter;
            tvTxt.color = new Color(0.85f, 0.9f, 1f);

            // ==================== SECCIÓN 1: FOTOS ====================
            GameObject photoSectionObj = new GameObject("PhotoSection");
            photoSectionObj.transform.SetParent(modalObj.transform, false);
            var psRect = photoSectionObj.AddComponent<RectTransform>();
            psRect.anchorMin = new Vector2(0f, 0.09f);
            psRect.anchorMax = new Vector2(1f, 0.67f);
            psRect.offsetMin = Vector2.zero; psRect.offsetMax = Vector2.zero;

            // Visor de Cámara en Vivo
            GameObject camViewObj = new GameObject("CamView");
            camViewObj.transform.SetParent(photoSectionObj.transform, false);
            var cRect = camViewObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.04f, 0.20f);
            cRect.anchorMax = new Vector2(0.50f, 0.98f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var camRaw = camViewObj.AddComponent<RawImage>();
            camRaw.color = Color.white;
            camRaw.raycastTarget = true;
            localWebcamMgr.cameraPreviewUI = camRaw;

            var dragHandler = camViewObj.AddComponent<PhotoDragHandler>();
            dragHandler.manager = localWebcamMgr;

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

            // Botones de orientación (Girar y Espejo)
            CreateCameraOrientationControls(photoSectionObj.transform, localWebcamMgr);

            // Selector de Doble Cabeza (Hombre-Máquina)
            GameObject headRow = new GameObject("HeadSelectorRow");
            headRow.transform.SetParent(photoSectionObj.transform, false);
            var hrRect = headRow.AddComponent<RectTransform>();
            hrRect.anchorMin = new Vector2(0.53f, 0.91f);
            hrRect.anchorMax = new Vector2(0.97f, 0.99f);
            hrRect.offsetMin = Vector2.zero;
            hrRect.offsetMax = Vector2.zero;

            GameObject h1BtnObj = new GameObject("Btn_Head1");
            h1BtnObj.transform.SetParent(headRow.transform, false);
            var h1Rect = h1BtnObj.AddComponent<RectTransform>();
            h1Rect.anchorMin = new Vector2(0f, 0f);
            h1Rect.anchorMax = new Vector2(0.485f, 1f);
            h1Rect.offsetMin = Vector2.zero;
            h1Rect.offsetMax = Vector2.zero;
            var h1Img = h1BtnObj.AddComponent<Image>();
            h1Img.color = new Color(0.2f, 0.85f, 0.4f);
            localWebcamMgr.head1ToggleButton = h1BtnObj.AddComponent<Button>();

            GameObject h1TxtObj = new GameObject("Text");
            h1TxtObj.transform.SetParent(h1BtnObj.transform, false);
            var h1TRect = h1TxtObj.AddComponent<RectTransform>();
            h1TRect.anchorMin = Vector2.zero; h1TRect.anchorMax = Vector2.one; h1TRect.sizeDelta = Vector2.zero;
            var h1Txt = h1TxtObj.AddComponent<Text>();
            h1Txt.text = "👤 CABEZA 1 (Izq)";
            h1Txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            h1Txt.fontSize = 13;
            h1Txt.fontStyle = FontStyle.Bold;
            h1Txt.alignment = TextAnchor.MiddleCenter;
            h1Txt.color = Color.white;
            localWebcamMgr.head1ButtonText = h1Txt;

            GameObject h2BtnObj = new GameObject("Btn_Head2");
            h2BtnObj.transform.SetParent(headRow.transform, false);
            var h2Rect = h2BtnObj.AddComponent<RectTransform>();
            h2Rect.anchorMin = new Vector2(0.515f, 0f);
            h2Rect.anchorMax = new Vector2(1f, 1f);
            h2Rect.offsetMin = Vector2.zero;
            h2Rect.offsetMax = Vector2.zero;
            var h2Img = h2BtnObj.AddComponent<Image>();
            h2Img.color = new Color(0.25f, 0.28f, 0.35f);
            localWebcamMgr.head2ToggleButton = h2BtnObj.AddComponent<Button>();

            GameObject h2TxtObj = new GameObject("Text");
            h2TxtObj.transform.SetParent(h2BtnObj.transform, false);
            var h2TRect = h2TxtObj.AddComponent<RectTransform>();
            h2TRect.anchorMin = Vector2.zero; h2TRect.anchorMax = Vector2.one; h2TRect.sizeDelta = Vector2.zero;
            var h2Txt = h2TxtObj.AddComponent<Text>();
            h2Txt.text = "👤 CABEZA 2 (Der)";
            h2Txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            h2Txt.fontSize = 13;
            h2Txt.fontStyle = FontStyle.Bold;
            h2Txt.alignment = TextAnchor.MiddleCenter;
            h2Txt.color = Color.white;
            localWebcamMgr.head2ButtonText = h2Txt;

            localWebcamMgr.headSelectorRow = headRow;
            headRow.SetActive(false);

            // Panel de Guía de Expresión Facial (Instrucciones para las 5 fotos)
            GameObject guideBox = new GameObject("ExpressionGuideBox");
            guideBox.transform.SetParent(photoSectionObj.transform, false);
            var gbRect = guideBox.AddComponent<RectTransform>();
            gbRect.anchorMin = new Vector2(0.53f, 0.80f);
            gbRect.anchorMax = new Vector2(0.97f, 0.89f);
            gbRect.offsetMin = Vector2.zero;
            gbRect.offsetMax = Vector2.zero;

            var gbBg = guideBox.AddComponent<Image>();
            gbBg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            var gbOutline = guideBox.AddComponent<Outline>();
            gbOutline.effectColor = new Color(0.2f, 0.8f, 1f);
            gbOutline.effectDistance = new Vector2(1, 1);

            GameObject gTitleObj = new GameObject("GuideTitle");
            gTitleObj.transform.SetParent(guideBox.transform, false);
            var gtRect = gTitleObj.AddComponent<RectTransform>();
            gtRect.anchorMin = new Vector2(0, 0.52f);
            gtRect.anchorMax = new Vector2(1, 1f);
            gtRect.offsetMin = new Vector2(10, 0);
            gtRect.offsetMax = new Vector2(-10, 0);
            localWebcamMgr.guideTitleText = gTitleObj.AddComponent<Text>();
            localWebcamMgr.guideTitleText.text = "1. 📷 FOTO NORMAL";
            localWebcamMgr.guideTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            localWebcamMgr.guideTitleText.fontSize = 15;
            localWebcamMgr.guideTitleText.fontStyle = FontStyle.Bold;
            localWebcamMgr.guideTitleText.alignment = TextAnchor.MiddleLeft;
            localWebcamMgr.guideTitleText.color = new Color(1f, 0.9f, 0.2f);

            GameObject gDescObj = new GameObject("GuideDesc");
            gDescObj.transform.SetParent(guideBox.transform, false);
            var gdRect = gDescObj.AddComponent<RectTransform>();
            gdRect.anchorMin = new Vector2(0, 0);
            gdRect.anchorMax = new Vector2(1, 0.52f);
            gdRect.offsetMin = new Vector2(10, 2);
            gdRect.offsetMax = new Vector2(-10, -2);
            localWebcamMgr.guideDescriptionText = gDescObj.AddComponent<Text>();
            localWebcamMgr.guideDescriptionText.text = "Mirando a cámara • Boca relajada • Rostro centrado";
            localWebcamMgr.guideDescriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            localWebcamMgr.guideDescriptionText.fontSize = 12;
            localWebcamMgr.guideDescriptionText.alignment = TextAnchor.MiddleLeft;
            localWebcamMgr.guideDescriptionText.color = new Color(0.85f, 0.9f, 1f);

            // Botones para capturar las 5 fotos
            CreateCaptureButton(photoSectionObj.transform, localWebcamMgr, "📷 1. Foto NORMAL (Pose Neutral)", FaceType.Base, new Vector2(560, -100), out Image thumbBase);
            CreateCaptureButton(photoSectionObj.transform, localWebcamMgr, "📷 2. Foto DOLOR (Mueca de Golpe)", FaceType.Dolor, new Vector2(560, -158), out Image thumbHurt);
            CreateCaptureButton(photoSectionObj.transform, localWebcamMgr, "📷 3. Foto RABIA (Furia para Rage)", FaceType.Enojo, new Vector2(560, -216), out Image thumbAngry);
            CreateCaptureButton(photoSectionObj.transform, localWebcamMgr, "📷 4. Foto GANADOR (Celebración)", FaceType.Ganador, new Vector2(560, -274), out Image thumbWinner);
            CreateCaptureButton(photoSectionObj.transform, localWebcamMgr, "📷 5. Foto PERDEDOR (K.O. / Derrota)", FaceType.KO, new Vector2(560, -332), out Image thumbKO);

            localWebcamMgr.previewThumbBase = thumbBase;
            localWebcamMgr.previewThumbHurt = thumbHurt;
            localWebcamMgr.previewThumbAngry = thumbAngry;
            localWebcamMgr.previewThumbWinner = thumbWinner;
            localWebcamMgr.previewThumbKO = thumbKO;

            // Campo de texto: NOMBRE: __________
            GameObject nameRow = new GameObject("NameInputRow");
            nameRow.transform.SetParent(photoSectionObj.transform, false);
            var nrRect = nameRow.AddComponent<RectTransform>();
            nrRect.anchorMin = new Vector2(0.53f, 0.01f);
            nrRect.anchorMax = new Vector2(0.97f, 0.11f);
            nrRect.offsetMin = Vector2.zero;
            nrRect.offsetMax = Vector2.zero;

            var nrBg = nameRow.AddComponent<Image>();
            nrBg.color = new Color(0.12f, 0.14f, 0.20f, 0.95f);

            GameObject nameLabelObj = new GameObject("Label");
            nameLabelObj.transform.SetParent(nameRow.transform, false);
            var nlRect = nameLabelObj.AddComponent<RectTransform>();
            nlRect.anchorMin = new Vector2(0, 0);
            nlRect.anchorMax = new Vector2(0.28f, 1);
            nlRect.offsetMin = new Vector2(8, 0);
            nlRect.offsetMax = Vector2.zero;
            var nlTxt = nameLabelObj.AddComponent<Text>();
            nlTxt.text = "NOMBRE:";
            nlTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nlTxt.fontSize = 15;
            nlTxt.fontStyle = FontStyle.Bold;
            nlTxt.alignment = TextAnchor.MiddleLeft;
            nlTxt.color = new Color(1f, 0.85f, 0.2f);

            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(nameRow.transform, false);
            var inRect = inputObj.AddComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0.30f, 0.1f);
            inRect.anchorMax = new Vector2(0.98f, 0.9f);
            inRect.offsetMin = Vector2.zero;
            inRect.offsetMax = Vector2.zero;
            var inBg = inputObj.AddComponent<Image>();
            inBg.color = new Color(0.08f, 0.08f, 0.12f);

            GameObject inTxtObj = new GameObject("Text");
            inTxtObj.transform.SetParent(inputObj.transform, false);
            var itRect = inTxtObj.AddComponent<RectTransform>();
            itRect.anchorMin = Vector2.zero;
            itRect.anchorMax = Vector2.one;
            itRect.sizeDelta = Vector2.zero;
            var inTxt = inTxtObj.AddComponent<Text>();
            inTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inTxt.fontSize = 15;
            inTxt.color = Color.white;
            inTxt.alignment = TextAnchor.MiddleLeft;

            var inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = inTxt;
            inputField.text = "El Gordo";
            localWebcamMgr.fighterNameInput = inputField;

            // ==================== SECCIÓN 2: VOCES ====================
            GameObject voiceSectionObj = new GameObject("VoiceSection");
            voiceSectionObj.transform.SetParent(modalObj.transform, false);
            var vsRect = voiceSectionObj.AddComponent<RectTransform>();
            vsRect.anchorMin = new Vector2(0.04f, 0.09f);
            vsRect.anchorMax = new Vector2(0.96f, 0.67f);
            vsRect.offsetMin = Vector2.zero; vsRect.offsetMax = Vector2.zero;

            BuildVoiceStudioUI(voiceSectionObj.transform, voiceMgr);
            voiceSectionObj.SetActive(false);

            // Control de Pestañas
            tabFacesBtn.onClick.AddListener(() =>
            {
                photoSectionObj.SetActive(true);
                voiceSectionObj.SetActive(false);
                tabFacesImg.color = new Color(0.2f, 0.55f, 0.95f);
                tabVoicesImg.color = new Color(0.20f, 0.22f, 0.30f);
            });

            tabVoicesBtn.onClick.AddListener(() =>
            {
                photoSectionObj.SetActive(false);
                voiceSectionObj.SetActive(true);
                tabFacesImg.color = new Color(0.20f, 0.22f, 0.30f);
                tabVoicesImg.color = new Color(0.95f, 0.35f, 0.25f);

                voiceMgr.SetTargetFighter(localWebcamMgr.targetFighterId, localWebcamMgr.currentHeadIndex);
                voiceMgr.CheckMicrophoneDevices();
                voiceMgr.UpdateAllSlotVisuals();
            });

            // Botón Cerrar / Volver
            GameObject closeBtnObj = new GameObject("Btn_CloseModal");
            closeBtnObj.transform.SetParent(modalObj.transform, false);
            var clRect = closeBtnObj.AddComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0.5f, 0.045f);
            clRect.anchorMax = new Vector2(0.5f, 0.045f);
            clRect.pivot = new Vector2(0.5f, 0.5f);
            clRect.sizeDelta = new Vector2(280, 46);
            clRect.anchoredPosition = Vector2.zero;

            var clImg = closeBtnObj.AddComponent<Image>();
            clImg.color = new Color(0.2f, 0.8f, 0.35f);
            ui.closeCustomizerButton = closeBtnObj.AddComponent<Button>();
            ui.closeCustomizerButton.onClick.AddListener(() =>
            {
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.CloseFaceCustomizer();
                }
                else
                {
                    modalObj.SetActive(false);
                }
            });

            GameObject clTxtObj = new GameObject("Text");
            clTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var clTRect = clTxtObj.AddComponent<RectTransform>();
            clTRect.anchorMin = Vector2.zero;
            clTRect.anchorMax = Vector2.one;
            clTRect.sizeDelta = Vector2.zero;
            var clTxt = clTxtObj.AddComponent<Text>();
            clTxt.text = "✅ ¡LISTO! VOLVER";
            clTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            clTxt.fontSize = 18;
            clTxt.fontStyle = FontStyle.Bold;
            clTxt.alignment = TextAnchor.MiddleCenter;
            clTxt.color = Color.white;

            ui.faceCustomizerPanel = modalObj;
            modalObj.SetActive(false);
        }

        private void BuildVoiceStudioUI(Transform parent, VoiceCaptureManager voiceMgr)
        {
            // Barra de estado superior: Micrófono y Badge de grabación
            GameObject headerObj = new GameObject("VoiceHeaderBar");
            headerObj.transform.SetParent(parent, false);
            var hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 0.89f);
            hRect.anchorMax = new Vector2(1, 1f);
            hRect.offsetMin = Vector2.zero; hRect.offsetMax = Vector2.zero;

            var hBg = headerObj.AddComponent<Image>();
            hBg.color = new Color(0.11f, 0.14f, 0.20f, 0.95f);
            var hOutline = headerObj.AddComponent<Outline>();
            hOutline.effectColor = new Color(0.2f, 0.7f, 1f, 0.5f);
            hOutline.effectDistance = new Vector2(1, 1);

            GameObject micTxtObj = new GameObject("MicStatus");
            micTxtObj.transform.SetParent(headerObj.transform, false);
            var mtRect = micTxtObj.AddComponent<RectTransform>();
            mtRect.anchorMin = new Vector2(0.02f, 0);
            mtRect.anchorMax = new Vector2(0.48f, 1);
            mtRect.offsetMin = Vector2.zero; mtRect.offsetMax = Vector2.zero;
            voiceMgr.micStatusText = micTxtObj.AddComponent<Text>();
            voiceMgr.micStatusText.text = "🎤 Micrófono: Buscando...";
            voiceMgr.micStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            voiceMgr.micStatusText.fontSize = 13;
            voiceMgr.micStatusText.alignment = TextAnchor.MiddleLeft;
            voiceMgr.micStatusText.color = Color.white;

            GameObject badgeObj = new GameObject("RecordingBadge");
            badgeObj.transform.SetParent(headerObj.transform, false);
            var bRect = badgeObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.50f, 0);
            bRect.anchorMax = new Vector2(0.98f, 1);
            bRect.offsetMin = Vector2.zero; bRect.offsetMax = Vector2.zero;
            voiceMgr.recordingStatusBadge = badgeObj.AddComponent<Text>();
            voiceMgr.recordingStatusBadge.text = "🎙️ Presiona '🔴 GRABAR' para guardar tu propio grito de batalla.";
            voiceMgr.recordingStatusBadge.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            voiceMgr.recordingStatusBadge.fontSize = 13;
            voiceMgr.recordingStatusBadge.fontStyle = FontStyle.Bold;
            voiceMgr.recordingStatusBadge.alignment = TextAnchor.MiddleRight;
            voiceMgr.recordingStatusBadge.color = new Color(1f, 0.9f, 0.3f);

            // 5 Slots de Voz
            voiceMgr.slotAttack = CreateVoiceSlotRow(parent, voiceMgr, VoiceType.Attack, "1. 💥 GOLPE FUERTE / ATAQUE", "¡Toma! / ¡Muere!", 0.70f, 0.86f, 1.8f);
            voiceMgr.slotSpecial = CreateVoiceSlotRow(parent, voiceMgr, VoiceType.Special, "2. 🌀 HADOUKEN / ESPECIAL", "¡Hadouken! / ¡Fuego!", 0.53f, 0.69f, 2.2f);
            voiceMgr.slotHurt = CreateVoiceSlotRow(parent, voiceMgr, VoiceType.Hurt, "3. 😣 DOLOR / DAÑO", "¡Ayyy! / ¡Ouch!", 0.36f, 0.52f, 1.8f);
            voiceMgr.slotVictory = CreateVoiceSlotRow(parent, voiceMgr, VoiceType.Victory, "4. 👑 VICTORIA / CELEBRACIÓN", "¡Soy el mejor! / ¡Gané!", 0.19f, 0.35f, 2.5f);
            voiceMgr.slotKO = CreateVoiceSlotRow(parent, voiceMgr, VoiceType.KO, "5. 💀 DERROTA / K.O.", "¡Nooooo! / ¡Maldición!", 0.02f, 0.18f, 2.5f);

            voiceMgr.UpdateAllSlotVisuals();
        }

        private VoiceCaptureManager.VoiceSlotUI CreateVoiceSlotRow(Transform parent, VoiceCaptureManager voiceMgr, VoiceType type, string title, string hint, float yMin, float yMax, float maxDuration)
        {
            GameObject rowObj = new GameObject($"VoiceSlot_{type}");
            rowObj.transform.SetParent(parent, false);
            var rRect = rowObj.AddComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0, yMin);
            rRect.anchorMax = new Vector2(1, yMax);
            rRect.offsetMin = Vector2.zero;
            rRect.offsetMax = Vector2.zero;

            var rBg = rowObj.AddComponent<Image>();
            rBg.color = new Color(0.12f, 0.15f, 0.22f, 0.95f);
            var rOutline = rowObj.AddComponent<Outline>();
            rOutline.effectColor = new Color(0.25f, 0.55f, 0.85f, 0.6f);
            rOutline.effectDistance = new Vector2(1, 1);

            var slotUI = new VoiceCaptureManager.VoiceSlotUI { type = type };

            // Label
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(rowObj.transform, false);
            var lRect = lblObj.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0.02f, 0);
            lRect.anchorMax = new Vector2(0.42f, 1);
            lRect.offsetMin = Vector2.zero; lRect.offsetMax = Vector2.zero;
            slotUI.labelText = lblObj.AddComponent<Text>();
            slotUI.labelText.text = $"<b>{title}</b>\n<size=11><color=#99BBEE>Ejemplo: {hint}</color></size>";
            slotUI.labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotUI.labelText.fontSize = 13;
            slotUI.labelText.alignment = TextAnchor.MiddleLeft;
            slotUI.labelText.color = Color.white;

            // Status Badge
            GameObject statObj = new GameObject("Status");
            statObj.transform.SetParent(rowObj.transform, false);
            var stRect = statObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.43f, 0);
            stRect.anchorMax = new Vector2(0.61f, 1);
            stRect.offsetMin = Vector2.zero; stRect.offsetMax = Vector2.zero;
            slotUI.statusText = statObj.AddComponent<Text>();
            slotUI.statusText.text = "<color=#AAAAAA>○ Arcade Default</color>";
            slotUI.statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotUI.statusText.fontSize = 13;
            slotUI.statusText.alignment = TextAnchor.MiddleCenter;

            // Botón Grabar
            GameObject recBtn = new GameObject("Btn_Record");
            recBtn.transform.SetParent(rowObj.transform, false);
            var rbRect = recBtn.AddComponent<RectTransform>();
            rbRect.anchorMin = new Vector2(0.62f, 0.14f);
            rbRect.anchorMax = new Vector2(0.75f, 0.86f);
            rbRect.offsetMin = Vector2.zero; rbRect.offsetMax = Vector2.zero;
            slotUI.recordButtonBg = recBtn.AddComponent<Image>();
            slotUI.recordButtonBg.color = new Color(0.85f, 0.22f, 0.2f);
            slotUI.recordButton = recBtn.AddComponent<Button>();
            slotUI.recordButton.onClick.AddListener(() => voiceMgr.ToggleRecording(type, maxDuration));

            GameObject rbTxtObj = new GameObject("Text");
            rbTxtObj.transform.SetParent(recBtn.transform, false);
            var rbtRect = rbTxtObj.AddComponent<RectTransform>();
            rbtRect.anchorMin = Vector2.zero; rbtRect.anchorMax = Vector2.one; rbtRect.sizeDelta = Vector2.zero;
            slotUI.recordButtonText = rbTxtObj.AddComponent<Text>();
            slotUI.recordButtonText.text = "🔴 GRABAR";
            slotUI.recordButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slotUI.recordButtonText.fontSize = 13;
            slotUI.recordButtonText.fontStyle = FontStyle.Bold;
            slotUI.recordButtonText.alignment = TextAnchor.MiddleCenter;
            slotUI.recordButtonText.color = Color.white;

            // Botón Escuchar
            GameObject playBtn = new GameObject("Btn_Play");
            playBtn.transform.SetParent(rowObj.transform, false);
            var pbRect = playBtn.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.76f, 0.14f);
            pbRect.anchorMax = new Vector2(0.88f, 0.86f);
            pbRect.offsetMin = Vector2.zero; pbRect.offsetMax = Vector2.zero;
            var pbImg = playBtn.AddComponent<Image>();
            pbImg.color = new Color(0.18f, 0.52f, 0.95f);
            slotUI.playButton = playBtn.AddComponent<Button>();
            slotUI.playButton.onClick.AddListener(() => voiceMgr.PlayPreview(type));

            GameObject pbTxtObj = new GameObject("Text");
            pbTxtObj.transform.SetParent(playBtn.transform, false);
            var pbtRect = pbTxtObj.AddComponent<RectTransform>();
            pbtRect.anchorMin = Vector2.zero; pbtRect.anchorMax = Vector2.one; pbtRect.sizeDelta = Vector2.zero;
            var pbTxt = pbTxtObj.AddComponent<Text>();
            pbTxt.text = "▶️ ESCUCHAR";
            pbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pbTxt.fontSize = 12;
            pbTxt.fontStyle = FontStyle.Bold;
            pbTxt.alignment = TextAnchor.MiddleCenter;
            pbTxt.color = Color.white;

            // Botón Reset
            GameObject delBtn = new GameObject("Btn_Delete");
            delBtn.transform.SetParent(rowObj.transform, false);
            var dbRect = delBtn.AddComponent<RectTransform>();
            dbRect.anchorMin = new Vector2(0.89f, 0.14f);
            dbRect.anchorMax = new Vector2(0.98f, 0.86f);
            dbRect.offsetMin = Vector2.zero; dbRect.offsetMax = Vector2.zero;
            var dbImg = delBtn.AddComponent<Image>();
            dbImg.color = new Color(0.35f, 0.38f, 0.45f);
            slotUI.deleteButton = delBtn.AddComponent<Button>();
            slotUI.deleteButton.onClick.AddListener(() => voiceMgr.DeleteVoice(type));

            GameObject dbTxtObj = new GameObject("Text");
            dbTxtObj.transform.SetParent(delBtn.transform, false);
            var dbtRect = dbTxtObj.AddComponent<RectTransform>();
            dbtRect.anchorMin = Vector2.zero; dbtRect.anchorMax = Vector2.one; dbtRect.sizeDelta = Vector2.zero;
            var dbTxt = dbTxtObj.AddComponent<Text>();
            dbTxt.text = "🗑️ RESET";
            dbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dbTxt.fontSize = 12;
            dbTxt.fontStyle = FontStyle.Bold;
            dbTxt.alignment = TextAnchor.MiddleCenter;
            dbTxt.color = new Color(0.9f, 0.9f, 0.9f);

            return slotUI;
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

            string[] names = { "1. El Gordo", "2. El Flaco", "3. El Musculoso", "4. La Mujer", "5. La Guerrera", "6. El Dos Cabezas" };
            Color[] colors = {
                new Color(0.9f, 0.2f, 0.2f),
                new Color(0.2f, 0.7f, 0.35f),
                new Color(0.95f, 0.8f, 0.1f),
                new Color(0.8f, 0.25f, 0.85f),
                new Color(0.85f, 0.15f, 0.22f),
                new Color(0.25f, 0.45f, 0.85f)
            };

            float btnWidth = 1f / 6f;
            for (int i = 0; i < 6; i++)
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
                txt.fontSize = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = (i == 2) ? Color.black : Color.white;
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
            lTxt.text = "Editando: <b>Luchador 1 (El Gordo)</b>";
            lTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lTxt.fontSize = 17;
            lTxt.alignment = TextAnchor.MiddleLeft;
            lTxt.color = new Color(1f, 0.9f, 0.2f);
            mgr.targetFighterLabel = lTxt;
        }

        private void CreateCameraOrientationControls(Transform parent, WebcamCaptureManager mgr)
        {
            // Fila 1: Girar, Espejo, Zoom +, Zoom -
            GameObject container = new GameObject("CameraControls_Row1");
            container.transform.SetParent(parent, false);
            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.135f);
            rect.anchorMax = new Vector2(0.50f, 0.195f);
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
            vTxt.fontSize = 12;
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
            hTxt.fontSize = 12;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = Color.white;

            // Botón Zoom + (Acercar más la foto al rostro)
            GameObject btnZoomIn = new GameObject("Btn_ZoomIn");
            btnZoomIn.transform.SetParent(container.transform, false);
            var zInRect = btnZoomIn.AddComponent<RectTransform>();
            zInRect.anchorMin = new Vector2(0.51f, 0);
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
            zInTxt.fontSize = 12;
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
            zOutTxt.fontSize = 12;
            zOutTxt.fontStyle = FontStyle.Bold;
            zOutTxt.alignment = TextAnchor.MiddleCenter;
            zOutTxt.color = Color.white;

            // Fila 2: Flechas de Desplazamiento y Recentrado (Mover Foto Izq, Arriba, Abajo, Der, Centrar)
            GameObject panContainer = new GameObject("CameraControls_Row2_Pan");
            panContainer.transform.SetParent(parent, false);
            var pRect = panContainer.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.04f, 0.070f);
            pRect.anchorMax = new Vector2(0.50f, 0.130f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            CreatePanButton(panContainer.transform, "◀ Izq", new Vector2(0f, 0f), new Vector2(0.18f, 1f), new Color(0.20f, 0.45f, 0.75f), () => mgr.PanLeft(0.04f));
            CreatePanButton(panContainer.transform, "▲ Arriba", new Vector2(0.20f, 0f), new Vector2(0.38f, 1f), new Color(0.20f, 0.45f, 0.75f), () => mgr.PanUp(0.04f));
            CreatePanButton(panContainer.transform, "▼ Abajo", new Vector2(0.40f, 0f), new Vector2(0.58f, 1f), new Color(0.20f, 0.45f, 0.75f), () => mgr.PanDown(0.04f));
            CreatePanButton(panContainer.transform, "▶ Der", new Vector2(0.60f, 0f), new Vector2(0.78f, 1f), new Color(0.20f, 0.45f, 0.75f), () => mgr.PanRight(0.04f));
            CreatePanButton(panContainer.transform, "⟲ Reset", new Vector2(0.80f, 0f), new Vector2(1.00f, 1f), new Color(0.75f, 0.35f, 0.20f), () => mgr.ResetPanAndZoom());

            // Estado y Consejo de arrastre
            GameObject statusObj = new GameObject("Status_OrientationZoom");
            statusObj.transform.SetParent(parent, false);
            var sRect = statusObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.04f, 0.005f);
            sRect.anchorMax = new Vector2(0.50f, 0.065f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;
            var sTxt = statusObj.AddComponent<Text>();
            sTxt.text = $"Giro: 0° | Espejo: OFF | Zoom: {mgr.faceZoom:F1}x • Arrastra con ratón";
            sTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sTxt.fontSize = 11;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = new Color(0.85f, 0.92f, 1f);
            mgr.flipStatusText = sTxt;
        }

        private void CreatePanButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Btn_Pan_" + label);
            btnObj.transform.SetParent(parent, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = aMin;
            bRect.anchorMax = aMax;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var bImg = btnObj.AddComponent<Image>();
            bImg.color = bgColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 11;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
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

            // Botón Capturar con Cámara
            GameObject btnObj = new GameObject("Button_Capture");
            btnObj.transform.SetParent(container.transform, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0, 0.5f);
            bRect.anchorMax = new Vector2(0, 0.5f);
            bRect.pivot = new Vector2(0, 0.5f);
            bRect.sizeDelta = new Vector2(285, 58);
            bRect.anchoredPosition = new Vector2(75, 0);

            var bImg = btnObj.AddComponent<Image>();
            bImg.color = emotion == FaceType.Enojo 
                ? new Color(0.9f, 0.2f, 0.2f) 
                : (emotion == FaceType.Dolor ? new Color(0.85f, 0.55f, 0.15f) : (emotion == FaceType.Ganador ? new Color(0.18f, 0.75f, 0.35f) : (emotion == FaceType.KO ? new Color(0.4f, 0.4f, 0.5f) : new Color(0.2f, 0.5f, 0.85f))));

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                mgr.SetGuideForEmotion(emotion);
                mgr.CaptureCurrentFrameAs(emotion);
            });

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            // Botón 📁 Subir Foto desde Archivo (PC)
            GameObject uploadBtnObj = new GameObject("Button_Upload");
            uploadBtnObj.transform.SetParent(container.transform, false);
            var uRect = uploadBtnObj.AddComponent<RectTransform>();
            uRect.anchorMin = new Vector2(0, 0.5f);
            uRect.anchorMax = new Vector2(0, 0.5f);
            uRect.pivot = new Vector2(0, 0.5f);
            uRect.sizeDelta = new Vector2(90, 58);
            uRect.anchoredPosition = new Vector2(368, 0);

            var uImg = uploadBtnObj.AddComponent<Image>();
            uImg.color = new Color(0.12f, 0.55f, 0.45f);

            var uBtn = uploadBtnObj.AddComponent<Button>();
            uBtn.onClick.AddListener(() =>
            {
                mgr.UploadPhotoFromFile(emotion);
            });

            GameObject uTxtObj = new GameObject("Text");
            uTxtObj.transform.SetParent(uploadBtnObj.transform, false);
            var utRect = uTxtObj.AddComponent<RectTransform>();
            utRect.anchorMin = Vector2.zero;
            utRect.anchorMax = Vector2.one;
            utRect.sizeDelta = Vector2.zero;

            var utTxt = uTxtObj.AddComponent<Text>();
            utTxt.text = "📁 Subir";
            utTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            utTxt.fontSize = 13;
            utTxt.fontStyle = FontStyle.Bold;
            utTxt.alignment = TextAnchor.MiddleCenter;
            utTxt.color = Color.white;
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
            bm.startInSelectMenu = true;
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
            bgImg.color = new Color(0.04f, 0.06f, 0.12f, 0.99f);

            var selectUI = menuObj.AddComponent<CharacterSelectUI>();
            ui.characterSelectPanel = menuObj;

            // Encabezado Arcade Street Fighter II
            GameObject headerObj = new GameObject("HeaderPanel");
            headerObj.transform.SetParent(menuObj.transform, false);
            var hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0, 0.89f);
            hRect.anchorMax = new Vector2(1, 0.99f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;

            GameObject titleObj = new GameObject("ArcadeTitle");
            titleObj.transform.SetParent(headerObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.40f);
            tRect.anchorMax = new Vector2(1, 1f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "💥 FIGHT FACE 💥";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.90f, 0.20f);
            var tOutline = titleObj.AddComponent<Outline>();
            tOutline.effectColor = new Color(0.85f, 0.15f, 0.05f, 0.95f);
            tOutline.effectDistance = new Vector2(3, -3);

            GameObject subObj = new GameObject("SubTitle");
            subObj.transform.SetParent(headerObj.transform, false);
            var sRect = subObj.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0, 0f);
            sRect.anchorMax = new Vector2(1, 0.45f);
            sRect.offsetMin = Vector2.zero;
            sRect.offsetMax = Vector2.zero;

            var subTxt = subObj.AddComponent<Text>();
            subTxt.text = "✨ SELECCIONA TU PELEADOR • PERSONALIZA CARAS, VOCES Y NOMBRES ✨";
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 14;
            subTxt.fontStyle = FontStyle.Bold;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(1f, 0.80f, 0.25f);

            // ----------------- CUADRÍCULA ROSTER CENTRAL (5 LUCHADORES) -----------------
            GameObject rosterGridObj = new GameObject("RosterGrid");
            rosterGridObj.transform.SetParent(menuObj.transform, false);
            var rgRect = rosterGridObj.AddComponent<RectTransform>();
            rgRect.anchorMin = new Vector2(0.12f, 0.64f);
            rgRect.anchorMax = new Vector2(0.88f, 0.88f);
            rgRect.offsetMin = Vector2.zero;
            rgRect.offsetMax = Vector2.zero;

            var rosterBg = rosterGridObj.AddComponent<Image>();
            rosterBg.color = new Color(0.08f, 0.10f, 0.18f, 0.85f);
            var rgOutline = rosterGridObj.AddComponent<Outline>();
            rgOutline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.8f);
            rgOutline.effectDistance = new Vector2(2, 2);

            FighterBodyType[] roster = CharacterSelectUI.Roster;
            int count = roster.Length;
            selectUI.rosterSlotButtons = new Button[count];
            selectUI.rosterThumbImages = new Image[count];
            selectUI.rosterP1Badges = new GameObject[count];
            selectUI.rosterP2Badges = new GameObject[count];
            selectUI.rosterOutlines = new Outline[count];

            float slotWidth = 1f / count;
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                FighterBodyType bType = roster[i];

                GameObject slotObj = new GameObject($"Slot_{bType}");
                slotObj.transform.SetParent(rosterGridObj.transform, false);
                var slRect = slotObj.AddComponent<RectTransform>();
                slRect.anchorMin = new Vector2(i * slotWidth + 0.015f, 0.06f);
                slRect.anchorMax = new Vector2((i + 1) * slotWidth - 0.015f, 0.94f);
                slRect.offsetMin = Vector2.zero;
                slRect.offsetMax = Vector2.zero;

                var slImg = slotObj.AddComponent<Image>();
                slImg.color = new Color(0.12f, 0.15f, 0.24f);

                var slOutline = slotObj.AddComponent<Outline>();
                slOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.7f);
                slOutline.effectDistance = new Vector2(2, 2);
                selectUI.rosterOutlines[i] = slOutline;

                var slBtn = slotObj.AddComponent<Button>();
                selectUI.rosterSlotButtons[i] = slBtn;

                // Miniatura de retrato
                GameObject thumbObj = new GameObject("Thumbnail");
                thumbObj.transform.SetParent(slotObj.transform, false);
                var thRect = thumbObj.AddComponent<RectTransform>();
                thRect.anchorMin = new Vector2(0.08f, 0.28f);
                thRect.anchorMax = new Vector2(0.92f, 0.94f);
                thRect.offsetMin = Vector2.zero;
                thRect.offsetMax = Vector2.zero;

                var thImg = thumbObj.AddComponent<Image>();
                thImg.preserveAspect = true;
                thImg.sprite = FaceLoader.LoadFighterThumbSprite(bType);
                selectUI.rosterThumbImages[i] = thImg;

                // Nombre del personaje en el slot
                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(slotObj.transform, false);
                var lbRect = lblObj.AddComponent<RectTransform>();
                lbRect.anchorMin = new Vector2(0, 0);
                lbRect.anchorMax = new Vector2(1, 0.26f);
                lbRect.offsetMin = Vector2.zero;
                lbRect.offsetMax = Vector2.zero;

                var lbTxt = lblObj.AddComponent<Text>();
                lbTxt.text = FaceLoader.GetFighterDisplayName(bType).ToUpper().Replace("EL ", "").Replace("LA ", "");
                lbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lbTxt.fontSize = 13;
                lbTxt.fontStyle = FontStyle.Bold;
                lbTxt.alignment = TextAnchor.MiddleCenter;
                lbTxt.color = new Color(1f, 0.9f, 0.25f);

                // Insignia 1P (Esquina superior izquierda)
                GameObject p1Badge = new GameObject("P1Badge");
                p1Badge.transform.SetParent(slotObj.transform, false);
                var p1bRect = p1Badge.AddComponent<RectTransform>();
                p1bRect.anchorMin = new Vector2(0, 0.65f);
                p1bRect.anchorMax = new Vector2(0.38f, 1f);
                p1bRect.offsetMin = Vector2.zero;
                p1bRect.offsetMax = Vector2.zero;

                var p1bImg = p1Badge.AddComponent<Image>();
                p1bImg.color = new Color(0.95f, 0.15f, 0.1f, 0.95f);
                var p1bTxtObj = new GameObject("Text");
                p1bTxtObj.transform.SetParent(p1Badge.transform, false);
                var p1bTRect = p1bTxtObj.AddComponent<RectTransform>();
                p1bTRect.anchorMin = Vector2.zero;
                p1bTRect.anchorMax = Vector2.one;
                p1bTRect.sizeDelta = Vector2.zero;
                var p1bTxt = p1bTxtObj.AddComponent<Text>();
                p1bTxt.text = "1P";
                p1bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                p1bTxt.fontSize = 12;
                p1bTxt.fontStyle = FontStyle.Bold;
                p1bTxt.alignment = TextAnchor.MiddleCenter;
                p1bTxt.color = Color.white;
                selectUI.rosterP1Badges[i] = p1Badge;

                // Insignia 2P / CPU (Esquina superior derecha)
                GameObject p2Badge = new GameObject("P2Badge");
                p2Badge.transform.SetParent(slotObj.transform, false);
                var p2bRect = p2Badge.AddComponent<RectTransform>();
                p2bRect.anchorMin = new Vector2(0.62f, 0.65f);
                p2bRect.anchorMax = new Vector2(1f, 1f);
                p2bRect.offsetMin = Vector2.zero;
                p2bRect.offsetMax = Vector2.zero;

                var p2bImg = p2Badge.AddComponent<Image>();
                p2bImg.color = new Color(0.15f, 0.55f, 1f, 0.95f);
                var p2bTxtObj = new GameObject("Text");
                p2bTxtObj.transform.SetParent(p2Badge.transform, false);
                var p2bTRect = p2bTxtObj.AddComponent<RectTransform>();
                p2bTRect.anchorMin = Vector2.zero;
                p2bTRect.anchorMax = Vector2.one;
                p2bTRect.sizeDelta = Vector2.zero;
                var p2bTxt = p2bTxtObj.AddComponent<Text>();
                p2bTxt.text = "2P";
                p2bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                p2bTxt.fontSize = 11;
                p2bTxt.fontStyle = FontStyle.Bold;
                p2bTxt.alignment = TextAnchor.MiddleCenter;
                p2bTxt.color = Color.white;
                selectUI.rosterP2Badges[i] = p2Badge;
            }

            // ----------------- PANEL IZQUIERDO: JUGADOR 1 -----------------
            BuildPlayerProfileCard(menuObj.transform, selectUI, 1, new Vector2(0.02f, 0.16f), new Vector2(0.485f, 0.63f));

            // ----------------- PANEL DERECHO: JUGADOR 2 / CPU -----------------
            BuildPlayerProfileCard(menuObj.transform, selectUI, 2, new Vector2(0.515f, 0.16f), new Vector2(0.98f, 0.63f));

            // ----------------- SELECTOR DE ESCENARIOS (CASA DE CADA LUCHADOR) -----------------
            GameObject stagePanelObj = new GameObject("Panel_StageSelector");
            stagePanelObj.transform.SetParent(menuObj.transform, false);
            var stgRect = stagePanelObj.AddComponent<RectTransform>();
            stgRect.anchorMin = new Vector2(0.18f, 0.086f);
            stgRect.anchorMax = new Vector2(0.82f, 0.150f);
            stgRect.offsetMin = Vector2.zero;
            stgRect.offsetMax = Vector2.zero;

            var stgBg = stagePanelObj.AddComponent<Image>();
            stgBg.color = new Color(0.07f, 0.10f, 0.18f, 0.95f);
            var stgOutline = stagePanelObj.AddComponent<Outline>();
            stgOutline.effectColor = new Color(0.35f, 0.55f, 0.85f, 0.8f);
            stgOutline.effectDistance = new Vector2(2, 2);

            // Botón Escenario Anterior
            GameObject btnPrevObj = new GameObject("Btn_StagePrev");
            btnPrevObj.transform.SetParent(stagePanelObj.transform, false);
            var bpRect = btnPrevObj.AddComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0.01f, 0.08f);
            bpRect.anchorMax = new Vector2(0.15f, 0.92f);
            bpRect.offsetMin = Vector2.zero;
            bpRect.offsetMax = Vector2.zero;
            var bpImg = btnPrevObj.AddComponent<Image>();
            bpImg.color = new Color(0.20f, 0.28f, 0.45f);
            var bpOutline = btnPrevObj.AddComponent<Outline>();
            bpOutline.effectColor = new Color(0.4f, 0.6f, 1f);
            bpOutline.effectDistance = new Vector2(1, 1);
            selectUI.stagePrevButton = btnPrevObj.AddComponent<Button>();

            GameObject bpTxtObj = new GameObject("Text");
            bpTxtObj.transform.SetParent(btnPrevObj.transform, false);
            var bpTRect = bpTxtObj.AddComponent<RectTransform>();
            bpTRect.anchorMin = Vector2.zero;
            bpTRect.anchorMax = Vector2.one;
            bpTRect.sizeDelta = Vector2.zero;
            var bpTxt = bpTxtObj.AddComponent<Text>();
            bpTxt.text = "◀ ANTERIOR";
            bpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bpTxt.fontSize = 11;
            bpTxt.fontStyle = FontStyle.Bold;
            bpTxt.alignment = TextAnchor.MiddleCenter;
            bpTxt.color = Color.white;

            // Miniatura del escenario
            GameObject prevImgObj = new GameObject("StagePreviewImage");
            prevImgObj.transform.SetParent(stagePanelObj.transform, false);
            var pviRect = prevImgObj.AddComponent<RectTransform>();
            pviRect.anchorMin = new Vector2(0.16f, 0.08f);
            pviRect.anchorMax = new Vector2(0.28f, 0.92f);
            pviRect.offsetMin = Vector2.zero;
            pviRect.offsetMax = Vector2.zero;
            selectUI.stagePreviewImage = prevImgObj.AddComponent<Image>();
            selectUI.stagePreviewImage.preserveAspect = true;
            var pviOutline = prevImgObj.AddComponent<Outline>();
            pviOutline.effectColor = new Color(0.9f, 0.8f, 0.2f, 0.8f);
            pviOutline.effectDistance = new Vector2(1, 1);

            // Textos del escenario (Nombre + Descripción)
            GameObject stgNameObj = new GameObject("StageName");
            stgNameObj.transform.SetParent(stagePanelObj.transform, false);
            var snRect = stgNameObj.AddComponent<RectTransform>();
            snRect.anchorMin = new Vector2(0.295f, 0.48f);
            snRect.anchorMax = new Vector2(0.84f, 0.96f);
            snRect.offsetMin = Vector2.zero;
            snRect.offsetMax = Vector2.zero;
            selectUI.stageNameText = stgNameObj.AddComponent<Text>();
            selectUI.stageNameText.text = "ESCENARIO: AUTO (CASA DEL OPONENTE)";
            selectUI.stageNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            selectUI.stageNameText.fontSize = 14;
            selectUI.stageNameText.fontStyle = FontStyle.Bold;
            selectUI.stageNameText.alignment = TextAnchor.MiddleLeft;
            selectUI.stageNameText.color = new Color(1f, 0.90f, 0.2f);

            GameObject stgDescObj = new GameObject("StageDesc");
            stgDescObj.transform.SetParent(stagePanelObj.transform, false);
            var sdRect = stgDescObj.AddComponent<RectTransform>();
            sdRect.anchorMin = new Vector2(0.295f, 0.04f);
            sdRect.anchorMax = new Vector2(0.84f, 0.48f);
            sdRect.offsetMin = Vector2.zero;
            sdRect.offsetMax = Vector2.zero;
            selectUI.stageDescText = stgDescObj.AddComponent<Text>();
            selectUI.stageDescText.text = "Selecciona dinámicamente el ring local del rival.";
            selectUI.stageDescText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            selectUI.stageDescText.fontSize = 11;
            selectUI.stageDescText.fontStyle = FontStyle.Normal;
            selectUI.stageDescText.alignment = TextAnchor.MiddleLeft;
            selectUI.stageDescText.color = new Color(0.75f, 0.85f, 1f);

            // Botón Escenario Siguiente
            GameObject btnNextObj = new GameObject("Btn_StageNext");
            btnNextObj.transform.SetParent(stagePanelObj.transform, false);
            var bnRect = btnNextObj.AddComponent<RectTransform>();
            bnRect.anchorMin = new Vector2(0.85f, 0.08f);
            bnRect.anchorMax = new Vector2(0.99f, 0.92f);
            bnRect.offsetMin = Vector2.zero;
            bnRect.offsetMax = Vector2.zero;
            var bnImg = btnNextObj.AddComponent<Image>();
            bnImg.color = new Color(0.20f, 0.28f, 0.45f);
            var bnOutline = btnNextObj.AddComponent<Outline>();
            bnOutline.effectColor = new Color(0.4f, 0.6f, 1f);
            bnOutline.effectDistance = new Vector2(1, 1);
            selectUI.stageNextButton = btnNextObj.AddComponent<Button>();

            GameObject bnTxtObj = new GameObject("Text");
            bnTxtObj.transform.SetParent(btnNextObj.transform, false);
            var bnTRect = bnTxtObj.AddComponent<RectTransform>();
            bnTRect.anchorMin = Vector2.zero;
            bnTRect.anchorMax = Vector2.one;
            bnTRect.sizeDelta = Vector2.zero;
            var bnTxt = bnTxtObj.AddComponent<Text>();
            bnTxt.text = "SIGUIENTE ▶";
            bnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bnTxt.fontSize = 11;
            bnTxt.fontStyle = FontStyle.Bold;
            bnTxt.alignment = TextAnchor.MiddleCenter;
            bnTxt.color = Color.white;

            // ----------------- BARRA INFERIOR DE ACCIÓN -----------------
            // Botón 1: ¡A PELEAR! (Arcade Fight)
            GameObject fightBtnObj = new GameObject("Btn_StartFight");
            fightBtnObj.transform.SetParent(menuObj.transform, false);
            var fbRect = fightBtnObj.AddComponent<RectTransform>();
            fbRect.anchorMin = new Vector2(0.16f, 0.015f);
            fbRect.anchorMax = new Vector2(0.36f, 0.078f);
            fbRect.offsetMin = Vector2.zero;
            fbRect.offsetMax = Vector2.zero;

            var fbImg = fightBtnObj.AddComponent<Image>();
            fbImg.color = new Color(1f, 0.35f, 0.05f);
            var fbOutline = fightBtnObj.AddComponent<Outline>();
            fbOutline.effectColor = new Color(1f, 0.9f, 0.2f);
            fbOutline.effectDistance = new Vector2(3, 3);

            selectUI.fightButton = fightBtnObj.AddComponent<Button>();
            selectUI.fightButton.onClick.AddListener(selectUI.OnStartFight);

            GameObject fbTxtObj = new GameObject("Text");
            fbTxtObj.transform.SetParent(fightBtnObj.transform, false);
            var fbTRect = fbTxtObj.AddComponent<RectTransform>();
            fbTRect.anchorMin = Vector2.zero;
            fbTRect.anchorMax = Vector2.one;
            fbTRect.sizeDelta = Vector2.zero;
            var fbTxt = fbTxtObj.AddComponent<Text>();
            fbTxt.text = "🔥 ¡A PELEAR! [ENTER]";
            fbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fbTxt.fontSize = 16;
            fbTxt.fontStyle = FontStyle.Bold;
            fbTxt.alignment = TextAnchor.MiddleCenter;
            fbTxt.color = Color.white;
            fbTxt.raycastTarget = false;

            // Botón 2: MODO TORNEO (Arcade Ladder)
            GameObject tourneyBtnObj = new GameObject("Btn_TournamentMode");
            tourneyBtnObj.transform.SetParent(menuObj.transform, false);
            var tbTourneyRect = tourneyBtnObj.AddComponent<RectTransform>();
            tbTourneyRect.anchorMin = new Vector2(0.37f, 0.015f);
            tbTourneyRect.anchorMax = new Vector2(0.56f, 0.078f);
            tbTourneyRect.offsetMin = Vector2.zero;
            tbTourneyRect.offsetMax = Vector2.zero;

            var tbTourneyImg = tourneyBtnObj.AddComponent<Image>();
            tbTourneyImg.color = new Color(1f, 0.65f, 0.10f);
            var tbTourneyOutline = tourneyBtnObj.AddComponent<Outline>();
            tbTourneyOutline.effectColor = new Color(1f, 0.95f, 0.3f);
            tbTourneyOutline.effectDistance = new Vector2(3, 3);

            selectUI.tournamentButton = tourneyBtnObj.AddComponent<Button>();
            selectUI.tournamentButton.onClick.AddListener(selectUI.OnStartTournament);

            GameObject tbTourneyTxtObj = new GameObject("Text");
            tbTourneyTxtObj.transform.SetParent(tourneyBtnObj.transform, false);
            var tbTourneyTRect = tbTourneyTxtObj.AddComponent<RectTransform>();
            tbTourneyTRect.anchorMin = Vector2.zero;
            tbTourneyTRect.anchorMax = Vector2.one;
            tbTourneyTRect.sizeDelta = Vector2.zero;
            var tbTourneyTxt = tbTourneyTxtObj.AddComponent<Text>();
            tbTourneyTxt.text = "🏆 MODO TORNEO [F3]";
            tbTourneyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tbTourneyTxt.fontSize = 15;
            tbTourneyTxt.fontStyle = FontStyle.Bold;
            tbTourneyTxt.alignment = TextAnchor.MiddleCenter;
            tbTourneyTxt.color = Color.black;
            tbTourneyTxt.raycastTarget = false;

            // Botón 3: MODO ENTRENAMIENTO (Práctica)
            GameObject trainBtnObj = new GameObject("Btn_TrainingMode");
            trainBtnObj.transform.SetParent(menuObj.transform, false);
            var tbRect = trainBtnObj.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.57f, 0.015f);
            tbRect.anchorMax = new Vector2(0.76f, 0.078f);
            tbRect.offsetMin = Vector2.zero;
            tbRect.offsetMax = Vector2.zero;

            var tbImg = trainBtnObj.AddComponent<Image>();
            tbImg.color = new Color(0.10f, 0.70f, 0.40f);
            var tbOutline = trainBtnObj.AddComponent<Outline>();
            tbOutline.effectColor = new Color(0.4f, 1f, 0.6f);
            tbOutline.effectDistance = new Vector2(3, 3);

            selectUI.trainingButton = trainBtnObj.AddComponent<Button>();
            selectUI.trainingButton.onClick.AddListener(selectUI.OnStartTrainingMode);

            GameObject tbTxtObj = new GameObject("Text");
            tbTxtObj.transform.SetParent(trainBtnObj.transform, false);
            var tbTRect = tbTxtObj.AddComponent<RectTransform>();
            tbTRect.anchorMin = Vector2.zero;
            tbTRect.anchorMax = Vector2.one;
            tbTRect.sizeDelta = Vector2.zero;
            var tbTxt = tbTxtObj.AddComponent<Text>();
            tbTxt.text = "🥋 ENTRENAR [F2]";
            tbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tbTxt.fontSize = 15;
            tbTxt.fontStyle = FontStyle.Bold;
            tbTxt.alignment = TextAnchor.MiddleCenter;
            tbTxt.color = Color.white;
            tbTxt.raycastTarget = false;

            // Botón 4: 📶 MULTIJUGADOR EN RED LOCAL WI-FI
            GameObject wifiBtnObj = new GameObject("Btn_WifiLobby");
            wifiBtnObj.transform.SetParent(menuObj.transform, false);
            var wbRect = wifiBtnObj.AddComponent<RectTransform>();
            wbRect.anchorMin = new Vector2(0.77f, 0.015f);
            wbRect.anchorMax = new Vector2(0.98f, 0.078f);
            wbRect.offsetMin = Vector2.zero;
            wbRect.offsetMax = Vector2.zero;

            var wbImg = wifiBtnObj.AddComponent<Image>();
            wbImg.color = new Color(0.12f, 0.65f, 0.90f);
            var wbOutline = wifiBtnObj.AddComponent<Outline>();
            wbOutline.effectColor = new Color(0.5f, 0.9f, 1f);
            wbOutline.effectDistance = new Vector2(3, 3);

            var wbBtn = wifiBtnObj.AddComponent<Button>();
            wbBtn.onClick.AddListener(() =>
            {
                if (NetworkLobbyUI.Instance != null)
                {
                    NetworkLobbyUI.Instance.OpenLobby();
                }
            });

            GameObject wbTxtObj = new GameObject("Text");
            wbTxtObj.transform.SetParent(wifiBtnObj.transform, false);
            var wbTRect = wbTxtObj.AddComponent<RectTransform>();
            wbTRect.anchorMin = Vector2.zero;
            wbTRect.anchorMax = Vector2.one;
            wbTRect.sizeDelta = Vector2.zero;
            var wbTxt = wbTxtObj.AddComponent<Text>();
            wbTxt.text = "📶 MULTIJUGADOR WI-FI [N]";
            wbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            wbTxt.fontSize = 15;
            wbTxt.fontStyle = FontStyle.Bold;
            wbTxt.alignment = TextAnchor.MiddleCenter;
            wbTxt.color = Color.white;
            wbTxt.raycastTarget = false;

            // Pistas de controles (Inferior Izquierda)
            GameObject hintObj = new GameObject("ControlsHint");
            hintObj.transform.SetParent(menuObj.transform, false);
            var hiRect = hintObj.AddComponent<RectTransform>();
            hiRect.anchorMin = new Vector2(0.010f, 0.015f);
            hiRect.anchorMax = new Vector2(0.15f, 0.078f);
            hiRect.offsetMin = Vector2.zero;
            hiRect.offsetMax = Vector2.zero;

            var hiTxt = hintObj.AddComponent<Text>();
            hiTxt.text = "🎮 1P: Teclado o Joystick [D-Pad] • 📱 [T] Touch Móvil\n[M] Música • [V] Volumen Voces • [C] Caras";
            hiTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hiTxt.fontSize = 11;
            hiTxt.alignment = TextAnchor.MiddleLeft;
            hiTxt.color = new Color(0.7f, 0.8f, 0.95f);

            // Botón 4: Control de Música Opcional (ON / OFF)
            GameObject musicBtnObj = new GameObject("Btn_MusicToggle");
            musicBtnObj.transform.SetParent(menuObj.transform, false);
            var mbRect = musicBtnObj.AddComponent<RectTransform>();
            mbRect.anchorMin = new Vector2(0.885f, 0.048f);
            mbRect.anchorMax = new Vector2(0.992f, 0.082f);
            mbRect.offsetMin = Vector2.zero;
            mbRect.offsetMax = Vector2.zero;

            var mbImg = musicBtnObj.AddComponent<Image>();
            mbImg.color = new Color(0.15f, 0.58f, 0.90f);
            var mbOutline = musicBtnObj.AddComponent<Outline>();
            mbOutline.effectColor = new Color(0.4f, 0.85f, 1f);
            mbOutline.effectDistance = new Vector2(2, 2);

            var mbBtn = musicBtnObj.AddComponent<Button>();
            mbBtn.onClick.AddListener(selectUI.OnToggleMusicClicked);

            GameObject mbTxtObj = new GameObject("Text");
            mbTxtObj.transform.SetParent(musicBtnObj.transform, false);
            var mbTRect = mbTxtObj.AddComponent<RectTransform>();
            mbTRect.anchorMin = Vector2.zero;
            mbTRect.anchorMax = Vector2.one;
            mbTRect.sizeDelta = Vector2.zero;
            var mbTxt = mbTxtObj.AddComponent<Text>();
            mbTxt.text = "🎵 MÚSICA [M]: SÍ";
            mbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mbTxt.fontSize = 11;
            mbTxt.fontStyle = FontStyle.Bold;
            mbTxt.alignment = TextAnchor.MiddleCenter;
            mbTxt.color = Color.white;

            selectUI.musicToggleButton = mbBtn;
            selectUI.musicToggleText = mbTxt;
            selectUI.musicToggleImage = mbImg;

            // Botón 5: Control de Volumen de Voces de Batalla (100%, 150%, 200%, 250%, etc.)
            GameObject voiceBtnObj = new GameObject("Btn_VoiceVolume");
            voiceBtnObj.transform.SetParent(menuObj.transform, false);
            var vbRect = voiceBtnObj.AddComponent<RectTransform>();
            vbRect.anchorMin = new Vector2(0.885f, 0.012f);
            vbRect.anchorMax = new Vector2(0.992f, 0.046f);
            vbRect.offsetMin = Vector2.zero;
            vbRect.offsetMax = Vector2.zero;

            var vbImg = voiceBtnObj.AddComponent<Image>();
            vbImg.color = new Color(0.20f, 0.65f, 0.35f);
            var vbOutline = voiceBtnObj.AddComponent<Outline>();
            vbOutline.effectColor = new Color(0.5f, 1f, 0.6f);
            vbOutline.effectDistance = new Vector2(2, 2);

            var vbBtn = voiceBtnObj.AddComponent<Button>();
            vbBtn.onClick.AddListener(selectUI.OnCycleVoiceVolumeClicked);

            GameObject vbTxtObj = new GameObject("Text");
            vbTxtObj.transform.SetParent(voiceBtnObj.transform, false);
            var vbTRect = vbTxtObj.AddComponent<RectTransform>();
            vbTRect.anchorMin = Vector2.zero;
            vbTRect.anchorMax = Vector2.one;
            vbTRect.sizeDelta = Vector2.zero;
            var vbTxt = vbTxtObj.AddComponent<Text>();
            vbTxt.text = "🗣️ VOCES [V]: 150%";
            vbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vbTxt.fontSize = 11;
            vbTxt.fontStyle = FontStyle.Bold;
            vbTxt.alignment = TextAnchor.MiddleCenter;
            vbTxt.color = Color.white;

            selectUI.voiceVolumeButton = vbBtn;
            selectUI.voiceVolumeText = vbTxt;
            selectUI.voiceVolumeImage = vbImg;

            // Modal de confirmación y personalización previa al combate
            BuildPreFightPromptModal(menuObj.transform, selectUI);
        }

        private void BuildPlayerProfileCard(Transform parent, CharacterSelectUI selectUI, int playerNum, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject cardObj = new GameObject($"Card_Player{playerNum}");
            cardObj.transform.SetParent(parent, false);
            var cRect = cardObj.AddComponent<RectTransform>();
            cRect.anchorMin = anchorMin;
            cRect.anchorMax = anchorMax;
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.10f, 0.16f, 0.96f);
            var cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = playerNum == 1 ? new Color(1f, 0.25f, 0.15f) : new Color(0.15f, 0.60f, 1f);
            cardOutline.effectDistance = new Vector2(3, 3);

            // Encabezado del perfil
            GameObject pBanner = new GameObject("Header");
            pBanner.transform.SetParent(cardObj.transform, false);
            var pbRect = pBanner.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0, 0.88f);
            pbRect.anchorMax = new Vector2(1, 1f);
            pbRect.offsetMin = Vector2.zero;
            pbRect.offsetMax = Vector2.zero;

            var pText = pBanner.AddComponent<Text>();
            pText.text = playerNum == 1 ? "🔴 JUGADOR 1" : "🔵 JUGADOR 2 / OPONENTE";
            pText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pText.fontSize = 18;
            pText.fontStyle = FontStyle.Bold;
            pText.alignment = TextAnchor.MiddleLeft;
            pText.color = playerNum == 1 ? new Color(1f, 0.45f, 0.35f) : new Color(0.35f, 0.8f, 1f);

            // Botón de alternar CPU para Player 2
            if (playerNum == 2)
            {
                GameObject aiBtnObj = new GameObject("Btn_ToggleAI");
                aiBtnObj.transform.SetParent(cardObj.transform, false);
                var aiRect = aiBtnObj.AddComponent<RectTransform>();
                aiRect.anchorMin = new Vector2(0.55f, 0.89f);
                aiRect.anchorMax = new Vector2(0.98f, 0.99f);
                aiRect.offsetMin = Vector2.zero;
                aiRect.offsetMax = Vector2.zero;

                var aiImg = aiBtnObj.AddComponent<Image>();
                aiImg.color = new Color(0.2f, 0.45f, 0.9f);
                selectUI.p2AIToggleButton = aiBtnObj.AddComponent<Button>();

                GameObject aiTxtObj = new GameObject("Text");
                aiTxtObj.transform.SetParent(aiBtnObj.transform, false);
                var aiTRect = aiTxtObj.AddComponent<RectTransform>();
                aiTRect.anchorMin = Vector2.zero;
                aiTRect.anchorMax = Vector2.one;
                aiTRect.sizeDelta = Vector2.zero;
                selectUI.p2AILabelText = aiTxtObj.AddComponent<Text>();
                selectUI.p2AILabelText.text = "🤖 MODO: CPU (IA)";
                selectUI.p2AILabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                selectUI.p2AILabelText.fontSize = 12;
                selectUI.p2AILabelText.fontStyle = FontStyle.Bold;
                selectUI.p2AILabelText.alignment = TextAnchor.MiddleCenter;
                selectUI.p2AILabelText.color = Color.white;
            }

            // Imagen del Cuerpo / Tarjeta de Arte Completa
            GameObject bodyImgObj = new GameObject("FighterCardImage");
            bodyImgObj.transform.SetParent(cardObj.transform, false);
            var bRect = bodyImgObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.04f, 0.22f);
            bRect.anchorMax = new Vector2(0.48f, 0.86f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var bodyImg = bodyImgObj.AddComponent<Image>();
            bodyImg.preserveAspect = true;
            if (playerNum == 1) selectUI.p1FighterCardImage = bodyImg;
            else selectUI.p2FighterCardImage = bodyImg;

            // Superponer cara real del luchador en la cabeza del cuerpo en la tarjeta
            GameObject headOverlayObj = new GameObject("CardHeadOverlay");
            headOverlayObj.transform.SetParent(bodyImgObj.transform, false);
            var hoRect = headOverlayObj.AddComponent<RectTransform>();
            hoRect.anchorMin = new Vector2(0.5f, 0.77f);
            hoRect.anchorMax = new Vector2(0.5f, 0.77f);
            hoRect.pivot = new Vector2(0.5f, 0.5f);
            hoRect.sizeDelta = new Vector2(66, 76);

            var hoImg = headOverlayObj.AddComponent<Image>();
            hoImg.preserveAspect = true;
            hoImg.raycastTarget = false;

            // Panel Derecho: Estadísticas + Galería de 4 Rostros (Normal, Dolor, Rabia, Ganador)
            GameObject rightSide = new GameObject("RightSideInfo");
            rightSide.transform.SetParent(cardObj.transform, false);
            var rsRect = rightSide.AddComponent<RectTransform>();
            rsRect.anchorMin = new Vector2(0.48f, 0.22f);
            rsRect.anchorMax = new Vector2(0.97f, 0.86f);
            rsRect.offsetMin = Vector2.zero;
            rsRect.offsetMax = Vector2.zero;

            // 1. Estadísticas Street Fighter II (Fuerza, Velocidad, Defensa, Técnica)
            GameObject statsObj = new GameObject("StatsBars");
            statsObj.transform.SetParent(rightSide.transform, false);
            var stRect = statsObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0, 0.56f);
            stRect.anchorMax = new Vector2(1, 1.0f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;

            var stTxt = statsObj.AddComponent<Text>();
            stTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stTxt.fontSize = 12;
            stTxt.fontStyle = FontStyle.Bold;
            stTxt.alignment = TextAnchor.MiddleLeft;
            stTxt.color = new Color(0.9f, 0.95f, 1f);
            if (playerNum == 1) selectUI.p1StatsText = stTxt;
            else selectUI.p2StatsText = stTxt;

            // 2. Galería de 4 Rostros Personalizados
            GameObject rackObj = new GameObject("Panel_4Faces");
            rackObj.transform.SetParent(rightSide.transform, false);
            var rkRect = rackObj.AddComponent<RectTransform>();
            rkRect.anchorMin = new Vector2(0, 0);
            rkRect.anchorMax = new Vector2(1, 0.54f);
            rkRect.offsetMin = Vector2.zero;
            rkRect.offsetMax = Vector2.zero;

            // Título de la galería
            GameObject rkTitleObj = new GameObject("Title");
            rkTitleObj.transform.SetParent(rackObj.transform, false);
            var rtRect = rkTitleObj.AddComponent<RectTransform>();
            rtRect.anchorMin = new Vector2(0, 0.78f);
            rtRect.anchorMax = new Vector2(1, 1.0f);
            rtRect.offsetMin = Vector2.zero;
            rtRect.offsetMax = Vector2.zero;
            var rtTxt = rkTitleObj.AddComponent<Text>();
            rtTxt.text = "★ 5 ROSTROS (Clic para cambiar o subir) ★";
            rtTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rtTxt.fontSize = 11;
            rtTxt.fontStyle = FontStyle.Bold;
            rtTxt.alignment = TextAnchor.MiddleCenter;
            rtTxt.color = new Color(1f, 0.88f, 0.2f);

            // Contenedor de 5 ranuras horizontales
            GameObject slotsContainer = new GameObject("SlotsContainer");
            slotsContainer.transform.SetParent(rackObj.transform, false);
            var scRect = slotsContainer.AddComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0, 0);
            scRect.anchorMax = new Vector2(1, 0.76f);
            scRect.offsetMin = Vector2.zero;
            scRect.offsetMax = Vector2.zero;

            Image[] faceThumbs = new Image[5];
            Button[] faceButtons = new Button[5];
            string[] faceTitles = { "1. NORMAL", "2. DOLOR", "3. RABIA", "4. GANADOR", "5. K.O." };
            float slotW = 1f / 5f;

            for (int f = 0; f < 5; f++)
            {
                int faceIdx = f;
                FaceType emotionType = (f == 0) ? FaceType.Base : (f == 1 ? FaceType.Dolor : (f == 2 ? FaceType.Enojo : (f == 3 ? FaceType.Ganador : FaceType.KO)));

                GameObject slotObj = new GameObject($"Slot_Face_{f + 1}");
                slotObj.transform.SetParent(slotsContainer.transform, false);
                var sRect = slotObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(f * slotW + 0.01f, 0);
                sRect.anchorMax = new Vector2((f + 1) * slotW - 0.01f, 1);
                sRect.offsetMin = Vector2.zero;
                sRect.offsetMax = Vector2.zero;

                var slotBg = slotObj.AddComponent<Image>();
                slotBg.color = new Color(0.12f, 0.15f, 0.24f, 0.95f);
                var slotOutline = slotObj.AddComponent<Outline>();
                slotOutline.effectColor = new Color(0.4f, 0.55f, 0.8f, 0.8f);
                slotOutline.effectDistance = new Vector2(1, 1);

                var slotBtn = slotObj.AddComponent<Button>();
                faceButtons[f] = slotBtn;

                // Miniatura de la cara
                GameObject thumbObj = new GameObject("Thumbnail");
                thumbObj.transform.SetParent(slotObj.transform, false);
                var thRect = thumbObj.AddComponent<RectTransform>();
                thRect.anchorMin = new Vector2(0.04f, 0.28f);
                thRect.anchorMax = new Vector2(0.96f, 0.96f);
                thRect.offsetMin = Vector2.zero;
                thRect.offsetMax = Vector2.zero;

                var thImg = thumbObj.AddComponent<Image>();
                thImg.preserveAspect = true;
                faceThumbs[f] = thImg;

                // Etiqueta del tipo de cara
                GameObject lblObj = new GameObject("Label");
                lblObj.transform.SetParent(slotObj.transform, false);
                var lRect = lblObj.AddComponent<RectTransform>();
                lRect.anchorMin = new Vector2(0, 0);
                lRect.anchorMax = new Vector2(1, 0.26f);
                lRect.offsetMin = Vector2.zero;
                lRect.offsetMax = Vector2.zero;

                var lTxt = lblObj.AddComponent<Text>();
                lTxt.text = faceTitles[f];
                lTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lTxt.fontSize = 8;
                lTxt.fontStyle = FontStyle.Bold;
                lTxt.alignment = TextAnchor.MiddleCenter;
                lTxt.color = new Color(1f, 0.92f, 0.4f);
            }

            if (playerNum == 1)
            {
                selectUI.p1FaceThumbs = faceThumbs;
                selectUI.p1FaceSlotButtons = faceButtons;
                selectUI.p1HeadPreviewImage = hoImg;
            }
            else
            {
                selectUI.p2FaceThumbs = faceThumbs;
                selectUI.p2FaceSlotButtons = faceButtons;
                selectUI.p2HeadPreviewImage = hoImg;
            }

            // Nombre Editable con InputField
            GameObject nameRow = new GameObject("FighterNameRow");
            nameRow.transform.SetParent(cardObj.transform, false);
            var nrRect = nameRow.AddComponent<RectTransform>();
            nrRect.anchorMin = new Vector2(0.04f, 0.12f);
            nrRect.anchorMax = new Vector2(0.96f, 0.20f);
            nrRect.offsetMin = Vector2.zero;
            nrRect.offsetMax = Vector2.zero;

            var nrBg = nameRow.AddComponent<Image>();
            nrBg.color = new Color(0.08f, 0.10f, 0.18f, 0.95f);
            var nrOut = nameRow.AddComponent<Outline>();
            nrOut.effectColor = new Color(0.35f, 0.55f, 0.85f, 0.8f);
            nrOut.effectDistance = new Vector2(1, 1);

            GameObject nameLabelObj = new GameObject("Label");
            nameLabelObj.transform.SetParent(nameRow.transform, false);
            var nlRect = nameLabelObj.AddComponent<RectTransform>();
            nlRect.anchorMin = new Vector2(0.02f, 0);
            nlRect.anchorMax = new Vector2(0.30f, 1);
            nlRect.offsetMin = Vector2.zero;
            nlRect.offsetMax = Vector2.zero;
            var nlTxt = nameLabelObj.AddComponent<Text>();
            nlTxt.text = "✏️ NOMBRE:";
            nlTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nlTxt.fontSize = 12;
            nlTxt.fontStyle = FontStyle.Bold;
            nlTxt.alignment = TextAnchor.MiddleLeft;
            nlTxt.color = new Color(1f, 0.88f, 0.2f);

            GameObject inObj = new GameObject("Text");
            inObj.transform.SetParent(nameRow.transform, false);
            var inRect = inObj.AddComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0.32f, 0);
            inRect.anchorMax = new Vector2(0.98f, 1);
            inRect.offsetMin = Vector2.zero;
            inRect.offsetMax = Vector2.zero;
            var nTxt = inObj.AddComponent<Text>();
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 15;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.alignment = TextAnchor.MiddleLeft;
            nTxt.color = Color.white;

            var nInput = nameRow.AddComponent<InputField>();
            nInput.textComponent = nTxt;
            nInput.text = "Luchador";

            if (playerNum == 1)
            {
                selectUI.p1NameText = nTxt;
                selectUI.p1NameInputField = nInput;
            }
            else
            {
                selectUI.p2NameText = nTxt;
                selectUI.p2NameInputField = nInput;
            }

            // Botón 1: Cámara y Voces
            GameObject camBtnObj = new GameObject($"Btn_Webcam_P{playerNum}");
            camBtnObj.transform.SetParent(cardObj.transform, false);
            var cbRect = camBtnObj.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.04f, 0.02f);
            cbRect.anchorMax = new Vector2(0.48f, 0.105f);
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
            cbTxt.text = $"📷 CÁMARA";
            cbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cbTxt.fontSize = 13;
            cbTxt.fontStyle = FontStyle.Bold;
            cbTxt.alignment = TextAnchor.MiddleCenter;
            cbTxt.color = Color.white;

            // Botón 2: Subir Rostro desde PC
            GameObject uploadBtnObj = new GameObject($"Btn_Upload_P{playerNum}");
            uploadBtnObj.transform.SetParent(cardObj.transform, false);
            var ubRect = uploadBtnObj.AddComponent<RectTransform>();
            ubRect.anchorMin = new Vector2(0.52f, 0.02f);
            ubRect.anchorMax = new Vector2(0.96f, 0.105f);
            ubRect.offsetMin = Vector2.zero;
            ubRect.offsetMax = Vector2.zero;

            var ubImg = uploadBtnObj.AddComponent<Image>();
            ubImg.color = new Color(0.12f, 0.65f, 0.40f);
            var ubBtn = uploadBtnObj.AddComponent<Button>();

            if (playerNum == 1) selectUI.p1UploadFileButton = ubBtn;
            else selectUI.p2UploadFileButton = ubBtn;

            GameObject ubTxtObj = new GameObject("Text");
            ubTxtObj.transform.SetParent(uploadBtnObj.transform, false);
            var ubTRect = ubTxtObj.AddComponent<RectTransform>();
            ubTRect.anchorMin = Vector2.zero;
            ubTRect.anchorMax = Vector2.one;
            ubTRect.sizeDelta = Vector2.zero;
            var ubTxt = ubTxtObj.AddComponent<Text>();
            ubTxt.text = $"📁 SUBIR ROSTRO";
            ubTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ubTxt.fontSize = 13;
            ubTxt.fontStyle = FontStyle.Bold;
            ubTxt.alignment = TextAnchor.MiddleCenter;
            ubTxt.color = Color.white;
        }

        private void BuildPreFightPromptModal(Transform parent, CharacterSelectUI selectUI)
        {
            GameObject modal = new GameObject("Modal_PreFightPrompt");
            modal.transform.SetParent(parent, false);
            var mRect = modal.AddComponent<RectTransform>();
            mRect.anchorMin = new Vector2(0.5f, 0.5f);
            mRect.anchorMax = new Vector2(0.5f, 0.5f);
            mRect.pivot = new Vector2(0.5f, 0.5f);
            mRect.sizeDelta = new Vector2(720, 420);
            mRect.anchoredPosition = Vector2.zero;

            var bg = modal.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.16f, 0.98f);
            var outl = modal.AddComponent<Outline>();
            outl.effectColor = new Color(1f, 0.8f, 0.2f);
            outl.effectDistance = new Vector2(3, 3);

            selectUI.preFightModal = modal;

            // Título
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modal.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.80f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;
            var tTxt = titleObj.AddComponent<Text>();
            tTxt.text = "🥊 ¿LISTOS PARA EL COMBATE?";
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 24;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = new Color(1f, 0.9f, 0.2f);

            // Mensaje amigable
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(modal.transform, false);
            var dRect = descObj.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0.08f, 0.58f);
            dRect.anchorMax = new Vector2(0.92f, 0.78f);
            dRect.offsetMin = Vector2.zero; dRect.offsetMax = Vector2.zero;
            var dTxt = descObj.AddComponent<Text>();
            dTxt.text = "¿Deseas cambiar las fotos, gestos o voces de tus luchadores antes de entrar a la arena?";
            dTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dTxt.fontSize = 16;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = new Color(0.9f, 0.95f, 1f);

            // Botones de acción
            // 1. Cambiar fotos P1
            GameObject btnP1 = new GameObject("Btn_ChangeP1");
            btnP1.transform.SetParent(modal.transform, false);
            var p1Rect = btnP1.AddComponent<RectTransform>();
            p1Rect.anchorMin = new Vector2(0.08f, 0.38f);
            p1Rect.anchorMax = new Vector2(0.48f, 0.52f);
            p1Rect.offsetMin = Vector2.zero; p1Rect.offsetMax = Vector2.zero;
            var p1Img = btnP1.AddComponent<Image>();
            p1Img.color = new Color(0.2f, 0.55f, 0.95f);
            var p1Btn = btnP1.AddComponent<Button>();
            p1Btn.onClick.AddListener(() =>
            {
                modal.SetActive(false);
                selectUI.OpenP1Webcam();
            });
            CreateModalButtonText(btnP1.transform, "📷 CAMBIAR FOTOS P1");

            // 2. Cambiar fotos P2
            GameObject btnP2 = new GameObject("Btn_ChangeP2");
            btnP2.transform.SetParent(modal.transform, false);
            var p2Rect = btnP2.AddComponent<RectTransform>();
            p2Rect.anchorMin = new Vector2(0.52f, 0.38f);
            p2Rect.anchorMax = new Vector2(0.92f, 0.52f);
            p2Rect.offsetMin = Vector2.zero; p2Rect.offsetMax = Vector2.zero;
            var p2Img = btnP2.AddComponent<Image>();
            p2Img.color = new Color(0.70f, 0.25f, 0.95f);
            var p2Btn = btnP2.AddComponent<Button>();
            p2Btn.onClick.AddListener(() =>
            {
                modal.SetActive(false);
                selectUI.OpenP2Webcam();
            });
            CreateModalButtonText(btnP2.transform, "📷 CAMBIAR FOTOS P2");

            // 3. Subir Rostro desde PC
            GameObject btnUp = new GameObject("Btn_UploadPC");
            btnUp.transform.SetParent(modal.transform, false);
            var upRect = btnUp.AddComponent<RectTransform>();
            upRect.anchorMin = new Vector2(0.08f, 0.23f);
            upRect.anchorMax = new Vector2(0.48f, 0.35f);
            upRect.offsetMin = Vector2.zero; upRect.offsetMax = Vector2.zero;
            var upImg = btnUp.AddComponent<Image>();
            upImg.color = new Color(0.15f, 0.65f, 0.40f);
            var upBtn = btnUp.AddComponent<Button>();
            upBtn.onClick.AddListener(() =>
            {
                modal.SetActive(false);
                selectUI.OnP1UploadPhoto();
            });
            CreateModalButtonText(btnUp.transform, "📁 SUBIR ROSTRO (PC)");

            // 4. ¡A PELEAR AHORA!
            GameObject btnFight = new GameObject("Btn_FightNow");
            btnFight.transform.SetParent(modal.transform, false);
            var fRect = btnFight.AddComponent<RectTransform>();
            fRect.anchorMin = new Vector2(0.52f, 0.23f);
            fRect.anchorMax = new Vector2(0.92f, 0.35f);
            fRect.offsetMin = Vector2.zero; fRect.offsetMax = Vector2.zero;
            var fImg = btnFight.AddComponent<Image>();
            fImg.color = new Color(1f, 0.4f, 0.1f);
            var fBtn = btnFight.AddComponent<Button>();
            fBtn.onClick.AddListener(() =>
            {
                selectUI.ExecuteStartFight();
            });
            CreateModalButtonText(btnFight.transform, "🔥 ¡A PELEAR!");

            // 5. Botón Cerrar / Cancelar
            GameObject btnCancel = new GameObject("Btn_Cancel");
            btnCancel.transform.SetParent(modal.transform, false);
            var cRect = btnCancel.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.30f, 0.05f);
            cRect.anchorMax = new Vector2(0.70f, 0.16f);
            cRect.offsetMin = Vector2.zero; cRect.offsetMax = Vector2.zero;
            var cImg = btnCancel.AddComponent<Image>();
            cImg.color = new Color(0.25f, 0.28f, 0.35f);
            var cBtn = btnCancel.AddComponent<Button>();
            cBtn.onClick.AddListener(() => modal.SetActive(false));
            CreateModalButtonText(btnCancel.transform, "VOLVER AL MENÚ");

            modal.SetActive(false);
        }

        private void CreateModalButtonText(Transform parent, string label)
        {
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(parent, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private Sprite CreateOvalGuideSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color lineCol = new Color(0.2f, 1f, 0.5f, 0.85f);
            Color fillCenter = new Color(0.2f, 1f, 0.5f, 0.05f);

            float cx = width / 2f;
            float cy = height / 2f;
            float rx = width * 0.485f;
            float ry = height * 0.490f;

            for (int y = 0; y < height; y++)
            {
                float ny = (y - cy) / ry;
                if (Mathf.Abs(ny) > 1f)
                {
                    for (int x = 0; x < width; x++) tex.SetPixel(x, y, clear);
                    continue;
                }

                float taper = 1f + (0.05f * ny);
                float currRx = rx * taper;

                for (int x = 0; x < width; x++)
                {
                    float nx = (x - cx) / currRx;
                    float distSq = (nx * nx) + (ny * ny);

                    if (distSq > 1.0f)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else if (distSq > 0.94f)
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

        private void BuildVirtualTouchControlsUI(Transform canvas)
        {
            GameObject rootObj = new GameObject("VirtualTouchControlsRoot");
            rootObj.transform.SetParent(canvas, false);
            var rootRect = rootObj.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var touchManager = rootObj.AddComponent<VirtualTouchControls>();
            touchManager.touchRootPanel = rootObj;
            var cg = rootObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = rootObj.AddComponent<CanvasGroup>();
            touchManager.touchCanvasGroup = cg;

            Font arcadeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. D-PAD CRUCETA TÁCTIL (Inferior Izquierda)
            GameObject dpadObj = new GameObject("DPad_Cluster");
            dpadObj.transform.SetParent(rootObj.transform, false);
            var dpadRect = dpadObj.AddComponent<RectTransform>();
            dpadRect.anchorMin = Vector2.zero;
            dpadRect.anchorMax = Vector2.zero;
            dpadRect.pivot = new Vector2(0.5f, 0.5f);
            dpadRect.anchoredPosition = new Vector2(195f, 195f);
            dpadRect.sizeDelta = new Vector2(280f, 280f);

            var dpadBg = dpadObj.AddComponent<Image>();
            dpadBg.color = new Color(0.06f, 0.08f, 0.14f, 0.45f);
            dpadBg.raycastTarget = false;
            var dpadOutline = dpadObj.AddComponent<Outline>();
            dpadOutline.effectColor = new Color(0.2f, 0.4f, 0.7f, 0.6f);
            dpadOutline.effectDistance = new Vector2(2, 2);

            CreateTouchDirectionButton(dpadObj.transform, "Btn_Left", new Vector2(-80f, 0f), new Vector2(85f, 85f), "◀\nIZQ", TouchAction.Left, arcadeFont);
            CreateTouchDirectionButton(dpadObj.transform, "Btn_Right", new Vector2(80f, 0f), new Vector2(85f, 85f), "▶\nDER", TouchAction.Right, arcadeFont);
            CreateTouchDirectionButton(dpadObj.transform, "Btn_Jump", new Vector2(0f, 80f), new Vector2(85f, 85f), "▲\nSALTO", TouchAction.Jump, arcadeFont);
            CreateTouchDirectionButton(dpadObj.transform, "Btn_Down", new Vector2(0f, -80f), new Vector2(85f, 85f), "▼\nABAJO", TouchAction.Down, arcadeFont);

            // 2. BOTONERA DE ACCIÓN ARCADE (Inferior Derecha)
            GameObject actObj = new GameObject("Action_Cluster");
            actObj.transform.SetParent(rootObj.transform, false);
            var actRect = actObj.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(1f, 0f);
            actRect.anchorMax = new Vector2(1f, 0f);
            actRect.pivot = new Vector2(1f, 0f);
            actRect.anchoredPosition = Vector2.zero;
            actRect.sizeDelta = new Vector2(520f, 360f);

            // Puño Ligero (LP)
            CreateTouchActionButton(actObj.transform, "Btn_Punch", new Vector2(-225f, 115f), new Vector2(90f, 90f), "👊\nPUÑO", new Color(0.85f, 0.25f, 0.15f, 0.85f), new Color(1f, 0.4f, 0.3f), TouchAction.Punch, arcadeFont);
            // Patada Ligera (LK)
            CreateTouchActionButton(actObj.transform, "Btn_Kick", new Vector2(-120f, 75f), new Vector2(90f, 90f), "🦶\nPATADA", new Color(0.15f, 0.50f, 0.90f, 0.85f), new Color(0.4f, 0.75f, 1f), TouchAction.Kick, arcadeFont);
            // Puño Fuerte (HP)
            CreateTouchActionButton(actObj.transform, "Btn_HeavyPunch", new Vector2(-225f, 225f), new Vector2(90f, 90f), "💥\nP.FUERTE", new Color(0.95f, 0.55f, 0.10f, 0.85f), new Color(1f, 0.8f, 0.3f), TouchAction.HeavyPunch, arcadeFont);
            // Patada Fuerte (HK)
            CreateTouchActionButton(actObj.transform, "Btn_HeavyKick", new Vector2(-120f, 185f), new Vector2(90f, 90f), "⚡\nK.FUERTE", new Color(0.70f, 0.20f, 0.80f, 0.85f), new Color(0.9f, 0.45f, 1f), TouchAction.HeavyKick, arcadeFont);
            // Hadouken Especial
            CreateTouchActionButton(actObj.transform, "Btn_Special", new Vector2(-335f, 180f), new Vector2(95f, 95f), "🔥\nHADOUKEN", new Color(0.10f, 0.85f, 0.95f, 0.90f), new Color(0.5f, 1f, 1f), TouchAction.Special, arcadeFont);
            // Agarre / Proyección
            CreateTouchActionButton(actObj.transform, "Btn_Grab", new Vector2(-335f, 75f), new Vector2(85f, 85f), "🤼\nAGARRE", new Color(0.15f, 0.75f, 0.50f, 0.85f), new Color(0.4f, 1f, 0.7f), TouchAction.Grab, arcadeFont);

            // 3. BOTÓN DE PAUSA / MENÚ TÁCTIL (Superior Derecha)
            GameObject pauseObj = new GameObject("Btn_TouchPause");
            pauseObj.transform.SetParent(rootObj.transform, false);
            var pauseRect = pauseObj.AddComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.pivot = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-25f, -25f);
            pauseRect.sizeDelta = new Vector2(70f, 70f);

            var pauseImg = pauseObj.AddComponent<Image>();
            pauseImg.color = new Color(0.12f, 0.15f, 0.22f, 0.85f);
            pauseImg.raycastTarget = true;
            var pauseOutline = pauseObj.AddComponent<Outline>();
            pauseOutline.effectColor = new Color(1f, 0.85f, 0.2f);
            pauseOutline.effectDistance = new Vector2(2, 2);

            var pauseHandler = pauseObj.AddComponent<TouchButtonHandler>();
            pauseHandler.action = TouchAction.Pause;

            GameObject pTxtObj = new GameObject("Text");
            pTxtObj.transform.SetParent(pauseObj.transform, false);
            var pTRect = pTxtObj.AddComponent<RectTransform>();
            pTRect.anchorMin = Vector2.zero;
            pTRect.anchorMax = Vector2.one;
            pTRect.sizeDelta = Vector2.zero;
            var pTxt = pTxtObj.AddComponent<Text>();
            pTxt.text = "⏸️";
            pTxt.font = arcadeFont;
            pTxt.fontSize = 28;
            pTxt.alignment = TextAnchor.MiddleCenter;
            pTxt.color = Color.white;
            pTxt.raycastTarget = false;

            // 4. BOTÓN FLOTANTE PARA FINALIZAR ENTRENAMIENTO EN MÓVILES (Superior Centro)
            GameObject exitTrainObj = new GameObject("Btn_ExitTrainingTouch");
            exitTrainObj.transform.SetParent(rootObj.transform, false);
            var etRect = exitTrainObj.AddComponent<RectTransform>();
            etRect.anchorMin = new Vector2(0.5f, 1f);
            etRect.anchorMax = new Vector2(0.5f, 1f);
            etRect.pivot = new Vector2(0.5f, 1f);
            etRect.anchoredPosition = new Vector2(0f, -20f);
            etRect.sizeDelta = new Vector2(340f, 54f);

            var etImg = exitTrainObj.AddComponent<Image>();
            etImg.color = new Color(0.95f, 0.15f, 0.15f, 0.95f);
            etImg.raycastTarget = true;
            var etOutline = exitTrainObj.AddComponent<Outline>();
            etOutline.effectColor = new Color(1f, 0.85f, 0.2f);
            etOutline.effectDistance = new Vector2(2, 2);

            var etHandler = exitTrainObj.AddComponent<TouchButtonHandler>();
            etHandler.action = TouchAction.ExitTraining;

            GameObject etTxtObj = new GameObject("Text");
            etTxtObj.transform.SetParent(exitTrainObj.transform, false);
            var etTRect = etTxtObj.AddComponent<RectTransform>();
            etTRect.anchorMin = Vector2.zero;
            etTRect.anchorMax = Vector2.one;
            etTRect.sizeDelta = Vector2.zero;
            var etTxt = etTxtObj.AddComponent<Text>();
            etTxt.text = "🛑 FINALIZAR ENTRENAMIENTO";
            etTxt.font = arcadeFont;
            etTxt.fontSize = 15;
            etTxt.fontStyle = FontStyle.Bold;
            etTxt.alignment = TextAnchor.MiddleCenter;
            etTxt.color = Color.white;
            etTxt.raycastTarget = false;

            touchManager.exitTrainingTouchButton = exitTrainObj;
            exitTrainObj.SetActive(false);
        }

        private static void CreateTouchDirectionButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, TouchAction action, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.32f, 0.75f);
            img.raycastTarget = true;
            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.65f, 1f, 0.8f);
            outline.effectDistance = new Vector2(2, 2);

            var handler = btnObj.AddComponent<TouchButtonHandler>();
            handler.action = action;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        private static void CreateTouchActionButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color, Color outlineColor, TouchAction action, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2, 2);

            var handler = btnObj.AddComponent<TouchButtonHandler>();
            handler.action = action;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 12;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
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
