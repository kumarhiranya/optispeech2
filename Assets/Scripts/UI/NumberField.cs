using Optispeech.Documentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Optispeech.UI {

    // It'd probably be more efficient to just make an image and set the cursor on enter/exit
    // I chose to do it this way (similar to how Tooltip is implemented) so that the cursor
    // still looks native, just with an indicator above it
    // Note this should be added to the object you can drag to change the number field value,
    // which is generally the label, so dragging the input field's text can still be used
    // to select the text inside that field
    /// <summary>
    /// Makes a label for a <see cref="TMP_InputField"/> support dragging to change the input
    /// field's numeric value, simila to Unity's number fields
    /// </summary>
    public class NumberField : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler {

        /// <summary>
        /// How quickly the numeric value will change as the label is dragged
        /// </summary>
        [SerializeField]
        private float slideSpeed = 1;
        /// <summary>
        /// The input field this label will control
        /// </summary>
        [SerializeField]
        private TMP_InputField inputField = default;

        /// <summary>
        /// A canvas that should be this gameObject's first child and contains an indicator that the label can be dragged
        /// </summary>
        private Transform sliderCanvas;
        /// <summary>
        /// The first child of <see cref="sliderCanvas"/>, which will be moved to the mouse cursor each frame while <see cref="sliderCanvas"/> is active
        /// </summary>
        private RectTransform slider;
        /// <summary>
        /// A gameobject that prevents input events from being passed to things except this class, while the label is being dragged
        /// </summary>
        private GameObject blocker;

        /// <summary>
        /// Whether or not the user is currently hovering over the label
        /// </summary>
        private bool isHovering = false;
        /// <summary>
        /// Whether or not the user is currently dragging the label
        /// </summary>
        private bool isDragging = false;
        /// <summary>
        /// Used to track fractional value when sliding integers
        /// </summary>
        private float partial = 0;

        [HideInDocumentation]
        private void Start() {
            sliderCanvas = transform.GetChild(0);
            slider = sliderCanvas.GetChild(0).GetComponent<RectTransform>();

            transform.DetachChildren();

            // This idea for this technique was originally inspired by TextMeshPro's dropdown component,
            // which uses a canvas to close the dropdown whenever you click off of it
            // You can view the textmeshpro source code yourself: when the project downloads it
            // from the package manager its stored in the textmeshpro folder inside
            // C:\Users\{Username}\AppData\Local\Unity\cache\packages\packages.unity.com
            // We don't use it to close anything, but just to make it so that the accordion doesn't
            // fade out while we're dragging the number field. It may make sense to abstract this
            // out if we end up needing to use it in other components as well
            // (and maybe avoid having so many canvases all over the place)

            // We're going to add a full screen recttransform to the accordion we're in
            // so it considers our cursor over the accordion when the recttransform is active,
            // which we'll make it for the entire time we're dragging
            blocker = new GameObject("Blocker");
            RectTransform blockerRect = blocker.AddComponent<RectTransform>();
            // Set its size while its connected to a root-level canvas so it fills the whole screen
            blockerRect.SetParent(GetComponentInParent<Canvas>().rootCanvas.transform, false);
            blockerRect.anchorMin = Vector3.zero;
            blockerRect.anchorMax = Vector3.one;
            blockerRect.sizeDelta = Vector2.zero;

            // Add image with an invisible color, to block the cursor
            blocker.AddComponent<Image>().color = Color.clear;

            // Make it inactive because we only want it blocking while we're dragging
            blocker.SetActive(false);
        }

        [HideInDocumentation]
        private void Update() {
            if (isHovering || isDragging) {
                slider.anchoredPosition = Input.mousePosition;

                // Note: We poll every frame because IDragHandler wouldn't give you delta x values
                // when the cursor is on the left or right edge of the screen, but the mouse axis will
                float delta = Input.GetAxis("Mouse X");
                if (isDragging && delta != 0) {
                    // Convert the field into a float, add the mouse movement to it, and convert it back to a string
                    // to apply it back to the (technically text) input field
                    float currValue = inputField.text == "" ? 0 : float.Parse(inputField.text);
                    if (inputField.contentType == TMP_InputField.ContentType.IntegerNumber) {
                        partial += delta * slideSpeed;
                        while (partial > 1) {
                            currValue++;
                            partial--;
                        }
                        while (partial < -1) {
                            currValue--;
                            partial++;
                        }
                        int newValue = Mathf.RoundToInt(currValue);
                        inputField.text = newValue == 0 ? "" : newValue.ToString();
                    } else {
                        float newValue = currValue + delta * slideSpeed;
                        inputField.text = Mathf.Abs(newValue) < float.Epsilon ? "" : newValue.ToString();
                    }
                }
            }
        }

        [HideInDocumentation]
        public void OnPointerEnter(PointerEventData eventData) {
            if (inputField.interactable) {
                isHovering = true;
                sliderCanvas.gameObject.SetActive(true);
            }
        }

        [HideInDocumentation]
        public void OnPointerExit(PointerEventData eventData) {
            isHovering = false;
            if (!isDragging)
                sliderCanvas.gameObject.SetActive(false);
        }

        [HideInDocumentation]
        public void OnBeginDrag(PointerEventData eventData) {
            if (inputField.interactable) {
                isDragging = true;
                sliderCanvas.gameObject.SetActive(true);
                blocker.SetActive(true);
            }
        }

        [HideInDocumentation]
        public void OnDrag(PointerEventData eventData) {
            // We need this handler or else the begin/end drag handlers won't work
            // We don't actually do anything "on drag", we just need to know IF we're dragging
        }

        [HideInDocumentation]
        public void OnEndDrag(PointerEventData eventData) {
            isDragging = false;
            if (!isHovering)
                sliderCanvas.gameObject.SetActive(false);
            blocker.SetActive(false);
        }
    }
}
