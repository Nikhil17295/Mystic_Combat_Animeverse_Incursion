using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using MysticCombat.Core;

namespace MysticCombat.Core
{
    /// <summary>
    /// Controls dynamic combat music and victory fanfares in the fighting arena:
    /// - Continuous Combat BGM: Double Dragon 2 - Final Battle 2 played seamlessly across all rounds.
    /// - Match Over / Win: Double Dragon 3 - Mission Clear (3-second triumphant fanfare).
    /// - Audio Balancing: In-match BGM default 0.50 with real-time Inspector sliders for instant live adjustment.
    /// - Strict State Isolation: Battle music ONLY triggers when round combat begins (never in the Main Menu).
    /// - Self-healing fallback: loads directly off disk if unbaked in Inspector.
    /// </summary>
    public class MatchAudioController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private MatchManager matchManager;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Tracks")]
        [Tooltip("Combat battle music played across all 3 rounds (Double Dragon 2 Final Battle 2).")]
        [SerializeField] private AudioClip battleMusic;

        [Tooltip("3-second victory jingle played when a fighter wins the match (Double Dragon 3 Mission Clear).")]
        [SerializeField] private AudioClip victoryJingle;

        [Header("Tournament SFX")]
        [Tooltip("Arcade boxing double-strike bell played on round start.")]
        [SerializeField] private AudioClip roundStartBellClip;

        [Tooltip("Resonant brass chime played on round defeat K.O.")]
        [SerializeField] private AudioClip koBellClip;

        [Header("Volume Sliders (Adjust Live in Inspector)")]
        [Tooltip("In-match combat BGM volume (balanced lower to let hits, kicks, and meditation SFX punch through).")]
        [SerializeField] [Range(0f, 1f)] private float battleMusicVolume = 0.50f;

        [Tooltip("Victory fanfare volume played on final match victory.")]
        [SerializeField] [Range(0f, 1f)] private float victoryJingleVolume = 0.85f;

        [Tooltip("Tournament round bell volume (boosted for clear, loud arcade impact).")]
        [SerializeField] [Range(0f, 1f)] private float bellVolume = 1.0f;

        [Tooltip("Delay in seconds after round start bell before battle music kicks in.")]
        [SerializeField] private float musicStartDelayAfterBell = 0.85f;

        private Coroutine battleMusicCoroutine;

        #endregion

        #region Properties

        public float BattleMusicVolume
        {
            get => battleMusicVolume;
            set
            {
                battleMusicVolume = Mathf.Clamp01(value);
                if (musicSource != null && musicSource.isPlaying)
                {
                    musicSource.volume = battleMusicVolume;
                }
            }
        }

        public float VictoryJingleVolume
        {
            get => victoryJingleVolume;
            set => victoryJingleVolume = Mathf.Clamp01(value);
        }

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (matchManager == null)
            {
                matchManager = GetComponent<MatchManager>();
                if (matchManager == null)
                {
                    matchManager = Object.FindAnyObjectByType<MatchManager>();
                }
            }

            // Setup Music AudioSource
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // 2D Sound, uniform across stage
            musicSource.volume = battleMusicVolume;
            musicSource.mute = false;

            // Setup SFX / Fanfare AudioSource
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("Victory_Audio_Source");
                sfxObj.transform.SetParent(transform, false);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = victoryJingleVolume;
            sfxSource.mute = false;

