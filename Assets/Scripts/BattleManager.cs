using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Gestiona las reglas del combate, temporizador, rondas y condiciones de victoria.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Luchadores")]
        public FighterController player1;
        public FighterController player2;

        [Header("Posiciones de Inicio")]
        public Vector3 p1SpawnPos = new Vector3(-3.5f, -2.90f, 0);
        public Vector3 p2SpawnPos = new Vector3(3.5f, -2.90f, 0);

        [Header("Configuración")]
        public int roundTimeSeconds = 99;
        public bool isP2ControlledByAI = true;
        public bool startInSelectMenu = true;
        public bool isTrainingMode = false;
        public FighterBodyType p1BodyType = FighterBodyType.Musculoso;
        public FighterBodyType p2BodyType = FighterBodyType.Gordo;

        [Header("Escenario")]
        public StageType selectedStage = StageType.Auto;
        public SpriteRenderer stageBackdropRenderer;

        [Header("Reglas Street Fighter II (Mejor de 3 Rondas)")]
        public int p1RoundsWon = 0;
        public int p2RoundsWon = 0;
        public int currentRound = 1;
        public const int RoundsToWin = 2; // 2 rondas ganadas para proclamarse campeón

        private float currentRoundTime;
        private bool isMatchActive = false;

        public bool IsMatchActive => isMatchActive;

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
        }

        private void Start()
        {
            StartCoroutine(MatchStartRoutine());
        }

        private void Update()
        {
            if (isTrainingMode && isMatchActive)
            {
                if (FightInput.GetTrainingReset())
                {
                    ResetTrainingPositions();
                }
            }
        }

        public void ApplyStage(StageType stage)
        {
            selectedStage = stage;
            if (stageBackdropRenderer == null)
            {
                GameObject bgObj = GameObject.Find("Backdrop");
                if (bgObj != null) stageBackdropRenderer = bgObj.GetComponent<SpriteRenderer>();
            }

            if (stageBackdropRenderer != null)
            {
                Sprite sp = FaceLoader.LoadStageSprite(stage, p2BodyType);
                if (sp != null)
                {
                    stageBackdropRenderer.sprite = sp;
                    float worldW = sp.rect.width / sp.pixelsPerUnit;
                    float scale = 22.0f / worldW;
                    stageBackdropRenderer.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private IEnumerator MatchStartRoutine()
        {
            isMatchActive = false;

            // Esperar que los componentes terminen inicialización
            yield return new WaitForSeconds(0.2f);

            ApplyFighterSetup(p1BodyType, p2BodyType, isP2ControlledByAI);

            if (startInSelectMenu && BattleUI.Instance != null && BattleUI.Instance.characterSelectPanel != null)
            {
                BattleUI.Instance.ShowCharacterSelectMenu();
            }
            else
            {
                RestartMatch();
            }
        }

        public void StartFightWithCharacters(FighterBodyType p1Body, FighterBodyType p2Body, bool p2AI, bool training = false, StageType stage = StageType.Auto)
        {
            if (TournamentManager.Instance != null)
            {
                TournamentManager.Instance.isTournamentModeActive = false;
            }

            p1BodyType = p1Body;
            p2BodyType = p2Body;
            isTrainingMode = training;
            isP2ControlledByAI = isTrainingMode ? false : p2AI;
            selectedStage = stage;

            ApplyFighterSetup(p1Body, p2Body, isP2ControlledByAI);

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.HideCharacterSelectMenu();
            }

            RestartMatch();
        }

        public void ApplyFighterSetup(FighterBodyType p1Body, FighterBodyType p2Body, bool p2AI)
        {
            p1BodyType = p1Body;
            p2BodyType = p2Body;
            isP2ControlledByAI = isTrainingMode ? false : p2AI;

            ApplyStage(selectedStage);

            if (player1 != null)
            {
                player1.fighterName = FaceLoader.GetFighterDisplayName(p1Body);
                if (player1.bodyController != null)
                {
                    player1.bodyController.SetClassicBody(p1Body);
                }
                // Jugador 1 carga el perfil del personaje seleccionado
                int p1Id = (int)p1Body + 1;
                FaceProfile p1Profile = FaceLoader.LoadProfileForFighter(p1Id, player1.fighterName, 0);
                if (player1.faceController != null)
                {
                    if (player1.bodyController != null && player1.bodyController.neckPoint != null)
                    {
                        player1.faceController.transform.SetParent(player1.bodyController.neckPoint, false);
                        player1.faceController.transform.localPosition = Vector3.zero;
                        player1.faceController.transform.localScale = Vector3.one;
                    }
                    player1.faceController.SetProfile(p1Profile);
                }

                if (p1Body == FighterBodyType.DosCabezas)
                {
                    if (player1.faceControllerRight == null)
                    {
                        GameObject rightHead = new GameObject("P1_Head_Right");
                        player1.faceControllerRight = rightHead.AddComponent<DynamicFaceController>();
                        var sr = rightHead.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = 12;
                    }
                    if (player1.bodyController != null && player1.bodyController.neckPointRight != null)
                    {
                        player1.faceControllerRight.transform.SetParent(player1.bodyController.neckPointRight, false);
                        player1.faceControllerRight.transform.localPosition = Vector3.zero;
                        player1.faceControllerRight.transform.localScale = Vector3.one;
                    }
                    FaceProfile p1Head2 = FaceLoader.LoadProfileForFighter(p1Id, "Cabeza 2", 1);
                    player1.faceControllerRight.SetProfile(p1Head2);
                    player1.voiceProfileHead2 = VoiceLoader.LoadProfileForFighter(p1Id, "Cabeza 2", 1);
                }
                else if (player1.faceControllerRight != null)
                {
                    Destroy(player1.faceControllerRight.gameObject);
                    player1.faceControllerRight = null;
                    player1.voiceProfileHead2 = null;
                }

                player1.voiceProfile = VoiceLoader.LoadProfileForFighter(p1Id, player1.fighterName, 0);
            }

            if (player2 != null)
            {
                player2.isTrainingDummy = isTrainingMode;
                player2.isAI = isTrainingMode ? false : p2AI;
                player2.fighterName = isTrainingMode 
                    ? "DUMMY: " + FaceLoader.GetFighterDisplayName(p2Body)
                    : FaceLoader.GetFighterDisplayName(p2Body);

                if (player2.bodyController != null)
                {
                    player2.bodyController.SetClassicBody(p2Body);
                }
                // Jugador 2 carga el perfil del personaje seleccionado
                int p2Id = (int)p2Body + 1;
                FaceProfile p2Profile = FaceLoader.LoadProfileForFighter(p2Id, player2.fighterName, 0);
                if (player2.faceController != null)
                {
                    if (player2.bodyController != null && player2.bodyController.neckPoint != null)
                    {
                        player2.faceController.transform.SetParent(player2.bodyController.neckPoint, false);
                        player2.faceController.transform.localPosition = Vector3.zero;
                        player2.faceController.transform.localScale = Vector3.one;
                    }
                    player2.faceController.SetProfile(p2Profile);
                }

                if (p2Body == FighterBodyType.DosCabezas)
                {
                    if (player2.faceControllerRight == null)
                    {
                        GameObject rightHead = new GameObject("P2_Head_Right");
                        player2.faceControllerRight = rightHead.AddComponent<DynamicFaceController>();
                        var sr = rightHead.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = 12;
                    }
                    if (player2.bodyController != null && player2.bodyController.neckPointRight != null)
                    {
                        player2.faceControllerRight.transform.SetParent(player2.bodyController.neckPointRight, false);
                        player2.faceControllerRight.transform.localPosition = Vector3.zero;
                        player2.faceControllerRight.transform.localScale = Vector3.one;
                    }
                    FaceProfile p2Head2 = FaceLoader.LoadProfileForFighter(p2Id, "Cabeza 2", 1);
                    player2.faceControllerRight.SetProfile(p2Head2);
                    player2.voiceProfileHead2 = VoiceLoader.LoadProfileForFighter(p2Id, "Cabeza 2", 1);
                }
                else if (player2.faceControllerRight != null)
                {
                    Destroy(player2.faceControllerRight.gameObject);
                    player2.faceControllerRight = null;
                    player2.voiceProfileHead2 = null;
                }

                player2.voiceProfile = VoiceLoader.LoadProfileForFighter(p2Id, player2.fighterName, 0);
            }

            if (BattleUI.Instance != null)
            {
                if (BattleUI.Instance.p1NameText != null && player1 != null)
                    BattleUI.Instance.p1NameText.text = "P1: " + player1.fighterName;
                if (BattleUI.Instance.p2NameText != null && player2 != null)
                    BattleUI.Instance.p2NameText.text = (isTrainingMode ? "DUMMY: " : (p2AI ? "CPU: " : "P2: ")) + player2.fighterName;
                BattleUI.Instance.RefreshFacePortraits();
            }
        }

        public void RestartMatch()
        {
            Time.timeScale = 1.0f;
            currentRoundTime = roundTimeSeconds;
            p1RoundsWon = 0;
            p2RoundsWon = 0;
            currentRound = 1;
            StartRound(1);
        }

        public void StartRound(int roundNum)
        {
            StopCoroutine("RoundIntroRoutine");
            StopCoroutine("RoundTimerRoutine");
            StartCoroutine(RoundIntroRoutine(roundNum));
        }

        private IEnumerator RoundIntroRoutine(int roundNum)
        {
            Time.timeScale = 1.0f;
            isMatchActive = false; // Bloquea ataques durante el anuncio de la ronda

            // Recargar perfiles frescos de disco por si el usuario actualizó fotos con webcam
            ApplyFighterSetup(p1BodyType, p2BodyType, isP2ControlledByAI);

            if (player1 != null)
            {
                player1.playerId = 1;
                player1.ResetFighter(p1SpawnPos);
                player1.isAlive = true;
                player1.estaEnStun = false;
                player1.isFullStunned = false;
                if (player2 != null) player1.opponent = player2.transform;
            }

            if (player2 != null)
            {
                player2.playerId = 2;
                player2.isTrainingDummy = isTrainingMode;
                player2.isAI = isTrainingMode ? false : isP2ControlledByAI;
                player2.ResetFighter(p2SpawnPos);
                player2.isAlive = true;
                if (player1 != null) player2.opponent = player1.transform;
            }

            if (DynamicFightCamera.Instance != null)
            {
                DynamicFightCamera.Instance.ResetCamera();
            }

            if (BattleUI.Instance != null)
            {
                if (BattleUI.Instance.winnerModalPanel != null)
                    BattleUI.Instance.winnerModalPanel.SetActive(false);

                BattleUI.Instance.UpdateHealth(1, 100, 100);
                BattleUI.Instance.UpdateHealth(2, 100, 100);
                BattleUI.Instance.UpdateStun(1, 0, 100);
                BattleUI.Instance.UpdateStun(2, 0, 100);
                BattleUI.Instance.UpdateRoundWins(p1RoundsWon, p2RoundsWon);

                if (isTrainingMode)
                {
                    BattleUI.Instance.SetTrainingModeUI(true);
                    BattleUI.Instance.UpdateTimerString("∞");
                    BattleUI.Instance.ShowBanner("🥋 MODO ENTRENAMIENTO\n[F1 / RETROCESO] REINICIAR POSICIÓN\n[M / ESC] SALIR AL MENÚ", 2.2f);
                    isMatchActive = true;
                    Time.timeScale = 1.0f;
                    StartCoroutine(RoundTimerRoutine());
                    yield break;
                }
                else
                {
                    BattleUI.Instance.SetTrainingModeUI(false);
                    BattleUI.Instance.UpdateTimer(roundTimeSeconds);

                    string roundMsg = (roundNum == 1) ? "ROUND 1" : (roundNum == 2 ? "ROUND 2" : "FINAL ROUND");
                    BattleUI.Instance.ShowBanner(roundMsg, 1.1f);
                }
            }

            yield return new WaitForSeconds(1.1f);

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner("¡FIGHT!", 0.9f);
            }

            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.PlayRoundBell();
            }

            yield return new WaitForSeconds(0.35f);

            isMatchActive = true;
            currentRoundTime = roundTimeSeconds;
            StartCoroutine(RoundTimerRoutine());
        }

        public void ResetTrainingPositions()
        {
            if (player1 != null)
            {
                player1.playerId = 1;
                player1.ResetFighter(p1SpawnPos);
                player1.isAlive = true;
                player1.estaEnStun = false;
                player1.isFullStunned = false;
                if (player2 != null) player1.opponent = player2.transform;
            }
            if (player2 != null)
            {
                player2.playerId = 2;
                player2.isTrainingDummy = true;
                player2.isAI = false;
                player2.ResetFighter(p2SpawnPos);
                player2.isAlive = true;
                if (player1 != null) player2.opponent = player1.transform;
            }
            if (DynamicFightCamera.Instance != null)
            {
                DynamicFightCamera.Instance.ResetCamera();
            }
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateHealth(1, 100, 100);
                BattleUI.Instance.UpdateHealth(2, 100, 100);
                BattleUI.Instance.ResetTrainingComboFeedback();
                BattleUI.Instance.ShowBanner("POSICIÓN REINICIADA", 0.7f);
            }
            isMatchActive = true;
            Time.timeScale = 1.0f;
        }

        private IEnumerator RoundTimerRoutine()
        {
            if (isTrainingMode)
            {
                while (isMatchActive && isTrainingMode)
                {
                    if (BattleUI.Instance != null)
                    {
                        BattleUI.Instance.UpdateTimerString("∞");
                    }
                    yield return new WaitForSeconds(1f);
                }
                yield break;
            }

            while (isMatchActive && currentRoundTime > 0)
            {
                yield return new WaitForSeconds(1f);
                currentRoundTime--;

                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.UpdateTimer(Mathf.CeilToInt(currentRoundTime));
                }

                if (currentRoundTime <= 0)
                {
                    OnTimeOver();
                }
            }
        }

        public void OnFighterKODefeated(FighterController defeated)
        {
            if (!isMatchActive || isTrainingMode) return;
            isMatchActive = false;
            StopCoroutine("RoundTimerRoutine");

            FighterController winner = (defeated == player1) ? player2 : player1;
            string winnerName = winner != null ? winner.fighterName : "¡Ganador!";

            Debug.Log($"[BattleManager] ¡K.O. en Ronda {currentRound}! Ganador de la ronda: {winnerName}");

            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.PlayKO();
            }

            // Hitstop / Cámara lenta dramática de K.O.
            StartCoroutine(KOSlowMotionRoutine(0.35f));

            if (defeated != null)
            {
                if (defeated.faceController != null) defeated.faceController.SetFace(FaceType.KO);
                if (defeated.faceControllerRight != null) defeated.faceControllerRight.SetFace(FaceType.KO);
            }

            if (winner != null)
            {
                winner.TriggerVictory();
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner("¡K.O.!", 2.0f);
            }

            // Anotar ronda ganada
            if (winner == player1) p1RoundsWon++;
            else p2RoundsWon++;

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateRoundWins(p1RoundsWon, p2RoundsWon);
            }

            // Comprobar si se ha alcanzado la victoria del combate (Mejor de 3)
            if (p1RoundsWon >= RoundsToWin || p2RoundsWon >= RoundsToWin)
            {
                StartCoroutine(ShowWinnerScreenDelayed(winner, defeated, 1.4f));
            }
            else
            {
                // Siguiente ronda (Round 2 o Final Round)
                StartCoroutine(NextRoundDelayed(2.2f));
            }
        }

        private IEnumerator NextRoundDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            currentRound++;
            StartRound(currentRound);
        }

        private IEnumerator KOSlowMotionRoutine(float realSeconds)
        {
            Time.timeScale = 0.25f;
            yield return new WaitForSecondsRealtime(realSeconds);
            Time.timeScale = 1.0f;
        }

        private IEnumerator ShowWinnerScreenDelayed(FighterController winner, FighterController loser, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (TournamentManager.Instance != null && TournamentManager.Instance.isTournamentModeActive)
            {
                TournamentManager.Instance.OnMatchWonBy(winner);
                yield break;
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowWinnerScreen(winner, loser);
                // La pantalla de victoria se mantiene abierta indefinidamente hasta que el usuario decida [Revancha] o [Elegir luchador]
            }
        }

        private void OnTimeOver()
        {
            if (!isMatchActive) return;
            isMatchActive = false;
            StopCoroutine("RoundTimerRoutine");

            FighterController winner = null;
            FighterController loser = null;

            if (player1 != null && player2 != null)
            {
                if (player1.currentHealth > player2.currentHealth)
                {
                    winner = player1;
                    loser = player2;
                }
                else if (player2.currentHealth > player1.currentHealth)
                {
                    winner = player2;
                    loser = player1;
                }
                else
                {
                    // Empate por tiempo: P1 gana por desempate de iniciativa
                    winner = player1;
                    loser = player2;
                }
            }

            if (winner != null) winner.TriggerVictory();
            if (loser != null && winner != loser)
            {
                if (loser.faceController != null) loser.faceController.SetFace(FaceType.KO);
                if (loser.faceControllerRight != null) loser.faceControllerRight.SetFace(FaceType.KO);
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner("¡TIME OVER!", 2.0f);
            }

            if (winner == player1) p1RoundsWon++;
            else p2RoundsWon++;

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateRoundWins(p1RoundsWon, p2RoundsWon);
            }

            if (p1RoundsWon >= RoundsToWin || p2RoundsWon >= RoundsToWin)
            {
                StartCoroutine(ShowWinnerScreenDelayed(winner, loser, 1.4f));
            }
            else
            {
                StartCoroutine(NextRoundDelayed(2.2f));
            }
        }

        public void ToggleP2AI()
        {
            isP2ControlledByAI = !isP2ControlledByAI;
            if (player2 != null)
            {
                player2.isAI = isP2ControlledByAI;
            }
            Debug.Log($"[BattleManager] Jugador 2 IA: {isP2ControlledByAI}");
        }
    }
}
