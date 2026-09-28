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
            // Si no hay hitboxes asignadas en el inspector, las creamos automáticamente
            if (punchHitbox == null)
            {
                GameObject punchObj = new GameObject("PunchHitbox");
                punchObj.transform.SetParent(transform, false);
                punchObj.transform.localPosition = new Vector3(0.7f, 0.8f, 0);
                var box = punchObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.6f, 0.5f);
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
                kickObj.transform.localPosition = new Vector3(0.8f, 0.2f, 0);
                var box = kickObj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.7f, 0.5f);
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

            // Mirar siempre hacia el oponente
            UpdateFacingDirection();

            if (isAI)
            {
                HandleAIUpdate();
            }
            else
            {
                HandlePlayerInput();
            }

            // Actualizar animaciones del cuerpo
            if (bodyController != null)
            {
                bodyController.UpdateAnimation(rb.linearVelocity.x, isGrounded);
            }
        }

        private void UpdateFacingDirection()
        {
            if (opponent == null || isAttacking) return;

            float dirToOpponent = opponent.position.x - transform.position.x;
            if (Mathf.Abs(dirToOpponent) > 0.3f)
            {
                facingDirection = dirToOpponent > 0 ? 1f : -1f;
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
            }
        }

        private void HandlePlayerInput()
        {
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

            // Ataques
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
            if (opponent == null) return;

            float dist = Mathf.Abs(opponent.position.x - transform.position.x);

            if (Time.time >= aiNextDecisionTime)
            {
                aiNextDecisionTime = Time.time + Random.Range(0.2f, 0.6f);

                // Si está lejos, acercarse
                if (dist > 1.6f)
                {
                    aiMoveDir = (opponent.position.x > transform.position.x) ? 1f : -1f;
                }
                // Si está cerca, pelear
                else
                {
                    aiMoveDir = Random.value < 0.3f ? ((opponent.position.x > transform.position.x) ? -1f : 1f) : 0f;

                    if (Time.time >= nextAttackTime && !isAttacking)
                    {
                        float atkRoll = Random.value;
                        if (atkRoll < 0.55f)
                        {
                            ExecutePunch();
                        }
                        else if (atkRoll < 0.85f)
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
        /// Ejecuta un Puñetazo:
        /// 1. Activa la cara de ENOJO y ODIO durante el golpe.
        /// 2. Dispara la animación de puño del cuerpo.
        /// 3. Habilita la hitbox de puño.
        /// </summary>
        public void ExecutePunch()
        {
            if (isAttacking || !isAlive) return;

            isAttacking = true;
            nextAttackTime = Time.time + punchDuration + 0.1f;

            // 1. CARA DE ENOJO Y ODIO
            if (faceController != null)
            {
                faceController.ShowTemporaryFace(FaceType.Enojo, punchDuration);
            }

            // 2. Animación del cuerpo
            if (bodyController != null)
            {
                bodyController.PlayPunchAnimation(punchDuration);
            }

            // 3. Activar Hitbox
            if (punchHitbox != null)
            {
                punchHitbox.ActivateHitbox(punchDamage, 5f, false);
            }

            StartCoroutine(EndAttackRoutine(punchDuration, punchHitbox));
        }

        /// <summary>
        /// Ejecuta una Patada:
        /// 1. Activa la cara de ENOJO y ODIO con mayor intensidad.
        /// 2. Dispara la animación de patada.
        /// 3. Habilita la hitbox de patada.
        /// </summary>
        public void ExecuteKick()
        {
            if (isAttacking || !isAlive) return;

            isAttacking = true;
            nextAttackTime = Time.time + kickDuration + 0.15f;

            // 1. CARA DE ENOJO Y ODIO
            if (faceController != null)
            {
                faceController.ShowTemporaryFace(FaceType.Enojo, kickDuration);
            }

            // 2. Animación del cuerpo
            if (bodyController != null)
            {
                bodyController.PlayKickAnimation(kickDuration);
            }

            // 3. Activar Hitbox
            if (kickHitbox != null)
            {
                kickHitbox.ActivateHitbox(kickDamage, 8f, true);
            }

            StartCoroutine(EndAttackRoutine(kickDuration, kickHitbox));
        }

        private IEnumerator EndAttackRoutine(float duration, Hitbox hitbox)
        {
            yield return new WaitForSeconds(duration);
            if (hitbox != null)
            {
                hitbox.DeactivateHitbox();
            }
            isAttacking = false;
        }

        /// <summary>
        /// Aplica daño al recibir un golpe:
        /// 1. Resta vida.
        /// 2. Activa la cara de DOLOR.
        /// 3. Aplica retroceso físico.
        /// 4. Dispara animación de daño.
        /// 5. Si la vida llega a 0, activa la cara de KO.
        /// </summary>
        public void TakeDamage(int amount, Vector2 knockback)
        {
            if (!isAlive) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);

            // Cancelar ataque si estaba golpeando
            if (isAttacking)
            {
                isAttacking = false;
                if (punchHitbox != null) punchHitbox.DeactivateHitbox();
                if (kickHitbox != null) kickHitbox.DeactivateHitbox();
            }

            // Aplicar retroceso físico
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);

            if (currentHealth > 0)
            {
                // 1. CARA DE DOLOR
                float hurtTime = 0.35f;
                if (faceController != null)
                {
                    faceController.ShowTemporaryFace(FaceType.Dolor, hurtTime);
                }

                if (bodyController != null)
                {
                    bodyController.PlayHurtAnimation(hurtTime);
                }
            }
            else
            {
                Die();
            }

            // Notificar a la UI
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.UpdateHealth(playerId, currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// El luchador ha sido derrotado (K.O.):
        /// 1. Activa permanentemente la cara de KO (ojos cerrados/en cruz).
        /// 2. Se detiene el movimiento.
        /// 3. Se reproduce animación de caída cómica.
        /// 4. Se declara el ganador en el BattleManager.
        /// </summary>
        private void Die()
        {
            isAlive = false;
            currentHealth = 0;

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
            rb.linearVelocity = Vector2.zero;

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