            // Self-healing disk fallback if clips not baked
            StartCoroutine(EnsureAudioClipsLoaded());
        }

        private void OnEnable()
        {
            if (matchManager != null)
            {
                matchManager.OnRoundStart += HandleRoundStart;
                matchManager.OnRoundEnd += HandleRoundEnd;
                matchManager.OnMatchOver += HandleMatchOver;
            }
        }

        private void OnDisable()
        {
            if (matchManager != null)
            {
                matchManager.OnRoundStart -= HandleRoundStart;
                matchManager.OnRoundEnd -= HandleRoundEnd;
                matchManager.OnMatchOver -= HandleMatchOver;
            }
        }

        private void Start()
        {
            // Combat music start is handled exclusively by HandleRoundStart following the round bell sequence
        }

        private void Update()
        {
            // Enable live real-time slider adjustments in the Inspector during play
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.volume = battleMusicVolume;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleRoundStart(int roundNumber)
        {
            // Strictly guard against playing combat music on main menu
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.MainMenu)
            {
                return;
            }

            // 1. Ring the start bell loud and resonant
            if (roundStartBellClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(roundStartBellClip, bellVolume);
            }

            // 2. Stop any existing music so the bell rings in the clear
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            // 3. Start battle BGM cleanly from 0:00 after the bell delay
            if (battleMusicCoroutine != null) StopCoroutine(battleMusicCoroutine);
            battleMusicCoroutine = StartCoroutine(StartBattleMusicDelayed(musicStartDelayAfterBell));
        }

        private IEnumerator StartBattleMusicDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (matchManager != null && matchManager.CurrentMatchState == MatchManager.MatchState.RoundInProgress)
            {
                PlayBattleMusic(resetToStart: true);
            }
        }

        private void HandleRoundEnd(int winnerPlayerIndex, int roundNumber)
        {
            // Cancel any pending delayed music start
            if (battleMusicCoroutine != null)
            {
                StopCoroutine(battleMusicCoroutine);
                battleMusicCoroutine = null;
            }

            // Immediately stop/pause battle music on round defeat
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            // Ring the KO gong loud and clear
            if (koBellClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(koBellClip, bellVolume);
            }
        }

        private void HandleMatchOver(int winnerPlayerIndex)
        {
            Debug.Log($"<color=#00e676>[MatchAudioController]</color> Match Over! Stopping battle BGM and playing Victory Fanfare for Player {winnerPlayerIndex}!");

            // 1. Cancel delayed music start
            if (battleMusicCoroutine != null)
            {
                StopCoroutine(battleMusicCoroutine);
                battleMusicCoroutine = null;
            }

            // 2. Stop battle music immediately
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            // 3. Play 3-second victory fanfare
            if (victoryJingle != null && sfxSource != null)
            {
                sfxSource.volume = victoryJingleVolume;
                sfxSource.PlayOneShot(victoryJingle, victoryJingleVolume);
            }
        }

        #endregion

        #region Playback Logic

        public void PlayBattleMusic(bool resetToStart = true)
        {
            if (musicSource == null) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.MainMenu) return;

            if (battleMusic != null)
            {
                musicSource.clip = battleMusic;
                musicSource.volume = battleMusicVolume;
                musicSource.loop = true;
                if (resetToStart)
                {
                    musicSource.time = 0f;
                }
                musicSource.Play();
                Debug.Log($"<color=#00e676>[MatchAudioController]</color> Playing Battle BGM from {(resetToStart ? "0:00" : musicSource.time.ToString("F1"))}: {battleMusic.name} (Vol={battleMusicVolume})");
            }
        }

        private IEnumerator FadeVolume(AudioSource source, float startVol, float targetVol, float duration)
        {
            if (source == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVol, targetVol, t);
                yield return null;
            }

            source.volume = targetVol;
        }

        #endregion

        #region Disk Fallback Loader

        private IEnumerator EnsureAudioClipsLoaded()
        {
            string musicFolder = Path.Combine(Application.dataPath, "Audio/Music");
            if (Directory.Exists(musicFolder))
            {
                if (battleMusic == null)
                {
                    yield return StartCoroutine(LoadClipFromFile(Path.Combine(musicFolder, "Double Dragon 2 - The Revenge (NES) Music - Final Battle 2.mp3"), (c) => battleMusic = c));
                }

                if (victoryJingle == null)
                {
                    yield return StartCoroutine(LoadClipFromFile(Path.Combine(musicFolder, "Double Dragon 3 - The Sacred Stone (NES) Music - Mission Clear.mp3"), (c) => victoryJingle = c));
                }
            }

            string sfxFolder = Path.Combine(Application.dataPath, "Audio/SFX");
            if (Directory.Exists(sfxFolder))
            {
                if (roundStartBellClip == null)
                {
                    string bellPath = Path.Combine(sfxFolder, "sfx_round_start_bell.ogg");
                    if (!File.Exists(bellPath)) bellPath = Path.Combine(sfxFolder, "sfx_round_start_bell.wav");
                    yield return StartCoroutine(LoadClipFromFile(bellPath, (c) => roundStartBellClip = c));
                }

                if (koBellClip == null)
                {
                    string koPath = Path.Combine(sfxFolder, "sfx_ko_bell.ogg");
                    if (!File.Exists(koPath)) koPath = Path.Combine(sfxFolder, "sfx_ko_bell.wav");
                    yield return StartCoroutine(LoadClipFromFile(koPath, (c) => koBellClip = c));
                }
            }
        }

        private IEnumerator LoadClipFromFile(string fullPath, System.Action<AudioClip> onLoaded)
        {
            if (!File.Exists(fullPath)) yield break;

            string uri = new System.Uri(fullPath).AbsoluteUri;
            string ext = Path.GetExtension(fullPath).ToLower();
            AudioType audioType = AudioType.MPEG;
            if (ext == ".wav") audioType = AudioType.WAV;
            else if (ext == ".ogg") audioType = AudioType.OGGVORBIS;
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        clip.name = Path.GetFileNameWithoutExtension(fullPath);
                        onLoaded?.Invoke(clip);
                        Debug.Log($"<color=#00e676>[MatchAudioController]</color> Loaded battle audio from disk: {clip.name}");
                    }
                }
            }
        }

        #endregion
    }
}
