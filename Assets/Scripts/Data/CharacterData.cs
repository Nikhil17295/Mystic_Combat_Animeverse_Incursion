using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticCombat.Data
{
    /// <summary>
    /// Plain C# data class that mirrors the character JSON structure.
    /// Deserialized via JsonUtility.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        #region Identity

        public string id;
        public string displayName;
        public string title;
        public string franchise;
        public string archetype;

        #endregion

        #region Base Stats

        public BaseStats baseStats;

        #endregion

        #region Attacks

        public AttackData lightAttack;
        public AttackData heavyAttack;

        #endregion

        #region Skills & Ultimate

        public SkillData skill;
        public UltimateData ultimate;

        #endregion

        #region Red Zone

        public RedZoneData redZone;

        #endregion

        #region Visuals

        public VisualData visuals;

        #endregion
    }

    /// <summary>
    /// Base stat block for a character (HP, speed, etc.).
    /// </summary>
    [Serializable]
    public class BaseStats
    {
        public int maxHealth = 100;
        public float moveSpeed = 5f;
        public float jumpForce = 12f;
        public float gravityScale = 3f;
        public float maxKi = 100f;
        public float kiGainPerHit = 8f;
        public float kiGainPerMeditate = 2f;
    }

    /// <summary>
    /// Data for a basic attack (light or heavy).
    /// </summary>
    [Serializable]
    public class AttackData
    {
        public string name;
        public int damage;
        public float knockback;
        public float startupFrames;
        public float activeFrames;
        public float recoveryFrames;
        public float kiGain;
        public Vector2Data hitboxOffset;
        public Vector2Data hitboxSize;
    }

    /// <summary>
    /// Data for a character's special skill.
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public string name;
        public string description;
        public int damage;
        public float knockback;
        public float kiCost;
        public float cooldown;
        public float startupFrames;
        public float activeFrames;
        public float recoveryFrames;
        public string skillType; // e.g. "projectile", "melee", "buff"
        public Vector2Data hitboxOffset;
        public Vector2Data hitboxSize;
        public ProjectileData projectile;
    }

    /// <summary>
    /// Data for a character's ultimate ability.
    /// </summary>
    [Serializable]
    public class UltimateData
    {
        public string name;
        public string description;
        public int damage;
        public float knockback;
        public float kiCost;
        public float startupFrames;
        public float activeFrames;
        public float recoveryFrames;
        public string ultimateType; // e.g. "cinematic", "area", "beam"
        public Vector2Data hitboxOffset;
        public Vector2Data hitboxSize;
    }

    /// <summary>
    /// Data for the Red Zone mechanic.
    /// </summary>
    [Serializable]
    public class RedZoneData
    {
        public float threshold = 10f;
        public float duration = 5f;
        public float recoveryKi = 20f;
        public float damageMultiplier = 1.5f;
        public string specialAbility;
    }

    /// <summary>
    /// Serializable 2D vector for JSON x/y pairs.
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

    /// <summary>
    /// Optional projectile data embedded in a skill.
    /// </summary>
    [Serializable]
    public class ProjectileData
    {
        public float speed;
        public float lifetime;
        public Vector2Data size;
    }

    /// <summary>
    /// Visual/sprite metadata for a character.
    /// </summary>
    [Serializable]
    public class VisualData
    {
        public string spriteSheet;
        public string portraitSprite;
        public string primaryColor;
        public string secondaryColor;
    }
}
