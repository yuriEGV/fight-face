using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Componente de Hitbox para puños y patadas.
    /// Detecta al oponente, aplica daño, retroceso (knockback) y efectos cómicos.
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

            // Evitar golpearse a sí mismo
            FighterController target = other.GetComponentInParent<FighterController>();
            if (target != null && target != ownerFighter && target.IsAlive)
            {
                hasHit = true;
                Vector2 hitPoint = other.ClosestPoint(transform.position);

                // Dirección del retroceso según hacia dónde mira el atacante
                float facingDir = ownerFighter != null ? ownerFighter.FacingDirection : 1f;
                Vector2 knockbackDir = new Vector2(facingDir * knockbackPower, knockbackPower * 0.4f);

                target.TakeDamage(damage, knockbackDir);

                // Efecto visual estilo cómic ("¡POW!", "¡BAM!", partículas)
                if (CombatEffectsManager.Instance != null)
                {
                    CombatEffectsManager.Instance.SpawnHitEffect(hitPoint, isHeavyAttack);
                }

                DeactivateHitbox();
            }
        }
    }
}
