using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Controlador principal del Luchador 2D estilo Street Fighter:
    /// - Atributos: Fuerza, Velocidad, Defensa, Técnica, Alcance.
    /// - Sistema de Ataques: Puño Ligero (LP), Puño Fuerte (HP), Patada Ligera (LK), Patada Fuerte (HK), Especial y Agarre.
    /// - Sistema de Defensa: Bloqueo alto (←) reduce 75% el daño y bloquea hitstun. Agarre rompe la guardia.
    /// - Sistema de Stun: Barra de aturdimiento que al llenarse deja al luchador vulnerable con mareo y estrellitas 💫.
    /// - Sistema RAGE: Al caer bajo el 20% de salud activa modo rabia (+25% daño, cara de Rabia 😡 permanente).
    /// - Sistema de Daño Facial: Las 4 fotos (Base, Dolor 😖 en golpes fuertes/stun, Rabia 😡 en Rage/especiales, Ganador 😎 al vencer).
    /// - Contador de Combos: 🔥 X HITS! 💥 Y DAMAGE.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FighterController : MonoBehaviour
    {
        [Header("Identificación")]
        public int playerId = 1;
        public string fighterName = "Luchador";
        public bool isAI = false;

        [Header("Estadísticas Base")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public float moveSpeed = 4.5f;
        public float jumpForce = 9.5f;

        [Header("Atributos del Personaje")]
        [Range(0.5f, 2.0f)] public float statFuerza = 1.0f;
        [Range(0.5f, 2.0f)] public float statVelocidad = 1.0f;
        [Range(0.5f, 2.0f)] public float statDefensa = 1.0f;
        [Range(0.5f, 2.0f)] public float statTecnica = 1.0f;
        [Range(0.5f, 2.0f)] public float statAlcance = 1.0f;

        [Header("Sistema de Stun (Aturdimiento)")]
        public float currentStun = 0f;
        public float maxStun = 100f;
        public bool isFullStunned = false;
        private float lastHitReceivedTime = 0f;
        private Coroutine activeStunRoutine;

        [Header("Sistema RAGE (Furia al 20% HP)")]
        public bool isRageActive = false;

        [Header("Combos")]
        public int currentComboHits = 0;
        public int currentComboDamage = 0;
        public int maxComboHits = 0;
        private float lastComboTime = 0f;

        [Header("Referencias de Componentes")]
        public DynamicFaceController faceController;
        public DynamicFaceController faceControllerRight;
        public FighterBodyController bodyController;
        public Transform opponent;
        public Hitbox punchHitbox;
        public Hitbox kickHitbox;

        [Header("Voces de Batalla")]
        public FighterVoiceProfile voiceProfile;
        public FighterVoiceProfile voiceProfileHead2;
        private float lastAttackVoiceTime = -1f;
        private float lastHurtVoiceTime = -1f;

        public void PlayVoice(VoiceType type, float volume = 1f)
        {
            // Bloquear totalmente voces de combate si los menús están abiertos o si la pelea aún no ha comenzado
            bool isMenuOpen = (BattleUI.Instance != null && BattleUI.Instance.characterSelectPanel != null && BattleUI.Instance.characterSelectPanel.activeSelf)
                           || (BattleUI.Instance != null && BattleUI.Instance.faceCustomizerPanel != null && BattleUI.Instance.faceCustomizerPanel.activeSelf);

            if (isMenuOpen) return;
            if (BattleManager.Instance != null && !BattleManager.Instance.IsMatchActive && type != VoiceType.Victory) return;
            if (Time.timeScale <= 0f) return;

            if (type == VoiceType.Attack)
            {
                if (Time.time - lastAttackVoiceTime < 0.55f) return;
                lastAttackVoiceTime = Time.time;
            }
            else if (type == VoiceType.Hurt)
            {
                if (Time.time - lastHurtVoiceTime < 0.35f) return;
                lastHurtVoiceTime = Time.time;
            }

            float pitch = UnityEngine.Random.Range(0.96f, 1.04f) * (playerId == 2 ? 1.05f : 0.98f);

            if (voiceProfile != null && voiceProfile.HasCustomVoice(type))
            {
                if (FightAudioManager.Instance != null)
                {
                    FightAudioManager.Instance.PlayFighterVoice(voiceProfile.GetVoice(type), volume, pitch);
                }
            }
            else
            {
                if (FightAudioManager.Instance != null)
                {
                    FightAudioManager.Instance.PlayProceduralVoice(type, volume, pitch);
                }
            }

            // Para Hombre-Máquina (Dos Cabezas), la segunda cabeza puede reaccionar en estéreo
            if (faceControllerRight != null && voiceProfileHead2 != null && voiceProfileHead2.HasCustomVoice(type))
            {
                if (FightAudioManager.Instance != null)
                {
                    FightAudioManager.Instance.PlayFighterVoice(voiceProfileHead2.GetVoice(type), volume * 0.9f, pitch * 1.10f);
                }
            }
        }

        [Header("Estado y Hitstun")]
        public bool estaEnStun = false;
        public bool isTrainingDummy = false;
        private float lastDummyHitTime = 0f;
        private Coroutine activeAttackRoutine;
        private Coroutine activeHitstunRoutine;

        private Rigidbody2D rb;
        private bool isGrounded = true;
        public bool isAlive = true;
        private bool isAttacking = false;
        private float facingDirection = 1f;
        private float nextAttackTime = 0f;
        private float currentHorizontalInput = 0f;

        // IA timers y comportamiento
        private float aiNextDecisionTime = 0f;
        private float aiMoveDir = 0f;

        public bool IsAlive { get => isAlive; set => isAlive = value; }
        public float FacingDirection => facingDirection;
        public float HealthPercent => (float)currentHealth / maxHealth;
        public float StunPercent => Mathf.Clamp01(currentStun / maxStun);

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            currentHealth = maxHealth;
            currentStun = 0f;
            isAlive = true;
            estaEnStun = false;
            isFullStunned = false;
            isRageActive = false;
        }

        private void Start()
        {
            if (faceController == null)
            {
                faceController = GetComponentInChildren<DynamicFaceController>();
            }
            if (bodyController == null)
            {
                bodyController = GetComponentInChildren<FighterBodyController>();
            }

            SetupHitboxes();
        }

        private void SetupHitboxes()
        {
            var hurtbox = GetComponent<Hurtbox>();
            if (hurtbox == null) hurtbox = gameObject.AddComponent<Hurtbox>();
            hurtbox.ownerFighter = this;

            if (punchHitbox == null)
            {
                GameObject punchObj = new GameObject("PunchHitbox");
                punchObj.transform.SetParent(transform, false);
                punchObj.transform.localPosition = new Vector3(0.95f * statAlcance, 1.30f, 0);
                var box = punchObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.9f * statAlcance, 0.6f);
                box.isTrigger = true;
                punchHitbox = punchObj.AddComponent<Hitbox>();
                punchHitbox.isKickAttack = false;
                punchHitbox.Initialize(this);
            }
            else
            {
                punchHitbox.isKickAttack = false;
                punchHitbox.Initialize(this);
            }

            if (kickHitbox == null)
            {
                GameObject kickObj = new GameObject("KickHitbox");
                kickObj.transform.SetParent(transform, false);
                kickObj.transform.localPosition = new Vector3(1.05f * statAlcance, 0.75f, 0);
                var box = kickObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.95f * statAlcance, 0.65f);
                box.isTrigger = true;
                kickHitbox = kickObj.AddComponent<Hitbox>();
                kickHitbox.isKickAttack = true;
                kickHitbox.Initialize(this);
            }
            else
            {
                kickHitbox.isKickAttack = true;
                kickHitbox.Initialize(this);
            }
        }

        private void Update()
        {
            if (!isAlive) return;

            // Si el combate no está activo o si el menú de selección / personalizador / modal está abierto, congelar input y ataques
            bool isMenuOpen = (BattleUI.Instance != null && BattleUI.Instance.characterSelectPanel != null && BattleUI.Instance.characterSelectPanel.activeSelf)
                           || (BattleUI.Instance != null && BattleUI.Instance.faceCustomizerPanel != null && BattleUI.Instance.faceCustomizerPanel.activeSelf)
                           || (BattleUI.Instance != null && BattleUI.Instance.winnerModalPanel != null && BattleUI.Instance.winnerModalPanel.activeSelf);

            if (isMenuOpen || BattleManager.Instance == null || !BattleManager.Instance.IsMatchActive)
            {
                currentHorizontalInput = 0f;
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
                stateMachine.Tick(FighterState.IDLE);
                if (bodyController != null)
                {
                    bodyController.UpdateAnimation(0f, isGrounded, false);
                }
                return;
            }

            // Orientación hacia el oponente
            if (!estaEnStun && !isFullStunned)
            {
                UpdateFacingDirection();
            }

            // Decadencia natural de la barra de Stun si no recibe golpes por 2 segundos
            if (!isFullStunned && currentStun > 0 && Time.time - lastHitReceivedTime > 2.0f)
            {
                currentStun = Mathf.Max(0f, currentStun - Time.deltaTime * 24f);
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.UpdateStun(playerId, currentStun, maxStun);
                }
            }

            // Expiración de combo si pasan 1.2 segundos sin conectar
            if (currentComboHits > 0 && Time.time - lastComboTime > 1.2f)
            {
                currentComboHits = 0;
                currentComboDamage = 0;
            }

            // Controles / IA si no está en aturdimiento
            if (!estaEnStun && !isFullStunned)
            {
                if (isTrainingDummy)
                {
                    // El dummy de entrenamiento no ataca, se mantiene en neutral
                    currentHorizontalInput = 0f;
                }
                else if (isAI)
                {
                    HandleAIUpdate();
                }
                else
                {
                    HandlePlayerInput();
                }
            }

            // Regeneración progresiva de salud del dummy en modo entrenamiento tras terminar el combo
            if (isTrainingDummy && isAlive && currentHealth < maxHealth && (Time.time - lastDummyHitTime > 1.1f))
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(1, Mathf.RoundToInt(maxHealth * 3f * Time.deltaTime)));
                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.UpdateHealth(playerId, currentHealth, maxHealth);
                }
            }

            // Máquina de estados (IDLE / WALKING / JUMPING / ATTACKING / BLOCKING / HIT / KNOCKED_OUT)
            stateMachine.Tick(DeriveState());

            // Animación del cuerpo
            if (bodyController != null)
            {
                bodyController.UpdateAnimation(rb.linearVelocity.x, isGrounded, IsGuarding());
            }
        }

        private readonly FighterStateMachine stateMachine = new FighterStateMachine();
        private readonly CommandRecognizer commands = new CommandRecognizer();
        public FighterState CurrentState => stateMachine.state;
        public int StateFrame => stateMachine.frame;

        private FighterState DeriveState()
        {
            if (!isAlive) return FighterState.KNOCKED_OUT;
            if (estaEnStun || isFullStunned) return FighterState.HIT;
            if (isAttacking) return FighterState.ATTACKING;
            if (!isGrounded) return FighterState.JUMPING;
            if (IsGuarding()) return FighterState.BLOCKING;
            if (Mathf.Abs(currentHorizontalInput) > 0.2f) return FighterState.WALKING;
            return FighterState.IDLE;
        }

        private void UpdateFacingDirection()
        {
            if (opponent == null || isAttacking || estaEnStun || isFullStunned) return;

            float dirToOpponent = opponent.position.x - transform.position.x;
            if (Mathf.Abs(dirToOpponent) > 0.3f)
            {
                facingDirection = dirToOpponent > 0 ? 1f : -1f;
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
            }
        }

        /// <summary>
        /// Comprueba si el jugador está bloqueando (caminando en dirección opuesta al rival).
        /// </summary>
        public bool IsGuarding()
        {
            if (opponent == null || isAttacking || isFullStunned || !isGrounded) return false;

            float dirToOpp = opponent.position.x - transform.position.x;
            // Si el oponente está a la derecha y presionamos izquierda (o viceversa)
            if (dirToOpp > 0 && currentHorizontalInput < -0.2f) return true;
            if (dirToOpp < 0 && currentHorizontalInput > 0.2f) return true;

            return false;
        }

        private void HandlePlayerInput()
        {
            if (estaEnStun || isFullStunned || !isAlive) return;

            currentHorizontalInput = 0f;
            bool jumpPressed = false;
            bool lpPressed = false;
            bool hpPressed = false;
            bool lkPressed = false;
            bool hkPressed = false;
            bool specialPressed = false;
            bool grabPressed = false;

            if (playerId == 1)
            {
                if (FightInput.GetP1Left()) currentHorizontalInput -= 1f;
                if (FightInput.GetP1Right()) currentHorizontalInput += 1f;
                jumpPressed = FightInput.GetP1Jump();
                lpPressed = FightInput.GetP1Punch();
                hpPressed = FightInput.GetP1HeavyPunch();
                lkPressed = FightInput.GetP1Kick();
                hkPressed = FightInput.GetP1HeavyKick();
                specialPressed = FightInput.GetP1Special();
                grabPressed = FightInput.GetP1Grab();
            }
            else
            {
                if (FightInput.GetP2Left()) currentHorizontalInput -= 1f;
                if (FightInput.GetP2Right()) currentHorizontalInput += 1f;
                jumpPressed = FightInput.GetP2Jump();
                lpPressed = FightInput.GetP2Punch();
                hpPressed = FightInput.GetP2HeavyPunch();
                lkPressed = FightInput.GetP2Kick();
                hkPressed = FightInput.GetP2HeavyKick();
                specialPressed = FightInput.GetP2Special();
                grabPressed = FightInput.GetP2Grab();
            }

            // Movimiento horizontal ajustado por atributo velocidad
            float finalSpeed = moveSpeed * statVelocidad;
            if (!isAttacking)
            {
                rb.linearVelocity = new Vector2(currentHorizontalInput * finalSpeed, rb.linearVelocity.y);
            }

            // Salto
            if (jumpPressed && isGrounded && !isAttacking)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                isGrounded = false;
            }

            // Ejecución de ataques
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                // Reconocedor de comandos: ↓ ↘ → + PUÑO = Hadouken (relativo a la dirección del rival)
                bool downHeld = playerId == 1 ? FightInput.GetP1Down() : FightInput.GetP2Down();
                float relX = currentHorizontalInput * facingDirection;
                commands.Feed(downHeld, relX > 0.2f, relX < -0.2f, lpPressed || hpPressed, Time.time);

                if (commands.DetectHadouken(Time.time))
                {
                    commands.Clear();
                    ExecuteHadouken();
                }
                else if (grabPressed)
                {
                    ExecuteGrab();
                }
                else if (specialPressed)
                {
                    ExecuteSpecialMove();
                }
                else if (hpPressed)
                {
                    ExecuteHeavyPunch();
                }
                else if (hkPressed)
                {
                    ExecuteHeavyKick();
                }
                else if (lpPressed)
                {
                    ExecuteLightPunch();
                }
                else if (lkPressed)
                {
                    ExecuteLightKick();
                }
            }
        }

        private void HandleAIUpdate()
        {
            if (opponent == null || estaEnStun || isFullStunned || !isAlive) return;

            float dist = Mathf.Abs(opponent.position.x - transform.position.x);
            float dirToOpponent = (opponent.position.x > transform.position.x) ? 1f : -1f;

            if (Time.time >= aiNextDecisionTime)
            {
                aiNextDecisionTime = Time.time + Random.Range(0.2f, 0.45f);

                // IA: chooseAction(distancia, vida del rival, rival atacando)
                FighterController oppFighter = opponent.GetComponent<FighterController>();
                bool oppAttacking = oppFighter != null && oppFighter.isAttacking && dist < 1.8f;
                int oppHp = oppFighter != null ? Mathf.RoundToInt(oppFighter.HealthPercent * 1000f) : 1000;
                AIAction aiAction = FighterAI.ChooseAction(Mathf.RoundToInt(dist * FighterAI.UnitsToPixels), oppHp, oppAttacking);

                if (aiAction == AIAction.DEFEND && Random.value < 0.40f)
                {
                    aiMoveDir = -dirToOpponent; // Retroceder para bloquear
                    currentHorizontalInput = aiMoveDir;
                }
                else if (aiAction == AIAction.APPROACH)
                {
                    // Zoning: lanza Hadouken desde lejos
                    if (dist > 2.6f && Time.time >= nextAttackTime && !isAttacking && Random.value < 0.18f)
                    {
                        ExecuteHadouken();
                    }
                    aiMoveDir = dirToOpponent; // Acercarse
                    currentHorizontalInput = aiMoveDir;
                }
                else if (aiAction == AIAction.RETREAT && Random.value < 0.5f)
                {
                    aiMoveDir = -dirToOpponent; // Tomar distancia
                    currentHorizontalInput = aiMoveDir;
                }
                else
                {
                    currentHorizontalInput = 0f;
                    aiMoveDir = Random.value < 0.25f ? -dirToOpponent : 0f;

                    if (Time.time >= nextAttackTime && !isAttacking)
                    {
                        float atkRoll = Random.value;
                        if (isRageActive && atkRoll < 0.35f)
                        {
                            ExecuteSpecialMove();
                        }
                        else if (atkRoll < 0.30f)
                        {
                            ExecuteLightPunch();
                        }
                        else if (atkRoll < 0.55f)
                        {
                            ExecuteHeavyPunch();
                        }
                        else if (atkRoll < 0.75f)
                        {
                            ExecuteLightKick();
                        }
                        else if (atkRoll < 0.90f)
                        {
                            ExecuteHeavyKick();
                        }
                        else if (dist < 1.2f)
                        {
                            ExecuteGrab();
                        }
                    }
                }

                // Salto ocasional de la IA
                if (Random.value < 0.12f && isGrounded && !isAttacking)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    isGrounded = false;
                }
            }

            if (!isAttacking)
            {
                rb.linearVelocity = new Vector2(aiMoveDir * (moveSpeed * statVelocidad * 0.85f), rb.linearVelocity.y);
            }
        }

        // ==================== SISTEMA DE ATAQUES Y ESPECIALES ====================

        public void ExecutePunch() => ExecuteLightPunch();
        public void ExecuteKick() => ExecuteLightKick();

        /// <summary>
        /// Puño Ligero (LP): Rápido, bajo daño, fácil de enlazar en combos.
        /// </summary>
        public void ExecuteLightPunch()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive) return;

            Attack atk = AttackLibrary.LightPunch;
            float startup = atk.StartupSeconds;
            float active = atk.ActiveSeconds;
            float recovery = atk.RecoverySeconds;
            int dmg = Mathf.RoundToInt(atk.damage * statFuerza * (isRageActive ? 1.25f : 1.0f));

            if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
            {
                BattleUI.Instance.ShowTrainingMove("Jab Ligero [F]", dmg);
            }

            StartAttackRoutine(startup, active, recovery, dmg, atk.knockback, false, isPunch: true);
        }

        /// <summary>
        /// Puño Fuerte (HP): Lento, gran impacto, alto daño y retroceso.
        /// </summary>
        public void ExecuteHeavyPunch()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive) return;

            Attack atk = AttackLibrary.HeavyPunch;
            float startup = atk.StartupSeconds;
            float active = atk.ActiveSeconds;
            float recovery = atk.RecoverySeconds;
            int dmg = Mathf.RoundToInt(atk.damage * statFuerza * (isRageActive ? 1.25f : 1.0f));

            if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
            {
                BattleUI.Instance.ShowTrainingMove("Cross Fuerte [R]", dmg);
            }

            StartAttackRoutine(startup, active, recovery, dmg, atk.knockback, true, isPunch: true);
        }

        /// <summary>
        /// Patada Ligera (LK): Ataque ágil medio.
        /// </summary>
        public void ExecuteLightKick()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive) return;

            Attack atk = AttackLibrary.LightKick;
            float startup = atk.StartupSeconds;
            float active = atk.ActiveSeconds;
            float recovery = atk.RecoverySeconds;
            int dmg = Mathf.RoundToInt(atk.damage * statFuerza * (isRageActive ? 1.25f : 1.0f));

            if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
            {
                BattleUI.Instance.ShowTrainingMove("Patada Baja [G]", dmg);
            }

            StartAttackRoutine(startup, active, recovery, dmg, atk.knockback, false, isPunch: false);
        }

        /// <summary>
        /// Patada Fuerte (HK): Patada demoledora que empuja al rival.
        /// </summary>
        public void ExecuteHeavyKick()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive) return;

            Attack atk = AttackLibrary.HeavyKick;
            float startup = atk.StartupSeconds;
            float active = atk.ActiveSeconds;
            float recovery = atk.RecoverySeconds;
            int dmg = Mathf.RoundToInt(atk.damage * statFuerza * (isRageActive ? 1.25f : 1.0f));

            if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
            {
                BattleUI.Instance.ShowTrainingMove("Patada Alta [T]", dmg);
            }

            StartAttackRoutine(startup, active, recovery, dmg, atk.knockback, true, isPunch: false);
        }

        /// <summary>
        /// HADOUKEN (↓ ↘ → + PUÑO): lanza un orbe de energía. Comando reconocido por CommandRecognizer.
        /// </summary>
        public void ExecuteHadouken()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive) return;

            Attack atk = AttackLibrary.Hadouken;
            int dmg = Mathf.RoundToInt(atk.damage * statFuerza * (isRageActive ? 1.25f : 1.0f));
            nextAttackTime = Time.time + atk.TotalSeconds + 0.35f;

            if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
            {
                BattleUI.Instance.ShowTrainingMove("Hadouken [↓↘→ + F]", dmg);
            }

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(HadoukenRoutine(atk, dmg));
        }

        private IEnumerator HadoukenRoutine(Attack atk, int dmg)
        {
            isAttacking = true;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (faceController != null) faceController.ShowTemporaryFace(FaceType.Enojo, atk.StartupSeconds + atk.ActiveSeconds + 0.2f);
            if (faceControllerRight != null) faceControllerRight.ShowTemporaryFace(FaceType.Enojo, atk.StartupSeconds + atk.ActiveSeconds + 0.2f);
            if (bodyController != null) bodyController.PlayHadoukenAnimation(atk.StartupSeconds, atk.ActiveSeconds, atk.RecoverySeconds);

            yield return new WaitForSeconds(atk.StartupSeconds);

            if (isAlive && !estaEnStun && !isFullStunned)
            {
                if (FightAudioManager.Instance != null)
                {
                    FightAudioManager.Instance.PlayHadouken();
                }
                PlayVoice(VoiceType.Special);
                Vector3 spawn = transform.position + new Vector3(facingDirection * 0.95f, 1.35f, 0f);
                Fireball.Spawn(this, spawn, facingDirection, dmg, atk.knockback);
            }

            yield return new WaitForSeconds(atk.ActiveSeconds + atk.RecoverySeconds);

            isAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Movimiento Especial / Super Embestida (→ → + PUÑO / Special):
        /// Carga frontal de alta velocidad con cara de Rabia 😡 y estela de combate.
        /// </summary>
        public void ExecuteSpecialMove()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive) return;

            if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
            {
                int dmgPreview = Mathf.RoundToInt(38 * statFuerza * (isRageActive ? 1.35f : 1.0f));
                BattleUI.Instance.ShowTrainingMove("Ataque Especial [Q]", dmgPreview);
            }

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(SpecialDashPunchRoutine());
        }

        private IEnumerator SpecialDashPunchRoutine()
        {
            isAttacking = true;
            nextAttackTime = Time.time + 0.65f;

            if (faceController != null) faceController.ShowTemporaryFace(FaceType.Enojo, 0.50f);
            if (faceControllerRight != null) faceControllerRight.ShowTemporaryFace(FaceType.Enojo, 0.50f);
            if (bodyController != null) bodyController.PlayPunchAnimation(0.08f, 0.25f, 0.15f);

            PlayVoice(VoiceType.Special);

            // Impulso hacia adelante
            rb.linearVelocity = new Vector2(facingDirection * 10f * statVelocidad, rb.linearVelocity.y);

            int dmg = Mathf.RoundToInt(38 * statFuerza * (isRageActive ? 1.35f : 1.0f));
            if (punchHitbox != null)
            {
                punchHitbox.ActivateHitbox(dmg, 10.5f, true);
            }

            yield return new WaitForSeconds(0.25f);

            if (punchHitbox != null) punchHitbox.DeactivateHitbox();

            yield return new WaitForSeconds(0.18f);

            isAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Agarre Clásico: Rompe la guardia del rival si está a corta distancia (inbloqueable).
        /// </summary>
        public void ExecuteGrab()
        {
            if (isAttacking || estaEnStun || isFullStunned || !isAlive || opponent == null) return;

            float dist = Vector2.Distance(transform.position, opponent.position);
            if (dist < 1.4f)
            {
                FighterController oppFighter = opponent.GetComponent<FighterController>();
                if (oppFighter != null && oppFighter.IsAlive)
                {
                    isAttacking = true;
                    nextAttackTime = Time.time + 0.60f;

                    if (faceController != null) faceController.ShowTemporaryFace(FaceType.Enojo, 0.45f);
                    if (faceControllerRight != null) faceControllerRight.ShowTemporaryFace(FaceType.Enojo, 0.45f);
                    if (CombatEffectsManager.Instance != null)
                    {
                        CombatEffectsManager.Instance.SpawnHitEffect((transform.position + opponent.position) * 0.5f, true);
                    }

                    // Daño directo inbloqueable
                    int dmg = Mathf.RoundToInt(22 * statFuerza);

                    if (playerId == 1 && BattleUI.Instance != null && BattleManager.Instance != null && BattleManager.Instance.isTrainingMode)
                    {
                        BattleUI.Instance.ShowTrainingMove("Agarre / Proyección [H]", dmg);
                    }

                    oppFighter.RecibirImpacto(dmg, 8.5f, transform.position.x, this, isHeavy: true, ignoreGuard: true);

                    RegisterComboHit(dmg);

                    StartCoroutine(FinishGrabRoutine());
                }
            }
        }

        private IEnumerator FinishGrabRoutine()
        {
            yield return new WaitForSeconds(0.40f);
            isAttacking = false;
        }

        private void StartAttackRoutine(float startup, float active, float recovery, int dmg, float knockback, bool isHeavy, bool isPunch)
        {
            nextAttackTime = Time.time + startup + active + recovery + 0.05f;
            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(AttackFrameDataRoutine(startup, active, recovery, dmg, knockback, isHeavy, isPunch));
        }

        private IEnumerator AttackFrameDataRoutine(float startup, float active, float recovery, int dmg, float knockback, bool isHeavy, bool isPunch)
        {
            isAttacking = true;

            Hitbox targetBox = isPunch ? punchHitbox : kickHitbox;
            if (targetBox != null) targetBox.DeactivateHitbox();

            // Cara de Enojo/Rabia durante el golpe
            if (faceController != null) faceController.ShowTemporaryFace(FaceType.Enojo, startup + active);
            if (faceControllerRight != null) faceControllerRight.ShowTemporaryFace(FaceType.Enojo, startup + active);

            if (FightAudioManager.Instance != null)
            {
                FightAudioManager.Instance.PlayWhoosh();
            }

            if (isHeavy)
            {
                PlayVoice(VoiceType.Attack);
            }

            if (bodyController != null)
            {
                if (isPunch) bodyController.PlayPunchAnimation(startup, active, recovery, isHeavy);
                else bodyController.PlayKickAnimation(startup, active, recovery, isHeavy);
            }

            yield return new WaitForSeconds(startup);

            // ACTIVE: Activación de hitbox ofensiva
            if (targetBox != null && isAlive && !estaEnStun && !isFullStunned)
            {
                targetBox.ActivateHitbox(dmg, knockback, isHeavy);
            }

            yield return new WaitForSeconds(active);

            // RECOVERY: Desactivar hitbox y retorno a guardia
            if (targetBox != null) targetBox.DeactivateHitbox();

            yield return new WaitForSeconds(recovery);

            isAttacking = false;
            activeAttackRoutine = null;
        }

        // ==================== SISTEMA DE IMPACTOS, STUN Y DAÑO FACIAL ====================

        public void RecibirImpacto(int danio, float fuerzaEmpuje, float origenX)
        {
            RecibirImpacto(danio, fuerzaEmpuje, origenX, null, false, false);
        }

        public void RecibirImpacto(int danio, float fuerzaEmpuje, float origenX, FighterController atacante, bool isHeavy = false, bool ignoreGuard = false)
        {
            if (!isAlive) return;

            lastHitReceivedTime = Time.time;

            // 1. COMPROBAR BLOQUEO (DEFENSA ←)
            if (!ignoreGuard && IsGuarding())
            {
                // Daño reducido un 65% (chip damage tangible)
                int danioBloqueado = Mathf.Max(2, Mathf.RoundToInt(danio * 0.35f));
                currentHealth = Mathf.Max(1, currentHealth - danioBloqueado);

                // Acumular stun reducido
                currentStun = Mathf.Min(maxStun, currentStun + (isHeavy ? 10f : 5f));

                if (CombatEffectsManager.Instance != null)
                {
                    CombatEffectsManager.Instance.SpawnBlockEffect(transform.position + Vector3.up * 1.1f);
                }

                // Ligero empuje hacia atrás sin interrumpir guardia
                float dirEmpuje = transform.position.x > origenX ? 1f : -1f;
                rb.linearVelocity = new Vector2(dirEmpuje * fuerzaEmpuje * 0.4f, rb.linearVelocity.y);

                if (BattleUI.Instance != null)
                {
                    BattleUI.Instance.UpdateHealth(playerId, currentHealth, maxHealth);
                    BattleUI.Instance.UpdateStun(playerId, currentStun, maxStun);
                }
                return;
            }

            // 2. IMPACTO LIMPIO (SIN BLOQUEAR)
            int danioFinal = Mathf.Max(1, Mathf.RoundToInt(danio / Mathf.Clamp(statDefensa, 0.75f, 1.25f)));
            currentHealth = Mathf.Max(0, currentHealth - danioFinal);

            // Acumular Stun: +15 en golpe normal, +28 en golpe pesado
            currentStun += isHeavy ? 28f : 15f;
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateStun(playerId, currentStun, maxStun);
            }

            // Reacción cómica física Munch-Punch (squash & stretch elástico)
            int hitDir = (origenX < transform.position.x) ? 1 : -1;
            if (faceController != null)
            {
                faceController.TriggerMunchPunchDeform(isHeavy ? 1.4f : 0.9f, hitDir);
            }
            if (faceControllerRight != null)
            {
                faceControllerRight.TriggerMunchPunchDeform(isHeavy ? 1.4f : 0.9f, hitDir);
            }

            // Reacción interactiva del público en las gradas
            if (CrowdCheerManager.Instance != null)
            {
                CrowdCheerManager.Instance.CheerOnHit(atacante != null ? atacante.currentComboHits : 1);
            }

            // Notificar combo al atacante
            if (atacante != null)
            {
                atacante.RegisterComboHit(danioFinal);
            }

            // Cancelar ataque en curso
            if (isAttacking)
            {
                isAttacking = false;
                if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
                if (punchHitbox != null) punchHitbox.DeactivateHitbox();
                if (kickHitbox != null) kickHitbox.DeactivateHitbox();
            }

            // 3. ACTIVAR SISTEMA RAGE (A <= 20% HP)
            if (currentHealth > 0 && currentHealth <= Mathf.RoundToInt(maxHealth * 0.20f) && !isRageActive)
            {
                ActivateRage();
            }

            // 4. VERIFICAR DERROTA / STUN COMPLETO / HITSTUN NORMAL
            if (isTrainingDummy)
            {
                lastDummyHitTime = Time.time;
                currentHealth = Mathf.Max(1, currentHealth);
                if (activeHitstunRoutine != null) StopCoroutine(activeHitstunRoutine);
                activeHitstunRoutine = StartCoroutine(RutinaHitstun(isHeavy ? 0.32f : 0.20f, fuerzaEmpuje, origenX, isHeavy));
            }
            else if (currentHealth <= 0)
            {
                Die();
            }
            else if (currentStun >= maxStun && !isFullStunned)
            {
                TriggerFullStun();
            }
            else
            {
                PlayVoice(VoiceType.Hurt);
                if (activeHitstunRoutine != null) StopCoroutine(activeHitstunRoutine);
                activeHitstunRoutine = StartCoroutine(RutinaHitstun(isHeavy ? 0.32f : 0.20f, fuerzaEmpuje, origenX, isHeavy));
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateHealth(playerId, currentHealth, maxHealth);
            }
        }

        public void RegisterComboHit(int dmg)
        {
            if (Time.time - lastComboTime < 1.2f)
            {
                currentComboHits++;
                currentComboDamage += dmg;
            }
            else
            {
                currentComboHits = 1;
                currentComboDamage = dmg;
            }

            lastComboTime = Time.time;
            if (currentComboHits > maxComboHits) maxComboHits = currentComboHits;

            if (BattleUI.Instance != null && currentComboHits >= 2)
            {
                BattleUI.Instance.ShowComboNotification(playerId, currentComboHits, currentComboDamage);
            }
        }

        private void ActivateRage()
        {
            isRageActive = true;
            Debug.Log($"[RAGE] ¡{fighterName} ha entrado en modo Furia (RAGE READY)!");

            if (faceController != null)
            {
                faceController.SetRageMode(true);
            }
            if (faceControllerRight != null)
            {
                faceControllerRight.SetRageMode(true);
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.SetRageBadge(playerId, true);
                BattleUI.Instance.ShowBanner($"¡{fighterName.ToUpper()} ENTRA EN MODO RAGE!", 1.8f);
            }
        }

        private IEnumerator RutinaHitstun(float duracion, float fuerza, float origenX, bool isHeavy)
        {
            estaEnStun = true;

            // Foto DOLOR 😖 en golpes fuertes o críticos
            if (faceController != null)
            {
                if (isHeavy) faceController.SetFace(FaceType.Dolor);
                else faceController.ShowTemporaryFace(FaceType.Dolor, duracion);
            }
            if (faceControllerRight != null)
            {
                if (isHeavy) faceControllerRight.SetFace(FaceType.Dolor);
                else faceControllerRight.ShowTemporaryFace(FaceType.Dolor, duracion);
            }

            if (bodyController != null)
            {
                bodyController.PlayHurtAnimation(duracion);
            }

            float direccion = transform.position.x > origenX ? 1f : -1f;
            rb.linearVelocity = new Vector2(direccion * fuerza, rb.linearVelocity.y * 0.5f);

            yield return new WaitForSeconds(duracion);

            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (faceController != null && isAlive && !isFullStunned)
            {
                faceController.ResetToBaseFace();
            }
            if (faceControllerRight != null && isAlive && !isFullStunned)
            {
                faceControllerRight.ResetToBaseFace();
            }

            estaEnStun = false;
            activeHitstunRoutine = null;
        }

        private void TriggerFullStun()
        {
            isFullStunned = true;
            currentStun = maxStun;

            if (activeStunRoutine != null) StopCoroutine(activeStunRoutine);
            activeStunRoutine = StartCoroutine(FullStunRoutine(2.0f));
        }

        private IEnumerator FullStunRoutine(float duration)
        {
            // Cara de DOLOR 😖 permanente durante todo el aturdimiento
            if (faceController != null) faceController.SetFace(FaceType.Dolor);
            if (faceControllerRight != null) faceControllerRight.SetFace(FaceType.Dolor);

            if (CombatEffectsManager.Instance != null)
            {
                CombatEffectsManager.Instance.SpawnStunStars(faceController != null ? faceController.transform : transform, duration);
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowStunBadge(playerId, true);
            }

            rb.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(duration);

            currentStun = 0f;
            isFullStunned = false;

            if (faceController != null && isAlive)
            {
                faceController.ResetToBaseFace();
            }
            if (faceControllerRight != null && isAlive)
            {
                faceControllerRight.ResetToBaseFace();
            }

            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.ShowStunBadge(playerId, false);
                BattleUI.Instance.UpdateStun(playerId, 0f, maxStun);
            }

            activeStunRoutine = null;
        }

        public void TakeDamage(int amount, Vector2 knockback)
        {
            float originX = transform.position.x - Mathf.Sign(knockback.x);
            RecibirImpacto(amount, knockback.magnitude, originX);
        }

        private void Die()
        {
            isAlive = false;
            estaEnStun = false;
            isFullStunned = false;
            currentHealth = 0;

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            if (activeHitstunRoutine != null) StopCoroutine(activeHitstunRoutine);
            if (activeStunRoutine != null) StopCoroutine(activeStunRoutine);

            if (punchHitbox != null) punchHitbox.DeactivateHitbox();
            if (kickHitbox != null) kickHitbox.DeactivateHitbox();

            // 1. CARA DE KO
            if (faceController != null)
            {
                faceController.SetFace(FaceType.KO);
            }
            if (faceControllerRight != null)
            {
                faceControllerRight.SetFace(FaceType.KO);
            }

            // 2. Animación de caída en el ring
            if (bodyController != null)
            {
                bodyController.PlayKODefeatedAnimation();
            }

            PlayVoice(VoiceType.KO);

            // Remate cómico final (Pastelazo de merengue o Yunque 100 Tons con pajaritos mareados)
            if (ComicFinisherManager.Instance != null)
            {
                int attDir = (opponent != null && opponent.position.x < transform.position.x) ? 1 : -1;
                ComicFinisherManager.Instance.TriggerComicFinisher(transform.position, attDir);
            }

            // Ovación y confeti del público en las gradas
            if (CrowdCheerManager.Instance != null)
            {
                CrowdCheerManager.Instance.CheerOnKO();
            }

            rb.linearVelocity = Vector2.zero;

            // 3. Ganador celebra con FOTO GANADOR 😎
            if (opponent != null)
            {
                FighterController winnerFighter = opponent.GetComponent<FighterController>();
                if (winnerFighter != null)
                {
                    winnerFighter.TriggerVictory();
                }

                if (DynamicFightCamera.Instance != null)
                {
                    DynamicFightCamera.Instance.TriggerKOZoom(opponent);
                }
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnFighterKODefeated(this);
            }
        }

        /// <summary>
        /// Victoria: El luchador triunfador exhibe su Foto Ganador (Foto 4).
        /// </summary>
        public void TriggerVictory()
        {
            if (!isAlive) return;

            if (faceController != null)
            {
                faceController.SetFace(FaceType.Ganador);
            }
            if (faceControllerRight != null)
            {
                faceControllerRight.SetFace(FaceType.Ganador);
            }

            if (bodyController != null)
            {
                bodyController.PlayVictoryAnimation();
            }

            PlayVoice(VoiceType.Victory);

            rb.linearVelocity = Vector2.zero;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            foreach (var contact in collision.contacts)
            {
                if (contact.normal.y > 0.6f)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        public void ResetFighter(Vector3 startPos)
        {
            transform.position = startPos;
            currentHealth = maxHealth;
            currentStun = 0f;
            isAlive = true;
            isAttacking = false;
            estaEnStun = false;
            isFullStunned = false;
            isRageActive = false;
            currentComboHits = 0;
            currentComboDamage = 0;
            rb.linearVelocity = Vector2.zero;

            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeHitstunRoutine != null) { StopCoroutine(activeHitstunRoutine); activeHitstunRoutine = null; }
            if (activeStunRoutine != null) { StopCoroutine(activeStunRoutine); activeStunRoutine = null; }

            if (punchHitbox != null) punchHitbox.DeactivateHitbox();
            if (kickHitbox != null) kickHitbox.DeactivateHitbox();

            if (faceController != null)
            {
                faceController.SetRageMode(false);
                faceController.ResetToBaseFace();
            }
            if (faceControllerRight != null)
            {
                faceControllerRight.SetRageMode(false);
                faceControllerRight.ResetToBaseFace();
            }
            if (bodyController != null)
            {
                bodyController.ResetBody();
            }
        }
    }
}
