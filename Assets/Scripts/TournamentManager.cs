using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
                "El Dos Cabezas"
            };

            Color[] suits = {
                new Color(0.9f, 0.2f, 0.2f),   // 1. Rojo / Azul
                new Color(0.2f, 0.65f, 0.35f), // 2. Verde kung-fu
                new Color(0.95f, 0.8f, 0.1f),  // 3. Dorado / Rojo Muay Thai
                new Color(0.75f, 0.2f, 0.85f), // 4. Morado / Magenta
                new Color(0.25f, 0.25f, 0.30f) // 5. Gris combate oscuro
            };

            Color[] gloves = {
                new Color(0.9f, 0.2f, 0.2f),  // Rojo boxeo
                new Color(0.15f, 0.15f, 0.15f),// Negro kung-fu
                new Color(0.95f, 0.25f, 0.25f),// Rojo Muay Thai
                new Color(0.9f, 0.2f, 0.6f),  // Magenta
                new Color(0.85f, 0.6f, 0.1f)  // Oro oxidado
            };

            for (int i = 1; i <= 5; i++)
            {
                var fighter = new TournamentFighterData
                {
                    id = i,
                    name = names[i - 1],
                    suitColor = suits[i - 1],
                    gloveColor = gloves[i - 1],
                    faceProfile = LoadOrGenerateProfile(i, names[i - 1])
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
            return roster.Find(f => f.id == id);
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
                BattleManager.Instance.player1.playerId = p1Data.id;
                BattleManager.Instance.player1.fighterName = p1Data.name;
                BattleManager.Instance.player1.faceController.SetProfile(p1Data.faceProfile);
                BattleManager.Instance.player1.bodyController.suitColor = p1Data.suitColor;
                BattleManager.Instance.player1.bodyController.gloveColor = p1Data.gloveColor;
            }

            // Configurar P2 (Oponente del Torneo)
            if (BattleManager.Instance.player2 != null && p2Data != null)
            {
                BattleManager.Instance.player2.playerId = p2Data.id;
                BattleManager.Instance.player2.fighterName = p2Data.name;
                BattleManager.Instance.player2.isAI = true; // CPU bot en el torneo
                BattleManager.Instance.player2.faceController.SetProfile(p2Data.faceProfile);
                BattleManager.Instance.player2.bodyController.suitColor = p2Data.suitColor;
                BattleManager.Instance.player2.bodyController.gloveColor = p2Data.gloveColor;
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
                case 3: return "¡LA GRAN FINAL!";
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
                        BattleUI.Instance.ShowBanner($"🏆 ¡CAMPEÓN DEL TORNEO! 🏆\n¡{winner.fighterName.ToUpper()} ES EL REY!", 6f);
                    }
                }
            }
            else
            {
                // El Jugador 1 perdió
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.ShowBanner($"HAS SIDO ELIMINADO\nPresiona [R] para Reintentar", 4f);
                }
            }
        }
    }
}
