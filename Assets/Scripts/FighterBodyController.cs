using System.Collections;
using UnityEngine;

namespace FightFace
{
    /// <summary>
    /// Gestiona y anima el cuerpo 2D del luchador (torso, brazos, piernas y cuello).
    /// Si no se han asignado sprites externos, genera automáticamente un cuerpo
    /// cartoon estilo brawler/arcade con guantes y botas para que el juego
    /// funcione de inmediato de forma cómica y fluida.
    /// </summary>
    public class FighterBodyController : MonoBehaviour
    {
        [Header("Referencias de Extremidades")]
        public Transform neckPoint;
        public Transform torso;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        [Header("Guantes / Puños")]
        public Transform rightFist;
        public Transform leftFist;

        [Header("Pies / Botas")]
        public Transform rightFoot;
        public Transform leftFoot;

        [Header("Estilo Visual")]
        public Color suitColor = new Color(0.85f, 0.2f, 0.2f);
        public Color gloveColor = new Color(1f, 0.8f, 0.1f);
        public Color skinColor = new Color(1f, 0.85f, 0.7f);

        private float walkCycle = 0f;
        private bool isAttacking = false;
        private bool isKO = false;
        private Vector3 initialTorsoPos;

        private void Awake()
        {
            if (neckPoint == null)
            {
                BuildProceduralBody();
            }
            if (torso != null)
            {
                initialTorsoPos = torso.localPosition;
            }
        }

        /// <summary>
        /// Construye proceduralmente un cuerpo de luchador simpático si no existe uno.
        /// </summary>
        public void BuildProceduralBody()
        {
            // Crear cuello
            GameObject neckObj = new GameObject("NeckPoint");
            neckObj.transform.SetParent(transform, false);
            neckObj.transform.localPosition = new Vector3(0, 1.25f, 0);
            neckPoint = neckObj.transform;

            // Crear torso
            GameObject torsoObj = new GameObject("Torso");
            torsoObj.transform.SetParent(transform, false);
            torsoObj.transform.localPosition = new Vector3(0, 0.65f, 0);
            torso = torsoObj.transform;
            var torsoSr = torsoObj.AddComponent<SpriteRenderer>();
            torsoSr.sprite = CreateCapsuleSprite(48, 70, suitColor);
            torsoSr.sortingOrder = 1;

            // Brazos y puños
            GameObject lArmObj = new GameObject("LeftArm");
            lArmObj.transform.SetParent(transform, false);
            lArmObj.transform.localPosition = new Vector3(-0.35f, 0.85f, 0);
            leftArm = lArmObj.transform;
            var lArmSr = lArmObj.AddComponent<SpriteRenderer>();
            lArmSr.sprite = CreateCapsuleSprite(22, 45, skinColor);
            lArmSr.sortingOrder = 0;

            GameObject lFistObj = new GameObject("LeftFist");
            lFistObj.transform.SetParent(leftArm, false);
            lFistObj.transform.localPosition = new Vector3(-0.1f, -0.3f, 0);
            leftFist = lFistObj.transform;
            var lFistSr = lFistObj.AddComponent<SpriteRenderer>();
            lFistSr.sprite = CreateCircleSprite(32, gloveColor);
            lFistSr.sortingOrder = 3;

            GameObject rArmObj = new GameObject("RightArm");
            rArmObj.transform.SetParent(transform, false);
            rArmObj.transform.localPosition = new Vector3(0.35f, 0.85f, 0);
            rightArm = rArmObj.transform;
            var rArmSr = rArmObj.AddComponent<SpriteRenderer>();
            rArmSr.sprite = CreateCapsuleSprite(22, 45, skinColor);
            rArmSr.sortingOrder = 2;

            GameObject rFistObj = new GameObject("RightFist");
            rFistObj.transform.SetParent(rightArm, false);
            rFistObj.transform.localPosition = new Vector3(0.1f, -0.3f, 0);
            rightFist = rFistObj.transform;
            var rFistSr = rFistObj.AddComponent<SpriteRenderer>();
            rFistSr.sprite = CreateCircleSprite(32, gloveColor);
            rFistSr.sortingOrder = 4;

            // Piernas y pies
            GameObject lLegObj = new GameObject("LeftLeg");
            lLegObj.transform.SetParent(transform, false);
            lLegObj.transform.localPosition = new Vector3(-0.25f, 0.25f, 0);
            leftLeg = lLegObj.transform;
            var lLegSr = lLegObj.AddComponent<SpriteRenderer>();
            lLegSr.sprite = CreateCapsuleSprite(24, 48, suitColor);
            lLegSr.sortingOrder = 0;

            GameObject lFootObj = new GameObject("LeftFoot");
            lFootObj.transform.SetParent(leftLeg, false);
            lFootObj.transform.localPosition = new Vector3(0, -0.35f, 0);
            leftFoot = lFootObj.transform;
            var lFootSr = lFootObj.AddComponent<SpriteRenderer>();
            lFootSr.sprite = CreateCapsuleSprite(30, 18, Color.black);
            lFootSr.sortingOrder = 1;

            GameObject rLegObj = new GameObject("RightLeg");
            rLegObj.transform.SetParent(transform, false);
            rLegObj.transform.localPosition = new Vector3(0.25f, 0.25f, 0);
            rightLeg = rLegObj.transform;
            var rLegSr = rLegObj.AddComponent<SpriteRenderer>();
            rLegSr.sprite = CreateCapsuleSprite(24, 48, suitColor);
            rLegSr.sortingOrder = 2;

            GameObject rFootObj = new GameObject("RightFoot");
            rFootObj.transform.SetParent(rightLeg, false);
            rFootObj.transform.localPosition = new Vector3(0, -0.35f, 0);
            rightFoot = rFootObj.transform;
            var rFootSr = rFootObj.AddComponent<SpriteRenderer>();
            rFootSr.sprite = CreateCapsuleSprite(30, 18, Color.black);
            rFootSr.sortingOrder = 3;
        }

