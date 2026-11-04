using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Componente de Hitbox para puños y patadas:
    /// - Activada exclusivamente durante los fotogramas Active del ataque.
    /// - Detecta la Hurtbox del rival.
    /// - Aplica daño, Hitstun y retroceso (knockback).
    /// - Dispara Hitstop arcade y sacudida de pantalla (FightImpactManager).
    /// - Genera efectos de impacto cómic ("¡POW!", "¡CRACK!").
    /// </summary>
    public class Hitbox : MonoBehaviour
    {
        [Header("Configuración de Golpe")]
        public int damage = 10;
        public float knockbackPower = 6f;
        public bool isHeavyAttack = false;
        public bool isKickAttack = false;

        private FighterController ownerFighter;
        private Collider2D col;
        private bool hasHit = false;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.enabled = false;
            }
        }

        public void Initialize(FighterController owner)
        {
            ownerFighter = owner;
        }

        public void ActivateHitbox(int dmg, float knockback, bool heavy)
        {
            damage = dmg;
            knockbackPower = knockback;
            isHeavyAttack = heavy;
            hasHit = false;

            if (col != null)
            {
                col.enabled = true;
                CheckImmediateOverlap();
            }
        }

        public void DeactivateHitbox()
        {
            if (col != null)
            {
                col.enabled = false;
            }
            hasHit = false;
        }

        private void CheckImmediateOverlap()
        {
            if (col == null || hasHit) return;

            Collider2D[] results = new Collider2D[12];
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            int count = col.Overlap(filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] != null && results[i] != col)
                {
                    ProcessHit(results[i]);
                    if (hasHit) break;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessHit(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessHit(other);
        }

        private void ProcessHit(Collider2D other)
        {
            if (hasHit || other == null) return;

            // 1. Detectar Hurtbox o FighterController del rival
            FighterController target = null;
            Hurtbox hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox != null)
            {
                target = hurtbox.ownerFighter;
            }
            if (target == null)
            {
                target = other.GetComponentInParent<FighterController>();
            }

            // Evitar golpearse a sí mismo o a un rival derrotado
            if (target != null && target != ownerFighter && target.IsAlive)
            {
                hasHit = true;
                Vector2 hitPoint = other.ClosestPoint(transform.position);

                // Origen del atacante para dirección de retroceso
                float attackerOriginX = ownerFighter != null ? ownerFighter.transform.position.x : transform.position.x;

                // 1. Aplicar daño y reacción de impacto / Hitstun
                target.RecibirImpacto(damage, knockbackPower, attackerOriginX, ownerFighter, isHeavyAttack);

                // Sonido procedural de impacto / bloqueo
                if (FightAudioManager.Instance != null)
                {
                    if (target.IsGuarding())
                    {
                        FightAudioManager.Instance.PlayBlock();
                    }
                    else if (isKickAttack)
                    {
                        FightAudioManager.Instance.PlayKick(isHeavyAttack);
                    }
                    else if (isHeavyAttack)
                    {
                        FightAudioManager.Instance.PlayHitHeavy();
                    }
                    else
                    {
                        FightAudioManager.Instance.PlayHitLight();
                    }
                }

                // 2. Activar el Hitstop arcade y sacudida sutil (sin desenfocar la cámara)
                float hitstopDuration = isHeavyAttack ? 0.065f : 0.035f;
                float shakeIntensity = isHeavyAttack ? 0.028f : 0.014f;

                if (FightImpactManager.Instance != null)
                {
                    FightImpactManager.Instance.ImpactoPesado(hitstopDuration, shakeIntensity);
                }

                // 3. Efectos visuales de texto cómic ("¡POW!", "¡CUACK!", "¡ZAS!")
                if (CombatEffectsManager.Instance != null)
                {
                    if (ownerFighter != null && ownerFighter.bodyController != null && ownerFighter.bodyController.currentBodyType == FighterBodyType.DosCabezas)
                    {
                        if (isKickAttack)
                        {
                            CombatEffectsManager.Instance.SpawnCustomHitEffect(hitPoint, "¡CUACK!", new Color(1f, 0.92f, 0.1f), isHeavyAttack);
                        }
                        else
                        {
                            string[] words = { "¡POW!", "¡ZAS!", "¡KAPOW!", "¡TOMA!", "¡PLAF!", "¡BOING!" };
                            string rw = words[Random.Range(0, words.Length)];
                            CombatEffectsManager.Instance.SpawnCustomHitEffect(hitPoint, rw, new Color(1f, 0.35f, 0.15f), isHeavyAttack);
                        }
                    }
                    else
                    {
                        CombatEffectsManager.Instance.SpawnHitEffect(hitPoint, isHeavyAttack);
                    }
                }

                // 4. Apagar inmediatamente para no golpear más de una vez en el mismo ataque
                DeactivateHitbox();
            }
        }
    }
}
