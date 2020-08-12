using Optispeech.Documentation;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Optispeech.UI {

    /// <summary>
    /// This class goes on a Panel and allows us to toggle
    /// it open or closed by clicking on the titlebar.
    /// When closed only the titlebar will be visible
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(ContentSizeFitter))]
    public class TogglePanel : MonoBehaviour {

        /// <summary>
        /// Whether or not this panel should be open initially
        /// </summary>
        public bool startOpen = true;

        /// <summary>
        /// This is the titlebar that'll act as the toggle for the panel
        /// The panel will shrink to the titlebar's size
        /// </summary>
        public Button titleBar = default;

        /// <summary>
        /// How many canvas pixels to move every frame
        /// </summary>
        [SerializeField]
        private float transitionSpeed = 15;

        /// <summary>
        /// Event that fires whenever the height of the panel changes.
        /// Passes two floats: the current height and then the new height
        /// </summary>
        public UnityEvent<float, float> heightChangeEvent = new UnityEvent<float, float>();

        /// <summary>
        /// Event that fires whenever the panel is toggled open or closed.
        /// Passed whether or not the panel is now open, and the amount the height has changed
        /// </summary>
        public UnityEvent<bool, float> panelToggledEvent = new UnityEvent<bool, float>();

        /// <summary>
        /// Whether or not this panel is currently open
        /// </summary>
        [HideInInspector]
        public bool isOpen;

        /// <summary>
        /// This panel's rect transform, for setting its size
        /// </summary>
        private RectTransform rect;
        /// <summary>
        /// This panel's title bar, for getting its size
        /// </summary>
        private RectTransform titleRect;
        /// <summary>
        /// Used to find the preferred size of the panel when its open
        /// </summary>
        private ContentSizeFitter fitter;
        /// <summary>
        /// The maximum height this panel is allowed to be
        /// </summary>
        private float maxHeight;

        [HideInDocumentation]
        private void Awake() {
            rect = GetComponent<RectTransform>();
            titleRect = titleBar.GetComponent<RectTransform>();
            fitter = GetComponent<ContentSizeFitter>();
            maxHeight = float.MaxValue;
        }

        [HideInDocumentation]
        private void OnEnable() {
            heightChangeEvent = new UnityEvent<float, float>();
            titleBar.onClick.RemoveListener(Toggle);
            titleBar.onClick.AddListener(Toggle);

            isOpen = startOpen;

            // Set initial height
            if (startOpen) {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                fitter.enabled = false;
            } else {
                fitter.enabled = false;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, titleRect.sizeDelta.y);
            }
        }

        /// <summary>
        /// Changes the maximum height the panel can reach
        /// </summary>
        /// <param name="maxHeight">The new upper limit </param>
        public void SetMaxHeight(float maxHeight) {
            if (this.maxHeight <= maxHeight) return;

            this.maxHeight = maxHeight;

            if (rect.rect.height <= maxHeight) return;

            if (isOpen) {
                StopAllCoroutines();
                StartCoroutine(AnimateHeightTo(maxHeight));
            }
        }

        /// <summary>
        /// Recalculates preferred height and transitions to that height
        /// </summary>
        public void Refresh() {
            // get current height
            float currHeight = rect.rect.height;
            // Enable our content size fitter, force a re-layout, and get the target height
            fitter.enabled = true;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            float newHeight = rect.rect.height;
            fitter.enabled = false;
            if (!isOpen) {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, titleRect.sizeDelta.y);
            } else if (currHeight != newHeight) {
                // if the heights are different, go back to the old height and animate to the new one
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currHeight);
                StopAllCoroutines();
                StartCoroutine(AnimateHeightTo(newHeight));
            }
        }

        /// <summary>
        /// Toggles whether this panel is open or not and transitions to the new height
        /// </summary>
        public void Toggle() {
            StopAllCoroutines();
            isOpen = !isOpen;
            if (isOpen) {
                Refresh();
            } else {
                StartCoroutine(AnimateHeightTo(titleRect.sizeDelta.y));
            }
        }

        /// <summary>
        /// Creates a coroutine that animates the height of this panel to the target height
        /// </summary>
        /// <param name="height">The height to transition to</param>
        /// <returns>A coroutine</returns>
        private IEnumerator AnimateHeightTo(float height) {
            panelToggledEvent.Invoke(isOpen, height - rect.rect.height);
            while (true) {
                float currHeight = rect.rect.height;
                if (Mathf.Abs(currHeight - height) <= transitionSpeed) {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                    heightChangeEvent.Invoke(currHeight, height);
                    // Break out of our loop - we reached our destination!
                    break;
                }

                // Move closer to height
                float newHeight = currHeight;
                if (currHeight > height) {
                    newHeight -= transitionSpeed;
                } else {
                    newHeight += transitionSpeed;
                }
                // And apply it to our panel rect transform
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);

                // Call any height change callbacks we have
                heightChangeEvent.Invoke(currHeight, newHeight);

                // Wait until next frame
                yield return null;
            }
        }
    }
}
