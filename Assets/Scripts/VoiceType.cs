namespace FightFace
{
    /// <summary>
    /// Los 5 tipos de voces / gritos de batalla para cada luchador:
    /// 1. Attack: Grito de ataque al golpear fuerte o finalizar un combo ("¡Toma!", "¡Muere!").
    /// 2. Special: Grito especial al ejecutar el Hadouken / Super Embestida ("¡Hadouken!", "¡Fuego!").
    /// 3. Hurt: Quejido o queja de dolor al recibir daño o stun ("¡Ayyy!", "¡Ouch!", "¡Mi cara!").
    /// 4. Victory: Frase o risa de victoria al noquear al oponente ("¡Soy el mejor!", "¡Victoria!").
    /// 5. KO: Grito agónico o queja dramática de derrota ("¡Nooooo!", "¡Maldita sea!").
    /// </summary>
    public enum VoiceType
    {
        Attack,
        Special,
        Hurt,
        Victory,
        KO
    }
}
