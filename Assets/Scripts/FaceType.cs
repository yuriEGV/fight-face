namespace FightFace
{
    /// <summary>
    /// Tipos de expresiones faciales para los luchadores.
    /// Incluye las 4 caras del sistema:
    /// - Base: Expresión neutra / en reposo / caminando.
    /// - Enojo: ¡Nueva cara! Expresión de odio, furia y ataque al golpear.
    /// - Dolor: Expresión de quejido, daño o mueca al recibir golpes.
    /// - KO: Expresión derrotada (ojos cerrados / noqueado) al llegar a 0 de vida.
    /// </summary>
    public enum FaceType
    {
        Base,   // Foto_Base.png - Rostro neutral / pose de pelea
        Enojo,  // Foto_Enojo.png - Rostro de enojo / odio / ataque (¡4ta cara!)
        Dolor,  // Foto_Dolor.png - Rostro de dolor / golpe recibido
        KO      // Foto_KO.png - Rostro de derrota / noqueado
    }
}
