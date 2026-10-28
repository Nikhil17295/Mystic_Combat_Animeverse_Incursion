using System;
using UnityEngine;

namespace MysticCombat.Data
{
    /// <summary>
    /// Plain C# data class that mirrors the character JSON structure.
    /// Deserialized via JsonUtility. Field names must match JSON keys exactly.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        #region Identity

        public string characterId;
        public string displayName;

        #endregion

        #region Base Stats

        public int maxHealth = 100;
        public float moveSpeed = 5f;
        public float jumpForce = 12f;
        public float gravityScale = 3f;

        #endregion

        #region Attacks

        public AttackData lightAttack;
        public AttackData heavyAttack;

        #endregion

        #region Skills & Ultimate

        public SkillData skill;
        public UltimateData ultimate;

        #endregion

        #region Ki & Meditation

        public float meditateKiPerSecond = 15f;
        public float kiGainOnDamageTaken = 3f;

        #endregion

        #region Red Zone

        public RedZoneData redZone;

        #endregion
    }

    /// <summary>
    /// Data for a basic attack (light or heavy).
    /// </summary>
    [Serializable]
    public class AttackData
    {
        public int damage;
        public float knockback;
        public int startupFrames;
        public int activeFrames;
        public int recoveryFrames;
        public Vector2Data hitboxOffset;
        public Vector2Data hitboxSize;
        public float kiGainOnHit;
    }

    /// <summary>
    /// Data for a character's special skill.
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public string name;
        public string type;        // "projectile", "instant", "toggle"
        public float kiCost;
        public int damage;
        public string buffs;
        public float speed;         // projectile speed (if applicable)
        public float lifetime;      // projectile lifetime (if applicable)
        public int hits;            // number of hits (for multi-hit skills)
        public int startupFrames;
        public int activeFrames;
        public int recoveryFrames;
    }

    /// <summary>
    /// Data for a character's ultimate ability.
    /// </summary>
    [Serializable]
    public class UltimateData
    {
        public string name;
        public float kiCost;
        public int damage;
        public Vector2Data hitboxSize;
        public int startupFrames;
        public int activeFrames;
        public int recoveryFrames;
        public string drawback;
    }

    /// <summary>
    /// Data for the Red Zone mechanic.
    /// </summary>
    [Serializable]
    public class RedZoneData
    {
        public float threshold = 10f;
        public float duration = 5f;
        public float moveSpeedMultiplier = 0.3f;
        public float recoveryKi = 20f;
        public float hitTimerReduction = 0.1f;
    }

    /// <summary>
    /// Serializable 2D vector for JSON x/y pairs.
    /// JsonUtility cannot deserialize Unity's Vector2 from { "x": ..., "y": ... },
    /// so we use this plain class instead.
    /// </summary>
    [Serializable]
    public class Vector2Data
    {
        public float x;
        public float y;

        /// <summary>
        /// Converts this data object to a Unity Vector2.
        /// </summary>
        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }
}
