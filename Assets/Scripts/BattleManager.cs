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
            }

            if (player2 != null)
            {
                player2.isAI = p2AI;
                player2.fighterName = FaceLoader.GetFighterDisplayName(p2Body);
                if (player2.bodyController != null)
                {
                    player2.bodyController.SetClassicBody(p2Body);
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

            if (TournamentManager.Instance != null && TournamentManager.Instance.isTournamentModeActive)
            {
                TournamentManager.Instance.OnMatchWonBy(winner);
            }
            else if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner($"¡K.O.!\n¡GANADOR: {winnerName.ToUpper()}!", 5f);
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
