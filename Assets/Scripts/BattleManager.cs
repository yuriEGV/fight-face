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
        public Vector3 p1SpawnPos = new Vector3(-3.5f, -1.8f, 0);
        public Vector3 p2SpawnPos = new Vector3(3.5f, -1.8f, 0);

        [Header("Configuración")]
        public int roundTimeSeconds = 99;
        public bool isP2ControlledByAI = true;
        public bool startInSelectMenu = true;
        public FighterBodyType p1BodyType = FighterBodyType.Musculoso;
        public FighterBodyType p2BodyType = FighterBodyType.Gordo;

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

        public void StartFightWithCharacters(FighterBodyType p1Body, FighterBodyType p2Body, bool p2AI)
        {
            p1BodyType = p1Body;
            p2BodyType = p2Body;
            isP2ControlledByAI = p2AI;

            ApplyFighterSetup(p1Body, p2Body, p2AI);

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
            isP2ControlledByAI = p2AI;

            if (player1 != null)
            {
                player1.fighterName = FaceLoader.GetFighterDisplayName(p1Body);
                if (player1.bodyController != null)
                {
                    player1.bodyController.SetClassicBody(p1Body);
                }
                FaceProfile p1Profile = FaceLoader.LoadProfileForFighter((int)p1Body + 1, player1.fighterName);
                if (player1.faceController != null)
                {
                    if (player1.bodyController != null && player1.bodyController.neckPoint != null)
                    {
                        player1.faceController.transform.SetParent(player1.bodyController.neckPoint, false);
                        player1.faceController.transform.localPosition = Vector3.zero;
                    }
                    player1.faceController.SetProfile(p1Profile);
                }

                // Dos Cabezas compatibilidad de doble cabeza
                if (p1Body == FighterBodyType.DosCabezas && player1.bodyController != null && player1.bodyController.neckPointRight != null)
                {
                    if (player1.faceControllerRight == null)
                    {
                        GameObject rHead = new GameObject("Head_Face_Right");
                        rHead.transform.SetParent(player1.bodyController.neckPointRight, false);
                        rHead.transform.localPosition = Vector3.zero;
                        player1.faceControllerRight = rHead.AddComponent<DynamicFaceController>();
                        var sr = rHead.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = 12;
                    }
                    player1.faceControllerRight.transform.SetParent(player1.bodyController.neckPointRight, false);
                    player1.faceControllerRight.SetProfile(p1Profile);
                    player1.faceControllerRight.SetFace(FaceType.Dolor);
                }
                else if (player1.faceControllerRight != null)
                {
                    Destroy(player1.faceControllerRight.gameObject);
                    player1.faceControllerRight = null;
                }
            }

            if (player2 != null)
            {
                player2.isAI = p2AI;
                player2.fighterName = FaceLoader.GetFighterDisplayName(p2Body);
                if (player2.bodyController != null)
                {
                    player2.bodyController.SetClassicBody(p2Body);
                }
                FaceProfile p2Profile = FaceLoader.LoadProfileForFighter((int)p2Body + 1, player2.fighterName);
                if (player2.faceController != null)
                {
                    if (player2.bodyController != null && player2.bodyController.neckPoint != null)
                    {
                        player2.faceController.transform.SetParent(player2.bodyController.neckPoint, false);
                        player2.faceController.transform.localPosition = Vector3.zero;
                    }
                    player2.faceController.SetProfile(p2Profile);
                }

                // Dos Cabezas compatibilidad de doble cabeza
                if (p2Body == FighterBodyType.DosCabezas && player2.bodyController != null && player2.bodyController.neckPointRight != null)
                {
                    if (player2.faceControllerRight == null)
                    {
                        GameObject rHead = new GameObject("Head_Face_Right");
                        rHead.transform.SetParent(player2.bodyController.neckPointRight, false);
                        rHead.transform.localPosition = Vector3.zero;
                        player2.faceControllerRight = rHead.AddComponent<DynamicFaceController>();
                        var sr = rHead.GetComponent<SpriteRenderer>();
                        sr.sortingOrder = 12;
                    }
                    player2.faceControllerRight.transform.SetParent(player2.bodyController.neckPointRight, false);
                    player2.faceControllerRight.SetProfile(p2Profile);
                    player2.faceControllerRight.SetFace(FaceType.Dolor);
                }
                else if (player2.faceControllerRight != null)
                {
                    Destroy(player2.faceControllerRight.gameObject);
                    player2.faceControllerRight = null;
                }
            }

            if (BattleUI.Instance != null)
            {
                if (BattleUI.Instance.p1NameText != null && player1 != null)
                    BattleUI.Instance.p1NameText.text = "P1: " + player1.fighterName;
                if (BattleUI.Instance.p2NameText != null && player2 != null)
                    BattleUI.Instance.p2NameText.text = (p2AI ? "CPU: " : "P2: ") + player2.fighterName;
                BattleUI.Instance.RefreshFacePortraits();
            }
        }

        public void RestartMatch()
        {
            currentRoundTime = roundTimeSeconds;
            isMatchActive = true;

            if (player1 != null)
            {
                player1.ResetFighter(p1SpawnPos);
                if (player2 != null) player1.opponent = player2.transform;
            }

            if (player2 != null)
            {
                player2.isAI = isP2ControlledByAI;
                player2.ResetFighter(p2SpawnPos);
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
                BattleUI.Instance.UpdateTimer(roundTimeSeconds);
                BattleUI.Instance.ShowBanner("¡ROUND 1 - A PELEAR!", 1.8f);
            }

            StopCoroutine("RoundTimerRoutine");
            StartCoroutine(RoundTimerRoutine());
        }

        private IEnumerator RoundTimerRoutine()
        {
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
            if (!isMatchActive) return;
            isMatchActive = false;

            FighterController winner = (defeated == player1) ? player2 : player1;
            string winnerName = winner != null ? winner.fighterName : "¡Ganador!";

            Debug.Log($"[BattleManager] ¡Fin del combate! Ganador: {winnerName}");

            if (winner != null)
            {
                winner.TriggerVictory();
            }

            if (TournamentManager.Instance != null && TournamentManager.Instance.isTournamentModeActive)
            {
                TournamentManager.Instance.OnMatchWonBy(winner);
            }
            else if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner("¡K.O.!", 2.0f);
                StartCoroutine(ShowWinnerModalDelayed(winner, defeated, 1.2f));
            }
        }

        private IEnumerator ShowWinnerModalDelayed(FighterController winner, FighterController loser, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowWinnerScreen(winner, loser);
            }
        }

        private void OnTimeOver()
        {
            isMatchActive = false;
            string winnerMsg = "¡EMPATE!";

            if (player1 != null && player2 != null)
            {
                if (player1.currentHealth > player2.currentHealth)
                {
                    winnerMsg = $"¡TIEMPO!\nGANADOR: {player1.fighterName.ToUpper()}";
                }
                else if (player2.currentHealth > player1.currentHealth)
                {
                    winnerMsg = $"¡TIEMPO!\nGANADOR: {player2.fighterName.ToUpper()}";
                }
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner(winnerMsg, 5f);
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
