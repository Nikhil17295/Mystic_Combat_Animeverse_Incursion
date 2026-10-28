using System.IO;
using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Spawns and manages authentic Dragon Ball Z style spiky flame auras around fighters.
    /// Uses an oscillating flame sprite cocoon (dbz_spiky_aura.png) wrapping around the fighter
    /// with high-frequency flame flickering and alpha pulsation (pure sprite-driven anime fire,
    /// with zero particle emitters for crystal-clear pixel art clarity).
    /// Tinted dynamically:
    /// - Super Saiyan Goku: Radiant DBZ golden flame
    /// - Base Goku (Meditate): Electric cyan flame
    /// - Saitama: Blazing crimson flame
    /// </summary>
    [RequireComponent(typeof(FighterController))]
    public class FighterKiAura : MonoBehaviour
    {
        #region Fields

        private FighterController controller;

        // Spiky Flame Sprite Shell
        private GameObject auraShellObj;
        private SpriteRenderer auraShellRenderer;
        private Vector3 baseScale = new Vector3(0.82f, 0.88f, 1f);

        private bool isAuraActive;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            controller = GetComponent<FighterController>();
            SetupAuraShell();
        }

        private void Update()
        {
            if (controller == null) return;

            bool shouldBeActive = controller.IsSuperSaiyan || controller.CurrentState == FighterState.Meditating;

            if (shouldBeActive && !isAuraActive)
            {
                StartAura();
            }
            else if (!shouldBeActive && isAuraActive)
            {
                StopAura();
            }

            if (isAuraActive)
            {
                UpdateAuraVisuals();
            }
        }

        #endregion

        #region Setup

        private void SetupAuraShell()
        {
            auraShellObj = new GameObject("Aura_Spiky_Flame_Shell");
            auraShellObj.transform.SetParent(transform);
            auraShellObj.transform.localPosition = new Vector3(0f, 0.05f, 0.05f); // Rest on floor at feet level
            auraShellObj.transform.localScale = baseScale;

            auraShellRenderer = auraShellObj.AddComponent<SpriteRenderer>();
            auraShellRenderer.sortingOrder = 1; // Behind fighter body (order 2)

            string spikyPath = Path.Combine(Application.dataPath, "Sprites", "Effects", "dbz_spiky_aura.png");
            if (File.Exists(spikyPath))
            {
                byte[] bytes = File.ReadAllBytes(spikyPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.LoadImage(bytes);

                // Pivot near lower-middle so scaling flares upward from feet
                Sprite sp = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.04f), // Pivot at bottom ground line
                    64f
                );
                auraShellRenderer.sprite = sp;
            }

            auraShellObj.SetActive(false);
        }

        #endregion

        #region Aura Controls

        public void StartAura()
        {
            isAuraActive = true;
            if (auraShellObj != null) auraShellObj.SetActive(true);
            UpdateAuraVisuals();
        }

        public void StopAura()
        {
            isAuraActive = false;
            if (auraShellObj != null) auraShellObj.SetActive(false);
        }

        private void UpdateAuraVisuals()
        {
            Color primaryColor;

            if (controller != null && controller.IsSuperSaiyan)
            {
                // Radiant Super Saiyan Gold
                primaryColor = new Color(1f, 0.88f, 0.22f, 0.85f);
            }
            else if (gameObject.name.ToLower().Contains("saitama"))
            {
                // Blazing Crimson/Scarlet
                primaryColor = new Color(1f, 0.18f, 0.15f, 0.85f);
            }
            else
            {
                // Vibrant Electric Cyan (Base Goku Ki)
                primaryColor = new Color(0.18f, 0.85f, 1f, 0.85f);
            }

            // Spiky flame shell rapid flickering & scale oscillation (simulating raging anime fire)
            if (auraShellObj != null && auraShellRenderer != null)
            {
                float flickerX = 1f + Mathf.Sin(Time.time * 16f) * 0.035f + Mathf.Cos(Time.time * 10f) * 0.02f;
                float flickerY = 1f + Mathf.Cos(Time.time * 14f) * 0.045f + Mathf.Sin(Time.time * 9f) * 0.025f;
                auraShellObj.transform.localScale = new Vector3(baseScale.x * flickerX, baseScale.y * flickerY, 1f);

                float alphaPulse = 0.80f + Mathf.PingPong(Time.time * 7f, 0.18f);
                primaryColor.a = alphaPulse;
                auraShellRenderer.color = primaryColor;
            }
        }

        #endregion
    }
}
