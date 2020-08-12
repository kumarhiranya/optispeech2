using Optispeech.Documentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Optispeech.UI {

    // TODO do I want it following the mouse, or just animating into the left/right/top/bottom?
    /// <summary>
    /// Creates a tooltip that appears when hovering over whatever this behaviour is attached to
    /// </summary>
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

        /// <summary>
        /// The first child of this gameObject, which will only be visible on hover
        /// </summary>
        private Transform tooltipCanvas;
        /// <summary>
        /// The first child of <see cref="tooltipCanvas"/>, which will be placed at the mouse's position every frame
        /// </summary>
        private RectTransform tooltip;
        /// <summary>
        /// The label to display the tooltip's text
        /// </summary>
        private TextMeshProUGUI text;

        /// <summary>
        /// Whether or not the tooltip is currently visible
        /// </summary>
        private bool showTooltip = false;

        [HideInDocumentation]
        private void Awake() {
            tooltipCanvas = transform.GetChild(0);
            tooltip = tooltipCanvas.GetChild(0).GetComponent<RectTransform>();
            text = tooltip.GetComponentInChildren<TextMeshProUGUI>();

            transform.DetachChildren();
        }

        [HideInDocumentation]
        private void Update() {
            if (showTooltip) {
                tooltip.anchoredPosition = Input.mousePosition;
            }
        }

        [HideInDocumentation]
        public void OnPointerEnter(PointerEventData eventData) {
            showTooltip = true;
            tooltipCanvas.gameObject.SetActive(true);
        }

        [HideInDocumentation]
        public void OnPointerExit(PointerEventData eventData) {
            showTooltip = false;
            tooltipCanvas.gameObject.SetActive(false);
        }

        [HideInDocumentation]
        void OnMouseEnter() {
            showTooltip = true;
            tooltipCanvas.gameObject.SetActive(true);
        }

        [HideInDocumentation]
        void OnMouseExit() {
            showTooltip = false;
            tooltipCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Change the text that appears in this tooltip
        /// </summary>
        /// <param name="text">The new tooltip text</param>
        public void SetText(string text) {
            this.text.text = text;
        }

        [HideInDocumentation]
        public void OnDestroy() {
            if (tooltipCanvas != null)
                Destroy(tooltipCanvas.gameObject);
        }
    }
}
