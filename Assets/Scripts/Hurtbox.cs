using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Hurtbox: Zona del cuerpo del luchador susceptible a recibir daño de las Hitboxes rivales.
    /// Siguiendo la arquitectura clásica de Street Fighter y juegos de pelea 2D.
    /// </summary>
    public class Hurtbox : MonoBehaviour
    {
        public FighterController ownerFighter;

        private void Awake()
        {
            if (ownerFighter == null)
            {
                ownerFighter = GetComponentInParent<FighterController>();
            }
        }
    }
}
