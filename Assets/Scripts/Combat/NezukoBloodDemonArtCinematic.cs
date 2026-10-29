using System.Collections;
using System.IO;
using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Systems;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Executes Nezuko's signature Blood Demon Art: Exploding Blood (Bakketsu).
    /// - Consumes 60 Ki, has a short windup (~0.35s).
    /// - Sure-hit move: Guaranteed hit targeting the opponent anywhere on the arena floor.
    /// - Spawns a massive animated blood-fire explosion directly on top of the opponent.
    /// - Deals 30 initial burst damage and inflicts a lingering burn effect (2 dmg/tick x 3 ticks).
    /// - Refunds 20 Ki upon connecting.
    /// - Has a 10-second cooldown.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class NezukoBloodDemonArtCinematic : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Tuning")]
        [SerializeField] private int burstDamage = 30;
        [SerializeField] private float kiRefund = 20f;
        [SerializeField] private float cooldownDuration = 10f;
        [SerializeField] private float windupDuration = 0.35f;

        [Header("Burn Effect")]
        [SerializeField] private int burnDamagePerTick = 2;
        [SerializeField] private int burnTicks = 3;
        [SerializeField] private float burnTickInterval = 0.5f;

        #endregion

        #region State

        private FighterController controller;
        private EnergySystem energySystem;
        private FighterAudio fighterAudio;
        private Sprite[] explosionFrames;
        private GameObject vfxInstance;
        private SpriteRenderer vfxRenderer;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            controller = GetComponent<FighterController>();
            energySystem = GetComponent<EnergySystem>();
            fighterAudio = GetComponent<FighterAudio>();
            LoadExplosionFrames();
        }

        #endregion

        #region Frame Loading

        private void LoadExplosionFrames()
        {
            string vfxDir = Path.Combine(Application.dataPath, "Sprites", "Effects", "ExplodingBlood");
            if (!Directory.Exists(vfxDir)) return;

            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 8; i++)
            {
                string path = Path.Combine(vfxDir, $"blood_fire_{i}.png");
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    tex.LoadImage(bytes);
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    list.Add(sp);
                }
            }
            explosionFrames = list.ToArray();
        }

        #endregion

        #region Public Trigger

        /// <summary>
        /// Executes the sure-hit Blood Demon Art sequence.
        /// </summary>
        public void TriggerBloodDemonArt(Transform target)
        {
            StartCoroutine(ExecuteSequence(target));
        }

        private IEnumerator ExecuteSequence(Transform target)
        {
            Debug.Log("<color=#f43f5e>[Blood Demon Art: Exploding Blood]</color> Nezuko begins blood ignition windup!");

            // 1. Short Windup (Nezuko clenches hand into fist, draws blood)
            VFXManager.Instance?.TriggerImpactFlash(0.08f);
            fighterAudio?.PlayUltimateAttack();

            yield return new WaitForSeconds(windupDuration);

            // 2. Sure-Hit Impact on Target (Target location or center stage fallback)
            Vector3 hitPos = target != null ? target.position + new Vector3(0f, 0.8f, 0f) : transform.position + new Vector3(2f, 0f, 0f);

            // Camera shake & dramatic flash
            VFXManager.Instance?.PlayHitSpark(hitPos, isHeavy: true, triggerHitStop: true, shakeMultiplier: 2.2f);
            VFXManager.Instance?.TriggerImpactFlash(0.12f);

            // 3. Play Animated Blood Explosion VFX directly over opponent
            if (explosionFrames != null && explosionFrames.Length > 0)
            {
                StartCoroutine(PlayExplosionAnimation(hitPos));
            }

            // 4. Deal 30 Burst Damage & Hitstun
            if (target != null)
            {
                HealthSystem targetHealth = target.GetComponent<HealthSystem>();
                FighterController targetFc = target.GetComponent<FighterController>();

                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(burstDamage);
                    Debug.Log($"<color=#f43f5e>[Blood Demon Art]</color> Dealt {burstDamage} SURE-HIT damage to {target.name}!");
                }

                if (targetFc != null)
                {
                    targetFc.ApplyHit(burstDamage, stunDuration: 0.85f);
                }

                // 5. Apply Lingering Fire Burn Status
                StartCoroutine(ApplyBurnTicks(targetHealth, targetFc, target));
            }

            // 6. Gain 20 Ki back
            if (energySystem != null)
            {
                energySystem.AddKi(kiRefund);
                Debug.Log($"<color=#f43f5e>[Blood Demon Art]</color> Nezuko gained {kiRefund} Ki back!");
            }
        }

        private IEnumerator PlayExplosionAnimation(Vector3 position)
        {
            GameObject obj = new GameObject("BloodDemonArt_ExplosionVFX");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(2.5f, 2.5f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 25; // Render cleanly in front of fighters

            for (int i = 0; i < explosionFrames.Length; i++)
            {
                sr.sprite = explosionFrames[i];
                yield return new WaitForSeconds(0.06f);
            }

            Destroy(obj);
        }

        private IEnumerator ApplyBurnTicks(HealthSystem targetHealth, FighterController targetFc, Transform target)
        {
            for (int i = 0; i < burnTicks; i++)
            {
                yield return new WaitForSeconds(burnTickInterval);
                if (targetHealth != null && targetHealth.CurrentHealth > 0)
                {
                    targetHealth.TakeDamage(burnDamagePerTick);
                    if (target != null)
                    {
                        VFXManager.Instance?.PlayHitSpark(target.position + new Vector3(0f, 0.6f + (i * 0.2f), 0f), isHeavy: false, triggerHitStop: false, shakeMultiplier: 0.15f);
                    }
                    Debug.Log($"<color=#f43f5e>[Blood Demon Art: Burn]</color> Burn tick dealt {burnDamagePerTick} dmg to {targetHealth.gameObject.name}!");
                }
            }
        }

        #endregion
    }
}