        private Sprite CreateCapsuleSprite(int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color outline = new Color(0.1f, 0.1f, 0.1f, 1f);
            float r = width * 0.45f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    // Comprobar cápsula redondeada
                    float cy = Mathf.Clamp(y, r, height - r);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(width / 2f, cy));
                    if (d <= r)
                    {
                        if (d >= r - 3f || y <= 1 || y >= height - 2)
                            tex.SetPixel(x, y, outline);
                        else
                            tex.SetPixel(x, y, color);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite CreateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color outline = new Color(0.1f, 0.1f, 0.1f, 1f);
            float r = size * 0.46f;
            Vector2 c = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, new Color(0, 0, 0, 0));
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= r)
                    {
                        if (d >= r - 3f)
                            tex.SetPixel(x, y, outline);
                        else
                            tex.SetPixel(x, y, color);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public void UpdateAnimation(float moveInput, bool isGrounded)
        {
            if (isKO || isAttacking) return;

            // Idle respiración cómica
            float idleBob = Mathf.Sin(Time.time * 6f) * 0.04f;
            if (torso != null)
            {
                torso.localPosition = initialTorsoPos + new Vector3(0, idleBob, 0);
            }

            if (Mathf.Abs(moveInput) > 0.1f && isGrounded)
            {
                // Caminar: mover piernas y brazos
                walkCycle += Time.deltaTime * 12f;
                float legAngle = Mathf.Sin(walkCycle) * 25f;
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0, 0, legAngle);
                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0, 0, -legAngle);

                float armAngle = Mathf.Sin(walkCycle) * 20f;
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, -armAngle);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, armAngle);
            }
            else
            {
                // Reset a pose relajada
                walkCycle = 0f;
                if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
                if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
                if (leftArm != null) leftArm.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f) * 5f);
                if (rightArm != null) rightArm.localRotation = Quaternion.Euler(0, 0, -Mathf.Sin(Time.time * 3f) * 5f);
            }
        }

        /// <summary>
        /// Animación de Puñetazo: el puño derecho se estira disparado hacia adelante
        /// y el torso se inclina con ímpetu.
        /// </summary>
        public void PlayPunchAnimation(float duration)
        {
            if (isKO) return;
            StartCoroutine(PunchRoutine(duration));
        }

        private IEnumerator PunchRoutine(float duration)
        {
            isAttacking = true;
            Vector3 armOriginal = rightArm.localPosition;
            Vector3 fistOriginal = rightFist.localPosition;

            float reachTime = duration * 0.35f;
            float returnTime = duration * 0.65f;

            // Estirar puño hacia adelante
            float t = 0f;
            while (t < reachTime)
            {
                rightArm.localPosition = Vector3.Lerp(armOriginal, armOriginal + new Vector3(0.5f, 0.1f, 0), t / reachTime);
                rightFist.localPosition = Vector3.Lerp(fistOriginal, fistOriginal + new Vector3(0.4f, 0.1f, 0), t / reachTime);
                rightArm.localRotation = Quaternion.Euler(0, 0, -15f);
                t += Time.deltaTime;
                yield return null;
            }

            // Regresar puño
            t = 0f;
            while (t < returnTime)
            {
                rightArm.localPosition = Vector3.Lerp(armOriginal + new Vector3(0.5f, 0.1f, 0), armOriginal, t / returnTime);
                rightFist.localPosition = Vector3.Lerp(fistOriginal + new Vector3(0.4f, 0.1f, 0), fistOriginal, t / returnTime);
                rightArm.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, -15f), Quaternion.identity, t / returnTime);
                t += Time.deltaTime;
                yield return null;
            }

            rightArm.localPosition = armOriginal;
            rightFist.localPosition = fistOriginal;
            rightArm.localRotation = Quaternion.identity;
            isAttacking = false;
        }

        /// <summary>
        /// Animación de Patada: la pierna derecha se estira con fuerza hacia el frente.
        /// </summary>
        public void PlayKickAnimation(float duration)
        {
            if (isKO) return;
            StartCoroutine(KickRoutine(duration));
        }

        private IEnumerator KickRoutine(float duration)
        {
            isAttacking = true;
            Quaternion legOriginal = rightLeg.localRotation;
            float half = duration * 0.5f;

            float t = 0f;
            while (t < half)
            {
                rightLeg.localRotation = Quaternion.Lerp(legOriginal, Quaternion.Euler(0, 0, 80f), t / half);
                t += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                rightLeg.localRotation = Quaternion.Lerp(Quaternion.Euler(0, 0, 80f), legOriginal, t / half);
                t += Time.deltaTime;
                yield return null;
            }

            rightLeg.localRotation = legOriginal;
            isAttacking = false;
        }

        /// <summary>
        /// Animación de Daño (Hurt): el cuerpo se echa hacia atrás y tiembla brevemente.
        /// </summary>
        public void PlayHurtAnimation(float duration)
        {
            if (isKO) return;
            StartCoroutine(HurtRoutine(duration));
        }

        private IEnumerator HurtRoutine(float duration)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float shake = Mathf.Sin(elapsed * 40f) * 0.08f;
                transform.localPosition = originalPos + new Vector3(-0.1f + shake, 0, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
        }

        /// <summary>
        /// Animación de KO: el luchador gira cómicamente y cae de espaldas en el suelo.
        /// </summary>
        public void PlayKODefeatedAnimation()
        {
            isKO = true;
            isAttacking = false;
            StopAllCoroutines();
            StartCoroutine(KORoutine());
        }

        private IEnumerator KORoutine()
        {
            Quaternion startRot = transform.localRotation;
            Quaternion targetRot = Quaternion.Euler(0, 0, -90f);
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = new Vector3(startPos.x, startPos.y - 0.7f, startPos.z);

            float duration = 0.6f;
            float t = 0f;

            while (t < duration)
            {
                transform.localRotation = Quaternion.Lerp(startRot, targetRot, t / duration);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, t / duration);
                t += Time.deltaTime;
                yield return null;
            }

            transform.localRotation = targetRot;
            transform.localPosition = targetPos;
        }

        public void ResetBody()
        {
            isKO = false;
            isAttacking = false;
            transform.localRotation = Quaternion.identity;
            if (torso != null) torso.localPosition = initialTorsoPos;
            if (rightArm != null) rightArm.localRotation = Quaternion.identity;
            if (leftArm != null) leftArm.localRotation = Quaternion.identity;
            if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
        }
    }
}
