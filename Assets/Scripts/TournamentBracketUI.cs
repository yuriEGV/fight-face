using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Interfaz Gráfica interactiva estilo Street Fighter / Arcade para el Modo Campeonato:
    /// - Selector inicial de torneo: 8 Jugadores (~30 min) o 4 Jugadores (~15 min).
    /// - Selección de modalidad: Pase y Juegue (Amigos), Solitario vs CPU, o Red Wi-Fi.
    /// - Árbol visual de llaves (Bracket) con retratos, nombres, marcas de KO y clasificados.
    /// - Anuncio de cada combate y opción de simulación rápida en peleas CPU vs CPU.
    /// </summary>
    public class TournamentBracketUI : MonoBehaviour
    {
        public static TournamentBracketUI Instance { get; private set; }

        private GameObject setupModalRoot;
        private GameObject bracketModalRoot;

        // Elementos del Setup
        private Button btnSize8;
        private Button btnSize4;
        private Text txtSize8;
        private Text txtSize4;
        private Button btnModePassPlay;
        private Button btnModeSingle;
        private Button btnModeWifi;
        private Text txtModePassPlay;
        private Text txtModeSingle;
        private Text txtModeWifi;

        private TournamentSize selectedSize = TournamentSize.EightPlayers;
        private TournamentParticipationMode selectedMode = TournamentParticipationMode.PassAndPlay;

        // Elementos del Bracket
        private Text bracketHeaderTitle;
        private Text nextFightBannerText;
        private Button btnStartNextFight;
        private Button btnSimulateNextFight;
        private Transform bracketContainer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void BuildUI(Transform canvas)
        {
            if (setupModalRoot != null) return;

            Font arcadeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            BuildSetupModal(canvas, arcadeFont);
            BuildBracketModal(canvas, arcadeFont);
        }

        // =========================================================================
        // 1. MODAL DE CONFIGURACIÓN DEL TORNEO (4 u 8 Jugadores)
        // =========================================================================
        private void BuildSetupModal(Transform canvas, Font font)
        {
            setupModalRoot = new GameObject("Modal_TournamentSetup");
            setupModalRoot.transform.SetParent(canvas, false);
            var r = setupModalRoot.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.04f, 0.04f);
            r.anchorMax = new Vector2(0.96f, 0.96f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            var bg = setupModalRoot.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.98f);
            var outl = setupModalRoot.AddComponent<Outline>();
            outl.effectColor = new Color(1f, 0.8f, 0.2f, 0.9f);
            outl.effectDistance = new Vector2(3, 3);

            // Título
            CreateText(setupModalRoot.transform, "Title", new Vector2(0.05f, 0.88f), new Vector2(0.90f, 0.98f),
                "🏆 GRAN CAMPEONATO FIGHT FACE - SELECCIÓN DE TORNEO 🏆", 26, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), font);

            // Subtítulo Duración / Participantes
            CreateText(setupModalRoot.transform, "SubSize", new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.86f),
                "1. ELIGE LA DURACIÓN Y TAMAÑO DEL TORNEO:", 18, FontStyle.Bold, new Color(1f, 0.92f, 0.4f), font);

            // Botón 8 Jugadores (Recomendado ~30 min)
            btnSize8 = CreateButton(setupModalRoot.transform, "Btn_Size8", new Vector2(0.04f, 0.58f), new Vector2(0.49f, 0.78f),
                "👑 TORNEO 8 LUCHADORES\n<size=16>★ DURACIÓN: ~30 MINUTOS (RECOMENDADO)</size>\n<size=14>Cuartos de Final (4 peleas) ➔ Semis ➔ 3er Puesto ➔ ¡Gran Final!</size>",
                new Color(0.12f, 0.70f, 0.35f), font, 19, () => SelectSize(TournamentSize.EightPlayers));
            txtSize8 = btnSize8.GetComponentInChildren<Text>();

            // Botón 4 Jugadores (~15 min)
            btnSize4 = CreateButton(setupModalRoot.transform, "Btn_Size4", new Vector2(0.51f, 0.58f), new Vector2(0.96f, 0.78f),
                "⚡ TORNEO 4 LUCHADORES\n<size=16>★ DURACIÓN: ~15 MINUTOS (MODO EXPRÉS)</size>\n<size=14>Semifinales directas (2 peleas) ➔ 3er Puesto ➔ ¡Gran Final!</size>",
                new Color(0.10f, 0.14f, 0.22f), font, 19, () => SelectSize(TournamentSize.FourPlayers));
            txtSize4 = btnSize4.GetComponentInChildren<Text>();

            // Subtítulo Modalidad
            CreateText(setupModalRoot.transform, "SubMode", new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.55f),
                "2. ELIGE CÓMO SE JUEGA (MODALIDAD HÍBRIDA):", 18, FontStyle.Bold, new Color(1f, 0.92f, 0.4f), font);

            // Opción 1: Pase y Juegue (Amigos)
            btnModePassPlay = CreateButton(setupModalRoot.transform, "Btn_ModePassPlay", new Vector2(0.04f, 0.23f), new Vector2(0.33f, 0.46f),
                "👥 PASE Y JUEGUE\n<size=15>Turnos en el mismo móvil o PC.\nDos amigos juegan cada pelea.</size>",
                new Color(0.95f, 0.50f, 0.10f), font, 18, () => SelectMode(TournamentParticipationMode.PassAndPlay));
            txtModePassPlay = btnModePassPlay.GetComponentInChildren<Text>();

            // Opción 2: Solitario (Tú vs Máquina)
            btnModeSingle = CreateButton(setupModalRoot.transform, "Btn_ModeSingle", new Vector2(0.355f, 0.23f), new Vector2(0.645f, 0.46f),
                "🤖 SOLITARIO VS CPU\n<size=15>Tú solo contra la máquina.\nEscalera arcade de rivales.</size>",
                new Color(0.10f, 0.14f, 0.22f), font, 18, () => SelectMode(TournamentParticipationMode.SinglePlayerVsAi));
            txtModeSingle = btnModeSingle.GetComponentInChildren<Text>();

            // Opción 3: Red Local Wi-Fi
            btnModeWifi = CreateButton(setupModalRoot.transform, "Btn_ModeWifi", new Vector2(0.67f, 0.23f), new Vector2(0.96f, 0.46f),
                "📶 RED WI-FI (2 MÓVILES)\n<size=15>Combates por Wi-Fi o Hotspot.\nRetos en vivo entre dos celulares.</size>",
                new Color(0.10f, 0.14f, 0.22f), font, 18, () => SelectMode(TournamentParticipationMode.LocalWifi));
            txtModeWifi = btnModeWifi.GetComponentInChildren<Text>();

            // Botón INICIAR TORNEO
            CreateButton(setupModalRoot.transform, "Btn_StartTourney", new Vector2(0.16f, 0.045f), new Vector2(0.84f, 0.18f),
                "🔥 ¡INICIAR CAMPEONATO AHORA! 🔥", new Color(0.95f, 0.20f, 0.10f), font, 24, OnConfirmStartTournament);

            // Botón Volver / Cerrar Setup (X)
            CreateButton(setupModalRoot.transform, "Btn_CloseSetup", new Vector2(0.92f, 0.90f), new Vector2(0.98f, 0.98f),
                "✖", new Color(0.70f, 0.15f, 0.15f), font, 22, () =>
                {
                    CloseAll();
                    if (CharacterSelectUI.Instance != null) CharacterSelectUI.Instance.gameObject.SetActive(true);
                });

            setupModalRoot.SetActive(false);
        }

        private void SelectSize(TournamentSize size)
        {
            selectedSize = size;
            bool is8 = (size == TournamentSize.EightPlayers);
            if (btnSize8 != null)
            {
                btnSize8.GetComponent<Image>().color = is8 ? new Color(0.12f, 0.70f, 0.35f) : new Color(0.10f, 0.14f, 0.22f, 0.85f);
                var outl = btnSize8.GetComponent<Outline>();
                if (outl != null)
                {
                    outl.effectColor = is8 ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.25f, 0.35f, 0.50f, 0.4f);
                    outl.effectDistance = is8 ? new Vector2(3f, 3f) : new Vector2(1f, 1f);
                }
            }
            if (btnSize4 != null)
            {
                btnSize4.GetComponent<Image>().color = !is8 ? new Color(0.15f, 0.55f, 0.95f) : new Color(0.10f, 0.14f, 0.22f, 0.85f);
                var outl = btnSize4.GetComponent<Outline>();
                if (outl != null)
                {
                    outl.effectColor = !is8 ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.25f, 0.35f, 0.50f, 0.4f);
                    outl.effectDistance = !is8 ? new Vector2(3f, 3f) : new Vector2(1f, 1f);
                }
            }
        }

        private void SelectMode(TournamentParticipationMode mode)
        {
            selectedMode = mode;
            UpdateButtonMode(btnModePassPlay, mode == TournamentParticipationMode.PassAndPlay, new Color(0.95f, 0.50f, 0.10f));
            UpdateButtonMode(btnModeSingle, mode == TournamentParticipationMode.SinglePlayerVsAi, new Color(0.15f, 0.55f, 0.95f));
            UpdateButtonMode(btnModeWifi, mode == TournamentParticipationMode.LocalWifi, new Color(0.12f, 0.70f, 0.40f));
        }

        private void UpdateButtonMode(Button btn, bool isSelected, Color activeColor)
        {
            if (btn == null) return;
            btn.GetComponent<Image>().color = isSelected ? activeColor : new Color(0.10f, 0.14f, 0.22f, 0.85f);
            var outl = btn.GetComponent<Outline>();
            if (outl != null)
            {
                outl.effectColor = isSelected ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.25f, 0.35f, 0.50f, 0.4f);
                outl.effectDistance = isSelected ? new Vector2(3f, 3f) : new Vector2(1f, 1f);
            }
        }

        private void OnConfirmStartTournament()
        {
            setupModalRoot.SetActive(false);
            if (TournamentManager.Instance != null)
            {
                int p1Id = 1;
                if (BattleManager.Instance != null && BattleManager.Instance.player1 != null)
                {
                    p1Id = (int)BattleManager.Instance.p1BodyType + 1;
                }
                TournamentManager.Instance.StartConfiguredTournament(selectedSize, selectedMode, p1Id);
            }
        }

        // =========================================================================
        // 2. MODAL DEL CUADRO DE LLAVES (TOURNAMENT BRACKET)
        // =========================================================================
        private void BuildBracketModal(Transform canvas, Font font)
        {
            bracketModalRoot = new GameObject("Modal_TournamentBracket");
            bracketModalRoot.transform.SetParent(canvas, false);
            var r = bracketModalRoot.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.04f, 0.04f);
            r.anchorMax = new Vector2(0.96f, 0.96f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            var bg = bracketModalRoot.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.09f, 0.98f);
            var outl = bracketModalRoot.AddComponent<Outline>();
            outl.effectColor = new Color(0.2f, 0.7f, 1f, 0.9f);
            outl.effectDistance = new Vector2(3, 3);

            // Título
            bracketHeaderTitle = CreateText(bracketModalRoot.transform, "Title", new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f),
                "🏆 LLAVES DEL CAMPEONATO (BRACKET) 🏆", 22, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), font);

            // Botón Salir (X)
            CreateButton(bracketModalRoot.transform, "Btn_Exit", new Vector2(0.93f, 0.91f), new Vector2(0.985f, 0.97f),
                "✕", new Color(0.85f, 0.2f, 0.2f), font, 16, CloseAll);

            // Contenedor visual del árbol de llaves
            GameObject treeObj = new GameObject("BracketTreeContainer");
            treeObj.transform.SetParent(bracketModalRoot.transform, false);
            var tRect = treeObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.02f, 0.20f);
            tRect.anchorMax = new Vector2(0.98f, 0.89f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            bracketContainer = treeObj.transform;

            // Barra inferior con información del próximo combate
            GameObject botBar = new GameObject("BottomBar");
            botBar.transform.SetParent(bracketModalRoot.transform, false);
            var bbRect = botBar.AddComponent<RectTransform>();
            bbRect.anchorMin = new Vector2(0.02f, 0.02f);
            bbRect.anchorMax = new Vector2(0.98f, 0.18f);
            bbRect.offsetMin = Vector2.zero;
            bbRect.offsetMax = Vector2.zero;
            var bbImg = botBar.AddComponent<Image>();
            bbImg.color = new Color(0.06f, 0.10f, 0.18f, 0.95f);

            nextFightBannerText = CreateText(botBar.transform, "NextFightBanner", new Vector2(0.02f, 0.1f), new Vector2(0.48f, 0.9f),
                "🥊 PRÓXIMO COMBATE: ...", 16, FontStyle.Bold, Color.white, font);
            nextFightBannerText.alignment = TextAnchor.MiddleLeft;

            // Botón Jugar Combate
            btnStartNextFight = CreateButton(botBar.transform, "Btn_PlayFight", new Vector2(0.50f, 0.12f), new Vector2(0.78f, 0.88f),
                "🔥 ¡JUGAR ESTE COMBATE!", new Color(0.95f, 0.35f, 0.10f), font, 16, () =>
                {
                    bracketModalRoot.SetActive(false);
                    if (TournamentManager.Instance != null)
                    {
                        TournamentManager.Instance.LoadCurrentTournamentMatch();
                    }
                });

            // Botón Simular (para CPU vs CPU)
            btnSimulateNextFight = CreateButton(botBar.transform, "Btn_SimulateFight", new Vector2(0.80f, 0.12f), new Vector2(0.98f, 0.88f),
                "⚡ SIMULAR (RÁPIDO)", new Color(0.20f, 0.50f, 0.85f), font, 13, () =>
                {
                    if (TournamentManager.Instance != null)
                    {
                        TournamentManager.Instance.SimulateCurrentCpuMatch();
                    }
                });

            // Botón Salir del Torneo
            CreateButton(bracketModalRoot.transform, "Btn_ExitTourney", new Vector2(0.93f, 0.91f), new Vector2(0.985f, 0.985f),
                "✖", new Color(0.70f, 0.15f, 0.15f), font, 18, () =>
                {
                    CloseAll();
                    if (TournamentManager.Instance != null)
                    {
                        TournamentManager.Instance.isTournamentModeActive = false;
                    }
                    if (CharacterSelectUI.Instance != null)
                    {
                        CharacterSelectUI.Instance.gameObject.SetActive(true);
                    }
                });

            bracketModalRoot.SetActive(false);
        }

        // =========================================================================
        // 3. APERTURA Y RENDERIZADO DEL BRACKET VISUAL
        // =========================================================================
        public void OpenSetupModal()
        {
            CloseAll();
            if (setupModalRoot != null)
            {
                setupModalRoot.transform.SetAsLastSibling();
                setupModalRoot.SetActive(true);
                SelectSize(selectedSize);
                SelectMode(selectedMode);
            }
        }

        public void OpenBracketModal()
        {
            CloseAll();
            if (bracketModalRoot != null)
            {
                bracketModalRoot.transform.SetAsLastSibling();
                bracketModalRoot.SetActive(true);
                RefreshBracketView();
            }
        }

        public void CloseAll()
        {
            if (setupModalRoot != null) setupModalRoot.SetActive(false);
            if (bracketModalRoot != null) bracketModalRoot.SetActive(false);
        }

        public bool IsAnyModalOpen()
        {
            return (setupModalRoot != null && setupModalRoot.activeSelf) ||
                   (bracketModalRoot != null && bracketModalRoot.activeSelf);
        }

        public bool IsActive => IsAnyModalOpen();

        public void RefreshBracketView()
        {
            if (TournamentManager.Instance == null || bracketContainer == null) return;

            // Limpiar hijos anteriores del contenedor
            foreach (Transform child in bracketContainer)
            {
                Destroy(child.gameObject);
            }

            var mgr = TournamentManager.Instance;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            bool is8 = mgr.currentTournamentSize == TournamentSize.EightPlayers;
            bracketHeaderTitle.text = is8 ? "🏆 GRAN COPA FIGHT FACE (8 LUCHADORES - ~30 MIN) 🏆"
                                          : "⚡ TORNEO EXPRÉS FIGHT FACE (4 LUCHADORES - ~15 MIN) ⚡";

            // Renderizar los combates según el árbol
            if (is8)
            {
                Render8PlayerBracket(mgr, font);
            }
            else
            {
                Render4PlayerBracket(mgr, font);
            }

            // Actualizar banner de próximo combate
            var curMatch = mgr.GetCurrentMatch();
            if (curMatch != null && !curMatch.isCompleted)
            {
                string f1 = curMatch.fighter1 != null ? curMatch.fighter1.name : "???";
                string f2 = curMatch.fighter2 != null ? curMatch.fighter2.name : "???";
                string tag1 = curMatch.fighter1 != null && curMatch.fighter1.isHuman ? $"[J{curMatch.fighter1.humanPlayerNumber}]" : "[CPU]";
                string tag2 = curMatch.fighter2 != null && curMatch.fighter2.isHuman ? $"[J{curMatch.fighter2.humanPlayerNumber}]" : "[CPU]";

                nextFightBannerText.text = $"🥊 <b>{curMatch.roundName.ToUpper()}</b>\n{f1} {tag1}  <b>VS</b>  {f2} {tag2}";

                btnStartNextFight.gameObject.SetActive(true);
                btnSimulateNextFight.gameObject.SetActive(curMatch.isCpuVsCpu);
            }
            else
            {
                nextFightBannerText.text = "🏆 ¡EL CAMPEONATO HA FINALIZADO!";
                btnStartNextFight.gameObject.SetActive(false);
                btnSimulateNextFight.gameObject.SetActive(false);
            }
        }

        private void Render8PlayerBracket(TournamentManager mgr, Font font)
        {
            // Columnas:
            // Col 1 (Cuartos de final): 4 combates (x = 0.02 a 0.28)
            // Col 2 (Semifinales): 2 combates (x = 0.32 a 0.58)
            // Col 3 (Final y 3er puesto): 2 combates (x = 0.62 a 0.98)

            // Cuartos de Final (Matches 0, 1, 2, 3)
            float step4 = 0.23f;
            for (int i = 0; i < 4; i++)
            {
                float yCenter = 0.85f - (i * step4);
                if (i < mgr.allMatches.Count)
                {
                    CreateMatchCard(bracketContainer, mgr.allMatches[i], new Vector2(0.02f, yCenter - 0.09f), new Vector2(0.28f, yCenter + 0.09f), font, i == mgr.currentMatchIndex);
                }
            }

            // Semifinales (Matches 4, 5)
            float step2 = 0.46f;
            for (int i = 0; i < 2; i++)
            {
                float yCenter = 0.73f - (i * step2);
                int mIdx = 4 + i;
                if (mIdx < mgr.allMatches.Count)
                {
                    CreateMatchCard(bracketContainer, mgr.allMatches[mIdx], new Vector2(0.33f, yCenter - 0.10f), new Vector2(0.60f, yCenter + 0.10f), font, mIdx == mgr.currentMatchIndex);
                }
            }

            // 3er Puesto (Bronce - Match 6)
            if (6 < mgr.allMatches.Count)
            {
                CreateMatchCard(bracketContainer, mgr.allMatches[6], new Vector2(0.65f, 0.12f), new Vector2(0.96f, 0.35f), font, 6 == mgr.currentMatchIndex, "🥉 3ER PUESTO (BRONCE)");
            }

            // ¡Gran Final! (Oro - Match 7)
            if (7 < mgr.allMatches.Count)
            {
                CreateMatchCard(bracketContainer, mgr.allMatches[7], new Vector2(0.65f, 0.55f), new Vector2(0.98f, 0.85f), font, 7 == mgr.currentMatchIndex, "👑 ¡GRAN FINAL (ORO)!");
            }
        }

        private void Render4PlayerBracket(TournamentManager mgr, Font font)
        {
            // Semifinales (Matches 0, 1)
            float step2 = 0.46f;
            for (int i = 0; i < 2; i++)
            {
                float yCenter = 0.73f - (i * step2);
                if (i < mgr.allMatches.Count)
                {
                    CreateMatchCard(bracketContainer, mgr.allMatches[i], new Vector2(0.08f, yCenter - 0.12f), new Vector2(0.44f, yCenter + 0.12f), font, i == mgr.currentMatchIndex);
                }
            }

            // 3er Puesto (Match 2)
            if (2 < mgr.allMatches.Count)
            {
                CreateMatchCard(bracketContainer, mgr.allMatches[2], new Vector2(0.52f, 0.12f), new Vector2(0.92f, 0.38f), font, 2 == mgr.currentMatchIndex, "🥉 3ER PUESTO (BRONCE)");
            }

            // Gran Final (Match 3)
            if (3 < mgr.allMatches.Count)
            {
                CreateMatchCard(bracketContainer, mgr.allMatches[3], new Vector2(0.52f, 0.55f), new Vector2(0.94f, 0.85f), font, 3 == mgr.currentMatchIndex, "👑 ¡GRAN FINAL (ORO)!");
            }
        }

        private void CreateMatchCard(Transform parent, TournamentMatch match, Vector2 aMin, Vector2 aMax, Font font, bool isCurrent, string customHeader = null)
        {
            GameObject cardObj = new GameObject("Match_" + match.matchIndex);
            cardObj.transform.SetParent(parent, false);
            var rect = cardObj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = cardObj.AddComponent<Image>();
            img.color = isCurrent ? new Color(0.12f, 0.22f, 0.40f, 0.95f) : new Color(0.06f, 0.08f, 0.14f, 0.88f);

            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = isCurrent ? new Color(1f, 0.85f, 0.1f, 0.95f) : (match.isCompleted ? new Color(0.2f, 0.8f, 0.4f, 0.7f) : new Color(0.3f, 0.4f, 0.6f, 0.5f));
            outline.effectDistance = isCurrent ? new Vector2(2.5f, 2.5f) : new Vector2(1.5f, 1.5f);

            // Título de la tarjeta
            string title = customHeader ?? match.roundName;
            CreateText(cardObj.transform, "Title", new Vector2(0.02f, 0.72f), new Vector2(0.98f, 0.98f),
                title, 11, FontStyle.Bold, isCurrent ? new Color(1f, 0.9f, 0.2f) : Color.white, font);

            // Fighter 1 Row
            CreateFighterRow(cardObj.transform, match.fighter1, match.winner == match.fighter1 && match.isCompleted, match.isCompleted && match.winner != match.fighter1, new Vector2(0.03f, 0.38f), new Vector2(0.97f, 0.68f), font);

            // Fighter 2 Row
            CreateFighterRow(cardObj.transform, match.fighter2, match.winner == match.fighter2 && match.isCompleted, match.isCompleted && match.winner != match.fighter2, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.35f), font);
        }

        private void CreateFighterRow(Transform card, TournamentFighterData fighter, bool isWinner, bool isLoser, Vector2 aMin, Vector2 aMax, Font font)
        {
            GameObject rowObj = new GameObject("FighterRow");
            rowObj.transform.SetParent(card, false);
            var rect = rowObj.AddComponent<RectTransform>();
            rect.anchorMin = aMin;
            rect.anchorMax = aMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = rowObj.AddComponent<Image>();
            img.color = isWinner ? new Color(0.10f, 0.45f, 0.20f, 0.75f) : (isLoser ? new Color(0.35f, 0.10f, 0.10f, 0.65f) : new Color(0.12f, 0.15f, 0.22f, 0.70f));

            string nameStr = fighter != null ? fighter.name : "Por Definir...";
            string tagStr = fighter != null ? (fighter.isHuman ? $"[J{fighter.humanPlayerNumber}]" : "[CPU]") : "";
            string iconStatus = isWinner ? " 👑 ¡GANA!" : (isLoser ? " ✕ KO" : "");

            var txt = CreateText(rowObj.transform, "Name", new Vector2(0.05f, 0f), new Vector2(0.95f, 1f),
                $"{nameStr} <color=#99CCFF>{tagStr}</color><color=#FFDD44>{iconStatus}</color>", 11, FontStyle.Bold, isWinner ? new Color(0.4f, 1f, 0.5f) : (isLoser ? new Color(0.8f, 0.4f, 0.4f) : Color.white), font);
            txt.alignment = TextAnchor.MiddleLeft;
        }

        // =========================================================================
        // UTILIDADES DE CREACIÓN DE UI
        // =========================================================================
        private static Text CreateText(Transform parent, string name, Vector2 aMin, Vector2 aMax, string content, int size, FontStyle style, Color color, Font font)
        {
            GameObject tObj = new GameObject(name);
            tObj.transform.SetParent(parent, false);
            var r = tObj.AddComponent<RectTransform>();
            r.anchorMin = aMin;
            r.anchorMax = aMax;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            var txt = tObj.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 aMin, Vector2 aMax, string label, Color color, Font font, int size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject bObj = new GameObject(name);
            bObj.transform.SetParent(parent, false);
            var r = bObj.AddComponent<RectTransform>();
            r.anchorMin = aMin;
            r.anchorMax = aMax;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;

            var img = bObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var outl = bObj.AddComponent<Outline>();
            outl.effectColor = new Color(1f, 1f, 1f, 0.4f);
            outl.effectDistance = new Vector2(1.5f, 1.5f);

            var btn = bObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(bObj.transform, false);
            var tr = tObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            var txt = tObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.lineSpacing = 1.15f;

            return btn;
        }
    }
}
