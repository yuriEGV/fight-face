using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace FightFace
{
    public enum NetworkRole
    {
        None,
        Host,
        Client
    }

    [System.Serializable]
    public class DiscoveredRoom
    {
        public string ip;
        public string roomPin;
        public string roomName;
        public FighterBodyType hostBody;
        public StageType stage;
        public long lastSeenTick;
    }

    /// <summary>
    /// Gestor de Multijugador en Red Local (Wi-Fi / Hotspot) para Fight Face:
    /// - Detección automática de salas mediante Broadcast + Subnet Sweep activo (cero configuración).
    /// - Soporte para Código Rápido de 1 o 2 dígitos (PIN fácil) sin necesidad de saber qué es una IP.
    /// - Comunicación por sockets UDP ultra rápidos (30-60 Hz) con sincronización fluida de posiciones y animaciones.
    /// - Transición sincronizada e inmediata a la arena de combate en ambos dispositivos.
    /// </summary>
    public class NetworkFightManager : MonoBehaviour
    {
        public static NetworkFightManager Instance { get; private set; }

        public const int PORT_GAME = 7777;
        public const int PORT_DISCOVERY = 7778;

        public NetworkRole Role { get; private set; } = NetworkRole.None;
        public bool IsNetworkMatchActive => Role != NetworkRole.None && isConnected;
        public bool IsHost => Role == NetworkRole.Host;
        public bool IsClient => Role == NetworkRole.Client;

        // Estado de conexión
        private bool isConnected = false;
        private IPEndPoint remoteEndPoint;
        private UdpClient gameSocket;
        private UdpClient discoverySocket;
        private Thread receiveThread;
        private Thread discoveryThread;
        private volatile bool isRunningThreads = false;

        // Cola segura de acciones para ejecutar en el hilo principal de Unity
        private readonly Queue<Action> mainThreadQueue = new Queue<Action>();

        // Salas descubiertas en la red
        public List<DiscoveredRoom> discoveredRooms = new List<DiscoveredRoom>();
        public Action onRoomsUpdated;
        public Action onConnectedToHost;
        public Action onClientConnected;
        public Action onDisconnected;

        // Entradas remotas recibidas del Cliente (para que el Host las use en P2)
        public bool RemoteP2Left { get; private set; }
        public bool RemoteP2Right { get; private set; }
        public bool RemoteP2Down { get; private set; }
        public bool RemoteP2Jump { get; private set; }
        public bool RemoteP2Punch { get; private set; }
        public bool RemoteP2Kick { get; private set; }
        public bool RemoteP2HeavyPunch { get; private set; }
        public bool RemoteP2HeavyKick { get; private set; }
        public bool RemoteP2Special { get; private set; }
        public bool RemoteP2Grab { get; private set; }

        private float lastHeartbeatReceived = 0f;
        private float lastSnapshotSent = 0f;
        private float lastInputSent = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void EnqueueMainThread(Action action)
        {
            if (action == null) return;
            lock (mainThreadQueue)
            {
                mainThreadQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            // 1. Ejecutar SIEMPRE las acciones pendientes en el hilo principal de Unity
            lock (mainThreadQueue)
            {
                while (mainThreadQueue.Count > 0)
                {
                    try
                    {
                        mainThreadQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NetworkFight] Error ejecutando acción en hilo principal: {ex}");
                    }
                }
            }

            if (!IsNetworkMatchActive) return;

            // Timeout de desconexión (si pasan más de 6 segundos sin paquetes)
            if (Time.unscaledTime - lastHeartbeatReceived > 6.0f && lastHeartbeatReceived > 0f)
            {
                Debug.LogWarning("[NetworkFight] ⚠️ Conexión perdida con el rival por inactividad.");
                Disconnect("Se perdió la conexión con el rival");
                return;
            }

            // Si somos HOST: enviamos el estado completo de la pelea a ~35 Hz al Cliente
            if (IsHost && Time.unscaledTime - lastSnapshotSent > 0.028f)
            {
                lastSnapshotSent = Time.unscaledTime;
                SendHostStateSnapshot();
            }

            // Si somos CLIENTE: enviamos nuestras entradas al Host a ~40 Hz
            if (IsClient && Time.unscaledTime - lastInputSent > 0.025f)
            {
                lastInputSent = Time.unscaledTime;
                SendClientInputs();
            }
        }

        // =========================================================================
        // 1. CREAR SALA (HOST)
        // =========================================================================
        public void StartHost(string hostName, FighterBodyType body, StageType stage)
        {
            StopNetwork();

            Role = NetworkRole.Host;
            isRunningThreads = true;
            isConnected = false;

            try
            {
                gameSocket = new UdpClient(PORT_GAME);
                gameSocket.EnableBroadcast = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkFight] Error abriendo puerto de juego {PORT_GAME}: {ex.Message}");
                StopNetwork();
                return;
            }

            // Socket de descubrimiento para responder PINGs de clientes
            try
            {
                discoverySocket = new UdpClient();
                discoverySocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                discoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_DISCOVERY));
            }
            catch { }

            // Hilo de recepción de paquetes del juego
            receiveThread = new Thread(ReceiveGamePacketsHost);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Hilo de escucha de Pings en descubrimiento
            if (discoverySocket != null)
            {
                discoveryThread = new Thread(() => ListenForDiscoveryPingsHost(hostName, body, stage));
                discoveryThread.IsBackground = true;
                discoveryThread.Start();
            }

            // Corrutina para anunciar la sala periódicamente
            StartCoroutine(BroadcastRoomPresence(hostName, body, stage));

            Debug.Log($"[NetworkFight] 📡 Servidor Host creado en puerto {PORT_GAME}. PIN: {GetQuickRoomCode()}");
        }

        private IEnumerator BroadcastRoomPresence(string hostName, FighterBodyType body, StageType stage)
        {
            UdpClient bcClient = null;
            try
            {
                bcClient = new UdpClient();
                bcClient.EnableBroadcast = true;
            }
            catch { }

            string roomPin = GetQuickRoomCode();
            string msg = $"FF_ROOM|{hostName}|{(int)body}|{(int)stage}|{roomPin}";
            byte[] data = Encoding.UTF8.GetBytes(msg);

            var broadcastTargets = GetSubnetBroadcastEndPoints(PORT_DISCOVERY);

            while (Role == NetworkRole.Host && !isConnected)
            {
                if (bcClient != null)
                {
                    foreach (var ep in broadcastTargets)
                    {
                        try
                        {
                            bcClient.Send(data, data.Length, ep);
                        }
                        catch { }
                    }
                }

                yield return new WaitForSecondsRealtime(0.85f);
            }

            bcClient?.Close();
        }

        private void ListenForDiscoveryPingsHost(string hostName, FighterBodyType body, StageType stage)
        {
            IPEndPoint clientEp = new IPEndPoint(IPAddress.Any, 0);
            string roomPin = GetQuickRoomCode();
            string roomMsg = $"FF_ROOM|{hostName}|{(int)body}|{(int)stage}|{roomPin}";
            byte[] roomData = Encoding.UTF8.GetBytes(roomMsg);

            while (isRunningThreads && discoverySocket != null)
            {
                try
                {
                    byte[] data = discoverySocket.Receive(ref clientEp);
                    string text = Encoding.UTF8.GetString(data);

                    if (text.StartsWith("FF_PING"))
                    {
                        // Responder directamente al cliente que hizo el ping
                        discoverySocket.Send(roomData, roomData.Length, clientEp);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        // =========================================================================
        // 2. BUSCAR Y UNIRSE A SALA (CLIENTE)
        // =========================================================================
        public void StartScanningRooms()
        {
            StopNetwork();

            Role = NetworkRole.Client;
            discoveredRooms.Clear();
            isRunningThreads = true;
            isConnected = false;

            try
            {
                discoverySocket = new UdpClient();
                discoverySocket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                discoverySocket.Client.Bind(new IPEndPoint(IPAddress.Any, PORT_DISCOVERY));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkFight] Error en puerto de descubrimiento {PORT_DISCOVERY}: {ex.Message}");
            }

            discoveryThread = new Thread(ListenForRoomsClient);
            discoveryThread.IsBackground = true;
            discoveryThread.Start();

            // Enviar sondeos rápidos por Wi-Fi
            StartCoroutine(ProbeNetworkCoroutine());

            Debug.Log("[NetworkFight] 🔍 Buscando salas en la red Wi-Fi...");
        }

        private IEnumerator ProbeNetworkCoroutine()
        {
            UdpClient probeClient = null;
            try
            {
                probeClient = new UdpClient();
                probeClient.EnableBroadcast = true;
            }
            catch { }

            byte[] pingData = Encoding.UTF8.GetBytes("FF_PING");
            var targets = GetSubnetBroadcastEndPoints(PORT_DISCOVERY);

            // 1. Enviar Ping por broadcast
            if (probeClient != null)
            {
                foreach (var ep in targets)
                {
                    try { probeClient.Send(pingData, pingData.Length, ep); } catch { }
                }
            }

            // 2. Barrido activo de subred local (IPs .1 a .30 y .100 a .120 comunes en celulares)
            string prefix = GetSubnetPrefix();
            for (int i = 1; i <= 35; i++)
            {
                if (Role != NetworkRole.Client || isConnected) break;
                try
                {
                    probeClient?.Send(pingData, pingData.Length, new IPEndPoint(IPAddress.Parse(prefix + i), PORT_DISCOVERY));
                }
                catch { }

                if (i % 6 == 0) yield return null;
            }

            probeClient?.Close();
        }

        private void ListenForRoomsClient()
        {
            IPEndPoint senderEp = new IPEndPoint(IPAddress.Any, 0);
            while (isRunningThreads && discoverySocket != null)
            {
                try
                {
                    byte[] data = discoverySocket.Receive(ref senderEp);
                    string text = Encoding.UTF8.GetString(data);

                    if (text.StartsWith("FF_ROOM|"))
                    {
                        string[] parts = text.Split('|');
                        if (parts.Length >= 4)
                        {
                            string ip = senderEp.Address.ToString();
                            string rName = parts[1];
                            int bId = int.Parse(parts[2]);
                            int sId = int.Parse(parts[3]);
                            string rPin = parts.Length > 4 ? parts[4] : GetPinFromIp(ip);

                            lock (discoveredRooms)
                            {
                                var existing = discoveredRooms.Find(r => r.ip == ip);
                                if (existing != null)
                                {
                                    existing.roomName = rName;
                                    existing.hostBody = (FighterBodyType)bId;
                                    existing.stage = (StageType)sId;
                                    existing.roomPin = rPin;
                                    existing.lastSeenTick = DateTime.UtcNow.Ticks;
                                }
                                else
                                {
                                    discoveredRooms.Add(new DiscoveredRoom
                                    {
                                        ip = ip,
                                        roomPin = rPin,
                                        roomName = rName,
                                        hostBody = (FighterBodyType)bId,
                                        stage = (StageType)sId,
                                        lastSeenTick = DateTime.UtcNow.Ticks
                                    });
                                }
                            }

                            EnqueueMainThread(() => onRoomsUpdated?.Invoke());
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        public void ConnectToHost(string hostIpOrPin, FighterBodyType clientBody)
        {
            string hostIp = ResolveIpFromQuickCode(hostIpOrPin);

            // Cerrar socket de descubrimiento para liberar el puerto
            try
            {
                discoverySocket?.Close();
                discoverySocket = null;
            }
            catch { }

            try
            {
                gameSocket = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), PORT_GAME);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkFight] Error conectando a IP {hostIp}: {ex.Message}");
                Disconnect("Dirección IP o Código inválido");
                return;
            }

            isRunningThreads = true;
            receiveThread = new Thread(ReceiveGamePacketsClient);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Enviar petición de unión
            string joinMsg = $"FF_JOIN|{SystemInfo.deviceName}|{(int)clientBody}";
            byte[] data = Encoding.UTF8.GetBytes(joinMsg);
            gameSocket.Send(data, data.Length, remoteEndPoint);

            Debug.Log($"[NetworkFight] 🤝 Enviando solicitud de unión a {hostIp}...");
        }

        // =========================================================================
        // 3. RECEPCIÓN DE PAQUETES (HOST)
        // =========================================================================
        private void ReceiveGamePacketsHost()
        {
            IPEndPoint clientEp = new IPEndPoint(IPAddress.Any, 0);

            while (isRunningThreads && gameSocket != null)
            {
                try
                {
                    byte[] data = gameSocket.Receive(ref clientEp);
                    string text = Encoding.UTF8.GetString(data);

                    if (text.StartsWith("FF_JOIN|"))
                    {
                        remoteEndPoint = clientEp;
                        isConnected = true;
                        lastHeartbeatReceived = Time.unscaledTime;

                        string[] parts = text.Split('|');
                        FighterBodyType clientBody = parts.Length > 2 ? (FighterBodyType)int.Parse(parts[2]) : FighterBodyType.Gordo;

                        // Aceptar cliente
                        int hostBodyInt = BattleManager.Instance != null ? (int)BattleManager.Instance.p1BodyType : 2;
                        int stageInt = BattleManager.Instance != null ? (int)BattleManager.Instance.selectedStage : 3;
                        string acceptMsg = $"FF_ACCEPT|{hostBodyInt}|{(int)clientBody}|{stageInt}";
                        byte[] accData = Encoding.UTF8.GetBytes(acceptMsg);
                        gameSocket.Send(accData, accData.Length, remoteEndPoint);

                        // Iniciar la pelea de inmediato en el hilo principal
                        EnqueueMainThread(() =>
                        {
                            Time.timeScale = 1.0f;
                            if (NetworkLobbyUI.Instance != null)
                            {
                                NetworkLobbyUI.Instance.CloseLobby();
                            }
                            if (CharacterSelectUI.Instance != null)
                            {
                                CharacterSelectUI.Instance.gameObject.SetActive(false);
                            }
                            if (BattleUI.Instance != null)
                            {
                                BattleUI.Instance.HideCharacterSelectMenu();
                            }
                            if (BattleManager.Instance != null)
                            {
                                BattleManager.Instance.StartFightWithCharacters(
                                    BattleManager.Instance.p1BodyType,
                                    clientBody,
                                    p2AI: false,
                                    training: false,
                                    stage: BattleManager.Instance.selectedStage
                                );
                            }
                            if (BattleUI.Instance != null)
                            {
                                BattleUI.Instance.ShowBanner("📶 ¡OPONENTE CONECTADO POR WI-FI!\n¡A PELEAR!", 2.5f);
                            }
                            onClientConnected?.Invoke();
                        });
                    }
                    else if (text.StartsWith("FF_INPUT|"))
                    {
                        lastHeartbeatReceived = Time.unscaledTime;
                        string[] parts = text.Split('|');
                        if (parts.Length >= 10)
                        {
                            RemoteP2Left = parts[1] == "1";
                            RemoteP2Right = parts[2] == "1";
                            RemoteP2Down = parts[3] == "1";
                            RemoteP2Jump = parts[4] == "1";
                            RemoteP2Punch = parts[5] == "1";
                            RemoteP2Kick = parts[6] == "1";
                            RemoteP2HeavyPunch = parts[7] == "1";
                            RemoteP2HeavyKick = parts[8] == "1";
                            RemoteP2Special = parts[9] == "1";
                            RemoteP2Grab = parts.Length > 10 && parts[10] == "1";
                        }
                    }
                    else if (text.StartsWith("FF_LEAVE"))
                    {
                        EnqueueMainThread(() => Disconnect("El rival se ha desconectado"));
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        // =========================================================================
        // 4. RECEPCIÓN DE PAQUETES (CLIENTE)
        // =========================================================================
        private void ReceiveGamePacketsClient()
        {
            IPEndPoint hostEp = new IPEndPoint(IPAddress.Any, 0);

            while (isRunningThreads && gameSocket != null)
            {
                try
                {
                    byte[] data = gameSocket.Receive(ref hostEp);
                    string text = Encoding.UTF8.GetString(data);

                    if (text.StartsWith("FF_ACCEPT|"))
                    {
                        isConnected = true;
                        lastHeartbeatReceived = Time.unscaledTime;

                        string[] parts = text.Split('|');
                        FighterBodyType hostBody = (FighterBodyType)int.Parse(parts[1]);
                        FighterBodyType clientBody = (FighterBodyType)int.Parse(parts[2]);
                        StageType stage = (StageType)int.Parse(parts[3]);

                        EnqueueMainThread(() =>
                        {
                            Time.timeScale = 1.0f;
                            if (NetworkLobbyUI.Instance != null)
                            {
                                NetworkLobbyUI.Instance.CloseLobby();
                            }
                            if (CharacterSelectUI.Instance != null)
                            {
                                CharacterSelectUI.Instance.gameObject.SetActive(false);
                            }
                            if (BattleUI.Instance != null)
                            {
                                BattleUI.Instance.HideCharacterSelectMenu();
                            }
                            if (BattleManager.Instance != null)
                            {
                                BattleManager.Instance.StartFightWithCharacters(
                                    hostBody,
                                    clientBody,
                                    p2AI: false,
                                    training: false,
                                    stage: stage
                                );
                            }
                            if (BattleUI.Instance != null)
                            {
                                BattleUI.Instance.ShowBanner("📶 ¡CONECTADO AL HOST POR WI-FI!\n¡COMBATE INICIADO!", 2.5f);
                            }
                            onConnectedToHost?.Invoke();
                        });
                    }
                    else if (text.StartsWith("FF_STATE|"))
                    {
                        lastHeartbeatReceived = Time.unscaledTime;
                        ApplyHostSnapshot(text);
                    }
                    else if (text.StartsWith("FF_LEAVE"))
                    {
                        EnqueueMainThread(() => Disconnect("La sala fue cerrada por el Host"));
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        // =========================================================================
        // 5. ENVÍO Y APLICACIÓN DE ESTADOS
        // =========================================================================
        private void SendClientInputs()
        {
            if (gameSocket == null || remoteEndPoint == null) return;

            // En el cliente, leemos el estado de entrada táctil y teclado
            bool left = VirtualTouchControls.IsLeftPressed || FightInput.GetP1LeftRaw() || FightInput.GetP2LeftRaw();
            bool right = VirtualTouchControls.IsRightPressed || FightInput.GetP1RightRaw() || FightInput.GetP2RightRaw();
            bool down = VirtualTouchControls.IsDownPressed || FightInput.GetP1DownRaw() || FightInput.GetP2DownRaw();
            bool jump = VirtualTouchControls.IsJumpPressed || FightInput.GetP1JumpRaw() || FightInput.GetP2JumpRaw();
            bool lp = VirtualTouchControls.IsPunchPressed || FightInput.GetP1PunchRaw() || FightInput.GetP2PunchRaw();
            bool lk = VirtualTouchControls.IsKickPressed || FightInput.GetP1KickRaw() || FightInput.GetP2KickRaw();
            bool hp = VirtualTouchControls.IsHeavyPunchPressed || FightInput.GetP1HeavyPunchRaw() || FightInput.GetP2HeavyPunchRaw();
            bool hk = VirtualTouchControls.IsHeavyKickPressed || FightInput.GetP1HeavyKickRaw() || FightInput.GetP2HeavyKickRaw();
            bool sp = VirtualTouchControls.IsSpecialPressed || FightInput.GetP1SpecialRaw() || FightInput.GetP2SpecialRaw();
            bool gr = VirtualTouchControls.IsGrabPressed || FightInput.GetP1GrabRaw() || FightInput.GetP2GrabRaw();

            string msg = $"FF_INPUT|{(left ? "1" : "0")}|{(right ? "1" : "0")}|{(down ? "1" : "0")}|{(jump ? "1" : "0")}|{(lp ? "1" : "0")}|{(lk ? "1" : "0")}|{(hp ? "1" : "0")}|{(hk ? "1" : "0")}|{(sp ? "1" : "0")}|{(gr ? "1" : "0")}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            try
            {
                gameSocket.Send(data, data.Length, remoteEndPoint);
            }
            catch { }
        }

        private void SendHostStateSnapshot()
        {
            if (gameSocket == null || remoteEndPoint == null || BattleManager.Instance == null) return;

            var p1 = BattleManager.Instance.player1;
            var p2 = BattleManager.Instance.player2;
            if (p1 == null || p2 == null) return;

            Vector2 p1Pos = p1.transform.position;
            Vector2 p2Pos = p2.transform.position;
            var rb1 = p1.GetComponent<Rigidbody2D>();
            var rb2 = p2.GetComponent<Rigidbody2D>();
            float p1vx = rb1 != null ? rb1.linearVelocity.x : 0f;
            float p2vx = rb2 != null ? rb2.linearVelocity.x : 0f;
            float p1Hp = p1.currentHealth;
            float p2Hp = p2.currentHealth;
            int p1Facing = p1.transform.localScale.x >= 0f ? 1 : -1;
            int p2Facing = p2.transform.localScale.x >= 0f ? 1 : -1;
            int timer = (int)BattleManager.Instance.CurrentRoundTime;

            string msg = string.Format(CultureInfo.InvariantCulture,
                "FF_STATE|{0:F2}|{1:F2}|{2:F2}|{3:F0}|{4}|{5}|{6:F2}|{7:F2}|{8:F2}|{9:F0}|{10}|{11}|{12}",
                p1Pos.x, p1Pos.y, p1vx, p1Hp, (int)p1.CurrentState, p1Facing,
                p2Pos.x, p2Pos.y, p2vx, p2Hp, (int)p2.CurrentState, p2Facing,
                timer);

            byte[] data = Encoding.UTF8.GetBytes(msg);
            try
            {
                gameSocket.Send(data, data.Length, remoteEndPoint);
            }
            catch { }
        }

        private void ApplyHostSnapshot(string packet)
        {
            string[] parts = packet.Split('|');
            if (parts.Length < 13) return;

            if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float p1x) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float p1y) &&
                float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float p1vx) &&
                float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float p1hp) &&
                int.TryParse(parts[5], out int p1State) &&
                int.TryParse(parts[6], out int p1Facing) &&
                float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float p2x) &&
                float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float p2y) &&
                float.TryParse(parts[9], NumberStyles.Float, CultureInfo.InvariantCulture, out float p2vx) &&
                float.TryParse(parts[10], NumberStyles.Float, CultureInfo.InvariantCulture, out float p2hp) &&
                int.TryParse(parts[11], out int p2State) &&
                int.TryParse(parts[12], out int p2Facing) &&
                int.TryParse(parts[13], out int timer))
            {
                EnqueueMainThread(() =>
                {
                    if (BattleManager.Instance != null)
                    {
                        var p1 = BattleManager.Instance.player1;
                        var p2 = BattleManager.Instance.player2;

                        if (p1 != null)
                        {
                            // P1 es el Host remoto: interpolar posición y animar
                            p1.transform.position = Vector3.Lerp(p1.transform.position, new Vector3(p1x, p1y, 0f), 0.65f);
                            p1.transform.localScale = new Vector3(Mathf.Abs(p1.transform.localScale.x) * p1Facing, p1.transform.localScale.y, p1.transform.localScale.z);
                            int p1HealthInt = Mathf.RoundToInt(p1hp);
                            p1.currentHealth = p1HealthInt;
                            if (p1.bodyController != null)
                            {
                                p1.bodyController.UpdateAnimation(p1vx, isGrounded: p1y <= -2.75f, guardHeld: p1State == (int)FighterState.BLOCKING);
                            }
                            if (BattleUI.Instance != null) BattleUI.Instance.UpdateHealth(1, p1HealthInt, p1.maxHealth);
                        }

                        if (p2 != null)
                        {
                            // P2 es el cliente local: suavizar posición para corregir desvíos
                            p2.transform.position = Vector3.Lerp(p2.transform.position, new Vector3(p2x, p2y, 0f), 0.35f);
                            p2.transform.localScale = new Vector3(Mathf.Abs(p2.transform.localScale.x) * p2Facing, p2.transform.localScale.y, p2.transform.localScale.z);
                            int p2HealthInt = Mathf.RoundToInt(p2hp);
                            p2.currentHealth = p2HealthInt;
                            if (p2.bodyController != null)
                            {
                                p2.bodyController.UpdateAnimation(p2vx, isGrounded: p2y <= -2.75f, guardHeld: p2State == (int)FighterState.BLOCKING);
                            }
                            if (BattleUI.Instance != null) BattleUI.Instance.UpdateHealth(2, p2HealthInt, p2.maxHealth);
                        }

                        if (BattleUI.Instance != null)
                        {
                            BattleUI.Instance.UpdateTimer(timer);
                        }
                    }
                });
            }
        }

        // =========================================================================
        // 6. DESCONEXIÓN
        // =========================================================================
        public void Disconnect(string reason = null)
        {
            if (isConnected && gameSocket != null && remoteEndPoint != null)
            {
                try
                {
                    byte[] leave = Encoding.UTF8.GetBytes("FF_LEAVE");
                    gameSocket.Send(leave, leave.Length, remoteEndPoint);
                }
                catch { }
            }

            StopNetwork();

            if (!string.IsNullOrEmpty(reason) && BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowBanner($"⚠️ {reason}", 3.0f);
            }

            onDisconnected?.Invoke();
        }

        public void StopNetwork()
        {
            isRunningThreads = false;
            isConnected = false;
            Role = NetworkRole.None;
            remoteEndPoint = null;

            try { gameSocket?.Close(); } catch { }
            try { discoverySocket?.Close(); } catch { }
            gameSocket = null;
            discoverySocket = null;

            StopAllCoroutines();

            RemoteP2Left = false;
            RemoteP2Right = false;
            RemoteP2Down = false;
            RemoteP2Jump = false;
            RemoteP2Punch = false;
            RemoteP2Kick = false;
            RemoteP2HeavyPunch = false;
            RemoteP2HeavyKick = false;
            RemoteP2Special = false;
            RemoteP2Grab = false;
        }

        private void OnApplicationQuit()
        {
            StopNetwork();
        }

        private void OnDestroy()
        {
            StopNetwork();
        }

        // =========================================================================
        // 7. UTILIDADES DE RED Y CÓDIGO RÁPIDO (PIN)
        // =========================================================================
        public static List<IPAddress> GetAllLocalIPv4Addresses()
        {
            var list = new List<IPAddress>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var u in ipProps.UnicastAddresses)
                    {
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork && !u.Address.ToString().StartsWith("127."))
                        {
                            list.Add(u.Address);
                        }
                    }
                }
            }
            catch { }

            if (list.Count == 0)
            {
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.ToString().StartsWith("127."))
                        {
                            list.Add(ip);
                        }
                    }
                }
                catch { }
            }

            return list;
        }

        public static string GetLocalIPAddress()
        {
            var list = GetAllLocalIPv4Addresses();
            // Priorizar direcciones de red local Wi-Fi / Hotspot comunes
            foreach (var ip in list)
            {
                string s = ip.ToString();
                if (s.StartsWith("192.168.") || s.StartsWith("10.") || s.StartsWith("172."))
                    return s;
            }
            return list.Count > 0 ? list[0].ToString() : "127.0.0.1";
        }

        public static string GetSubnetPrefix()
        {
            string ip = GetLocalIPAddress();
            int lastDot = ip.LastIndexOf('.');
            if (lastDot > 0)
            {
                return ip.Substring(0, lastDot + 1);
            }
            return "192.168.1.";
        }

        public static string GetQuickRoomCode()
        {
            return GetPinFromIp(GetLocalIPAddress());
        }

        public static string GetPinFromIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return "1";
            int lastDot = ip.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < ip.Length - 1)
            {
                return ip.Substring(lastDot + 1);
            }
            return "1";
        }

        public static string ResolveIpFromQuickCode(string input)
        {
            if (string.IsNullOrEmpty(input)) return GetLocalIPAddress();
            input = input.Trim();

            // Si ya contiene un punto (ej: "192.168.137.2") es una IP completa
            if (input.Contains("."))
            {
                return input;
            }

            // Si es un código corto (ej: "2"), autocompletar con la subred local actual
            string prefix = GetSubnetPrefix();
            return prefix + input;
        }

        public static List<IPEndPoint> GetSubnetBroadcastEndPoints(int port)
        {
            var list = new List<IPEndPoint>();
            list.Add(new IPEndPoint(IPAddress.Broadcast, port));

            try
            {
                string prefix = GetSubnetPrefix();
                list.Add(new IPEndPoint(IPAddress.Parse(prefix + "255"), port));
                list.Add(new IPEndPoint(IPAddress.Parse(prefix + "1"), port));
            }
            catch { }

            return list;
        }
    }

    /// <summary>
    /// Despachador seguro para delegar llamadas al hilo principal de Unity.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        public static void Enqueue(Action action)
        {
            if (NetworkFightManager.Instance != null)
            {
                NetworkFightManager.Instance.EnqueueMainThread(action);
            }
        }
    }
}
