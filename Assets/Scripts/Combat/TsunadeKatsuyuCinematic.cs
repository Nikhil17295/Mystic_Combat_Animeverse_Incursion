using System.Collections;
using System.IO;
using UnityEngine;
using MysticCombat.Fighter;
using MysticCombat.Systems;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Executes Tsunade's signature Ultimate: Katsuyu Summon & Creation Rebirth (Byakugou).
    /// - Costs 60 Ki, 20s cooldown.
    /// - Tsunade stays in her original body and summons Katsuyu (the giant slug).
    /// - Katsuyu travels forward in a straight line across the arena, dealing 30 damage to whoever she hits.
    /// - Simultaneously bestows Tsunade with her 10-second Healing Stage (Creation Rebirth):
    ///   * Distinctive glowing emerald medical chakra aura.
    ///   * Regenerates +2 HP per second.
    ///   * Generates +2 Ki per second.
    ///   * Takes half damage (50% incoming damage reduction).
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class TsunadeKatsuyuCinematic : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Slug Tuning")]
        [SerializeField] private int slugDamage = 30;
        [Tooltip("Slither travel speed. Reduced to 3.1f (2x slower) so players can clearly observe Katsuyu.")]
        [SerializeField] private float slugSpeed = 3.1f;
        [Tooltip("Slug lifetime on stage to allow traversing the arena at half speed.")]
        [SerializeField] private float slugLifetime = 6.5f;
        [SerializeField] private float slugPpu = 33.0f;
        [Tooltip("Visual scale multiplier for Katsuyu slug summon.")]
        [SerializeField] private float slugScale = 1.85f;

        [Header("Healing Stage Tuning")]
        [SerializeField] private float healingDuration = 10.0f;
        [SerializeField] private float cooldownDuration = 20.0f;

        #endregion

        #region State

        private FighterController controller;
        private EnergySystem energySystem;
        private HealthSystem healthSystem;
        private FighterAudio fighterAudio;
        private Sprite[] slugFrames;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            controller = GetComponent<FighterController>();
            energySystem = GetComponent<EnergySystem>();
            healthSystem = GetComponent<HealthSystem>();
            fighterAudio = GetComponent<FighterAudio>();
            LoadSlugFrames();
        }

        #endregion

        #region Frame Loading

        private void LoadSlugFrames()
        {
            string framesDir = Path.Combine(Application.dataPath, "Sprites", "Fighters", "Tsunade", "Frames");
            if (!Directory.Exists(framesDir)) return;

            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < 8; i++)
            {
                string path = Path.Combine(framesDir, $"ultimate_{i}.png");
                if (File.Exists(path))
                {
                    byte[] data = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    if (tex.LoadImage(data))
                    {
                        Sprite sp = Sprite.Create(
                            tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.15f),
                            slugPpu
                        );
                        list.Add(sp);
                    }
                }
            }

            slugFrames = list.ToArray();
            Debug.Log($"<color=#10b981>[TsunadeKatsuyuCinematic]</color> Loaded {slugFrames.Length} Katsuyu slug frames.");
        }

        #endregion

        #region Public Trigger

        /// <summary>
        /// Triggers Katsuyu summon and activates Tsunade's 10-second Healing Stage.
        /// </summary>
        public void TriggerKatsuyuSummon(Transform opponent)
        {
            StartCoroutine(SummonRoutine(opponent));
        }

        private IEnumerator SummonRoutine(Transform opponent)
        {
            // 1. Play summon invocation audio & flash
            if (fighterAudio != null)
            {
                fighterAudio.PlayUltimateAttack();
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.TriggerImpactFlash(0.25f);
                VFXManager.Instance.TriggerCameraShake(0.35f, 0.30f);
            }

            // 2. Activate Tsunade's 10s Healing Stage immediately!
            if (controller != null)
            {
                controller.ActivateHealingStage(healingDuration);
                controller.SetUltimateCooldown(cooldownDuration);
            }

            // 3. Spawn Katsuyu GameObject in facing direction
            float facing = transform.localScale.x > 0 ? 1f : -1f;
            if (opponent != null)
            {
                facing = opponent.position.x >= transform.position.x ? 1f : -1f;
            }

            Vector3 spawnPos = transform.position + new Vector3(facing * 1.6f, -0.2f, 0f);
            GameObject slugObj = new GameObject("Katsuyu_Summon");
            slugObj.transform.position = spawnPos;
            slugObj.transform.localScale = new Vector3(facing * slugScale, slugScale, 1f);

            SpriteRenderer sr = slugObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            // Spawn dust on appear
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDustPuff(spawnPos);
            }

            // 4. Slither Routine
            float elapsed = 0f;
            int frameIdx = 0;
            float animTimer = 0f;
            float frameRate = 1f / 5.5f; // 5.5 FPS slither cycle matching 2x slower movement
            bool hasHit = false;

            while (elapsed < slugLifetime)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                animTimer += dt;

                // Move forward in a straight line
                slugObj.transform.position += new Vector3(facing * slugSpeed * dt, 0f, 0f);

                // Animate frames
                if (slugFrames != null && slugFrames.Length > 0)
                {
                    if (animTimer >= frameRate)
                    {
                        animTimer -= frameRate;
                        frameIdx = (frameIdx + 1) % slugFrames.Length;
                    }
                    sr.sprite = slugFrames[frameIdx];
                }

                // Check hit against opponent
                if (!hasHit && opponent != null)
                {
                    float dist = Vector2.Distance(slugObj.transform.position, opponent.position);
                    if (dist < 2.3f)
                    {
                        hasHit = true;
                        HealthSystem targetHealth = opponent.GetComponent<HealthSystem>();
                        FighterController targetFc = opponent.GetComponent<FighterController>();

                        if (targetHealth != null)
                        {
                            targetHealth.TakeDamage(slugDamage);
                        }
                        if (targetFc != null)
                        {
                            targetFc.ApplyHit(slugDamage, stunDuration: 0.85f);
                        }

                        if (VFXManager.Instance != null)
                        {
                            VFXManager.Instance.PlayHitSpark(slugObj.transform.position, true);
                            VFXManager.Instance.TriggerCameraShake(0.40f, 0.25f);
                            VFXManager.Instance.TriggerHitStop(0.08f);
                        }

                        Debug.Log($"<color=#10b981>[KATSUYU STRIKE]</color> Katsuyu hit {opponent.name} for {slugDamage} damage!");

                        // Dissolve after impact
                        break;
                    }
                }

                yield return null;
            }

            // Dissolve effect
            if (slugObj != null)
            {
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayDustPuff(slugObj.transform.position);
                }
                Destroy(slugObj);
            }
        }

        #endregion
    }
}
