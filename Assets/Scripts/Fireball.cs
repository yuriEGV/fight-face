using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Proyectil del Hadouken (↓ ↘ → + Puño). Orbe de energía procedural que viaja recto
    /// y daña al primer luchador rival que toque.
    /// </summary>
    public class Fireball : MonoBehaviour
    {
        private FighterController owner;
        private int damage;
        private float knockback;
        private float dir;
        private float speed = 9f;
        private float life = 2.2f;
        private bool consumed = false;
        private static Sprite s_Orb;

        public static Fireball Spawn(FighterController owner, Vector3 pos, float dir, int damage, float knockback)
        {
            GameObject go = new GameObject("Fireball");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.9f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrb();
            sr.sortingOrder = 13;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.28f;

            var fb = go.AddComponent<Fireball>();
            fb.owner = owner;
            fb.damage = damage;
            fb.knockback = knockback;
            fb.dir = dir >= 0f ? 1f : -1f;
            rb.linearVelocity = new Vector2(fb.dir * fb.speed, 0f);
            return fb;
        }

        private void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);

            float pulse = 0.9f + Mathf.Sin(Time.time * 30f) * 0.08f;
            transform.localScale = new Vector3(pulse * dir, pulse, 1f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || other == null) return;

            FighterController target = null;
            Hurtbox hb = other.GetComponent<Hurtbox>();
            if (hb != null) target = hb.ownerFighter;
            if (target == null) target = other.GetComponentInParent<FighterController>();

            if (target == null)
            {
                // Pared u obstáculo sólido
                if (!other.isTrigger && other.GetComponent<Fireball>() == null) Destroy(gameObject);
                return;
            }

            if (target == owner || !target.IsAlive) return;

            consumed = true;
            target.RecibirImpacto(damage, knockback, transform.position.x - dir, owner, true, false);

            if (FightAudioManager.Instance != null) FightAudioManager.Instance.PlayHadoukenHit();
            if (FightImpactManager.Instance != null) FightImpactManager.Instance.ImpactoPesado(0.065f, 0.028f);
            if (CombatEffectsManager.Instance != null) CombatEffectsManager.Instance.SpawnHitEffect(transform.position, true);

            Destroy(gameObject);
        }

        private static Sprite GetOrb()
        {
            if (s_Orb != null) return s_Orb;
            int n = 96;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 c = new Vector2(n * 0.5f, n * 0.5f);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (n * 0.5f);
                    if (d >= 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                    // Núcleo blanco -> halo cian -> borde azul transparente
                    Color core = Color.Lerp(Color.white, new Color(0.35f, 0.85f, 1f), Mathf.Clamp01(d * 1.6f));
                    core = Color.Lerp(core, new Color(0.15f, 0.35f, 1f), Mathf.Clamp01((d - 0.55f) * 2.2f));
                    core.a = Mathf.Clamp01((1f - d) * 2.4f);
                    tex.SetPixel(x, y, core);
                }
            }
            tex.Apply();
            s_Orb = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return s_Orb;
        }
    }
}
