using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Represents the hurtbox (vulnerable area) of a fighter.
    /// Day 1 stub — provides owner reference so hitboxes can identify the target.
    /// The GameObject this is attached to should be tagged "Hurtbox".
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HurtboxController : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("The root transform of the fighter this hurtbox belongs to.")]
        [SerializeField] private Transform owner;

        #endregion

        #region Properties

        /// <summary>
        /// The root transform of the fighter that owns this hurtbox.
        /// </summary>
        public Transform Owner => owner;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Ensure the tag is set (will only work if "Hurtbox" tag exists in the project)
            try
            {
                gameObject.tag = "Hurtbox";
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[HurtboxController] 'Hurtbox' tag not found in project. " +
                                 $"Please add it via Edit > Project Settings > Tags and Layers.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the owner of this hurtbox.
        /// </summary>
        /// <param name="ownerTransform">The root transform of the fighter.</param>
        public void SetOwner(Transform ownerTransform)
        {
            owner = ownerTransform;
        }

        /// <summary>
        /// Returns the FighterController on the owner, if present.
        /// </summary>
        /// <returns>The FighterController component, or null.</returns>
        public FighterController GetFighter()
        {
            if (owner == null) return null;
            return owner.GetComponent<FighterController>();
        }

        #endregion
    }
}
