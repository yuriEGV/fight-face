using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
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
        public string roomName;
        public FighterBodyType hostBody;
        public StageType stage;
        public float lastSeenTime;
    }

    /// <summary>
    /// Gestor de Multijugador en Red Local (Wi-Fi / Hotspot) para Fight Face:
    /// Permite combates en tiempo real entre múltiples celulares (o celular contra PC)
    /// conectados a la misma red Wi-Fi o punto de acceso portátil.
    /// Utiliza sockets UDP ultra rápidos sin depender de servidores externos.
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
        private bool isRunningThreads = false;

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

        private void Update()
        {
            if (!IsNetworkMatchActive) return;

            // Timeout de desconexión (si pasan más de 5 segundos sin paquetes)
            if (Time.unscaledTime - lastHeartbeatReceived > 5.0f && lastHeartbeatReceived > 0f)
            {
                Debug.LogWarning("[NetworkFight] ⚠️ Conexión perdida con el rival por inactividad.");
                Disconnect("Se perdió la conexión con el rival");
                return;
            }

            // Si somos HOST: enviamos el estado de la pelea a 30 Hz al Cliente
            if (IsHost && Time.unscaledTime - lastSnapshotSent > 0.033f)
            {
                lastSnapshotSent = Time.unscaledTime;
                SendHostStateSnapshot();
            }

            // Si somos CLIENTE: enviamos nuestras pulsaciones al Host a 30-60 Hz
            if (IsClient && Time.unscaledTime - lastInputSent > 0.025f)
            {
                lastInputSent = Time.unscaledTime;
                SendClientInputs();
            }
        }

        // ======================= CREAR SALA (HOST) =======================

        public void StartHost(string hostName, FighterBodyType body, StageType stage)
        {
            StopNetwork();

            Role = NetworkRole.Host;
            isRunningThreads = true;

            try
            {
                gameSocket = new UdpClient(PORT_GAME);
                gameSocket.EnableBroadcast = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkFight] Error abriendo puerto {PORT_GAME}: {ex.Message}");
                StopNetwork();
                return;
            }

            // Hilo de recepción de paquetes del juego
            receiveThread = new Thread(ReceiveGamePacketsHost);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Corrutina para anunciar la sala por broadcast
            StartCoroutine(BroadcastRoomPresence(hostName, body, stage));

            Debug.Log($"[NetworkFight] 📡 Servidor Host creado en puerto {PORT_GAME}. Esperando rival...");
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

            IPEndPoint bcEp = new IPEndPoint(IPAddress.Broadcast, PORT_DISCOVERY);

            while (Role == NetworkRole.Host && !isConnected)
            {
                string msg = $"FF_ROOM|{hostName}|{(int)body}|{(int)stage}";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                try
                {
                    bcClient?.Send(data, data.Length, bcEp);
                }
                catch { }

                yield return new WaitForSecondsRealtime(1.0f);
            }

            bcClient?.Close();
        }

        // ======================= BUSCAR Y UNIRSE A SALA (CLIENTE) =======================

        public void StartScanningRooms()
        {
            StopNetwork();

            Role = NetworkRole.Client;
            discoveredRooms.Clear();
            isRunningThreads = true;

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

            discoveryThread = new Thread(ListenForRooms);
            discoveryThread.IsBackground = true;
            discoveryThread.Start();

            Debug.Log("[NetworkFight] 🔍 Buscando salas en la red Wi-Fi...");
        }

        private void ListenForRooms()
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

                            lock (discoveredRooms)
                            {
                                var existing = discoveredRooms.Find(r => r.ip == ip);
                                if (existing != null)
                                {
                                    existing.roomName = rName;
                                    existing.hostBody = (FighterBodyType)bId;
                                    existing.stage = (StageType)sId;
                                    existing.lastSeenTime = Time.unscaledTime;
                                }
                                else
                                {
                                    discoveredRooms.Add(new DiscoveredRoom
                                    {
                                        ip = ip,
                                        roomName = rName,
                                        hostBody = (FighterBodyType)bId,
                                        stage = (StageType)sId,
                                        lastSeenTime = Time.unscaledTime
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        public void ConnectToHost(string hostIp, FighterBodyType clientBody)
        {
            // Cerrar socket de descubrimiento para no consumir recursos
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
                Disconnect("Dirección IP inválida");
                return;
            }

            receiveThread = new Thread(ReceiveGamePacketsClient);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Enviar petición de unión
            string joinMsg = $"FF_JOIN|{SystemInfo.deviceName}|{(int)clientBody}";
            byte[] data = Encoding.UTF8.GetBytes(joinMsg);
            gameSocket.Send(data, data.Length, remoteEndPoint);

            Debug.Log($"[NetworkFight] 🤝 Enviando solicitud de unión a {hostIp}...");
        }

        // ======================= RECEPCIÓN DE PAQUETES (HOST) =======================

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
                        // Nuevo cliente conectado
                        remoteEndPoint = clientEp;
                        isConnected = true;
                        lastHeartbeatReceived = Time.unscaledTime;

                        string[] parts = text.Split('|');
                        FighterBodyType clientBody = parts.Length > 2 ? (FighterBodyType)int.Parse(parts[2]) : FighterBodyType.Gordo;

                        // Aceptar cliente
                        string acceptMsg = $"FF_ACCEPT|{(int)BattleManager.Instance.p1BodyType}|{(int)clientBody}|{(int)BattleManager.Instance.selectedStage}";
                        byte[] accData = Encoding.UTF8.GetBytes(acceptMsg);
                        gameSocket.Send(accData, accData.Length, remoteEndPoint);

                        // Iniciar la pelea en el hilo principal
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
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
                        // Decodificar inputs de P2
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
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
                            Disconnect("El rival se ha desconectado");
                        });
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        // ======================= RECEPCIÓN DE PAQUETES (CLIENTE) =======================

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

                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
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
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
                            Disconnect("La sala fue cerrada por el Host");
                        });
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        // ======================= ENVÍO DE DATOS =======================

        private void SendClientInputs()
        {
            if (gameSocket == null || remoteEndPoint == null) return;

            // En el cliente, el jugador usa los controles táctiles de P1 para controlar su personaje P2
            bool left = VirtualTouchControls.IsLeftPressed || FightInput.GetP1Left();
            bool right = VirtualTouchControls.IsRightPressed || FightInput.GetP1Right();
            bool down = VirtualTouchControls.IsDownPressed || FightInput.GetP1Down();
            bool jump = VirtualTouchControls.ConsumeJumpPressed() || FightInput.GetP1Jump();
            bool lp = VirtualTouchControls.ConsumePunchPressed() || FightInput.GetP1Punch();
            bool lk = VirtualTouchControls.ConsumeKickPressed() || FightInput.GetP1Kick();
            bool hp = VirtualTouchControls.ConsumeHeavyPunchPressed() || FightInput.GetP1HeavyPunch();
            bool hk = VirtualTouchControls.ConsumeHeavyKickPressed() || FightInput.GetP1HeavyKick();
            bool sp = VirtualTouchControls.ConsumeSpecialPressed() || FightInput.GetP1Special();
            bool gr = FightInput.GetP1Grab();

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
            float p1Hp = p1.currentHealth;
            float p2Hp = p2.currentHealth;
            int timer = (int)BattleManager.Instance.CurrentRoundTime;

            string msg = $"FF_STATE|{p1Pos.x:F2}|{p1Pos.y:F2}|{p1Hp:F0}|{(int)p1.CurrentState}|{p2Pos.x:F2}|{p2Pos.y:F2}|{p2Hp:F0}|{(int)p2.CurrentState}|{timer}";
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
            if (parts.Length < 10) return;

            if (float.TryParse(parts[1], out float p1x) &&
                float.TryParse(parts[2], out float p1y) &&
                float.TryParse(parts[3], out float p1hp) &&
                int.TryParse(parts[4], out int p1State) &&
                float.TryParse(parts[5], out float p2x) &&
                float.TryParse(parts[6], out float p2y) &&
                float.TryParse(parts[7], out float p2hp) &&
                int.TryParse(parts[8], out int p2State) &&
                int.TryParse(parts[9], out int timer))
            {
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    if (BattleManager.Instance != null)
                    {
                        var p1 = BattleManager.Instance.player1;
                        var p2 = BattleManager.Instance.player2;

                        if (p1 != null)
                        {
                            p1.transform.position = Vector3.Lerp(p1.transform.position, new Vector3(p1x, p1y, 0f), 0.5f);
                            int p1HealthInt = Mathf.RoundToInt(p1hp);
                            p1.currentHealth = p1HealthInt;
                            if (BattleUI.Instance != null) BattleUI.Instance.UpdateHealth(1, p1HealthInt, p1.maxHealth);
                        }

                        if (p2 != null)
                        {
                            p2.transform.position = Vector3.Lerp(p2.transform.position, new Vector3(p2x, p2y, 0f), 0.5f);
                            int p2HealthInt = Mathf.RoundToInt(p2hp);
                            p2.currentHealth = p2HealthInt;
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

        // ======================= DESCONEXIÓN =======================

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
                BattleUI.Instance.ShowBanner($"📶 {reason.ToUpper()}", 3.0f);
            }

            onDisconnected?.Invoke();
        }

        private void StopNetwork()
        {
            isRunningThreads = false;
            isConnected = false;
            Role = NetworkRole.None;
            remoteEndPoint = null;

            try
            {
                gameSocket?.Close();
                gameSocket = null;
            }
            catch { }

            try
            {
                discoverySocket?.Close();
                discoverySocket = null;
            }
            catch { }

            try
            {
                if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
                receiveThread = null;
            }
            catch { }

            try
            {
                if (discoveryThread != null && discoveryThread.IsAlive) discoveryThread.Abort();
                discoveryThread = null;
            }
            catch { }
        }

        private void OnDestroy()
        {
            StopNetwork();
        }

        // ======================= UTILIDAD: OBTENER IP LOCAL =======================

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string s = ip.ToString();
                        if (!s.StartsWith("127."))
                        {
                            return s;
                        }
                    }
                }
            }
            catch { }
            return "192.168.1.X";
        }
    }

    /// <summary>
    /// Despachador de acciones a ejecutar de forma segura en el hilo principal de Unity desde threads secundarios.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> executionQueue = new Queue<Action>();
        private static UnityMainThreadDispatcher instance;

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("MainThreadDispatcher");
            instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            lock (executionQueue)
            {
                while (executionQueue.Count > 0)
                {
                    executionQueue.Dequeue()?.Invoke();
                }
            }
        }
    }
}
