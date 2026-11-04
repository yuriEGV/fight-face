using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    [Serializable]
    public class TournamentFighterData
    {
        public int id;
        public string name;
        public Color suitColor;
        public Color gloveColor;
        public FaceProfile faceProfile;
    }

    /// <summary>
    /// Gestiona el Modo Campeonato estilo Street Fighter / Arcade Ladder:
    /// - Roster de hasta 8 luchadores con sus fotos y perfiles propios.
    /// - Escala de rondas: Cuartos de final -> Semifinal -> Gran Final.
    /// - Al noquear al rival, avanzas a la siguiente ronda contra el próximo luchador.
    /// - Coronación como ¡CAMPEÓN DEL TORNEO! al derrotar al Jefe Final.
    /// </summary>
    public class TournamentManager : MonoBehaviour
    {
        public static TournamentManager Instance { get; private set; }

        [Header("Configuración del Campeonato")]
        public int currentRound = 1;
        public int totalRounds = 3;
        public bool isTournamentModeActive = true;

        [Header("Luchador del Jugador 1")]
        public int selectedPlayer1Id = 1;

        [Header("Roster del Torneo")]
        public List<TournamentFighterData> roster = new List<TournamentFighterData>();

        [Header("Arcade Continue")]
        public GameObject continuePanelRoot;
        public Text continueNumberText;
        public Text continueInfoText;
        public bool isContinueActive = false;
        private Coroutine continueCoroutine;

        private int[] opponentLadder = { 2, 3, 4 }; // Rivales para Ronda 1, Semifinal y Final

        public int CurrentRound => currentRound;
        public int CurrentOpponentId => (currentRound <= opponentLadder.Length) ? opponentLadder[currentRound - 1] : 2;

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

            InitializeRoster();
        }

        public void InitializeRoster()
        {
            roster.Clear();

            string[] names = {
                "El Gordo",
                "El Flaco",
                "El Musculoso",
                "La Mujer",
                "La Guerrera",
                "Hombre-Máquina"
            };

            Color[] suits = {
                new Color(0.9f, 0.2f, 0.2f),   // 1. Rojo / Azul
                new Color(0.2f, 0.65f, 0.35f), // 2. Verde kung-fu
                new Color(0.95f, 0.8f, 0.1f),  // 3. Dorado / Rojo Muay Thai
                new Color(0.75f, 0.2f, 0.85f), // 4. Morado / Magenta
                new Color(0.85f, 0.15f, 0.22f),// 5. Rojo carmesí / Gi artes marciales
                new Color(0.25f, 0.25f, 0.30f) // 6. Gris combate oscuro Boss
            };

            Color[] gloves = {
                new Color(0.9f, 0.2f, 0.2f),  // Rojo boxeo
                new Color(0.15f, 0.15f, 0.15f),// Negro kung-fu
                new Color(0.95f, 0.25f, 0.25f),// Rojo Muay Thai
                new Color(0.9f, 0.2f, 0.6f),  // Magenta
                new Color(0.15f, 0.15f, 0.18f),// Muñequeras negras
                new Color(0.85f, 0.6f, 0.1f)  // Oro oxidado
            };

            for (int i = 1; i <= 6; i++)
            {
                FighterBodyType bType = (FighterBodyType)(i - 1);
                string fName = FaceLoader.GetFighterDisplayName(bType);
                var fighter = new TournamentFighterData
                {
                    id = i,
                    name = fName,
                    suitColor = suits[i - 1],
                    gloveColor = gloves[i - 1],
                    faceProfile = LoadOrGenerateProfile(i, fName)
                };
                roster.Add(fighter);
            }
        }

        public FaceProfile LoadOrGenerateProfile(int id, string defaultName)
        {
            FaceProfile profile = new FaceProfile();
            profile.fighterId = id;
            profile.fighterName = defaultName;

            string folder = Path.Combine(Application.persistentDataPath, "Luchadores", $"Luchador_{id}");
            if (Directory.Exists(folder) && profile.LoadFromDirectory(folder))
            {
                Debug.Log($"[Tournament] Fotos personalizadas cargadas para {defaultName} (Luchador {id})");
            }
            else
            {
                profile = FaceLoader.CreateDefaultProceduralProfile(id);
                profile.fighterName = defaultName;
            }

            return profile;
        }

        public TournamentFighterData GetFighterData(int id)
        {
            var data = roster.Find(f => f.id == id);
            if (data == null)
            {
                FighterBodyType bType = (FighterBodyType)Mathf.Clamp(id - 1, 0, 8);
                string fName = FaceLoader.GetFighterDisplayName(bType);
                data = new TournamentFighterData
                {
                    id = id,
                    name = fName,
                    suitColor = new Color(0.85f, 0.25f, 0.25f),
                    gloveColor = new Color(0.95f, 0.2f, 0.2f),
                    faceProfile = LoadOrGenerateProfile(id, fName)
                };
                roster.Add(data);
            }
            return data;
        }

        /// <summary>
        /// Inicia un nuevo Campeonato desde la Ronda 1.
        /// </summary>
        public void StartNewTournament(int player1FighterId = 1)
        {
            selectedPlayer1Id = player1FighterId;
            currentRound = 1;
            isTournamentModeActive = true;

            // Elegir escalera de oponentes excluyendo al seleccionado por P1
            List<int> available = new List<int>();
            for (int i = 1; i <= 5; i++)
            {
                if (i != selectedPlayer1Id && i != 5) available.Add(i);
            }

            // Los 3 rivales del torneo (Ronda 1, Semifinal, Gran Final)
            opponentLadder = new int[3];
            opponentLadder[0] = available.Count > 0 ? available[0] : 2;
            opponentLadder[1] = available.Count > 1 ? available[1] : 3;
            // Jefe final: El Dos Cabezas (ID 5), o si P1 lo eligió, el siguiente más fuerte
            opponentLadder[2] = (selectedPlayer1Id == 5) ? (available.Count > 2 ? available[2] : 1) : 5;

            LoadTournamentRound();
        }

        /// <summary>
        /// Carga los datos de los luchadores para la ronda actual en la arena.
        /// </summary>
        public void LoadTournamentRound()
        {
            if (BattleManager.Instance == null) return;

            int p1Id = selectedPlayer1Id;
            int p2Id = CurrentOpponentId;

            var p1Data = GetFighterData(p1Id);
            var p2Data = GetFighterData(p2Id);

            // Configurar P1
            if (BattleManager.Instance.player1 != null && p1Data != null)
            {
                BattleManager.Instance.player1.playerId = 1;
                BattleManager.Instance.player1.fighterName = p1Data.name;
                BattleManager.Instance.player1.faceController.SetProfile(p1Data.faceProfile);
                BattleManager.Instance.player1.bodyController.suitColor = p1Data.suitColor;
                BattleManager.Instance.player1.bodyController.gloveColor = p1Data.gloveColor;
            }

            // Configurar P2 (Oponente del Torneo)
            if (BattleManager.Instance.player2 != null && p2Data != null)
            {
                BattleManager.Instance.player2.playerId = 2;
                BattleManager.Instance.player2.fighterName = p2Data.name;
                BattleManager.Instance.player2.isAI = true; // CPU bot en el torneo
                BattleManager.Instance.player2.faceController.SetProfile(p2Data.faceProfile);
                BattleManager.Instance.player2.bodyController.suitColor = p2Data.suitColor;
                BattleManager.Instance.player2.bodyController.gloveColor = p2Data.gloveColor;
            }

            // En la Gran Final (Ronda 3) se pelea en el MGM Grand Arena. En rondas previas, en el escenario casa del rival.
            if (currentRound >= 3)
            {
                BattleManager.Instance.ApplyStage(StageType.MGMGrand);
            }
            else if (p2Data != null)
            {
                FighterBodyType oppBody = (FighterBodyType)Mathf.Clamp(p2Data.id - 1, 0, 5);
                StageType homeStage = FaceLoader.GetHomeStageForFighter(oppBody);
                BattleManager.Instance.ApplyStage(homeStage);
            }

            string roundTitle = GetRoundTitle(currentRound);
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.p1NameText.text = "P1: " + p1Data.name;
                BattleUI.Instance.p2NameText.text = "Rival: " + p2Data.name + " (CPU)";
                BattleUI.Instance.ShowBanner($"🥊 {roundTitle} 🥊\n{p1Data.name} VS {p2Data.name}", 3f);
            }

            BattleManager.Instance.RestartMatch();
        }

        public string GetRoundTitle(int round)
        {
            switch (round)
            {
                case 1: return "CUARTOS DE FINAL";
                case 2: return "SEMIFINAL DEL CAMPEONATO";
                case 3: return "¡LA GRAN FINAL EN EL MGM GRAND!";
                default: return $"RONDA {round}";
            }
        }

        /// <summary>
        /// Se invoca cuando un luchador gana una pelea del campeonato.
        /// </summary>
        public void OnMatchWonBy(FighterController winner)
        {
            if (!isTournamentModeActive) return;

            if (winner == BattleManager.Instance.player1)
            {
                // El Jugador 1 ganó
                if (currentRound < totalRounds)
                {
                    currentRound++;
                    string nextTitle = GetRoundTitle(currentRound);
                    if (BattleUI.Instance != null)
                    {
                        BattleUI.Instance.ShowBanner($"¡K.O.! ¡VICTORIA!\nAVANZAS A: {nextTitle}", 3.5f);
                    }
                    Invoke(nameof(LoadTournamentRound), 3.5f);
                }
                else
                {
                    // ¡GANÓ EL CAMPEONATO!
                    if (BattleUI.Instance != null)
                    {
                        BattleUI.Instance.ShowBanner($"🏆 ¡CAMPEÓN DEL TORNEO! 🏆\n¡{winner.fighterName.ToUpper()} ES EL REY!", 3f);
                    }

                    StartCoroutine(LaunchTournamentCelebrationRoutine(winner));
                }
            }
            else
            {
                // El Jugador 1 perdió -> Cuenta regresiva estilo Arcade para continuar
                StartContinueCountdown();
            }
        }

        private void Update()
        {
            if (isContinueActive)
            {
                if (FightInput.GetConfirm())
                {
                    OnContinueTournamentClicked();
                }
                else if (FightInput.GetMenuToggle())
                {
                    OnGiveUpTournamentClicked();
                }
            }
        }

        public void StartContinueCountdown()
        {
            if (continueCoroutine != null)
            {
                StopCoroutine(continueCoroutine);
            }
            continueCoroutine = StartCoroutine(ContinueCountdownRoutine());
        }

        private IEnumerator ContinueCountdownRoutine()
        {
            // Pausa breve para apreciar el K.O.
            yield return new WaitForSeconds(1.8f);

            if (continuePanelRoot == null && BattleUI.Instance != null)
            {
                BuildContinueUI(BattleUI.Instance.transform);
            }

            if (continuePanelRoot == null) yield break;

            isContinueActive = true;
            continuePanelRoot.transform.SetAsLastSibling();
            continuePanelRoot.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var p2Data = GetFighterData(CurrentOpponentId);
            string oppName = p2Data != null ? p2Data.name : "Rival";
            string roundTitle = GetRoundTitle(currentRound);
            if (continueInfoText != null)
            {
                continueInfoText.text = $"🥊 {roundTitle} • VS {oppName.ToUpper()}\n¿Deseas continuar este combate?";
            }

            // Cuenta regresiva arcade de 9 a 0
            for (int sec = 9; sec >= 0; sec--)
            {
                if (!isContinueActive) yield break;

                if (continueNumberText != null)
                {
                    continueNumberText.text = sec.ToString();
                    continueNumberText.color = sec <= 3 ? new Color(1f, 0.25f, 0.25f) : new Color(1f, 0.85f, 0.15f);
                }

                float elapsed = 0f;
                while (elapsed < 1.0f)
                {
                    if (!isContinueActive) yield break;
                    elapsed += Time.unscaledDeltaTime;
                    if (continueNumberText != null)
                    {
                        continueNumberText.transform.localScale = Vector3.Lerp(Vector3.one * 1.35f, Vector3.one, elapsed);
                    }
                    yield return null;
                }
            }

            // Tiempo agotado: Game Over
            if (continueNumberText != null)
            {
                continueNumberText.text = "0";
            }
            if (continueInfoText != null)
            {
                continueInfoText.text = "☠️ ¡TIEMPO AGOTADO! GAME OVER ☠️";
            }

            yield return new WaitForSeconds(1.5f);
            OnGiveUpTournamentClicked();
        }

        public void OnContinueTournamentClicked()
        {
            if (!isContinueActive) return;
            isContinueActive = false;

            if (continueCoroutine != null)
            {
                StopCoroutine(continueCoroutine);
                continueCoroutine = null;
            }

            if (continuePanelRoot != null)
            {
                continuePanelRoot.SetActive(false);
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner($"¡CONTINUAR COMBATE!\n{GetRoundTitle(currentRound)}", 2.2f);
            }

            // Reanuda el torneo desde la ronda actual en la que cayó
            LoadTournamentRound();
        }

        public void OnGiveUpTournamentClicked()
        {
            isContinueActive = false;

            if (continueCoroutine != null)
            {
                StopCoroutine(continueCoroutine);
                continueCoroutine = null;
            }

            if (continuePanelRoot != null)
            {
                continuePanelRoot.SetActive(false);
            }

            isTournamentModeActive = false;
            currentRound = 1;

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowCharacterSelectMenu();
            }
        }

        public void BuildContinueUI(Transform canvasTransform)
        {
            if (continuePanelRoot != null) return;

            continuePanelRoot = new GameObject("Panel_ArcadeContinue");
            continuePanelRoot.transform.SetParent(canvasTransform, false);
            var prRect = continuePanelRoot.AddComponent<RectTransform>();
            prRect.anchorMin = Vector2.zero;
            prRect.anchorMax = Vector2.one;
            prRect.offsetMin = Vector2.zero;
            prRect.offsetMax = Vector2.zero;

            var bg = continuePanelRoot.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.08f, 0.88f);
            bg.raycastTarget = true;

            // Cuadro central
            GameObject boxObj = new GameObject("ContinueBox");
            boxObj.transform.SetParent(continuePanelRoot.transform, false);
            var bRect = boxObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.24f, 0.20f);
            bRect.anchorMax = new Vector2(0.76f, 0.80f);
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var boxBg = boxObj.AddComponent<Image>();
            boxBg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
            var boxOut = boxObj.AddComponent<Outline>();
            boxOut.effectColor = new Color(1f, 0.8f, 0.1f, 0.9f);
            boxOut.effectDistance = new Vector2(3, 3);

            // Título Arcade: CONTINUE?
            GameObject titleObj = new GameObject("Title_Continue");
            titleObj.transform.SetParent(boxObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.76f);
            tRect.anchorMax = new Vector2(1f, 0.96f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "¡CONTINUE?";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 38;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.25f, 0.25f);

            // Información de combate / Ronda
            GameObject infoObj = new GameObject("Info_Round");
            infoObj.transform.SetParent(boxObj.transform, false);
            var iRect = infoObj.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.05f, 0.58f);
            iRect.anchorMax = new Vector2(0.95f, 0.74f);
            iRect.offsetMin = Vector2.zero;
            iRect.offsetMax = Vector2.zero;
            continueInfoText = infoObj.AddComponent<Text>();
            continueInfoText.text = "🥊 RONDA DEL CAMPEONATO\n¿Deseas continuar este combate?";
            continueInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            continueInfoText.fontSize = 16;
            continueInfoText.fontStyle = FontStyle.Bold;
            continueInfoText.alignment = TextAnchor.MiddleCenter;
            continueInfoText.color = new Color(0.9f, 0.95f, 1f);

            // Número gigante de cuenta regresiva (9 a 0)
            GameObject numObj = new GameObject("Countdown_Number");
            numObj.transform.SetParent(boxObj.transform, false);
            var nRect = numObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.2f, 0.24f);
            nRect.anchorMax = new Vector2(0.8f, 0.58f);
            nRect.offsetMin = Vector2.zero;
            nRect.offsetMax = Vector2.zero;
            continueNumberText = numObj.AddComponent<Text>();
            continueNumberText.text = "9";
            continueNumberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            continueNumberText.fontSize = 90;
            continueNumberText.fontStyle = FontStyle.Bold;
            continueNumberText.alignment = TextAnchor.MiddleCenter;
            continueNumberText.color = new Color(1f, 0.85f, 0.15f);

            var numShadow = numObj.AddComponent<Outline>();
            numShadow.effectColor = new Color(0.9f, 0.15f, 0.1f, 0.8f);
            numShadow.effectDistance = new Vector2(3, -3);

            // Botones de acción inferiores
            GameObject btnRow = new GameObject("ButtonsRow");
            btnRow.transform.SetParent(boxObj.transform, false);
            var bRowRect = btnRow.AddComponent<RectTransform>();
            bRowRect.anchorMin = new Vector2(0.06f, 0.05f);
            bRowRect.anchorMax = new Vector2(0.94f, 0.22f);
            bRowRect.offsetMin = Vector2.zero;
            bRowRect.offsetMax = Vector2.zero;

            // Botón 1: CONTINUAR
            CreateContinueActionButton(btnRow.transform, "CONTINUAR [ESPACIO / ENTER]", new Vector2(0f, 0f), new Vector2(0.58f, 1f), new Color(0.12f, 0.65f, 0.35f), () =>
            {
                OnContinueTournamentClicked();
            });

            // Botón 2: RENDIRSE
            CreateContinueActionButton(btnRow.transform, "SALIR [ESC]", new Vector2(0.62f, 0f), new Vector2(1f, 1f), new Color(0.75f, 0.25f, 0.25f), () =>
            {
                OnGiveUpTournamentClicked();
            });

            continuePanelRoot.SetActive(false);
        }

        private void CreateContinueActionButton(Transform parent, string label, Vector2 aMin, Vector2 aMax, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label.Replace(" ", ""));
            btnObj.transform.SetParent(parent, false);
            var bRect = btnObj.AddComponent<RectTransform>();
            bRect.anchorMin = aMin;
            bRect.anchorMax = aMax;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;

            var bImg = btnObj.AddComponent<Image>();
            bImg.color = bgColor;
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bImg;
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
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        private System.Collections.IEnumerator LaunchTournamentCelebrationRoutine(FighterController winner)
        {
            yield return new WaitForSeconds(2.0f);
            try
            {
                if (TournamentCelebrationUI.Instance != null)
                {
                    TournamentCelebrationUI.Instance.ShowCelebration(winner);
                }
                else if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowCharacterSelectMenu();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TournamentManager] Error al mostrar pantalla de celebración: {ex}");
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowCharacterSelectMenu();
                }
            }
        }
    }
}
