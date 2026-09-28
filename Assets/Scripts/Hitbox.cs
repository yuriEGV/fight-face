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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;

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
                float facingDir = ownerFighter != null ? ownerFighter.FacingDirection : 1f;
                Vector2 knockbackDir = new Vector2(facingDir * knockbackPower, knockbackPower * 0.35f);

                // 1. Aplicar daño y reacción de impacto / Hitstun
                target.RecibirImpacto(damage, knockbackPower, attackerOriginX, ownerFighter, isHeavyAttack);

                // 2. Activar el Hitstop profesional y Screen Shake
                float hitstopDuration = isHeavyAttack ? 0.09f : 0.06f;
                float shakeIntensity = isHeavyAttack ? 0.22f : 0.12f;

                if (FightImpactManager.Instance != null)
                {
                    FightImpactManager.Instance.ImpactoPesado(hitstopDuration, shakeIntensity);
                }

                // 3. Efectos visuales de texto cómic ("¡POW!", "¡CRACK!", "¡BAM!")
                if (CombatEffectsManager.Instance != null)
                {
                    CombatEffectsManager.Instance.SpawnHitEffect(hitPoint, isHeavyAttack);
                }

                // 4. Apagar inmediatamente para no golpear más de una vez en el mismo ataque
                DeactivateHitbox();
            }
        }
    }
}
