using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Controlador principal del Luchador 2D:
    /// Gestiona salud, física, movimiento, ataques, IA bot, y la reacción
    /// entre las 4 caras (Base, Enojo al atacar, Dolor al ser golpeado, KO al morir).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FighterController : MonoBehaviour
    {
        [Header("Identificación")]
        public int playerId = 1;
        public string fighterName = "Luchador";
        public bool isAI = false;

        [Header("Estadísticas")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public float moveSpeed = 4.5f;
        public float jumpForce = 9.5f;

        [Header("Ataques")]
        public int punchDamage = 10;
        public int kickDamage = 18;
        public float punchDuration = 0.25f;
        public float kickDuration = 0.35f;

        [Header("Referencias de Componentes")]
        public DynamicFaceController faceController;
        public FighterBodyController bodyController;
        public Transform opponent;
        public Hitbox punchHitbox;
        public Hitbox kickHitbox;

        [Header("Estado y Hitstun")]
        public bool estaEnStun = false;
        private Coroutine activeAttackRoutine;
        private Coroutine activeHitstunRoutine;

        private Rigidbody2D rb;
        private bool isGrounded = true;
        private bool isAlive = true;
        private bool isAttacking = false;
        private float facingDirection = 1f;
        private float nextAttackTime = 0f;

        // IA timers
        private float aiNextDecisionTime = 0f;
        private float aiMoveDir = 0f;

        public bool IsAlive => isAlive;
        public float FacingDirection => facingDirection;
        public float HealthPercent => (float)currentHealth / maxHealth;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            currentHealth = maxHealth;
            isAlive = true;
            estaEnStun = false;
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
            // Registrar Hurtbox corporal para detección de impacto estilo Street Fighter
            var hurtbox = GetComponent<Hurtbox>();
            if (hurtbox == null) hurtbox = gameObject.AddComponent<Hurtbox>();
            hurtbox.ownerFighter = this;

            // Si no hay hitboxes asignadas en el inspector, las creamos automáticamente
            if (punchHitbox == null)
            {
                GameObject punchObj = new GameObject("PunchHitbox");
                punchObj.transform.SetParent(transform, false);
                punchObj.transform.localPosition = new Vector3(0.85f, 0.85f, 0);
                var box = punchObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.8f, 0.6f);
                box.isTrigger = true;
                punchHitbox = punchObj.AddComponent<Hitbox>();
                punchHitbox.Initialize(this);
            }
            else
            {
                punchHitbox.Initialize(this);
            }

            if (kickHitbox == null)
            {
                GameObject kickObj = new GameObject("KickHitbox");
                kickObj.transform.SetParent(transform, false);
                kickObj.transform.localPosition = new Vector3(0.95f, 0.35f, 0);
                var box = kickObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.9f, 0.6f);
                box.isTrigger = true;
                kickHitbox = kickObj.AddComponent<Hitbox>();
                kickHitbox.Initialize(this);
            }
            else
            {
                kickHitbox.Initialize(this);
            }
        }

        private void Update()
        {
            if (!isAlive) return;

            // Mirar siempre hacia el oponente mientras no ataque ni esté aturdido
            if (!estaEnStun)
            {
                UpdateFacingDirection();
            }

            // Si está en Hitstun (aturdido por un impacto), no puede moverse ni atacar
            if (!estaEnStun)
            {
                if (isAI)
                {
                    HandleAIUpdate();
                }
                else
                {
                    HandlePlayerInput();
                }
            }

            // Actualizar animaciones del cuerpo
            if (bodyController != null)
            {
                bodyController.UpdateAnimation(rb.linearVelocity.x, isGrounded);
            }
        }

        private void UpdateFacingDirection()
        {
            if (opponent == null || isAttacking || estaEnStun) return;

            float dirToOpponent = opponent.position.x - transform.position.x;
            if (Mathf.Abs(dirToOpponent) > 0.3f)
            {
                facingDirection = dirToOpponent > 0 ? 1f : -1f;
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
            }
        }

        private void HandlePlayerInput()
        {
            if (estaEnStun || !isAlive) return;

            float horizontal = 0f;
            bool jumpPressed = false;
            bool punchPressed = false;
            bool kickPressed = false;

            if (playerId == 1)
            {
                if (FightInput.GetP1Left()) horizontal -= 1f;
                if (FightInput.GetP1Right()) horizontal += 1f;
                jumpPressed = FightInput.GetP1Jump();
                punchPressed = FightInput.GetP1Punch();
                kickPressed = FightInput.GetP1Kick();
            }
            else
            {
                if (FightInput.GetP2Left()) horizontal -= 1f;
                if (FightInput.GetP2Right()) horizontal += 1f;
                jumpPressed = FightInput.GetP2Jump();
                punchPressed = FightInput.GetP2Punch();
                kickPressed = FightInput.GetP2Kick();
            }

            // Movimiento
            if (!isAttacking)
            {
                rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
            }

            // Salto
            if (jumpPressed && isGrounded && !isAttacking)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                isGrounded = false;
            }

            // Ataques con Frame Data
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                if (punchPressed)
                {
                    ExecutePunch();
                }
                else if (kickPressed)
                {
                    ExecuteKick();
                }
            }
        }

        private void HandleAIUpdate()
        {
            if (opponent == null || estaEnStun || !isAlive) return;

            float dist = Mathf.Abs(opponent.position.x - transform.position.x);

            if (Time.time >= aiNextDecisionTime)
            {
                aiNextDecisionTime = Time.time + Random.Range(0.2f, 0.5f);

                // Si está lejos, acercase
                if (dist > 1.6f)
                {
                    aiMoveDir = (opponent.position.x > transform.position.x) ? 1f : -1f;
                }
                // Si está cerca, golpear
                else
                {
                    aiMoveDir = Random.value < 0.25f ? ((opponent.position.x > transform.position.x) ? -1f : 1f) : 0f;

                    if (Time.time >= nextAttackTime && !isAttacking)
                    {
                        float atkRoll = Random.value;
                        if (atkRoll < 0.55f)
                        {
                            ExecutePunch();
                        }
                        else if (atkRoll < 0.90f)
                        {
                            ExecuteKick();
                        }
                    }
                }

                // Salto ocasional de la IA
                if (Random.value < 0.15f && isGrounded && !isAttacking)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    isGrounded = false;
                }
            }

            if (!isAttacking)
            {
                rb.linearVelocity = new Vector2(aiMoveDir * (moveSpeed * 0.85f), rb.linearVelocity.y);
            }
        }

        /// <summary>
        /// Puñetazo basado en Frame Data:
        /// 1. Startup: Fotogramas de preparación (viento atrás, sin daño).
        /// 2. Active: Fotogramas donde el golpe colisiona (Hitbox activa, extremidad articulada extendida).
        /// 3. Recovery: Fotogramas de vulnerabilidad donde el personaje vuelve a guardia.
        /// </summary>
        public void ExecutePunch()
        {
            if (isAttacking || estaEnStun || !isAlive) return;

            float startup = 0.08f;
            float active = 0.10f;
            float recovery = 0.14f;

            nextAttackTime = Time.time + startup + active + recovery + 0.08f;

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(PunchFrameDataRoutine(startup, active, recovery));
        }

        private IEnumerator PunchFrameDataRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;

            // 1. STARTUP: Viento atrás y cara de enojo
            if (punchHitbox != null) punchHitbox.DeactivateHitbox();
            if (faceController != null) faceController.ShowTemporaryFace(FaceType.Enojo, startup + active);
            if (bodyController != null) bodyController.PlayPunchAnimation(startup, active, recovery);

            yield return new WaitForSeconds(startup);

            // 2. ACTIVE: Impacto, extremidad visible extendida y colisión activa
            if (punchHitbox != null && isAlive && !estaEnStun)
            {
                punchHitbox.ActivateHitbox(punchDamage, 5.5f, false);
            }

            yield return new WaitForSeconds(active);

            // 3. RECOVERY: Apagar hitbox y volver a guardia
            if (punchHitbox != null) punchHitbox.DeactivateHitbox();

            yield return new WaitForSeconds(recovery);

            isAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Patada pesada basada en Frame Data.
        /// </summary>
        public void ExecuteKick()
        {
            if (isAttacking || estaEnStun || !isAlive) return;

            float startup = 0.12f;
            float active = 0.12f;
            float recovery = 0.18f;

            nextAttackTime = Time.time + startup + active + recovery + 0.10f;

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            activeAttackRoutine = StartCoroutine(KickFrameDataRoutine(startup, active, recovery));
        }

        private IEnumerator KickFrameDataRoutine(float startup, float active, float recovery)
        {
            isAttacking = true;

            // 1. STARTUP: Cargar pierna
            if (kickHitbox != null) kickHitbox.DeactivateHitbox();
            if (faceController != null) faceController.ShowTemporaryFace(FaceType.Enojo, startup + active);
            if (bodyController != null) bodyController.PlayKickAnimation(startup, active, recovery);

            yield return new WaitForSeconds(startup);

            // 2. ACTIVE: Patada extendida
            if (kickHitbox != null && isAlive && !estaEnStun)
            {
                kickHitbox.ActivateHitbox(kickDamage, 8.5f, true);
            }

            yield return new WaitForSeconds(active);

            // 3. RECOVERY: Bajar pierna
            if (kickHitbox != null) kickHitbox.DeactivateHitbox();

            yield return new WaitForSeconds(recovery);

            isAttacking = false;
            activeAttackRoutine = null;
        }

        /// <summary>
        /// Aplica daño al recibir un impacto rival con sistema de Hitstun y empuje horizontal.
        /// </summary>
        public void RecibirImpacto(int danio, float fuerzaEmpuje, float origenX)
        {
            if (!isAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - danio);

            // Cancelar ataque si estaba golpeando
            if (isAttacking)
            {
                isAttacking = false;
                if (activeAttackRoutine != null)
                {
                    StopCoroutine(activeAttackRoutine);
                    activeAttackRoutine = null;
                }
                if (punchHitbox != null) punchHitbox.DeactivateHitbox();
                if (kickHitbox != null) kickHitbox.DeactivateHitbox();
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (activeHitstunRoutine != null)
                {
                    StopCoroutine(activeHitstunRoutine);
                }
                activeHitstunRoutine = StartCoroutine(RutinaHitstun(0.35f, fuerzaEmpuje, origenX));
            }

            // Notificar inmediatamente a la interfaz gráfica
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateHealth(playerId, currentHealth, maxHealth);
            }
        }

        private IEnumerator RutinaHitstun(float duracion, float fuerza, float origenX)
        {
            estaEnStun = true;

            // Cara de DOLOR durante todo el aturdimiento
            if (faceController != null)
            {
                faceController.SetFace(FaceType.Dolor);
            }

            if (bodyController != null)
            {
                bodyController.PlayHurtAnimation(duracion);
            }

            // Calcular dirección opuesta al atacante (empuje horizontal)
            float direccion = transform.position.x > origenX ? 1f : -1f;
            rb.linearVelocity = new Vector2(direccion * fuerza, rb.linearVelocity.y * 0.5f);

            yield return new WaitForSeconds(duracion);

            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // Retorno a cara normal si sigue con vida
            if (faceController != null && isAlive)
            {
                faceController.ResetToBaseFace();
            }

            estaEnStun = false;
            activeHitstunRoutine = null;
        }

        public void TakeDamage(int amount, Vector2 knockback)
        {
            float originX = transform.position.x - Mathf.Sign(knockback.x);
            RecibirImpacto(amount, knockback.magnitude, originX);
        }

        /// <summary>
        /// El luchador ha sido derrotado (K.O.):
        /// 1. Activa permanentemente la cara de KO.
        /// 2. Detiene movimiento y desactiva colisiones ofensivas.
        /// 3. Reproduce animación cómica de caída.
        /// 4. Dispara el zoom dramático de cámara.
        /// 5. Declara el ganador en el BattleManager.
        /// </summary>
        private void Die()
        {
            isAlive = false;
            estaEnStun = false;
            currentHealth = 0;

            if (activeAttackRoutine != null) StopCoroutine(activeAttackRoutine);
            if (activeHitstunRoutine != null) StopCoroutine(activeHitstunRoutine);

            if (punchHitbox != null) punchHitbox.DeactivateHitbox();
            if (kickHitbox != null) kickHitbox.DeactivateHitbox();

            // 1. CARA DE KO
            if (faceController != null)
            {
                faceController.SetFace(FaceType.KO);
            }

            // 2. Animación de KO
            if (bodyController != null)
            {
                bodyController.PlayKODefeatedAnimation();
            }

            rb.linearVelocity = Vector2.zero;

            Debug.Log($"¡[K.O.] {fighterName} (Jugador {playerId}) ha sido noqueado!");

            // 3. Zoom dramático de cámara sobre el ganador
            if (DynamicFightCamera.Instance != null && opponent != null)
            {
                DynamicFightCamera.Instance.TriggerKOZoom(opponent);
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnFighterKODefeated(this);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Detectar suelo
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
            isAlive = true;
            isAttacking = false;
            estaEnStun = false;
            rb.linearVelocity = Vector2.zero;

            if (activeAttackRoutine != null) { StopCoroutine(activeAttackRoutine); activeAttackRoutine = null; }
            if (activeHitstunRoutine != null) { StopCoroutine(activeHitstunRoutine); activeHitstunRoutine = null; }

            if (punchHitbox != null) punchHitbox.DeactivateHitbox();
            if (kickHitbox != null) kickHitbox.DeactivateHitbox();

            if (faceController != null)
            {
                faceController.ResetToBaseFace();
            }
            if (bodyController != null)
            {
                bodyController.ResetBody();
            }
        }
    }
}
