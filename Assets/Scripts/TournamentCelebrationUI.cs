using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Pantalla de Coronación y Final de Torneo para Fight Face:
    /// - Porristas animadas celebrando y ondeando pompones dorados y rojos.
    /// - Entrega del Gran Trofeo Dorado del Primer Premio (1er Lugar).
    /// - Lluvia festiva de confeti multicolor.
    /// - Historia y monólogo final exclusivo para cada luchador con su foto de Ganador y nombre personalizado.
    /// - Botones amigables para volver a elegir luchadores o jugar otro torneo.
    /// </summary>
    public class TournamentCelebrationUI : MonoBehaviour
    {
        public static TournamentCelebrationUI Instance { get; private set; }
        public bool IsActive => panelRoot != null && panelRoot.activeSelf;

        private GameObject panelRoot;
        private Image winnerFaceImage;
        private Text winnerNameText;
        private Text fighterMonologueText;
        private RectTransform leftPomPom;
        private RectTransform rightPomPom;
        private RectTransform trophyTransform;
        private Image trophyRays;
        private List<RectTransform> confettiList = new List<RectTransform>();
        private bool isAnimating = false;

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

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return;

            // Atajos directos por teclado o mando para salir de la pantalla de celebración
            if (FightInput.GetConfirm() || FightInput.GetMenuToggle())
            {
                OnChooseFightersClicked();
            }
            else if (FightInput.GetTournamentStart() || FightInput.GetRestart() || FightInput.GetKeyT())
            {
                OnPlayAnotherTournamentClicked();
            }
        }

        public void BuildCelebrationUI(Transform canvasTransform)
        {
            if (panelRoot != null) return;

            panelRoot = new GameObject("Panel_TournamentCelebration");
            panelRoot.transform.SetParent(canvasTransform, false);
            var prRect = panelRoot.AddComponent<RectTransform>();
            prRect.anchorMin = Vector2.zero;
            prRect.anchorMax = Vector2.one;
            prRect.offsetMin = Vector2.zero;
            prRect.offsetMax = Vector2.zero;

            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.10f, 0.98f);
            bg.raycastTarget = true; // Bloquea clics accidentales al ring durante la celebración

            // Rayos dorados de fondo para el trofeo
            GameObject raysObj = new GameObject("GoldenRays");
            raysObj.transform.SetParent(panelRoot.transform, false);
            var ryRect = raysObj.AddComponent<RectTransform>();
            ryRect.anchorMin = new Vector2(0.5f, 0.65f);
            ryRect.anchorMax = new Vector2(0.5f, 0.65f);
            ryRect.pivot = new Vector2(0.5f, 0.5f);
            ryRect.sizeDelta = new Vector2(500, 500);
            trophyRays = raysObj.AddComponent<Image>();
            trophyRays.sprite = CreateRaysSprite();
            trophyRays.color = new Color(1f, 0.85f, 0.2f, 0.18f);
            trophyRays.raycastTarget = false;

            // Encabezado
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(panelRoot.transform, false);
            var hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.05f, 0.87f);
            hRect.anchorMax = new Vector2(0.95f, 0.98f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;
            var hTxt = headerObj.AddComponent<Text>();
            hTxt.text = "🏆 ¡¡¡GRAN CAMPEÓN DEL TORNEO FIGHT FACE!!! 🏆";
            hTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hTxt.fontSize = 28;
            hTxt.fontStyle = FontStyle.Bold;
            hTxt.alignment = TextAnchor.MiddleCenter;
            hTxt.color = new Color(1f, 0.9f, 0.2f);
            hTxt.raycastTarget = false;
            var hOut = headerObj.AddComponent<Outline>();
            hOut.effectColor = new Color(0.9f, 0.2f, 0.0f, 0.9f);
            hOut.effectDistance = new Vector2(2, -2);

            // Subtítulo de ceremonia
            GameObject subObj = new GameObject("SubTitle");
            subObj.transform.SetParent(panelRoot.transform, false);
            var sbRect = subObj.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0.1f, 0.83f);
            sbRect.anchorMax = new Vector2(0.9f, 0.88f);
            sbRect.offsetMin = Vector2.zero;
            sbRect.offsetMax = Vector2.zero;
            var sbTxt = subObj.AddComponent<Text>();
            sbTxt.text = "✨ CEREMONIA DE PREMIACIÓN • ENTREGA DEL 1er LUGAR ✨";
            sbTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sbTxt.fontSize = 15;
            sbTxt.fontStyle = FontStyle.Bold;
            sbTxt.alignment = TextAnchor.MiddleCenter;
            sbTxt.color = new Color(1f, 0.75f, 0.3f);
            sbTxt.raycastTarget = false;

            // ---------------- PORRISTAS (CHEERLEADERS) ----------------
            CreateCheerleader(panelRoot.transform, true, out leftPomPom);
            CreateCheerleader(panelRoot.transform, false, out rightPomPom);

            // ---------------- CENTRO: GRAN TROFEO DORADO ----------------
            GameObject trophyObj = new GameObject("GoldenTrophy");
            trophyObj.transform.SetParent(panelRoot.transform, false);
            trophyTransform = trophyObj.AddComponent<RectTransform>();
            trophyTransform.anchorMin = new Vector2(0.5f, 0.65f);
            trophyTransform.anchorMax = new Vector2(0.5f, 0.65f);
            trophyTransform.pivot = new Vector2(0.5f, 0.5f);
            trophyTransform.sizeDelta = new Vector2(170, 200);

            var trImg = trophyObj.AddComponent<Image>();
            trImg.sprite = CreateGoldenTrophySprite();
            trImg.color = Color.white;
            trImg.raycastTarget = false;

            // ---------------- FOTO GANADOR EN MEDALLÓN Y NOMBRE ----------------
            GameObject faceCardObj = new GameObject("WinnerFaceCard");
            faceCardObj.transform.SetParent(panelRoot.transform, false);
            var fcRect = faceCardObj.AddComponent<RectTransform>();
            fcRect.anchorMin = new Vector2(0.5f, 0.40f);
            fcRect.anchorMax = new Vector2(0.5f, 0.40f);
            fcRect.pivot = new Vector2(0.5f, 0.5f);
            fcRect.sizeDelta = new Vector2(120, 150);

            var fcBg = faceCardObj.AddComponent<Image>();
            fcBg.color = new Color(0.12f, 0.16f, 0.28f, 0.95f);
            fcBg.raycastTarget = false;
            var fcOut = faceCardObj.AddComponent<Outline>();
            fcOut.effectColor = new Color(1f, 0.85f, 0.2f);
            fcOut.effectDistance = new Vector2(4, 4);

            // Sub-objeto dedicado para la foto del ganador (evita conflicto DisallowMultipleComponent con Image en faceCardObj)
            GameObject faceImgObj = new GameObject("WinnerFaceImage");
            faceImgObj.transform.SetParent(faceCardObj.transform, false);
            var fiRect = faceImgObj.AddComponent<RectTransform>();
            fiRect.anchorMin = Vector2.zero;
            fiRect.anchorMax = Vector2.one;
            fiRect.offsetMin = new Vector2(4, 4);
            fiRect.offsetMax = new Vector2(-4, -4);

            winnerFaceImage = faceImgObj.AddComponent<Image>();
            winnerFaceImage.preserveAspect = true;
            winnerFaceImage.raycastTarget = false;

            // Nombre del Campeón
            GameObject nameObj = new GameObject("WinnerName");
            nameObj.transform.SetParent(panelRoot.transform, false);
            var nmRect = nameObj.AddComponent<RectTransform>();
            nmRect.anchorMin = new Vector2(0.2f, 0.27f);
            nmRect.anchorMax = new Vector2(0.8f, 0.33f);
            nmRect.offsetMin = Vector2.zero;
            nmRect.offsetMax = Vector2.zero;
            winnerNameText = nameObj.AddComponent<Text>();
            winnerNameText.text = "¡EL CAMPEÓN!";
            winnerNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            winnerNameText.fontSize = 22;
            winnerNameText.fontStyle = FontStyle.Bold;
            winnerNameText.alignment = TextAnchor.MiddleCenter;
            winnerNameText.color = new Color(1f, 0.95f, 0.4f);
            winnerNameText.raycastTarget = false;

            // Monólogo / Final del luchador
            GameObject monoObj = new GameObject("MonologueBox");
            monoObj.transform.SetParent(panelRoot.transform, false);
            var mnRect = monoObj.AddComponent<RectTransform>();
            mnRect.anchorMin = new Vector2(0.15f, 0.15f);
            mnRect.anchorMax = new Vector2(0.85f, 0.26f);
            mnRect.offsetMin = Vector2.zero;
            mnRect.offsetMax = Vector2.zero;
            var mnBg = monoObj.AddComponent<Image>();
            mnBg.color = new Color(0.08f, 0.10f, 0.18f, 0.9f);
            mnBg.raycastTarget = false;
            var mnOut = monoObj.AddComponent<Outline>();
            mnOut.effectColor = new Color(0.3f, 0.7f, 1f, 0.7f);
            mnOut.effectDistance = new Vector2(2, 2);

            GameObject mnTxtObj = new GameObject("Text");
            mnTxtObj.transform.SetParent(monoObj.transform, false);
            var mntRect = mnTxtObj.AddComponent<RectTransform>();
            mntRect.anchorMin = Vector2.zero;
            mntRect.anchorMax = Vector2.one;
            mntRect.offsetMin = new Vector2(16, 8);
            mntRect.offsetMax = new Vector2(-16, -8);
            fighterMonologueText = mnTxtObj.AddComponent<Text>();
            fighterMonologueText.text = "¡Al fin el trofeo de primer lugar! ¡La fiesta va por cuenta de la casa!";
            fighterMonologueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fighterMonologueText.fontSize = 15;
            fighterMonologueText.fontStyle = FontStyle.BoldAndItalic;
            fighterMonologueText.alignment = TextAnchor.MiddleCenter;
            fighterMonologueText.color = Color.white;
            fighterMonologueText.raycastTarget = false;

            // ---------------- BOTONES INFERIORES AMIGABLES ----------------
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(panelRoot.transform, false);
            var brRect = btnRow.AddComponent<RectTransform>();
            brRect.anchorMin = new Vector2(0.12f, 0.03f);
            brRect.anchorMax = new Vector2(0.88f, 0.13f);
            brRect.offsetMin = Vector2.zero;
            brRect.offsetMax = Vector2.zero;

            // Botón 1: Volver a Elegir Luchadores
            CreateActionButton(btnRow.transform, "ELEGIR NUEVOS LUCHADORES", new Color(0.18f, 0.52f, 0.95f), new Vector2(0f, 0f), new Vector2(0.48f, 1f), () =>
            {
                OnChooseFightersClicked();
            });

            // Botón 2: Jugar Otro Torneo
            CreateActionButton(btnRow.transform, "JUGAR OTRO TORNEO", new Color(0.95f, 0.60f, 0.08f), new Vector2(0.52f, 0f), new Vector2(1f, 1f), () =>
            {
                OnPlayAnotherTournamentClicked();
            });

            // Generar piezas de confeti
            CreateConfettiParticles(panelRoot.transform);

            // CRÍTICO: Asegurar que los botones queden sobre el confeti y no sean bloqueados
            btnRow.transform.SetAsLastSibling();

            panelRoot.SetActive(false);
        }

        private void CreateCheerleader(Transform parent, bool isLeft, out RectTransform pomPomTransform)
        {
            GameObject cheerObj = new GameObject(isLeft ? "Cheerleader_Left" : "Cheerleader_Right");
            cheerObj.transform.SetParent(parent, false);
            var cRect = cheerObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(isLeft ? 0.05f : 0.80f, 0.30f);
            cRect.anchorMax = new Vector2(isLeft ? 0.20f : 0.95f, 0.80f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;

            var cImg = cheerObj.AddComponent<Image>();
            cImg.sprite = CreateCheerleaderSprite(isLeft);
            cImg.color = Color.white;
            cImg.raycastTarget = false;

            // Pompón brillante
            GameObject pomObj = new GameObject("PomPom");
            pomObj.transform.SetParent(cheerObj.transform, false);
            pomPomTransform = pomObj.AddComponent<RectTransform>();
            pomPomTransform.anchorMin = new Vector2(0.5f, 0.85f);
            pomPomTransform.anchorMax = new Vector2(0.5f, 0.85f);
            pomPomTransform.pivot = new Vector2(0.5f, 0.5f);
            pomPomTransform.sizeDelta = new Vector2(75, 75);

            var pomImg = pomObj.AddComponent<Image>();
            pomImg.sprite = CreatePomPomSprite();
            pomImg.color = new Color(1f, 0.85f, 0.15f);
            pomImg.raycastTarget = false;

            // Banner porrista
            GameObject banObj = new GameObject("CheerBanner");
            banObj.transform.SetParent(cheerObj.transform, false);
            var bnRect = banObj.AddComponent<RectTransform>();
            bnRect.anchorMin = new Vector2(-0.2f, 0.05f);
            bnRect.anchorMax = new Vector2(1.2f, 0.20f);
            bnRect.offsetMin = Vector2.zero;
            bnRect.offsetMax = Vector2.zero;
            var bnTxt = banObj.AddComponent<Text>();
            bnTxt.text = isLeft ? "🎉 ¡1er LUGAR! 🎉" : "✨ ¡CAMPEÓN! ✨";
            bnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bnTxt.fontSize = 13;
            bnTxt.fontStyle = FontStyle.Bold;
            bnTxt.alignment = TextAnchor.MiddleCenter;
            bnTxt.color = new Color(1f, 0.9f, 0.3f);
            bnTxt.raycastTarget = false;
        }

        private void CreateActionButton(Transform parent, string label, Color bgColor, Vector2 aMin, Vector2 aMax, Action onClick)
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
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var tRect = txtObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false; // CRÍTICO: el texto no debe bloquear el raycast del botón
        }

        private void CreateConfettiParticles(Transform parent)
        {
            confettiList.Clear();
            Color[] colors = {
                new Color(1f, 0.2f, 0.3f),
                new Color(1f, 0.85f, 0.1f),
                new Color(0.2f, 0.8f, 1f),
                new Color(0.3f, 1f, 0.4f),
                new Color(0.9f, 0.3f, 1f),
                new Color(1f, 1f, 1f)
            };

            for (int i = 0; i < 36; i++)
            {
                GameObject confObj = new GameObject($"Confetti_{i}");
                confObj.transform.SetParent(parent, false);
                var cRect = confObj.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(UnityEngine.Random.Range(0.05f, 0.95f), 1f);
                cRect.anchorMax = cRect.anchorMin;
                cRect.sizeDelta = new Vector2(UnityEngine.Random.Range(10, 20), UnityEngine.Random.Range(8, 16));
                cRect.anchoredPosition = new Vector2(0, UnityEngine.Random.Range(0, 400));

                var cImg = confObj.AddComponent<Image>();
                cImg.color = colors[i % colors.Length];
                cImg.raycastTarget = false; // El confeti es puramente visual, no debe interceptar raycasts
                confettiList.Add(cRect);
            }
        }

        public void ShowCelebration(FighterController winner)
        {
            if (panelRoot == null && BattleUI.Instance != null)
            {
                BuildCelebrationUI(BattleUI.Instance.transform);
            }

            if (panelRoot == null) return;

            // Traer el modal de celebración al frente absoluto de la pantalla
            panelRoot.transform.SetAsLastSibling();
            var btnRow = panelRoot.transform.Find("ButtonRow");
            if (btnRow != null)
            {
                btnRow.SetAsLastSibling();
            }
            panelRoot.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1.0f;

            string fighterName = winner != null ? winner.fighterName : "El Campeón";
            if (winnerNameText != null)
            {
                winnerNameText.text = $"👑 {fighterName.ToUpper()} 👑";
            }

            // Asignar Foto Ganador en el marco con fallback robusto
            if (winnerFaceImage != null)
            {
                winnerFaceImage.color = Color.white;
                Sprite winnerSprite = null;

                if (winner != null && winner.faceController != null && winner.faceController.Profile != null)
                {
                    winnerSprite = winner.faceController.Profile.GetSprite(FaceType.Ganador);
                    if (winnerSprite == null)
                    {
                        winnerSprite = winner.faceController.Profile.GetSprite(FaceType.Base);
                    }
                }

                if (winnerSprite == null && BattleManager.Instance != null)
                {
                    int p1Id = (int)BattleManager.Instance.p1BodyType + 1;
                    var prof = FaceLoader.LoadProfileForFighter(p1Id, fighterName, 0);
                    if (prof != null)
                    {
                        winnerSprite = prof.GetSprite(FaceType.Ganador) ?? prof.GetSprite(FaceType.Base);
                    }
                }

                if (winnerSprite != null)
                {
                    winnerFaceImage.sprite = winnerSprite;
                    winnerFaceImage.enabled = true;
                }
            }

            // Monólogo de victoria personalizado según el personaje
            if (fighterMonologueText != null)
            {
                int fighterId = 1;
                if (BattleManager.Instance != null)
                {
                    fighterId = (int)BattleManager.Instance.p1BodyType + 1;
                }
                fighterMonologueText.text = $"\"{GetVictoryMonologue(fighterId, fighterName)}\"";
            }

            // Reproducir voz de victoria
            if (winner != null)
            {
                winner.PlayVoice(VoiceType.Victory, 1.0f);
            }

            if (!isAnimating)
            {
                StartCoroutine(AnimateCelebrationRoutine());
            }
        }

        public void CloseCelebration()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            isAnimating = false;
            Time.timeScale = 1.0f;
        }

        public void OnChooseFightersClicked()
        {
            CloseCelebration();

            if (TournamentManager.Instance != null)
            {
                TournamentManager.Instance.isTournamentModeActive = false;
                TournamentManager.Instance.currentRound = 1;
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowCharacterSelectMenu();
            }
        }

        public void OnPlayAnotherTournamentClicked()
        {
            CloseCelebration();

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.HideCharacterSelectMenu();
            }

            if (TournamentManager.Instance != null)
            {
                int p1Id = TournamentManager.Instance.selectedPlayer1Id;
                if (p1Id <= 0 && BattleManager.Instance != null && BattleManager.Instance.player1 != null)
                {
                    p1Id = (int)BattleManager.Instance.p1BodyType + 1;
                }
                if (p1Id <= 0) p1Id = 1;

                TournamentManager.Instance.StartNewTournament(p1Id);
            }
        }

        private IEnumerator AnimateCelebrationRoutine()
        {
            isAnimating = true;
            float t = 0;

            while (panelRoot != null && panelRoot.activeSelf)
            {
                t += Time.deltaTime;

                // Rotar rayos dorados
                if (trophyRays != null)
                {
                    trophyRays.transform.Rotate(0, 0, 25f * Time.deltaTime);
                }

                // Balanceo festivo del gran trofeo
                if (trophyTransform != null)
                {
                    float trophyScale = 1.0f + Mathf.Sin(t * 3f) * 0.05f;
                    trophyTransform.localScale = new Vector3(trophyScale, trophyScale, 1f);
                }

                // Ondeo de pompones de las porristas
                if (leftPomPom != null)
                {
                    leftPomPom.anchoredPosition = new Vector2(Mathf.Sin(t * 6f) * 12f, 15f + Mathf.Cos(t * 7f) * 15f);
                    leftPomPom.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 8f) * 20f);
                }

                if (rightPomPom != null)
                {
                    rightPomPom.anchoredPosition = new Vector2(-Mathf.Sin(t * 6f) * 12f, 15f + Mathf.Cos(t * 7f) * 15f);
                    rightPomPom.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(t * 8f) * 20f);
                }

                // Caída de confeti
                for (int i = 0; i < confettiList.Count; i++)
                {
                    var conf = confettiList[i];
                    if (conf == null) continue;
                    Vector2 pos = conf.anchoredPosition;
                    pos.y -= (60f + (i * 4f)) * Time.deltaTime;
                    pos.x += Mathf.Sin(t * 4f + i) * 1.5f;

                    if (pos.y < -750f)
                    {
                        pos.y = 50f;
                    }
                    conf.anchoredPosition = pos;
                    conf.Rotate(0, 0, 120f * Time.deltaTime);
                }

                yield return null;
            }

            isAnimating = false;
        }

        private string GetVictoryMonologue(int fighterId, string name)
        {
            switch (fighterId)
            {
                case 1: // El Gordo
                    return $"¡¡¡CAMPEÓN DEL MUNDO!!! ¡Este trofeo de oro macizo pesa como 40 kilos! ¡Se acabaron las dietas, invito 50 pizzas a todo el público!";
                case 2: // El Flaco
                    return $"¡Demostré que la agilidad y los reflejos superan cualquier fuerza bruta! ¡El cheque de campeón va directo a mi dojo! ¡Gracias a las porristas por los aplausos!";
                case 3: // El Musculoso
                    return $"¡Poder imparable y victoria indiscutible! ¡El entrenamiento extremo rindió sus frutos! ¡Miren cómo brilla esta copa dorada en mis brazos!";
                case 4: // La Mujer
                    return $"¡Elegancia, precisión milimétrica y patadas de relámpago! ¡Las porristas y el ring entero coronan a la verdadera reina del torneo!";
                case 5: // La Guerrera
                    return $"¡El espíritu y la disciplina del combate triunfaron en el torneo! ¡Dedico este primer premio a todos los que creyeron en mi fuerza!";
                case 6: // Hombre-Máquina
                    return $"¡CÁLCULO FINAL DE BATALLA: 100% VICTORIA! ¡TROFEO DE ORO ALMACENADO CON ÉXITO! ¡SISTEMAS SOBRECALENTADOS DE PURA FELICIDAD!";
                case 7: // Playero
                    return $"¡El rey de las olas y del ring! ¡Este trofeo de oro combina perfecto con mis gafas de sol y mi patito salvavidas! ¡Nos vemos en la playa a celebrar!";
                case 8: // Oficinista
                    return $"¡Objetivo trimestral completado con 100% de éxito! ¡Mi maletín ahora llevará este glorioso trofeo de campeón! ¡El lunes renuncio y me dedico al boxeo!";
                case 9: // Pijamero
                    return $"¡Zzz... ¿gané? ¡Mis pantuflas de conejo resistieron hasta el final y somos campeones! ¡Ahora una siesta de victoria de 14 horas seguidas!";
                default:
                    return $"¡{name.ToUpper()} ha conquistado la cima de Fight Face! ¡Un campeón absoluto e histórico para la posteridad!";
            }
        }

        // ================= PROCEDURAL SPRITES =================
        private Sprite CreateGoldenTrophySprite()
        {
            int w = 128, h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 empty = new Color32(0, 0, 0, 0);

            Color32 goldBright = new Color32(255, 230, 80, 255);
            Color32 goldMid = new Color32(230, 180, 30, 255);
            Color32 goldDark = new Color32(180, 120, 10, 255);
            Color32 baseBrown = new Color32(80, 45, 15, 255);
            Color32 gemRuby = new Color32(230, 30, 60, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    pixels[idx] = empty;
                    float nx = (x - w / 2f) / (w / 2f); // -1 a 1
                    float ny = y / (float)h;           // 0 a 1

                    // Base del trofeo (madera y pedestal de oro)
                    if (y < 24)
                    {
                        if (Mathf.Abs(nx) < 0.70f)
                        {
                            pixels[idx] = (y < 12) ? baseBrown : goldMid;
                        }
                    }
                    // Tallo de la copa
                    else if (y < 60)
                    {
                        if (Mathf.Abs(nx) < 0.18f)
                        {
                            pixels[idx] = (Mathf.Abs(nx) < 0.08f) ? goldBright : goldMid;
                        }
                    }
                    // Copa dorada
                    else if (y < 135)
                    {
                        float cupR = 0.20f + (ny - 0.38f) * 0.90f;
                        if (Mathf.Abs(nx) < cupR)
                        {
                            // Brillo degradado
                            if (nx < -cupR * 0.5f) pixels[idx] = goldDark;
                            else if (nx < 0f) pixels[idx] = goldBright;
                            else pixels[idx] = goldMid;
                        }

                        // Asas del trofeo
                        float handleDist = Mathf.Abs(Mathf.Abs(nx) - (cupR + 0.18f));
                        if (handleDist < 0.08f && ny > 0.45f && ny < 0.80f)
                        {
                            pixels[idx] = goldBright;
                        }
                    }
                    // Estrella / Gema superior
                    else if (y < 155)
                    {
                        if (Mathf.Abs(nx) < 0.30f && Mathf.Abs(ny - 0.90f) < 0.07f)
                        {
                            pixels[idx] = gemRuby;
                        }
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateCheerleaderSprite(bool isLeft)
        {
            int w = 120, h = 240;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 empty = new Color32(0, 0, 0, 0);

            Color32 skin = new Color32(255, 215, 175, 255);
            Color32 hair = isLeft ? new Color32(180, 80, 20, 255) : new Color32(50, 40, 45, 255);
            Color32 uniformRed = new Color32(230, 40, 60, 255);
            Color32 uniformWhite = new Color32(245, 245, 250, 255);
            Color32 socks = new Color32(240, 240, 255, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    pixels[idx] = empty;
                    float nx = (x - w / 2f) / (w / 2f);
                    float ny = y / (float)h;

                    // Cabeza & Cabello
                    if (ny > 0.78f && ny < 0.95f && Mathf.Abs(nx) < 0.40f)
                    {
                        pixels[idx] = (ny > 0.88f || Mathf.Abs(nx) > 0.28f) ? hair : skin;
                    }
                    // Torso & Blusa deportiva
                    else if (ny > 0.52f && ny <= 0.78f && Mathf.Abs(nx) < 0.35f)
                    {
                        pixels[idx] = (ny > 0.65f) ? uniformRed : uniformWhite;
                    }
                    // Falda de porrista con tablas
                    else if (ny > 0.36f && ny <= 0.52f)
                    {
                        float skirtW = 0.32f + (0.52f - ny) * 0.8f;
                        if (Mathf.Abs(nx) < skirtW)
                        {
                            pixels[idx] = ((x / 6) % 2 == 0) ? uniformRed : uniformWhite;
                        }
                    }
                    // Piernas
                    else if (ny > 0.08f && ny <= 0.36f)
                    {
                        if (Mathf.Abs(nx - 0.16f) < 0.09f || Mathf.Abs(nx + 0.16f) < 0.09f)
                        {
                            pixels[idx] = (ny < 0.22f) ? socks : skin;
                        }
                    }
                    // Zapatillas
                    else if (ny <= 0.08f && (Mathf.Abs(nx - 0.16f) < 0.12f || Mathf.Abs(nx + 0.16f) < 0.12f))
                    {
                        pixels[idx] = uniformRed;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreatePomPomSprite()
        {
            int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            Color32 goldBright = new Color32(255, 235, 90, 255);
            Color32 goldDark = new Color32(215, 140, 20, 255);
            Color32 red = new Color32(240, 50, 70, 255);
            Color32 empty = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    if (r <= 0.95f)
                    {
                        // Textura irregular de pom-pón con tiras
                        int pattern = (x * 7 + y * 13) % 11;
                        if (pattern < 5) pixels[y * size + x] = goldBright;
                        else if (pattern < 8) pixels[y * size + x] = goldDark;
                        else pixels[y * size + x] = red;
                    }
                    else
                    {
                        pixels[y * size + x] = empty;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRaysSprite()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            Color32 rayColor = new Color32(255, 220, 50, 200);
            Color32 empty = new Color32(0, 0, 0, 0);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f;
                    float dy = y - size / 2f;
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f; // 0 a 360
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);

                    if (r <= 1f && ((int)(angle / 15f) % 2 == 0))
                    {
                        pixels[y * size + x] = rayColor;
                    }
                    else
                    {
                        pixels[y * size + x] = empty;
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
