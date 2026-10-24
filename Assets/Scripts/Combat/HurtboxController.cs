using UnityEngine;
using MysticCombat.Fighter;

namespace MysticCombat.Combat
{
    /// <summary>
    /// Represents the hurtbox (vulnerable body area) of a fighter.
    /// Draws visual green Scene Gizmos so designers can align the body vulnerable zone.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class HurtboxController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Owner")]
        [Tooltip("The root transform of the fighter this hurtbox belongs to.")]
        [SerializeField] private Transform owner;

        [Header("Gizmo Visualization")]
        [Tooltip("Whether to draw the green hurtbox gizmo in the Scene view.")]
        [SerializeField] private bool showGizmo = true;

        #endregion

        #region Properties

        public Transform Owner => owner;

        #endregion

        #region Cached Components

        private BoxCollider2D hurtboxCollider;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            hurtboxCollider = GetComponent<BoxCollider2D>();
            hurtboxCollider.isTrigger = true;

            try
            {
                gameObject.tag = "Hurtbox";
            }
            catch (UnityException)
            {
                Debug.LogWarning("[HurtboxController] 'Hurtbox' tag not found in TagManager. Please ensure Tag 'Hurtbox' is registered.");
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;
            if (hurtboxCollider == null) hurtboxCollider = GetComponent<BoxCollider2D>();
            if (hurtboxCollider == null) return;

            Vector3 center = transform.position + (Vector3)hurtboxCollider.offset;
            Vector3 size = new Vector3(hurtboxCollider.size.x, hurtboxCollider.size.y, 0.1f);

            // Translucent green fill
            Gizmos.color = new Color(0f, 1f, 0.2f, 0.22f);
            Gizmos.DrawCube(center, size);

            // Bright green outline
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.85f);
            Gizmos.DrawWireCube(center, size);
        }

        #endregion

        #region Public Methods

        public void SetOwner(Transform ownerTransform)
        {
            owner = ownerTransform;
        }

        public FighterController GetFighter()
        {
            if (owner == null) return null;
            return owner.GetComponent<FighterController>();
        }

        #endregion
    }
}
