using System.Collections.Generic;
using UnityEngine;

namespace FightFace
{
    // =====================================================================
    //  NÚCLEO DE COMBATE ESTILO STREET FIGHTER (datos de frames a 60 FPS)
    //  Traducción a C# de: struct Attack, collision(Box), State machine,
    //  detectHadouken() y chooseAction() de la especificación del proyecto.
    //  Las CARAS las suben los usuarios; movimientos, golpes y técnicas se
    //  definen aquí (en el código), no desde los perfiles de usuario.
    // =====================================================================

    /// <summary>Datos de frames de un ataque (60 frames = 1 segundo).</summary>
    public struct Attack
    {
        public string name;
        public int damage;
        public int startup;
        public int active;
        public int recovery;
        public int stun;
        public float knockback;

        public const float FPS = 60f;
        public float StartupSeconds => startup / FPS;
        public float ActiveSeconds => active / FPS;
        public float RecoverySeconds => recovery / FPS;
        public float TotalSeconds => (startup + active + recovery) / FPS;

        public Attack(string name, int damage, int startup, int active, int recovery, int stun, float knockback)
        {
            this.name = name;
            this.damage = damage;
            this.startup = startup;
            this.active = active;
            this.recovery = recovery;
            this.stun = stun;
            this.knockback = knockback;
        }
    }

    /// <summary>Biblioteca de técnicas. El daño está en escala de 100 HP (el juego usa maxHealth = 100).</summary>
    public static class AttackLibrary
    {
        public static readonly Attack LightPunch = new Attack("Light Punch", 14, 4, 3, 8, 10, 4.5f);
        public static readonly Attack HeavyPunch = new Attack("Heavy Punch", 26, 10, 4, 15, 25, 7.5f);
        public static readonly Attack LightKick = new Attack("Light Kick", 16, 6, 4, 10, 12, 5.5f);
        public static readonly Attack HeavyKick = new Attack("Heavy Kick", 30, 12, 5, 16, 28, 9.5f);
        public static readonly Attack Hadouken = new Attack("Hadouken", 28, 13, 3, 24, 30, 8.5f);
    }

    /// <summary>Caja de colisión entera (coordenadas de pantalla: top &lt; bottom).</summary>
    public struct FightBox
    {
        public int left, right, top, bottom;

        public FightBox(int left, int right, int top, int bottom)
        {
            this.left = left; this.right = right; this.top = top; this.bottom = bottom;
        }

        public static bool Collision(FightBox a, FightBox b)
        {
            return a.left < b.right &&
                   a.right > b.left &&
                   a.top < b.bottom &&
                   a.bottom > b.top;
        }
    }

    public enum FighterState
    {
        IDLE,
        WALKING,
        JUMPING,
        CROUCHING,
        ATTACKING,
        BLOCKING,
        HIT,
        KNOCKED_OUT
    }

    /// <summary>Motor de estados con contador de frames (changeState / updateState).</summary>
    public class FighterStateMachine
    {
        public FighterState state = FighterState.IDLE;
        public int frame = 0;

        public void ChangeState(FighterState next)
        {
            state = next;
            frame = 0;
        }

        /// <summary>Sincroniza con el estado real del luchador y avanza un frame.</summary>
        public void Tick(FighterState actual)
        {
            if (actual != state) ChangeState(actual);
            frame++;
        }
    }

    public enum MoveInput
    {
        DOWN,
        DOWN_FORWARD,
        FORWARD,
        DOWN_BACK,
        BACK,
        PUNCH
    }

    /// <summary>
    /// Reconocedor de comandos con buffer temporal.
    /// Hadouken = ↓ ↘ → + PUÑO dentro de una ventana de ~0.45 s.
    /// Dirección "adelante" siempre relativa al rival.
    /// </summary>
    public class CommandRecognizer
    {
        private struct Entry { public MoveInput input; public float time; }
        private readonly List<Entry> buffer = new List<Entry>(16);
        private bool hasDir = false;
        private MoveInput lastDir;

        public const float HadoukenWindow = 0.45f;

        public void Feed(bool down, bool forward, bool back, bool punchPressed, float time)
        {
            bool hasNow = true;
            MoveInput dirNow;
            if (down && forward) dirNow = MoveInput.DOWN_FORWARD;
            else if (down && back) dirNow = MoveInput.DOWN_BACK;
            else if (down) dirNow = MoveInput.DOWN;
            else if (forward) dirNow = MoveInput.FORWARD;
            else if (back) dirNow = MoveInput.BACK;
            else { hasNow = false; dirNow = MoveInput.DOWN; }

            if (hasNow && (!hasDir || dirNow != lastDir)) Push(dirNow, time);
            hasDir = hasNow;
            if (hasNow) lastDir = dirNow;

            if (punchPressed) Push(MoveInput.PUNCH, time);
        }

        private void Push(MoveInput input, float time)
        {
            if (buffer.Count >= 16) buffer.RemoveAt(0);
            buffer.Add(new Entry { input = input, time = time });
        }

        public void Clear() { buffer.Clear(); }

        /// <summary>detectHadouken(): DOWN, DOWN_FORWARD, FORWARD, PUNCH (últimos 4 inputs).</summary>
        public bool DetectHadouken(float now)
        {
            int n = buffer.Count;
            if (n < 4) return false;

            bool seq = buffer[n - 4].input == MoveInput.DOWN &&
                       buffer[n - 3].input == MoveInput.DOWN_FORWARD &&
                       buffer[n - 2].input == MoveInput.FORWARD &&
                       buffer[n - 1].input == MoveInput.PUNCH;

            return seq && (now - buffer[n - 4].time) <= HadoukenWindow && (now - buffer[n - 1].time) < 0.2f;
        }
    }

    public enum AIAction
    {
        APPROACH,
        RETREAT,
        JUMP,
        ATTACK,
        DEFEND
    }

    /// <summary>IA para rivales (chooseAction). Distancia en "píxeles" lógicos (1 unidad de mundo = 75 px).</summary>
    public static class FighterAI
    {
        public const float UnitsToPixels = 75f;

        public static AIAction ChooseAction(int distance, int opponentHealth, bool opponentAttacking)
        {
            if (opponentAttacking)
                return AIAction.DEFEND;

            if (distance > 120)
                return AIAction.APPROACH;

            if (distance < 30)
                return AIAction.RETREAT;

            if (opponentHealth < 100)
                return AIAction.ATTACK;   // remate

            return AIAction.ATTACK;
        }
    }
}
