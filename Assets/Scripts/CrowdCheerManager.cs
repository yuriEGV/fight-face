using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Gestiona el público interactivo en las gradas del ring con las fotos de los luchadores y amigos:
    /// - Espectadores en el fondo que rebotan al ritmo del combate.
    /// - Reaccionan en tiempo real a combos, golpes fuertes y ataques especiales saltando y ondeando banderines.
    /// - Celebración masiva de aplausos y confeti en cada K.O.
    /// </summary>
    public class CrowdCheerManager : MonoBehaviour
    {
        public static CrowdCheerManager Instance { get; private set; }

        private List<Transform> crowdMembers = new List<Transform>();
        private List<Vector3> crowdBasePositions = new List<Vector3>();
        private List<Transform> crowdFlags = new List<Transform>();
        private bool isCheering = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            BuildCrowd();
        }

        public void BuildCrowd()
        {
            // El usuario solicitó retirar al público que estaba sobre el escenario para mantener la arena limpia y sin distracciones
            foreach (var m in crowdMembers)
            {
                if (m != null) Destroy(m.gameObject);
            }
            crowdMembers.Clear();
            crowdBasePositions.Clear();
            crowdFlags.Clear();

            Transform existingRoot = transform.Find("ArenaCrowdRoot");
            if (existingRoot != null) Destroy(existingRoot.gameObject);
            return;
        }

        private IEnumerator CrowdIdleLoop()
        {
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime;

                if (!isCheering)
                {
                    for (int i = 0; i < crowdMembers.Count; i++)
                    {
                        var m = crowdMembers[i];
                        if (m == null) continue;
                        float offset = Mathf.Sin((t * 3f) + i) * 0.05f;
                        Vector3 baseP = crowdBasePositions[i];
                        m.position = new Vector3(baseP.x, baseP.y + offset, baseP.z);
                    }

                    for (int f = 0; f < crowdFlags.Count; f++)
                    {
                        var flag = crowdFlags[f];
                        if (flag == null) continue;
                        flag.localRotation = Quaternion.Euler(0, 0, Mathf.Sin((t * 5f) + f) * 15f);
                    }
                }

                yield return null;
            }
        }

        /// <summary>
        /// Se activa cuando un jugador conecta un combo, golpe fuerte o ataque especial.
        /// </summary>
        public void CheerOnHit(int comboCount = 1)
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(CheerBurstRoutine(0.6f + (comboCount * 0.15f)));
        }

        /// <summary>
        /// Se activa en el momento del K.O. final.
        /// </summary>
        public void CheerOnKO()
        {
            if (!gameObject.activeInHierarchy) return;
            StartCoroutine(CheerBurstRoutine(2.5f));
        }

        private IEnumerator CheerBurstRoutine(float duration)
        {
            isCheering = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < crowdMembers.Count; i++)
                {
                    var m = crowdMembers[i];
                    if (m == null) continue;
                    float jump = Mathf.Abs(Mathf.Sin((elapsed * 12f) + (i * 0.7f))) * 0.28f;
                    Vector3 baseP = crowdBasePositions[i];
                    m.position = new Vector3(baseP.x, baseP.y + jump, baseP.z);
                }

                for (int f = 0; f < crowdFlags.Count; f++)
                {
                    var flag = crowdFlags[f];
                    if (flag == null) continue;
                    flag.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 18f) * 35f);
                }

                yield return null;
            }

            isCheering = false;
        }

        private Sprite CreateSpectatorBodySprite()
        {
            int w = 48, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 shirt = new Color32(255, 255, 255, 255);
            Color32 skin = new Color32(255, 215, 175, 255);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x - w / 2f) / (w / 2f);
                    float ny = y / (float)h;
                    int idx = y * w + x;

                    // Manos arriba celebrando
                    if (ny > 0.60f && (Mathf.Abs(nx - 0.75f) < 0.18f || Mathf.Abs(nx + 0.75f) < 0.18f))
                    {
                        pixels[idx] = skin;
                    }
                    // Torso y hombros
                    else if (ny < 0.85f && Mathf.Abs(nx) < 0.65f)
                    {
                        pixels[idx] = shirt;
                    }
                    else
                    {
                        pixels[idx] = empty;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 48f);
        }

        private Sprite CreateFlagSprite(Color flagColor)
        {
            int w = 36, h = 36;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[w * h];
            Color32 empty = new Color32(0, 0, 0, 0);
            Color32 pole = new Color32(180, 160, 140, 255);
            Color32 fCol = flagColor;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    // Palo del banderín
                    if (x < 4)
                    {
                        pixels[idx] = pole;
                    }
                    // Banderín triangular
                    else if (x >= 4 && x < 32 && y > 10 && y < 34)
                    {
                        float taper = (32 - x) / 28f;
                        if (Mathf.Abs(y - 22) < taper * 12) pixels[idx] = fCol;
                        else pixels[idx] = empty;
                    }
                    else
                    {
                        pixels[idx] = empty;
                    }
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.1f, 0.1f), 36f);
        }
    }
}
