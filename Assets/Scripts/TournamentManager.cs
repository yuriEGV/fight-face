using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    public enum TournamentSize
    {
        FourPlayers = 4,
        EightPlayers = 8
    }

    public enum TournamentParticipationMode
    {
        PassAndPlay,        // Turnos entre amigos en el mismo celular o PC
        SinglePlayerVsAi,   // 1 jugador humano vs la máquina en escalera arcade
        LocalWifi           // Combates por red local Wi-Fi
    }

    [Serializable]
    public class TournamentFighterData
    {
        public int id;
        public string name;
        public FighterBodyType bodyType;
        public Color suitColor;
        public Color gloveColor;
        public FaceProfile faceProfile;
        public bool isHuman;
        public int humanPlayerNumber = 1; // 1 = P1, 2 = P2
    }

    [Serializable]
    public class TournamentMatch
    {
        public int matchIndex;
        public string roundName;
        public TournamentFighterData fighter1;
        public TournamentFighterData fighter2;
        public TournamentFighterData winner;
        public TournamentFighterData loser;
        public bool isCompleted;
        public int nextMatchIndex = -1;
        public bool nextMatchSlotIsFighter1 = true;
        public int loserMatchIndex = -1;
        public bool loserMatchSlotIsFighter1 = true;

        public bool isHumanVsHuman => fighter1 != null && fighter2 != null && fighter1.isHuman && fighter2.isHuman;
        public bool isHumanVsCpu => (fighter1 != null && fighter1.isHuman && (fighter2 == null || !fighter2.isHuman)) ||
                                    (fighter2 != null && fighter2.isHuman && (fighter1 == null || !fighter1.isHuman));
        public bool isCpuVsCpu => (fighter1 == null || !fighter1.isHuman) && (fighter2 == null || !fighter2.isHuman);
    }

    /// <summary>
    /// Gestiona el Modo Campeonato configurable de 4 u 8 jugadores (Sesión de ~15 o ~30 minutos)
    /// con modalidad híbrida completa: Pase y Juegue (amigos turnándose), Solitario vs CPU, o Red Wi-Fi.
    /// </summary>
    public class TournamentManager : MonoBehaviour
    {
        public static TournamentManager Instance { get; private set; }

        [Header("Configuración del Torneo")]
        public TournamentSize currentTournamentSize = TournamentSize.EightPlayers;
        public TournamentParticipationMode currentMode = TournamentParticipationMode.PassAndPlay;
        public bool isTournamentModeActive = false;

        [Header("Luchador Inicial P1")]
        public int selectedPlayer1Id = 1;

        [Header("Roster General")]
        public List<TournamentFighterData> roster = new List<TournamentFighterData>();

        [Header("Árbol de Combates (Bracket)")]
        public List<TournamentMatch> allMatches = new List<TournamentMatch>();
        public int currentMatchIndex = 0;

        [Header("Podio Final")]
        public TournamentFighterData firstPlaceWinner;
        public TournamentFighterData secondPlaceWinner;
        public TournamentFighterData thirdPlaceWinner;

        [Header("Arcade Continue (Para modo 1P vs CPU)")]
        public GameObject continuePanelRoot;
        public Text continueNumberText;
        public Text continueInfoText;
        public bool isContinueActive = false;
        private Coroutine continueCoroutine;

        public int CurrentRound => currentMatchIndex + 1;
        public int currentRound
        {
            get => currentMatchIndex + 1;
            set => currentMatchIndex = Mathf.Max(0, value - 1);
        }
        public int TotalMatches => allMatches.Count;

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

            // 9 Luchadores oficiales de Fight Face con personalidades y colores únicos
            Color[] suits = {
                new Color(0.9f, 0.2f, 0.2f),   // 1. Gordo (Rojo / Azul)
                new Color(0.2f, 0.65f, 0.35f), // 2. Flaco (Verde kung-fu Bruce Lee)
                new Color(0.95f, 0.8f, 0.1f),  // 3. Musculoso (Dorado / Muay Thai)
                new Color(0.75f, 0.2f, 0.85f), // 4. Mujer (Morado / Magenta Kickboxing)
                new Color(0.85f, 0.15f, 0.22f),// 5. Guerrera (Rojo carmesí Ninjutsu)
                new Color(0.25f, 0.25f, 0.30f),// 6. Hombre-Máquina (Cyborg cómico)
                new Color(0.10f, 0.65f, 0.95f),// 7. Playero (Azul cielo con patito inflable)
                new Color(0.20f, 0.22f, 0.35f),// 8. Oficinista (Traje ejecutivo y corbata)
                new Color(0.60f, 0.40f, 0.85f) // 9. Pijamero (Pijama estrellas y pantuflas conejo)
            };

            Color[] gloves = {
                new Color(0.9f, 0.2f, 0.2f),   // Gordo
                new Color(0.15f, 0.15f, 0.15f),// Flaco
                new Color(0.95f, 0.25f, 0.25f),// Musculoso
                new Color(0.9f, 0.2f, 0.6f),   // Mujer
                new Color(0.15f, 0.15f, 0.18f),// Guerrera
                new Color(0.85f, 0.6f, 0.1f),  // Hombre-Máquina
                new Color(1f, 0.85f, 0.1f),   // Playero (Amarillo patito)
                new Color(0.40f, 0.25f, 0.15f),// Oficinista (Cuero maletín)
                new Color(1f, 0.75f, 0.85f)    // Pijamero (Rosa conejo)
            };

            for (int i = 1; i <= 9; i++)
            {
                FighterBodyType bType = (FighterBodyType)(i - 1);
                string fName = FaceLoader.GetFighterDisplayName(bType);
                var fighter = new TournamentFighterData
                {
                    id = i,
                    name = fName,
                    bodyType = bType,
                    suitColor = suits[i - 1],
                    gloveColor = gloves[i - 1],
                    faceProfile = LoadOrGenerateProfile(i, fName),
                    isHuman = false,
                    humanPlayerNumber = 1
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
            if (data == null && roster.Count > 0)
            {
                return roster[0];
            }
            return data;
        }

        /// <summary>
        /// Inicia el diálogo de configuración del Campeonato (4 u 8 jugadores).
        /// </summary>
        public void StartNewTournament(int player1FighterId = 1)
        {
            selectedPlayer1Id = player1FighterId;
            if (TournamentBracketUI.Instance != null)
            {
                TournamentBracketUI.Instance.OpenSetupModal();
            }
            else
            {
                StartConfiguredTournament(TournamentSize.EightPlayers, TournamentParticipationMode.PassAndPlay, player1FighterId);
            }
        }

        /// <summary>
        /// Genera y arranca el torneo con el tamaño y modalidad seleccionados.
        /// </summary>
        public void StartConfiguredTournament(TournamentSize size, TournamentParticipationMode mode, int p1Id = 1)
        {
            currentTournamentSize = size;
            currentMode = mode;
            selectedPlayer1Id = p1Id;
            isTournamentModeActive = true;
            currentMatchIndex = 0;
            firstPlaceWinner = null;
            secondPlaceWinner = null;
            thirdPlaceWinner = null;

            InitializeRoster();
            GenerateBracketMatches();

            // Abrir la pantalla interactiva de llaves (Bracket)
            if (TournamentBracketUI.Instance != null)
            {
                TournamentBracketUI.Instance.OpenBracketModal();
            }
            else
            {
                LoadCurrentTournamentMatch();
            }
        }

        private void GenerateBracketMatches()
        {
            allMatches.Clear();

            // Seleccionar participantes aleatorios únicos
            List<TournamentFighterData> pool = new List<TournamentFighterData>(roster);
            List<TournamentFighterData> participants = new List<TournamentFighterData>();

            // El luchador elegido por el jugador 1 siempre está presente en el Slot 0
            var p1Fighter = GetFighterData(selectedPlayer1Id);
            if (p1Fighter != null)
            {
                p1Fighter.isHuman = true;
                p1Fighter.humanPlayerNumber = 1;
                participants.Add(p1Fighter);
                pool.Remove(p1Fighter);
            }

            int needed = (int)currentTournamentSize;
            while (participants.Count < needed && pool.Count > 0)
            {
                int rnd = UnityEngine.Random.Range(0, pool.Count);
                var f = pool[rnd];
                pool.RemoveAt(rnd);

                // En modo Pase y Juegue, asignar como humanos o CPUs según convenga
                if (currentMode == TournamentParticipationMode.PassAndPlay)
                {
                    // En pase y juegue, el segundo slot es Humano (J2) para combate inicial entre 2 amigos
                    if (participants.Count == 1)
                    {
                        f.isHuman = true;
                        f.humanPlayerNumber = 2;
                    }
                    else
                    {
                        f.isHuman = true;
                        f.humanPlayerNumber = (participants.Count % 2 == 0) ? 1 : 2;
                    }
                }
                else
                {
                    // En solitario o Wi-Fi, los rivales son controlados por CPU / rival
                    f.isHuman = false;
                }

                participants.Add(f);
            }

            if (currentTournamentSize == TournamentSize.EightPlayers)
            {
                BuildEightPlayerBracket(participants);
            }
            else
            {
                BuildFourPlayerBracket(participants);
            }
        }

        private void BuildEightPlayerBracket(List<TournamentFighterData> p)
        {
            // Total 8 combates:
            // Matches 0, 1, 2, 3: Cuartos de Final
            // Matches 4, 5: Semifinales
            // Match 6: 3er Puesto (Bronce)
            // Match 7: Gran Final (Oro)

            for (int i = 0; i < 4; i++)
            {
                var m = new TournamentMatch
                {
                    matchIndex = i,
                    roundName = $"CUARTOS DE FINAL {i + 1}/4",
                    fighter1 = (i * 2 < p.Count) ? p[i * 2] : null,
                    fighter2 = (i * 2 + 1 < p.Count) ? p[i * 2 + 1] : null,
                    nextMatchIndex = 4 + (i / 2),
                    nextMatchSlotIsFighter1 = (i % 2 == 0)
                };
                allMatches.Add(m);
            }

            // Semifinal 1
            allMatches.Add(new TournamentMatch
            {
                matchIndex = 4,
                roundName = "SEMIFINAL 1/2",
                fighter1 = null,
                fighter2 = null,
                nextMatchIndex = 7, // Gran Final
                nextMatchSlotIsFighter1 = true,
                loserMatchIndex = 6, // 3er Puesto
                loserMatchSlotIsFighter1 = true
            });

            // Semifinal 2
            allMatches.Add(new TournamentMatch
            {
                matchIndex = 5,
                roundName = "SEMIFINAL 2/2",
                fighter1 = null,
                fighter2 = null,
                nextMatchIndex = 7, // Gran Final
                nextMatchSlotIsFighter1 = false,
                loserMatchIndex = 6, // 3er Puesto
                loserMatchSlotIsFighter1 = false
            });

            // 3er Puesto (Bronce)
            allMatches.Add(new TournamentMatch
            {
                matchIndex = 6,
                roundName = "🥉 DUELO POR EL 3ER PUESTO (BRONCE)",
                fighter1 = null,
                fighter2 = null,
                nextMatchIndex = -1
            });

            // Gran Final (Oro y Trofeo)
            allMatches.Add(new TournamentMatch
            {
                matchIndex = 7,
                roundName = "👑 ¡LA GRAN FINAL EN EL MGM GRAND!",
                fighter1 = null,
                fighter2 = null,
                nextMatchIndex = -1
            });
        }

        private void BuildFourPlayerBracket(List<TournamentFighterData> p)
        {
            // Total 4 combates:
            // Match 0: Semifinal 1
            // Match 1: Semifinal 2
            // Match 2: 3er Puesto
            // Match 3: Gran Final

            allMatches.Add(new TournamentMatch
            {
                matchIndex = 0,
                roundName = "SEMIFINAL 1/2",
                fighter1 = p.Count > 0 ? p[0] : null,
                fighter2 = p.Count > 1 ? p[1] : null,
                nextMatchIndex = 3,
                nextMatchSlotIsFighter1 = true,
                loserMatchIndex = 2,
                loserMatchSlotIsFighter1 = true
            });

            allMatches.Add(new TournamentMatch
            {
                matchIndex = 1,
                roundName = "SEMIFINAL 2/2",
                fighter1 = p.Count > 2 ? p[2] : null,
                fighter2 = p.Count > 3 ? p[3] : null,
                nextMatchIndex = 3,
                nextMatchSlotIsFighter1 = false,
                loserMatchIndex = 2,
                loserMatchSlotIsFighter1 = false
            });

            // 3er Puesto
            allMatches.Add(new TournamentMatch
            {
                matchIndex = 2,
                roundName = "🥉 DUELO POR EL 3ER PUESTO (BRONCE)",
                fighter1 = null,
                fighter2 = null,
                nextMatchIndex = -1
            });

            // Gran Final
            allMatches.Add(new TournamentMatch
            {
                matchIndex = 3,
                roundName = "👑 ¡LA GRAN FINAL EN EL MGM GRAND!",
                fighter1 = null,
                fighter2 = null,
                nextMatchIndex = -1
            });
        }

        public TournamentMatch GetCurrentMatch()
        {
            if (currentMatchIndex >= 0 && currentMatchIndex < allMatches.Count)
            {
                return allMatches[currentMatchIndex];
            }
            return null;
        }

        /// <summary>
        /// Carga y ejecuta el combate actual del torneo en el ring.
        /// </summary>
        public void LoadCurrentTournamentMatch()
        {
            if (BattleManager.Instance == null) return;

            var match = GetCurrentMatch();
            if (match == null || match.isCompleted) return;

            var p1Data = match.fighter1;
            var p2Data = match.fighter2;

            if (p1Data == null || p2Data == null)
            {
                Debug.LogWarning("[TournamentManager] Esperando contendientes del combate...");
                return;
            }

            // Configurar P1
            if (BattleManager.Instance.player1 != null)
            {
                BattleManager.Instance.player1.playerId = 1;
                BattleManager.Instance.player1.fighterName = p1Data.name;
                BattleManager.Instance.player1.faceController.SetProfile(p1Data.faceProfile);
                BattleManager.Instance.player1.bodyController.SetClassicBody(p1Data.bodyType);
                BattleManager.Instance.player1.bodyController.suitColor = p1Data.suitColor;
                BattleManager.Instance.player1.bodyController.gloveColor = p1Data.gloveColor;
                BattleManager.Instance.player1.isAI = !p1Data.isHuman;
            }

            // Configurar P2
            if (BattleManager.Instance.player2 != null)
            {
                BattleManager.Instance.player2.playerId = 2;
                BattleManager.Instance.player2.fighterName = p2Data.name;
                BattleManager.Instance.player2.faceController.SetProfile(p2Data.faceProfile);
                BattleManager.Instance.player2.bodyController.SetClassicBody(p2Data.bodyType);
                BattleManager.Instance.player2.bodyController.suitColor = p2Data.suitColor;
                BattleManager.Instance.player2.bodyController.gloveColor = p2Data.gloveColor;
                BattleManager.Instance.player2.isAI = !p2Data.isHuman;
            }

            // Escenario: La Gran Final siempre en el MGM Grand Arena; los demás en su escenario
            bool isGrandFinal = (match.matchIndex == allMatches.Count - 1);
            if (isGrandFinal)
            {
                BattleManager.Instance.ApplyStage(StageType.MGMGrand);
            }
            else
            {
                StageType stage = FaceLoader.GetHomeStageForFighter(p2Data.bodyType);
                BattleManager.Instance.ApplyStage(stage);
            }

            // Anuncios en pantalla
            if (BattleUI.Instance != null)
            {
                string tag1 = p1Data.isHuman ? $"[J{p1Data.humanPlayerNumber}]" : "[CPU]";
                string tag2 = p2Data.isHuman ? $"[J{p2Data.humanPlayerNumber}]" : "[CPU]";
                BattleUI.Instance.p1NameText.text = $"{p1Data.name} {tag1}";
                BattleUI.Instance.p2NameText.text = $"{p2Data.name} {tag2}";

                string vsMsg = match.isHumanVsHuman
                    ? $"⚔️ ¡DUELO DE AMIGOS EN VIVO! ⚔️\n{p1Data.name} (J1)  VS  {p2Data.name} (J2)"
                    : $"🥊 {match.roundName.ToUpper()} 🥊\n{p1Data.name} {tag1}  VS  {p2Data.name} {tag2}";

                BattleUI.Instance.ShowBanner(vsMsg, 3.2f);
            }

            BattleManager.Instance.RestartMatch();
        }

        /// <summary>
        /// Simula rápidamente el resultado en peleas entre bots de la máquina.
        /// </summary>
        public void SimulateCurrentCpuMatch()
        {
            var match = GetCurrentMatch();
            if (match == null || !match.isCpuVsCpu || match.isCompleted) return;

            bool f1Wins = UnityEngine.Random.value >= 0.5f;
            var winner = f1Wins ? match.fighter1 : match.fighter2;
            var loser = f1Wins ? match.fighter2 : match.fighter1;

            CompleteMatch(match, winner, loser);

            if (TournamentBracketUI.Instance != null)
            {
                TournamentBracketUI.Instance.RefreshBracketView();
            }
        }

        /// <summary>
        /// Se invoca cuando un luchador gana un combate en la arena.
        /// </summary>
        public void OnMatchWonBy(FighterController winnerController)
        {
            if (!isTournamentModeActive) return;

            var match = GetCurrentMatch();
            if (match == null) return;

            bool p1Won = (winnerController == BattleManager.Instance.player1);
            var winnerData = p1Won ? match.fighter1 : match.fighter2;
            var loserData = p1Won ? match.fighter2 : match.fighter1;

            CompleteMatch(match, winnerData, loserData);

            bool isGrandFinal = (match.matchIndex == allMatches.Count - 1);
            if (isGrandFinal)
            {
                // ¡CORONACIÓN TOTAL DEL CAMPEÓN!
                firstPlaceWinner = winnerData;
                secondPlaceWinner = loserData;

                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowBanner($"🏆 ¡¡¡CAMPEÓN DE LA COPA FIGHT FACE!!! 🏆\n👑 ¡{winnerData.name.ToUpper()} SE LLEVA EL GRAN TROFEO! 👑", 3.8f);
                }

                StartCoroutine(LaunchTournamentCelebrationRoutine(winnerController));
            }
            else
            {
                // Volver a la pantalla del Bracket mostrando quién avanzó
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowBanner($"¡K.O. TOTAL!\n👑 ¡{winnerData.name.ToUpper()} AVANZA EN EL TORNEO!", 2.8f);
                }

                StartCoroutine(ReturnToBracketAfterDelay(2.5f));
            }
        }

        private void CompleteMatch(TournamentMatch match, TournamentFighterData winner, TournamentFighterData loser)
        {
            match.winner = winner;
            match.loser = loser;
            match.isCompleted = true;

            // Avanzar al ganador a la siguiente ronda
            if (match.nextMatchIndex >= 0 && match.nextMatchIndex < allMatches.Count)
            {
                var nextM = allMatches[match.nextMatchIndex];
                if (match.nextMatchSlotIsFighter1) nextM.fighter1 = winner;
                else nextM.fighter2 = winner;
            }

            // Enviar al perdedor al duelo por el 3er puesto
            if (match.loserMatchIndex >= 0 && match.loserMatchIndex < allMatches.Count)
            {
                var loserM = allMatches[match.loserMatchIndex];
                if (match.loserMatchSlotIsFighter1) loserM.fighter1 = loser;
                else loserM.fighter2 = loser;
            }

            // Si fue el combate por el 3er puesto
            if (match.roundName.Contains("3ER PUESTO"))
            {
                thirdPlaceWinner = winner;
            }

            currentMatchIndex++;
        }

        private IEnumerator ReturnToBracketAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (TournamentBracketUI.Instance != null)
            {
                TournamentBracketUI.Instance.OpenBracketModal();
            }
            else
            {
                LoadCurrentTournamentMatch();
            }
        }

        private IEnumerator LaunchTournamentCelebrationRoutine(FighterController winner)
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
            catch (Exception ex)
            {
                Debug.LogError($"[TournamentManager] Error al mostrar pantalla de celebración: {ex}");
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowCharacterSelectMenu();
                }
            }
        }

        // =========================================================================
        // CONTINUE COUNTDOWN
        // =========================================================================
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

            var match = GetCurrentMatch();
            string oppName = (match != null && match.fighter2 != null) ? match.fighter2.name : "Rival";
            string roundTitle = match != null ? match.roundName : "TORNEO";
            if (continueInfoText != null)
            {
                continueInfoText.text = $"🥊 {roundTitle} • VS {oppName.ToUpper()}\n¿Deseas reintentar este combate?";
            }

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

            OnGiveUpTournamentClicked();
        }

        public void OnContinueTournamentClicked()
        {
            isContinueActive = false;
            if (continuePanelRoot != null) continuePanelRoot.SetActive(false);
            LoadCurrentTournamentMatch();
        }

        public void OnGiveUpTournamentClicked()
        {
            isContinueActive = false;
            isTournamentModeActive = false;
            if (continuePanelRoot != null) continuePanelRoot.SetActive(false);
            if (TournamentBracketUI.Instance != null) TournamentBracketUI.Instance.CloseAll();
            if (BattleUI.Instance != null) BattleUI.Instance.ShowCharacterSelectMenu();
        }

        private void BuildContinueUI(Transform canvas)
        {
            if (continuePanelRoot != null) return;

            continuePanelRoot = new GameObject("Panel_ArcadeContinue");
            continuePanelRoot.transform.SetParent(canvas, false);
            var rect = continuePanelRoot.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = continuePanelRoot.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);

            GameObject boxObj = new GameObject("CenterBox");
            boxObj.transform.SetParent(continuePanelRoot.transform, false);
            var bRect = boxObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0.5f);
            bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(650, 480);

            var bImg = boxObj.AddComponent<Image>();
            bImg.color = new Color(0.08f, 0.10f, 0.18f, 0.98f);
            var bOutline = boxObj.AddComponent<Outline>();
            bOutline.effectColor = new Color(1f, 0.25f, 0.15f, 0.9f);
            bOutline.effectDistance = new Vector2(3, 3);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(boxObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.80f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = "¡K.O.! ¿CONTINUAR?";
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 26;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.3f, 0.2f);
            titleTxt.raycastTarget = false;

            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(boxObj.transform, false);
            var iRect = infoObj.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.05f, 0.65f);
            iRect.anchorMax = new Vector2(0.95f, 0.80f);
            iRect.offsetMin = Vector2.zero;
            iRect.offsetMax = Vector2.zero;
            continueInfoText = infoObj.AddComponent<Text>();
            continueInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            continueInfoText.fontSize = 15;
            continueInfoText.alignment = TextAnchor.MiddleCenter;
            continueInfoText.color = Color.white;
            continueInfoText.raycastTarget = false;

            GameObject numObj = new GameObject("NumberText");
            numObj.transform.SetParent(boxObj.transform, false);
            var nRect = numObj.AddComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0.35f, 0.25f);
            nRect.anchorMax = new Vector2(0.65f, 0.65f);
            nRect.offsetMin = Vector2.zero;
            nRect.offsetMax = Vector2.zero;
            continueNumberText = numObj.AddComponent<Text>();
            continueNumberText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            continueNumberText.fontSize = 90;
            continueNumberText.fontStyle = FontStyle.Bold;
            continueNumberText.alignment = TextAnchor.MiddleCenter;
            continueNumberText.color = new Color(1f, 0.85f, 0.15f);
            continueNumberText.raycastTarget = false;

            GameObject btnRow = new GameObject("ButtonsRow");
            btnRow.transform.SetParent(boxObj.transform, false);
            var bRowRect = btnRow.AddComponent<RectTransform>();
            bRowRect.anchorMin = new Vector2(0.06f, 0.05f);
            bRowRect.anchorMax = new Vector2(0.94f, 0.22f);
            bRowRect.offsetMin = Vector2.zero;
            bRowRect.offsetMax = Vector2.zero;

            CreateContinueActionButton(btnRow.transform, "CONTINUAR [ESPACIO / ENTER]", new Vector2(0f, 0f), new Vector2(0.58f, 1f), new Color(0.12f, 0.65f, 0.35f), OnContinueTournamentClicked);
            CreateContinueActionButton(btnRow.transform, "SALIR [ESC]", new Vector2(0.62f, 0f), new Vector2(1f, 1f), new Color(0.75f, 0.25f, 0.25f), OnGiveUpTournamentClicked);

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
            bImg.raycastTarget = true;
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
    }
}
