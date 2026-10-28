using UnityEngine;
using MysticCombat.Systems;

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

        [Tooltip("AudioSource for high-priority character vocal cries and special shouts.")]
        [SerializeField] private AudioSource voiceSource;

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
        [SerializeField] [Range(0f, 1f)] private float voiceVolume = 1.0f;

        [Tooltip("Looping Ki meditation aura hum volume (tuned down to a soft 12% ambient vibration so it never overpowers battle acoustics).")]
        [SerializeField] [Range(0f, 1f)] private float kiAuraVolume = 0.12f;

        [Header("Move-Specific Vocal Amplification")]
        [Tooltip("Volume multiplier applied to special skill vocal shouts (e.g. Consecutive Normal Punches).")]
        [SerializeField] [Range(0.5f, 2.5f)] private float voiceSkillMultiplier = 1.45f;

        [Tooltip("Volume multiplier applied to ultimate finisher vocal shouts (e.g. Serious Punch).")]
        [SerializeField] [Range(0.5f, 2.5f)] private float voiceUltimateMultiplier = 1.70f;

        #endregion

        #region Properties

        public float SfxVolume { get => sfxVolume; set => sfxVolume = Mathf.Clamp01(value); }
        public float ImpactVolume { get => impactVolume; set => impactVolume = Mathf.Clamp01(value); }
        public float WhiffVolume { get => whiffVolume; set => whiffVolume = Mathf.Clamp01(value); }
        public float VoiceVolume { get => voiceVolume; set => voiceVolume = Mathf.Clamp01(value); }
        public float KiAuraVolume { get => kiAuraVolume; set => kiAuraVolume = Mathf.Clamp01(value); }
        public float VoiceSkillMultiplier { get => voiceSkillMultiplier; set => voiceSkillMultiplier = Mathf.Clamp(value, 0.1f, 3.0f); }
        public float VoiceUltimateMultiplier { get => voiceUltimateMultiplier; set => voiceUltimateMultiplier = Mathf.Clamp(value, 0.1f, 3.0f); }

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
            InputSettingsManager.EnsureInitialized();
            SetupAudioSources();
            EnsureClipsLoaded();
        }

        /// <summary>
        /// Guarantees that essential voice, hit, and movement audio clips are populated at runtime,
        /// even when fighters are dynamically spawned or instantiated via code.
        /// </summary>
        public void EnsureClipsLoaded()
        {
            string fighterName = gameObject.name.ToLower();
            bool isSaitama = fighterName.Contains("saitama");
            bool isGoku = fighterName.Contains("goku");

            if (isSaitama)
            {
                if (voiceSkillClip == null) voiceSkillClip = Resources.Load<AudioClip>("Audio/Anime_Powers/consecutive_normal_punches");
                if (voiceUltimateClip == null) voiceUltimateClip = Resources.Load<AudioClip>("Audio/Anime_Powers/serious_punch");
                if (lightWhiffClip == null) lightWhiffClip = Resources.Load<AudioClip>("Audio/Swishes_Whiffs/swish_11");
                if (heavyWhiffClip == null) heavyWhiffClip = Resources.Load<AudioClip>("Audio/Swishes_Whiffs/swish_06");
                if (lightHitClip == null) lightHitClip = Resources.Load<AudioClip>("Audio/Hits_Punches/arcade_hit_01");
                if (heavyHitClip == null) heavyHitClip = Resources.Load<AudioClip>("Audio/Hits_Punches/saitama_normal_punch");
                voiceSkillMultiplier = 2.20f;   // Boosted so Consecutive Punches shout cuts through punch hits
                voiceUltimateMultiplier = 2.50f; // Boosted so Serious Punch is loud and dramatic
            }
            else if (isGoku)
            {
                // Goku has NO skill voice
                voiceSkillClip = null;
                // Ultimate (Q / O, Ki: 80): Kamehameha!
                if (voiceUltimateClip == null) voiceUltimateClip = Resources.Load<AudioClip>("Audio/Anime_Powers/goku_voice_kamehameha");
                if (lightWhiffClip == null) lightWhiffClip = Resources.Load<AudioClip>("Audio/Swishes_Whiffs/swish_13");
                if (heavyWhiffClip == null) heavyWhiffClip = Resources.Load<AudioClip>("Audio/Swishes_Whiffs/swish_09");
                if (lightHitClip == null) lightHitClip = Resources.Load<AudioClip>("Audio/Hits_Punches/arcade_hit_01");
                if (heavyHitClip == null) heavyHitClip = Resources.Load<AudioClip>("Audio/Hits_Punches/arcade_hit_04");
                voiceSkillMultiplier = 1.0f;
                voiceUltimateMultiplier = 0.50f; // Calibrated down so Kamehameha voice is ~50% (30% drop per user request)
            }

            if (jumpClip == null) jumpClip = Resources.Load<AudioClip>("Audio/Anime_Powers/jumpland_clean");
            if (landClip == null) landClip = Resources.Load<AudioClip>("Audio/Anime_Powers/jumpland_clean");
            if (crouchClip == null) crouchClip = Resources.Load<AudioClip>("Audio/Swishes_Whiffs/swish_05");
            if (dashClip == null) dashClip = Resources.Load<AudioClip>("Audio/Anime_Powers/teleport_dash");
            if (kiChargeLoopClip == null) kiChargeLoopClip = Resources.Load<AudioClip>("Audio/Anime_Powers/dbz_super_saiyan_aura");
            if (redZoneWarningClip == null) redZoneWarningClip = Resources.Load<AudioClip>("Audio/Hits_Punches/punch_foley_punch_1");

            // Guarantee soft ambient meditation volume on spawn
            kiAuraVolume = 0.12f;
        }

        private void OnValidate()
        {
            // Sync live sliders if tweaked in Inspector while playing
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
            if (voiceSource != null)
            {
                voiceSource.volume = voiceVolume * sfxVolume;
            }
            if (auraSource != null && auraSource.isPlaying)
            {
                auraSource.volume = kiAuraVolume * sfxVolume;
            }
        }

        private void Update()
        {
            // One-shot audio sources are kept at 1.0f so PlayOneShot handles master scaling without double multiplication
            if (sfxSource != null && sfxSource.volume != 1.0f)
            {
                sfxSource.volume = 1.0f;
            }

            if (voiceSource != null && voiceSource.volume != 1.0f)
            {
                voiceSource.volume = 1.0f;
            }

            if (auraSource != null && auraSource.isPlaying)
            {
                float targetAuraVol = kiAuraVolume * sfxVolume * InputSettingsManager.SFXVolume;
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
            sfxSource.volume = 1.0f;
            sfxSource.mute = false;

            // 2. Dedicated Voice Source (for loud, impactful anime vocal shouts)
            if (voiceSource == null)
            {
                Transform existingVoice = transform.Find("Voice_AudioSource");
                if (existingVoice != null)
                {
                    voiceSource = existingVoice.GetComponent<AudioSource>();
                }

                if (voiceSource == null)
                {
                    GameObject voiceObj = new GameObject("Voice_AudioSource");
                    voiceObj.transform.SetParent(transform, false);
                    voiceSource = voiceObj.AddComponent<AudioSource>();
                }
            }
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
            voiceSource.volume = 1.0f;
            voiceSource.mute = false;

            // 3. Looping Ki Aura Source (Soft ambient 12% hum)
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
            auraSource.volume = kiAuraVolume * sfxVolume * InputSettingsManager.SFXVolume;
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
                float vol = voiceVolume * sfxVolume * voiceSkillMultiplier;
                PlayVoiceSound(voiceSkillClip, vol);
            }
        }

        public void PlayVoiceUltimate()
        {
            if (voiceUltimateClip != null)
            {
                float vol = voiceVolume * sfxVolume * voiceUltimateMultiplier;
                PlayVoiceSound(voiceUltimateClip, vol);
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
            float masterSfx = InputSettingsManager.SFXVolume;
            sfxSource.PlayOneShot(clip, Mathf.Clamp(volume * masterSfx, 0f, 2.5f));
        }

        private void PlayVoiceSound(AudioClip clip, float volume)
        {
            if (clip == null) return;
            AudioSource src = (voiceSource != null) ? voiceSource : sfxSource;
            if (src != null)
            {
                float tunedVolume = volume;
                if (clip.name.ToLower().Contains("kamehameha"))
                {
                    // Strict clamp on Kamehameha voice line so it never blows out ears
                    tunedVolume = Mathf.Min(tunedVolume, 0.50f);
                }
                float masterSfx = InputSettingsManager.SFXVolume;
                src.PlayOneShot(clip, Mathf.Clamp(tunedVolume * masterSfx, 0f, 2.5f));
            }
        }

        #endregion
    }
}
