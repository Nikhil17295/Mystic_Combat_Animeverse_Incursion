using System.IO;
using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Renders authentic Dragon Ball Z style Kamehameha visual effects:
    /// 1. Charging Phase (2.15s): Gathering, pulsing electric cyan Ki orb with white-hot core
    ///    held in Goku's cupped palms at his hip ("KA... ME... HA... ME...").
    /// 2. Blasting Phase (1.21s): Massive solid light-blue energy beam shooting forward
    ///    across the screen ("HA-A-A-A!"), featuring origin muzzle flare, white laser core,
    ///    electric cyan casing, leading dome head, screen shake, and full AoE coverage.
    /// 3. Dissipation Phase: Smooth energy tapering as the attack finishes.
    /// Interruption-safe: immediately deactivates if Goku is hit or knocked down.
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class KamehamehaBeamVFX : MonoBehaviour
    {
        #region Fields

        private FighterController controller;
        private SpriteRenderer gokuSr;

        // Visual Hierarchy
        private GameObject beamRoot;
        private GameObject chargeOrbObj;
        private SpriteRenderer chargeOrbSr;

        private GameObject muzzleFlareObj;
        private SpriteRenderer muzzleFlareSr;

        private GameObject beamBodyObj;
        private SpriteRenderer beamBodySr;

        private GameObject beamHeadObj;
        private SpriteRenderer beamHeadSr;

        // Textures
        private Texture2D chargeOrbTex;
        private Texture2D muzzleFlareTex;
        private Texture2D beamBodyTex;
        private Texture2D beamHeadTex;

        // Tuning & Dimensions
        private const float BEAM_BODY_LENGTH = 7.2f;
        private const float BEAM_HEAD_LENGTH = 1.3f;
        private const float BASE_THICKNESS = 1.45f;
        private const float CHARGE_DURATION = 2.15f;

        private bool wasInBlastPhase;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            controller = GetComponent<FighterController>();
            gokuSr = GetComponent<SpriteRenderer>();

            LoadTextures();
            BuildBeamHierarchy();
        }

        private void Update()
        {
            if (controller == null) return;

            bool isGoku = gameObject.name.ToLower().Contains("goku");
            bool isUltimate = controller.CurrentState == FighterState.Ultimate && isGoku;

            if (!isUltimate)
            {
                if (wasInBlastPhase || (chargeOrbObj != null && chargeOrbObj.activeSelf))
                {
                    StopAllVFX();
                }
                wasInBlastPhase = false;
                return;
            }

            float startup = controller.AttackStartupTimer;
            float active = controller.AttackActiveTimer;

            bool faceLeft = gokuSr != null && gokuSr.flipX;
            float facingDir = faceLeft ? -1f : 1f;

            if (startup > 0f)
            {
                // --- PHASE 1: CHARGING KI ORB IN CUPPED HANDS ---
                wasInBlastPhase = false;
                UpdateChargeOrb(startup, facingDir);
            }
            else if (active > 0f)
            {
                // --- PHASE 2: SOLID LIGHT BLUE BEAM BLAST ---
                if (!wasInBlastPhase)
                {
                    wasInBlastPhase = true;
                    OnBlastErupt();
                }

                UpdateBeamBlast(active, facingDir);
            }
            else
            {
                StopAllVFX();
                wasInBlastPhase = false;
            }
        }

        private void OnDisable()
        {
            StopAllVFX();
            wasInBlastPhase = false;
        }

        #endregion

        #region Setup & Textures

        private void LoadTextures()
        {
            string fxDir = Path.Combine(Application.dataPath, "Sprites", "Effects");
            chargeOrbTex = LoadTextureFromFile(Path.Combine(fxDir, "kamehameha_charge_orb.png"));
            muzzleFlareTex = LoadTextureFromFile(Path.Combine(fxDir, "kamehameha_muzzle_flare.png"));
            beamBodyTex = LoadTextureFromFile(Path.Combine(fxDir, "kamehameha_beam_body.png"));
            beamHeadTex = LoadTextureFromFile(Path.Combine(fxDir, "kamehameha_beam_head.png"));
        }

        private Texture2D LoadTextureFromFile(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(bytes);
            return tex;
        }

        private void BuildBeamHierarchy()
        {
            beamRoot = new GameObject("Kamehameha_VFX_Root");
            beamRoot.transform.SetParent(transform);
            beamRoot.transform.localPosition = Vector3.zero;

            // 1. Charge Orb
            chargeOrbObj = new GameObject("Kamehameha_Charge_Orb");
            chargeOrbObj.transform.SetParent(beamRoot.transform);
            chargeOrbSr = chargeOrbObj.AddComponent<SpriteRenderer>();
            chargeOrbSr.sortingOrder = 5; // In front of fighter
            if (chargeOrbTex != null)
            {
                chargeOrbSr.sprite = Sprite.Create(chargeOrbTex, new Rect(0, 0, chargeOrbTex.width, chargeOrbTex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            chargeOrbObj.SetActive(false);

            // 2. Muzzle Flare
            muzzleFlareObj = new GameObject("Kamehameha_Muzzle_Flare");
            muzzleFlareObj.transform.SetParent(beamRoot.transform);
            muzzleFlareSr = muzzleFlareObj.AddComponent<SpriteRenderer>();
            muzzleFlareSr.sortingOrder = 6;
            if (muzzleFlareTex != null)
            {
                muzzleFlareSr.sprite = Sprite.Create(muzzleFlareTex, new Rect(0, 0, muzzleFlareTex.width, muzzleFlareTex.height), new Vector2(0.5f, 0.5f), 100f);
            }
            muzzleFlareObj.SetActive(false);

            // 3. Beam Body (Pivot at Left-Center so it extends forward)
            beamBodyObj = new GameObject("Kamehameha_Beam_Body");
            beamBodyObj.transform.SetParent(beamRoot.transform);
            beamBodySr = beamBodyObj.AddComponent<SpriteRenderer>();
            beamBodySr.sortingOrder = 5;
            if (beamBodyTex != null)
            {
                beamBodySr.sprite = Sprite.Create(beamBodyTex, new Rect(0, 0, beamBodyTex.width, beamBodyTex.height), new Vector2(0.0f, 0.5f), 100f);
            }
            beamBodyObj.SetActive(false);

            // 4. Beam Head (Pivot at Left-Center)
            beamHeadObj = new GameObject("Kamehameha_Beam_Head");
            beamHeadObj.transform.SetParent(beamRoot.transform);
            beamHeadSr = beamHeadObj.AddComponent<SpriteRenderer>();
            beamHeadSr.sortingOrder = 5;
            if (beamHeadTex != null)
            {
                beamHeadSr.sprite = Sprite.Create(beamHeadTex, new Rect(0, 0, beamHeadTex.width, beamHeadTex.height), new Vector2(0.0f, 0.5f), 100f);
            }
            beamHeadObj.SetActive(false);
        }

        #endregion

        #region Phase Animations

        private void UpdateChargeOrb(float startupTimer, float facingDir)
        {
            if (chargeOrbObj == null) return;

            if (!chargeOrbObj.activeSelf) chargeOrbObj.SetActive(true);
            if (beamBodyObj != null) beamBodyObj.SetActive(false);
            if (beamHeadObj != null) beamHeadObj.SetActive(false);
            if (muzzleFlareObj != null) muzzleFlareObj.SetActive(false);

            // Position at Goku's cupped palms behind hip
            // Facing right: hands are drawn back to left hip (-0.35f)
            // Facing left: hands are drawn back to right hip (+0.35f)
            Vector3 handPos = new Vector3(-facingDir * 0.35f, 0.88f, 0f);
            chargeOrbObj.transform.localPosition = handPos;

            // Swell from tiny spark to dense, crackling energy sphere
            float progress = Mathf.Clamp01(1f - (startupTimer / CHARGE_DURATION));
            float pulse = Mathf.Sin(Time.time * 30f) * 0.05f;
            float scale = Mathf.Lerp(0.20f, 0.88f, progress * progress) + pulse;
            chargeOrbObj.transform.localScale = new Vector3(scale, scale, 1f);

            // Glow alpha intensity
            if (chargeOrbSr != null)
            {
                float alpha = Mathf.Lerp(0.70f, 1.0f, progress);
                chargeOrbSr.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void OnBlastErupt()
        {
            // Visceral screen shake on Kamehameha release
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.TriggerCameraShake(0.30f, 0.38f);
                VFXManager.Instance.TriggerImpactFlash(0.04f);
            }

            Debug.Log("<color=#00e5ff>[KamehamehaBeamVFX] KAMEHAMEHA BLAST ERUPTED!</color>");
        }

        private void UpdateBeamBlast(float activeTimer, float facingDir)
        {
            if (chargeOrbObj != null) chargeOrbObj.SetActive(false);

            if (beamBodyObj != null && !beamBodyObj.activeSelf) beamBodyObj.SetActive(true);
            if (beamHeadObj != null && !beamHeadObj.activeSelf) beamHeadObj.SetActive(true);
            if (muzzleFlareObj != null && !muzzleFlareObj.activeSelf) muzzleFlareObj.SetActive(true);

            // Origin at thrusting palms
            Vector3 handPos = new Vector3(facingDir * 0.55f, 0.95f, 0f);

            // Roaring plasma fluctuation
            float pulseY = Mathf.Sin(Time.time * 26f) * 0.08f;
            float thickness = BASE_THICKNESS + pulseY;

            // Smooth fade out over the final 0.20s
            float alpha = 1.0f;
            if (activeTimer < 0.20f)
            {
                alpha = Mathf.Clamp01(activeTimer / 0.20f);
                thickness *= alpha;
            }

            Color beamCol = new Color(1f, 1f, 1f, alpha);

            // 1. Muzzle Flare
            if (muzzleFlareObj != null)
            {
                muzzleFlareObj.transform.localPosition = handPos;
                float flareScale = thickness * 1.15f;
                muzzleFlareObj.transform.localScale = new Vector3(flareScale, flareScale, 1f);
                if (muzzleFlareSr != null) muzzleFlareSr.color = beamCol;
            }

            // 2. Beam Body (Spans from handPos across BEAM_BODY_LENGTH)
            if (beamBodyObj != null)
            {
                beamBodyObj.transform.localPosition = handPos;
                float bodyScaleX = facingDir * (BEAM_BODY_LENGTH / 2.56f); // 256px at 100 PPU = 2.56 units
                beamBodyObj.transform.localScale = new Vector3(bodyScaleX, thickness, 1f);
                if (beamBodySr != null) beamBodySr.color = beamCol;
            }

            // 3. Beam Head (Positioned at the tip of the beam)
            if (beamHeadObj != null)
            {
                Vector3 headPos = handPos + new Vector3(facingDir * BEAM_BODY_LENGTH, 0f, 0f);
                beamHeadObj.transform.localPosition = headPos;
                float headScaleX = facingDir * (BEAM_HEAD_LENGTH / 1.28f);
                beamHeadObj.transform.localScale = new Vector3(headScaleX, thickness, 1f);
                if (beamHeadSr != null) beamHeadSr.color = beamCol;
            }
        }

        private void StopAllVFX()
        {
            if (chargeOrbObj != null) chargeOrbObj.SetActive(false);
            if (muzzleFlareObj != null) muzzleFlareObj.SetActive(false);
            if (beamBodyObj != null) beamBodyObj.SetActive(false);
            if (beamHeadObj != null) beamHeadObj.SetActive(false);
        }

        #endregion
    }
}
