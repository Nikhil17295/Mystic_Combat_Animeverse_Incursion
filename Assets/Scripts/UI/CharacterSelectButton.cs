using UnityEngine;
using UnityEngine.EventSystems;

namespace MysticCombat.UI
{
    /// <summary>
    /// Forwards pointer clicks and hovers to CharacterSelectController for mouse and touch input.
    /// </summary>
    public class CharacterSelectButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public enum ButtonType
        {
            SelectCard,
            RandomCpu,
            LockIn,
            Back
        }

        [SerializeField] private ButtonType buttonType;
        [SerializeField] private int cardIndex;
        [SerializeField] private CharacterSelectController controller;

        public void Initialize(ButtonType type, int index, CharacterSelectController charController)
        {
            buttonType = type;
            cardIndex = index;
            controller = charController;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Hover logic if needed
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller == null) return;

            switch (buttonType)
            {
                case ButtonType.SelectCard:
                    controller.OnPointerClickCard(cardIndex);
                    break;
                case ButtonType.RandomCpu:
                    controller.OnClickRandomCpuButton();
                    break;
                case ButtonType.LockIn:
                    controller.OnClickLockInButton();
                    break;
                case ButtonType.Back:
                    controller.OnClickBackButton();
                    break;
            }
        }
    }
}
