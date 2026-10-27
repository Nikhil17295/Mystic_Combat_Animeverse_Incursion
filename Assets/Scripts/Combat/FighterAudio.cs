using UnityEngine;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Fighter Audio Component handling character combat audio:
    /// - Decoupled from player slot (P1, P2, CPU, Network, or Replay)
    /// - Serialized AudioClips directly in the Inspector for easy drag-and-drop customization per character
    /// - General Combat SFX: whiffs, jumps, landings, dashes, and damage impacts
    /// - Character-Specific Specials: unique skill blasts, ultimate attacks, and looping Ki meditation auras
    /// - Character Voice Lines & Grunts: attack grunts, hurt pain callouts, skill/ultimate dialogues
    /// - Dual-channel audio architecture (polyphonic one-shots + looping aura)
    /// - Live Inspector volume sliders for real-time balancing during Play mode
    /// </summary>
    [DisallowMultipleComponent]
    public class FighterAudio : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Audio Sources (Auto-Generated if Empty)")]
        [Tooltip("AudioSource for polyphonic one-shot combat SFX.")]
        [SerializeField] private AudioSource sfxSource;

        [Tooltip("AudioSource for looping Ki meditation energy aura.")]
        [SerializeField] private AudioSource auraSource;

        [Header("Common Combat Movement SFX")]
        [Tooltip("Springy leap sound played on takeoff when entering Jumping state.")]
        [SerializeField] private AudioClip jumpClip;

        [Tooltip("Solid boots-on-floor thud played when landing from air onto ground.")]
        [SerializeField] private AudioClip landClip;

        [Tooltip("Subtle cloth rustle or stance drop played when entering Crouching state.")]
        [SerializeField] private AudioClip crouchClip;

        [Tooltip("High-speed air displacement sonic zip played when dashing.")]
        [SerializeField] private AudioClip dashClip;

        [Header("Attack Whiffs (Air Displacement)")]
        [Tooltip("Light punch/kick swoosh played when entering LightAttack state.")]
        [SerializeField] private AudioClip lightWhiffClip;

        [Tooltip("Heavy roundhouse / heavy strike swoosh played when entering HeavyAttack state.")]
        [SerializeField] private AudioClip heavyWhiffClip;

        [Header("Character-Specific Attack Sounds")]
        [Tooltip("Special attack sound (e.g. beam charge, energy sweep) played when entering Skill state.")]
        [SerializeField] private AudioClip skillAttackClip;

        [Tooltip("Cinematic ultimate attack sound played when entering Ultimate state.")]
        [SerializeField] private AudioClip ultimateAttackClip;

        [Header("Hit Impacts (Damage Feedback on Recipient)")]
        [Tooltip("Snappy crack played when a Light attack connects (<15 damage).")]
        [SerializeField] private AudioClip lightHitClip;

        [Tooltip("Visceral heavy bass crunch played when a Heavy attack connects (15-19 damage).")]
        [SerializeField] private AudioClip heavyHitClip;

        [Tooltip("Energy burst resonance played when a Special Skill connects (20-39 damage).")]
        [SerializeField] private AudioClip skillHitClip;

        [Tooltip("Massive cinematic explosion played when an Ultimate connects (>=40 damage).")]
        [SerializeField] private AudioClip ultimateHitClip;

        [Header("Ki & Mystic Energy")]
        [Tooltip("Celestial crackling aura hum looped continuously during Meditation state.")]
        [SerializeField] private AudioClip kiChargeLoopClip;

        [Tooltip("Ominous heartbeat / pulse warning played once when entering Red Zone exhaustion.")]
        [SerializeField] private AudioClip redZoneWarningClip;

        [Header("Voice Over & Cannon Dialogue (Character-Specific)")]
        [Tooltip("Effort vocal grunt played on basic attacks.")]
        [SerializeField] private AudioClip voiceAttackGruntClip;

        [Tooltip("Pain / hurt vocal callout played when receiving hit damage.")]
        [SerializeField] private AudioClip voiceHurtClip;

        [Tooltip("Special skill vocal shout (e.g., 'Kamehameha!' or 'Consecutive Normal Punches!').")]
        [SerializeField] private AudioClip voiceSkillClip;

        [Tooltip("Ultimate finisher vocal shout (e.g., 'Spirit Bomb!' or 'Serious Punch!').")]
        [SerializeField] private AudioClip voiceUltimateClip;

        [Header("Live Volume Sliders (Adjustable in Play Mode)")]
        [Tooltip("Master sound effect volume multiplier.")]
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 1.0f;

        [Tooltip("Whiff and movement swoosh volume.")]
        [SerializeField] [Range(0f, 1f)] private float whiffVolume = 0.65f;

        [Tooltip("Hit impact volume (balanced high for maximum punch weight).")]
        [SerializeField] [Range(0f, 1f)] private float impactVolume = 1.0f;

        [Tooltip("Character voice and vocal grunts volume.")]
        [SerializeField] [Range(0f, 1f)] private float voiceVolume = 0.9f;

        [Tooltip("Looping Ki meditation aura hum volume (tuned to 60% of original for balanced combat acoustics).")]
        [SerializeField] [Range(0f, 1f)] private float kiAuraVolume = 0.45f;

        #endregion

        #region Properties

        public float SfxVolume { get => sfxVolume; set => sfxVolume = Mathf.Clamp01(value); }
        public float ImpactVolume { get => impactVolume; set => impactVolume = Mathf.Clamp01(value); }
        public float WhiffVolume { get => whiffVolume; set => whiffVolume = Mathf.Clamp01(value); }
        public float VoiceVolume { get => voiceVolume; set => voiceVolume = Mathf.Clamp01(value); }
        public float KiAuraVolume { get => kiAuraVolume; set => kiAuraVolume = Mathf.Clamp01(value); }

        public AudioClip JumpClip { get => jumpClip; set => jumpClip = value; }
        public AudioClip LandClip { get => landClip; set => landClip = value; }
        public AudioClip CrouchClip { get => crouchClip; set => crouchClip = value; }
        public AudioClip DashClip { get => dashClip; set => dashClip = value; }
        public AudioClip LightWhiffClip { get => lightWhiffClip; set => lightWhiffClip = value; }
        public AudioClip HeavyWhiffClip { get => heavyWhiffClip; set => heavyWhiffClip = value; }
        public AudioClip SkillAttackClip { get => skillAttackClip; set => skillAttackClip = value; }
        public AudioClip UltimateAttackClip { get => ultimateAttackClip; set => ultimateAttackClip = value; }
        public AudioClip LightHitClip { get => lightHitClip; set => lightHitClip = value; }
        public AudioClip HeavyHitClip { get => heavyHitClip; set => heavyHitClip = value; }
        public AudioClip SkillHitClip { get => skillHitClip; set => skillHitClip = value; }
        public AudioClip UltimateHitClip { get => ultimateHitClip; set => ultimateHitClip = value; }
        public AudioClip KiChargeLoopClip { get => kiChargeLoopClip; set => kiChargeLoopClip = value; }
        public AudioClip RedZoneWarningClip { get => redZoneWarningClip; set => redZoneWarningClip = value; }
        public AudioClip VoiceAttackGruntClip { get => voiceAttackGruntClip; set => voiceAttackGruntClip = value; }
        public AudioClip VoiceHurtClip { get => voiceHurtClip; set => voiceHurtClip = value; }
        public AudioClip VoiceSkillClip { get => voiceSkillClip; set => voiceSkillClip = value; }
        public AudioClip VoiceUltimateClip { get => voiceUltimateClip; set => voiceUltimateClip = value; }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            SetupAudioSources();
        }

        private void OnValidate()
        {
            // Sync live sliders if tweaked in Inspector while playing
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
            if (auraSource != null && auraSource.isPlaying)
            {
                auraSource.volume = kiAuraVolume * sfxVolume;
            }
        }

        private void Update()
        {
            if (sfxSource != null && Mathf.Abs(sfxSource.volume - sfxVolume) > 0.001f)
            {
                sfxSource.volume = sfxVolume;
            }

            if (auraSource != null && auraSource.isPlaying)
            {
                float targetAuraVol = kiAuraVolume * sfxVolume;
                if (Mathf.Abs(auraSource.volume - targetAuraVol) > 0.001f)
                {
                    auraSource.volume = targetAuraVol;
                }
            }
        }

        #endregion

        #region Audio Source Initialization

        private void SetupAudioSources()
        {
            // 1. One-Shot SFX Source
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
                if (sfxSource == null)
                {
                    sfxSource = gameObject.AddComponent<AudioSource>();
                }
            }
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D uniform sound
            sfxSource.volume = sfxVolume;
            sfxSource.mute = false;

            // 2. Looping Ki Aura Source
            if (auraSource == null)
            {
                Transform existingChild = transform.Find("Ki_Aura_AudioSource");
                if (existingChild != null)
                {
                    auraSource = existingChild.GetComponent<AudioSource>();
                }

                if (auraSource == null)
                {
                    GameObject auraObj = new GameObject("Ki_Aura_AudioSource");
                    auraObj.transform.SetParent(transform, false);
                    auraSource = auraObj.AddComponent<AudioSource>();
                }
            }
            auraSource.loop = true;
            auraSource.playOnAwake = false;
            auraSource.spatialBlend = 0f;
            auraSource.volume = kiAuraVolume * sfxVolume;
            auraSource.mute = false;
        }

        #endregion

        #region Movement & Agility Playback

        public void PlayJump()
        {
            PlaySound(jumpClip, whiffVolume * 0.9f * sfxVolume);
        }

        public void PlayLand()
        {
            PlaySound(landClip, whiffVolume * 0.85f * sfxVolume);
        }

        public void PlayCrouch()
        {
            PlaySound(crouchClip, whiffVolume * 0.55f * sfxVolume);
        }

        public void PlayDash()
        {
            PlaySound(dashClip, whiffVolume * sfxVolume);
        }

        #endregion

        #region Attack Whiff & Startup Playback

        public void PlayLightWhiff()
        {
            PlaySound(lightWhiffClip, whiffVolume * sfxVolume);
        }

        public void PlayHeavyWhiff()
        {
            PlaySound(heavyWhiffClip, whiffVolume * sfxVolume);
        }

        public void PlaySkillAttack()
        {
            if (skillAttackClip != null)
            {
                PlaySound(skillAttackClip, impactVolume * 0.9f * sfxVolume);
            }

            PlayVoiceSkill();
        }

        public void PlayUltimateAttack()
        {
            if (ultimateAttackClip != null)
            {
                PlaySound(ultimateAttackClip, impactVolume * sfxVolume);
            }

            PlayVoiceUltimate();
        }

        #endregion

        #region Hit Impact Playback (Called on Target Fighter)

        public void PlayLightHit()
        {
            PlaySound(lightHitClip, impactVolume * sfxVolume);
        }

        public void PlayHeavyHit()
        {
            PlaySound(heavyHitClip, impactVolume * sfxVolume);
        }

        public void PlaySkillHit()
        {
            PlaySound(skillHitClip, impactVolume * sfxVolume);
        }

        public void PlayUltimateHit()
        {
            PlaySound(ultimateHitClip, impactVolume * sfxVolume);
        }

        #endregion

        #region Voice Over & Grunt Playback

        public void PlayVoiceAttackGrunt()
        {
            if (voiceAttackGruntClip != null)
            {
                PlaySound(voiceAttackGruntClip, voiceVolume * sfxVolume);
            }
        }

        public void PlayVoiceHurt()
        {
            if (voiceHurtClip != null)
            {
                PlaySound(voiceHurtClip, voiceVolume * sfxVolume);
            }
        }

        public void PlayVoiceSkill()
        {
            if (voiceSkillClip != null)
            {
                PlaySound(voiceSkillClip, voiceVolume * sfxVolume);
            }
        }

        public void PlayVoiceUltimate()
        {
            if (voiceUltimateClip != null)
            {
                PlaySound(voiceUltimateClip, voiceVolume * sfxVolume);
            }
        }

        #endregion

        #region Ki Meditation Aura & Red Zone

        public void StartKiMeditationAura()
        {
            if (auraSource == null || kiChargeLoopClip == null) return;

            if (!auraSource.isPlaying || auraSource.clip != kiChargeLoopClip)
            {
                auraSource.clip = kiChargeLoopClip;
                auraSource.volume = kiAuraVolume * sfxVolume;
                auraSource.loop = true;
                auraSource.Play();
            }
        }

        public void StopKiMeditationAura()
        {
            if (auraSource != null && auraSource.isPlaying)
            {
                auraSource.Stop();
            }
        }

        public void PlayRedZoneWarning()
        {
            PlaySound(redZoneWarningClip, impactVolume * 0.9f * sfxVolume);
        }

        #endregion

        #region Helper

        private void PlaySound(AudioClip clip, float volume)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        #endregion
    }
}
