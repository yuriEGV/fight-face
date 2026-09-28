namespace FightFace
{
    /// <summary>
    /// Tipos de expresiones faciales para los luchadores.
    /// Incluye las 4 fotos clave del sistema:
    /// - Base: Expresión normal / relajada / mirando a cámara.
    /// - Dolor: Expresión de daño / golpe crítico o aturdimiento (Stun).
    /// - Enojo / Rabia: Expresión agresiva / ceño fruncido (activada en Rage y ataques especiales).
    /// - Ganador: Expresión de victoria / celebración / sonrisa.
    /// - KO: Expresión derrotada / ojos cerrados.
    /// </summary>
    public enum FaceType
    {
        Base,       // Foto_Base.png - Rostro neutral / mirando de frente
        Dolor,      // Foto_Dolor.png - Rostro de dolor / mueca de golpe
        Enojo,      // Foto_Enojo.png o Foto_Rabia.png - Rostro de furia / Rage / Super
        Ganador,    // Foto_Ganador.png - Rostro de victoria / celebración (¡4ta foto!)
        KO          // Foto_KO.png - Rostro de derrota / noqueado
    }
}
