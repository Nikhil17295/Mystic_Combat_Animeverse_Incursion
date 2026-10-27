using UnityEngine;
using UnityEngine.EventSystems;

namespace MysticCombat.UI
{
    /// <summary>
    /// Forwards pointer enter and click events to MainMenuController.
    /// </summary>
    public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private int itemIndex;
        [SerializeField] private MainMenuController controller;

        public void Initialize(int index, MainMenuController menuController)
        {
            itemIndex = index;
            controller = menuController;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (controller != null)
            {
                controller.OnPointerHoverItem(itemIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null)
            {
                controller.OnPointerClickItem(itemIndex);
            }
        }
    }
}
