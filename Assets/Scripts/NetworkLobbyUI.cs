using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FightFace
{
    /// <summary>
    /// Interfaz visual para el emparejamiento Multijugador en Red Local (Wi-Fi / Hotspot):
    /// - Crear sala (Host / Jugador 1) con Código Rápido de 1 o 2 dígitos.
    /// - Descubrimiento automático de salas en la misma red Wi-Fi (Cliente / Jugador 2) con unirse en 1 clic.
    /// - Conexión ultra sencilla por Código Rápido (PIN) o IP directa sin complicaciones técnicas.
    /// </summary>
    public class NetworkLobbyUI : MonoBehaviour
    {
        public static NetworkLobbyUI Instance { get; private set; }

        private GameObject lobbyModalPanel;
        private Text statusText;
        private Transform roomListContainer;
        private InputField ipInputField;
        private GameObject hostWaitingBox;
        private Text hostInfoText;
        private GameObject clientScanBox;
        private Font arcadeFont;

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

        public void BuildLobbyUI(Transform canvas)
        {
            if (lobbyModalPanel != null) return;

            arcadeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            lobbyModalPanel = new GameObject("Modal_NetworkLobby");
            lobbyModalPanel.transform.SetParent(canvas, false);
            var rect = lobbyModalPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.06f);
            rect.anchorMax = new Vector2(0.94f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = lobbyModalPanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.12f, 0.98f);
            var outline = lobbyModalPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.85f, 1f, 0.9f);
            outline.effectDistance = new Vector2(3, 3);

            // Cabecera
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(lobbyModalPanel.transform, false);
            var hRect = headerObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 0.86f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.offsetMin = Vector2.zero;
            hRect.offsetMax = Vector2.zero;
            var hImg = headerObj.AddComponent<Image>();
            hImg.color = new Color(0.08f, 0.18f, 0.35f, 0.9f);

            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            var tRect = titleObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var tText = titleObj.AddComponent<Text>();
            tText.text = "📶 MULTIJUGADOR EN RED LOCAL (WI-FI / 2 CELULARES O PC)";
            tText.font = arcadeFont;
            tText.fontSize = 19;
            tText.fontStyle = FontStyle.Bold;
            tText.alignment = TextAnchor.MiddleCenter;
            tText.color = new Color(0.2f, 0.9f, 1f);

            // Botón Cerrar (X)
            GameObject closeBtnObj = new GameObject("Btn_Close");
            closeBtnObj.transform.SetParent(headerObj.transform, false);
            var cRect = closeBtnObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.92f, 0.15f);
            cRect.anchorMax = new Vector2(0.985f, 0.85f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            var cImg = closeBtnObj.AddComponent<Image>();
            cImg.color = new Color(0.85f, 0.2f, 0.2f);
            var cBtn = closeBtnObj.AddComponent<Button>();
            cBtn.onClick.AddListener(CloseLobby);

            GameObject cTxtObj = new GameObject("Text");
            cTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var ctRect = cTxtObj.AddComponent<RectTransform>();
            ctRect.anchorMin = Vector2.zero;
            ctRect.anchorMax = Vector2.one;
            ctRect.sizeDelta = Vector2.zero;
            var ctTxt = cTxtObj.AddComponent<Text>();
            ctTxt.text = "✕";
            ctTxt.font = arcadeFont;
            ctTxt.fontSize = 18;
            ctTxt.fontStyle = FontStyle.Bold;
            ctTxt.alignment = TextAnchor.MiddleCenter;
            ctTxt.color = Color.white;
            ctTxt.raycastTarget = false;

            // Panel de instrucciones / Estado
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(lobbyModalPanel.transform, false);
            var stRect = statusObj.AddComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0.04f, 0.77f);
            stRect.anchorMax = new Vector2(0.96f, 0.85f);
            stRect.offsetMin = Vector2.zero;
            stRect.offsetMax = Vector2.zero;
            statusText = statusObj.AddComponent<Text>();
            statusText.text = "Ambos dispositivos deben estar en la misma red Wi-Fi o Zona Portátil (Hotspot).";
            statusText.font = arcadeFont;
            statusText.fontSize = 13;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(0.8f, 0.9f, 1f);

            // Botones de Acción Principales: [CREAR SALA] | [BUSCAR SALAS]
            float btnWidth = 0.44f;
            CreateMenuButton(lobbyModalPanel.transform, "Btn_Host", new Vector2(0.05f, 0.65f), new Vector2(0.05f + btnWidth, 0.75f),
                "👑 CREAR SALA (HOST - JUGADOR 1)", new Color(0.15f, 0.65f, 0.35f), () => OnClickCreateHost(arcadeFont));

            CreateMenuButton(lobbyModalPanel.transform, "Btn_Scan", new Vector2(0.51f, 0.65f), new Vector2(0.51f + btnWidth, 0.75f),
                "🔍 BUSCAR SALAS (CLIENTE - JUGADOR 2)", new Color(0.18f, 0.50f, 0.90f), () => OnClickScanRooms(arcadeFont));

            // Caja de espera de Host
            hostWaitingBox = new GameObject("HostWaitingBox");
            hostWaitingBox.transform.SetParent(lobbyModalPanel.transform, false);
            var hwRect = hostWaitingBox.AddComponent<RectTransform>();
            hwRect.anchorMin = new Vector2(0.05f, 0.08f);
            hwRect.anchorMax = new Vector2(0.95f, 0.62f);
            hwRect.offsetMin = Vector2.zero;
            hwRect.offsetMax = Vector2.zero;
            var hwImg = hostWaitingBox.AddComponent<Image>();
            hwImg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);
            var hwOutline = hostWaitingBox.AddComponent<Outline>();
            hwOutline.effectColor = new Color(0.2f, 0.8f, 0.4f, 0.8f);

            GameObject hwTxtObj = new GameObject("HostInfoText");
            hwTxtObj.transform.SetParent(hostWaitingBox.transform, false);
            var hwtRect = hwTxtObj.AddComponent<RectTransform>();
            hwtRect.anchorMin = new Vector2(0.05f, 0.22f);
            hwtRect.anchorMax = new Vector2(0.95f, 0.92f);
            hwtRect.offsetMin = Vector2.zero;
            hwtRect.offsetMax = Vector2.zero;
            hostInfoText = hwTxtObj.AddComponent<Text>();
            hostInfoText.font = arcadeFont;
            hostInfoText.fontSize = 16;
            hostInfoText.lineSpacing = 1.35f;
            hostInfoText.alignment = TextAnchor.MiddleCenter;
            hostInfoText.color = Color.white;

            CreateMenuButton(hostWaitingBox.transform, "Btn_CancelHost", new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.19f),
                "CANCELAR SALA", new Color(0.85f, 0.25f, 0.25f), () =>
                {
                    if (NetworkFightManager.Instance != null) NetworkFightManager.Instance.Disconnect();
                    hostWaitingBox.SetActive(false);
                    statusText.text = "Sala cancelada.";
                });

            hostWaitingBox.SetActive(false);

            // Caja de escaneo de Cliente
            clientScanBox = new GameObject("ClientScanBox");
            clientScanBox.transform.SetParent(lobbyModalPanel.transform, false);
            var csRect = clientScanBox.AddComponent<RectTransform>();
            csRect.anchorMin = new Vector2(0.05f, 0.08f);
            csRect.anchorMax = new Vector2(0.95f, 0.62f);
            csRect.offsetMin = Vector2.zero;
            csRect.offsetMax = Vector2.zero;
            var csImg = clientScanBox.AddComponent<Image>();
            csImg.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);
            var csOutline = clientScanBox.AddComponent<Outline>();
            csOutline.effectColor = new Color(0.2f, 0.5f, 1f, 0.8f);

            // Contenedor de lista de salas encontradas
            GameObject listScroll = new GameObject("RoomListContainer");
            listScroll.transform.SetParent(clientScanBox.transform, false);
            var lsRect = listScroll.AddComponent<RectTransform>();
            lsRect.anchorMin = new Vector2(0.03f, 0.28f);
            lsRect.anchorMax = new Vector2(0.97f, 0.94f);
            lsRect.offsetMin = Vector2.zero;
            lsRect.offsetMax = Vector2.zero;
            roomListContainer = listScroll.transform;

            // Entrada simplificada: Código Rápido o IP
            GameObject ipBox = new GameObject("DirectIpBox");
            ipBox.transform.SetParent(clientScanBox.transform, false);
            var ipbRect = ipBox.AddComponent<RectTransform>();
            ipbRect.anchorMin = new Vector2(0.03f, 0.04f);
            ipbRect.anchorMax = new Vector2(0.97f, 0.24f);
            ipbRect.offsetMin = Vector2.zero;
            ipbRect.offsetMax = Vector2.zero;

            GameObject ipInpObj = new GameObject("Input_IP");
            ipInpObj.transform.SetParent(ipBox.transform, false);
            var ipiRect = ipInpObj.AddComponent<RectTransform>();
            ipiRect.anchorMin = new Vector2(0f, 0.08f);
            ipiRect.anchorMax = new Vector2(0.68f, 0.92f);
            ipiRect.offsetMin = Vector2.zero;
            ipiRect.offsetMax = Vector2.zero;
            var ipImg = ipInpObj.AddComponent<Image>();
            ipImg.color = new Color(0.15f, 0.18f, 0.28f);

            GameObject ipPlaceObj = new GameObject("Placeholder");
            ipPlaceObj.transform.SetParent(ipInpObj.transform, false);
            var ippRect = ipPlaceObj.AddComponent<RectTransform>();
            ippRect.anchorMin = Vector2.zero;
            ippRect.anchorMax = Vector2.one;
            ippRect.offsetMin = new Vector2(10, 0);
            ippRect.offsetMax = new Vector2(-10, 0);
            var ippTxt = ipPlaceObj.AddComponent<Text>();
            ippTxt.text = "Código de tu amigo (ej: 2) o IP...";
            ippTxt.font = arcadeFont;
            ippTxt.fontSize = 13;
            ippTxt.color = new Color(0.6f, 0.7f, 0.8f, 0.7f);
            ippTxt.alignment = TextAnchor.MiddleLeft;

            GameObject ipTextObj = new GameObject("Text");
            ipTextObj.transform.SetParent(ipInpObj.transform, false);
            var iptRect = ipTextObj.AddComponent<RectTransform>();
            iptRect.anchorMin = Vector2.zero;
            iptRect.anchorMax = Vector2.one;
            iptRect.offsetMin = new Vector2(10, 0);
            iptRect.offsetMax = new Vector2(-10, 0);
            var iptTxt = ipTextObj.AddComponent<Text>();
            iptTxt.font = arcadeFont;
            iptTxt.fontSize = 14;
            iptTxt.color = Color.white;
            iptTxt.alignment = TextAnchor.MiddleLeft;

            ipInputField = ipInpObj.AddComponent<InputField>();
            ipInputField.textComponent = iptTxt;
            ipInputField.placeholder = ippTxt;
            ipInputField.text = "";

            CreateMenuButton(ipBox.transform, "Btn_DirectConnect", new Vector2(0.70f, 0.08f), new Vector2(1f, 0.92f),
                "⚡ ¡UNIRME!", new Color(0.18f, 0.70f, 0.35f), OnClickDirectConnect);

            clientScanBox.SetActive(false);

            lobbyModalPanel.SetActive(false);
        }

        public void OpenLobby()
        {
            if (lobbyModalPanel == null) return;

            lobbyModalPanel.transform.SetAsLastSibling();
            lobbyModalPanel.SetActive(true);

            if (NetworkFightManager.Instance != null)
            {
                NetworkFightManager.Instance.onClientConnected = OnCombatReady;
                NetworkFightManager.Instance.onConnectedToHost = OnCombatReady;
                NetworkFightManager.Instance.onRoomsUpdated = () => RefreshRoomListUI(arcadeFont);
            }
        }

        public void CloseLobby()
        {
            if (lobbyModalPanel != null)
            {
                lobbyModalPanel.SetActive(false);
            }
            StopAllCoroutines();
        }

        public bool IsLobbyOpen()
        {
            return lobbyModalPanel != null && lobbyModalPanel.activeSelf;
        }

        public void ToggleLobby()
        {
            if (IsLobbyOpen())
            {
                CloseLobby();
            }
            else
            {
                OpenLobby();
            }
        }

        private void OnClickCreateHost(Font font)
        {
            clientScanBox.SetActive(false);
            hostWaitingBox.SetActive(true);

            string myIp = NetworkFightManager.GetLocalIPAddress();
            string myPin = NetworkFightManager.GetQuickRoomCode();
            string myName = SystemInfo.deviceName;
            FighterBodyType body = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.p1BodyType : FighterBodyType.Musculoso;
            StageType stage = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.selectedStage : StageType.MGMGrand;

            hostInfoText.text = $"👑 <b>SALA CREADA: {myName.ToUpper()}</b>\n\n" +
                               $"🔑 CÓDIGO RÁPIDO DE SALA: <size=26><color=#00FFAA><b>[ {myPin} ]</b></color></size>\n\n" +
                               $"Dile a tu amigo que toque <b>'BUSCAR SALAS'</b> o ingrese el código <b>{myPin}</b>.\n" +
                               $"<size=11><color=#88AACC>(IP Local Wi-Fi: {myIp})</color></size>\n\n" +
                               $"⏳ <i>Transmitiendo en la red Wi-Fi... Esperando al rival...</i>";

            statusText.text = "¡Sala transmitiendo en Wi-Fi! El jugador 2 puede unirse ahora con 1 toque.";

            NetworkFightManager.Instance.StartHost(myName, body, stage);
        }

        private void OnClickScanRooms(Font font)
        {
            hostWaitingBox.SetActive(false);
            clientScanBox.SetActive(true);

            statusText.text = "🔍 Buscando salas disponibles en tu Wi-Fi...";

            NetworkFightManager.Instance.StartScanningRooms();
            StopAllCoroutines();
            StartCoroutine(UpdateDiscoveredRoomsLoop(font));
        }

        private System.Collections.IEnumerator UpdateDiscoveredRoomsLoop(Font font)
        {
            while (clientScanBox != null && clientScanBox.activeInHierarchy)
            {
                RefreshRoomListUI(font);
                yield return new WaitForSecondsRealtime(0.75f);
            }
        }

        private void RefreshRoomListUI(Font font)
        {
            if (roomListContainer == null) return;

            // Limpiar anteriores
            foreach (Transform child in roomListContainer)
            {
                Destroy(child.gameObject);
            }

            var rooms = NetworkFightManager.Instance != null ? NetworkFightManager.Instance.discoveredRooms : null;
            if (rooms == null || rooms.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyNotice");
                emptyObj.transform.SetParent(roomListContainer, false);
                var er = emptyObj.AddComponent<RectTransform>();
                er.anchorMin = Vector2.zero;
                er.anchorMax = Vector2.one;
                er.sizeDelta = Vector2.zero;
                var et = emptyObj.AddComponent<Text>();
                et.text = "🔍 Buscando salas en el Wi-Fi...\n\n(Asegúrate de que tu amigo haya tocado 'CREAR SALA'\no ingresa abajo su Código de Sala)";
                et.font = font;
                et.fontSize = 14;
                et.lineSpacing = 1.3f;
                et.alignment = TextAnchor.MiddleCenter;
                et.color = new Color(0.7f, 0.85f, 1f, 0.85f);
                return;
            }

            statusText.text = "🟢 ¡Sala encontrada en tu Wi-Fi! Toca el botón abajo para jugar al instante:";

            float yOffset = 0f;
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                GameObject rObj = new GameObject($"Room_{i}");
                rObj.transform.SetParent(roomListContainer, false);
                var rRect = rObj.AddComponent<RectTransform>();
                rRect.anchorMin = new Vector2(0f, 1f);
                rRect.anchorMax = new Vector2(1f, 1f);
                rRect.pivot = new Vector2(0.5f, 1f);
                rRect.anchoredPosition = new Vector2(0f, -yOffset);
                rRect.sizeDelta = new Vector2(0f, 54f);
                yOffset += 60f;

                var rImg = rObj.AddComponent<Image>();
                rImg.color = new Color(0.10f, 0.28f, 0.45f);
                var rOutline = rObj.AddComponent<Outline>();
                rOutline.effectColor = new Color(0.2f, 0.95f, 0.5f);
                rOutline.effectDistance = new Vector2(2, 2);

                var rBtn = rObj.AddComponent<Button>();
                string targetIp = room.ip;
                rBtn.onClick.AddListener(() =>
                {
                    statusText.text = $"Conectando a {room.roomName}...";
                    FighterBodyType myBody = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.p1BodyType : FighterBodyType.Gordo;
                    NetworkFightManager.Instance.ConnectToHost(targetIp, myBody);
                });

                GameObject rtObj = new GameObject("Text");
                rtObj.transform.SetParent(rObj.transform, false);
                var rtRect = rtObj.AddComponent<RectTransform>();
                rtRect.anchorMin = Vector2.zero;
                rtRect.anchorMax = Vector2.one;
                rtRect.offsetMin = new Vector2(16, 0);
                rtRect.offsetMax = new Vector2(-16, 0);
                var rtTxt = rtObj.AddComponent<Text>();
                rtTxt.text = $"🎮 SALA: <b>{room.roomName.ToUpper()}</b>  |  CÓDIGO: <color=#00FFAA><b>[{room.roomPin}]</b></color>  |  Rival: <b>{FaceLoader.GetFighterDisplayName(room.hostBody)}</b>\n<size=12><color=#FFDD44>👉 ¡TOCA AQUÍ PARA PELEAR AL INSTANTE!</color></size>";
                rtTxt.font = font;
                rtTxt.fontSize = 13;
                rtTxt.color = Color.white;
                rtTxt.alignment = TextAnchor.MiddleLeft;
                rtTxt.raycastTarget = false;
            }
        }

        private void OnClickDirectConnect()
        {
            if (ipInputField == null || string.IsNullOrEmpty(ipInputField.text))
            {
                statusText.text = "Por favor ingresa el Código de sala (ej: 2) o la IP de tu amigo.";
                return;
            }

            string input = ipInputField.text.Trim();
            string resolved = NetworkFightManager.ResolveIpFromQuickCode(input);
            statusText.text = $"Conectando con código {input} ({resolved})...";
            FighterBodyType myBody = CharacterSelectUI.Instance != null ? CharacterSelectUI.Instance.p1BodyType : FighterBodyType.Gordo;
            NetworkFightManager.Instance.ConnectToHost(input, myBody);
        }

        private void OnCombatReady()
        {
            CloseLobby();
        }

        private static void CreateMenuButton(Transform parent, string name, Vector2 min, Vector2 max, string text, Color color, UnityEngine.Events.UnityAction action)
        {
            GameObject bObj = new GameObject(name);
            bObj.transform.SetParent(parent, false);
            var rect = bObj.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = bObj.AddComponent<Image>();
            img.color = color;
            var outline = bObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.4f);
            outline.effectDistance = new Vector2(1.5f, 1.5f);

            var btn = bObj.AddComponent<Button>();
            btn.onClick.AddListener(action);

            GameObject tObj = new GameObject("Text");
            tObj.transform.SetParent(bObj.transform, false);
            var tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
            var tTxt = tObj.AddComponent<Text>();
            tTxt.text = text;
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 13;
            tTxt.fontStyle = FontStyle.Bold;
            tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.color = Color.white;
            tTxt.raycastTarget = false;
        }
    }
}
