using Optispeech.Documentation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Optispeech.UI {

    /// <summary>
    /// This class looks for TogglePanels inside it and handles keeping the panels appear stacked and automatically
    /// closes panels to fit within the height of the program
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class Accordion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

        /// <summary>
        /// How quickly the transparency changes when changing between states,
        /// where 1 means moving from completely opague to completely transparent in one frame
        /// </summary>
        [SerializeField]
        private float transitionSpeed = .05f;

        /// <summary>
        /// Affects how transparent the panels appear when the camera is still,
        /// where 1 is completely opague and 0 is completely transparent
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        private float cameraStillTransparency = .6f;
        /// <summary>
        /// Affects how transparent the panels appear when the camera is being moved,
        /// where 1 is completely opague and 0 is completely transparent
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        private float cameraMoveTransparency = .2f;
        /// <summary>
        /// Affects how transparent the panels appear when the mouse is hovering over the accordion,
        /// where 1 is completely opague and 0 is completely transparent
        /// </summary>
        [SerializeField]
        [Range(0, 1)]
        private float mouseOverTransparency = 1f;

        /// <summary>
        /// List of panels in this accordion
        /// </summary>
        [SerializeField]
        private TogglePanel[] panels;
        /// <summary>
        /// List of each panels' transform, used for positioning
        /// </summary>
        [SerializeField]
        private RectTransform[] panelTransforms;
        /// <summary>
        /// How tall the title of each panel is, used for positioning
        /// </summary>
        private float titleHeight;

        /// <summary>
        /// A canvas group attached to this game object to set the transparency
        /// of the whole accordion
        /// </summary>
        private CanvasGroup canvasGroup;

        /// <summary>
        /// Flag that represents whether the mouse is currently over this accordion
        /// </summary>
        private bool isMouseOver = false;

        /// <summary>
        /// Used for ensuring the accordion stays within the height of the screen
        /// </summary>
        Canvas canvas;
        /// <summary>
        /// The height of the screen
        /// </summary>
        private float screenHeight;
        /// <summary>
        /// The height between the top of the accordion and the top of the screen
        /// </summary>
        private float remainingHeight;
        /// <summary>
        /// List of the indices of the currently open panels
        /// </summary>
        private List<int> openPanelIndices;

        [HideInDocumentation]
        private void OnEnable() {
            panels = GetComponentsInChildren<TogglePanel>();
            if (panels.Length == 0) return;

            canvas = GetComponentInParent<Canvas>();
            screenHeight = Screen.height;
            remainingHeight = screenHeight / canvas.scaleFactor;
            openPanelIndices = new List<int>();

            panelTransforms = panels.Select(panel => panel.GetComponent<RectTransform>()).ToArray();
            // Find the first button, and assume its a panel titlebar
            titleHeight = panels.First().titleBar.GetComponent<RectTransform>().rect.height;

            canvasGroup = GetComponent<CanvasGroup>();

            float maxHeight = GetComponent<RectTransform>().rect.height - panels.Length * titleHeight;

            float y = 0;
            // Iterate through panels in reverse
            for (int i = panels.Length - 1; i >= 0; i--) {
                panelTransforms[i].anchoredPosition = new Vector2(0, y);
                // Create a temp variable because i will keep changing, but now temp will allow
                // our event handler to get the value of i associated with this panel
                int temp = i;
                panels[i].heightChangeEvent.AddListener((currHeight, newHeight) => {
                    float diff = newHeight - currHeight;
                    for (int j = temp - 1; j >= 0; j--)
                        panelTransforms[j].anchoredPosition = new Vector2(0, panelTransforms[j].anchoredPosition.y + diff);
                });
                panels[i].panelToggledEvent.AddListener((isOpen, heightDifference) => {
                    // Update open panels list
                    openPanelIndices.Remove(temp);
                    if (isOpen)
                        openPanelIndices.Add(temp);
                    // Update reminaing height
                    remainingHeight -= heightDifference;
                    // Ensure the accordion fits on screen
                    EnsureAccordionHeight();
                });

                if (panels[i].startOpen) {
                    y += panels[i].GetComponent<RectTransform>().rect.height;
                    openPanelIndices.Add(i);
                } else {
                    y += titleHeight;
                }
            }

            remainingHeight -= y;
            EnsureAccordionHeight();

            // Set initial accordion transparency
            canvasGroup.alpha = cameraStillTransparency;
        }

        [HideInDocumentation]
        private void OnDisable() {
            foreach (TogglePanel panel in panels)
                panel.heightChangeEvent = null;
        }

        [HideInDocumentation]
        private void Update() {
            // Check for screen height changes
            if (Screen.height != screenHeight) {
                remainingHeight += (Screen.height - screenHeight) / canvas.scaleFactor;
                EnsureAccordionHeight();
                screenHeight = Screen.height;
            }

            if (isMouseOver) return;
            if (Input.GetMouseButtonDown(1)) {
                StopAllCoroutines();
                StartCoroutine(ChangeAlpha(cameraMoveTransparency));
            }
            if (Input.GetMouseButtonUp(1)) {
                StopAllCoroutines();
                StartCoroutine(ChangeAlpha(cameraStillTransparency));
            }
        }

        [HideInDocumentation]
        public void OnPointerEnter(PointerEventData eventData) {
            isMouseOver = true;
            StopAllCoroutines();
            StartCoroutine(ChangeAlpha(mouseOverTransparency));
        }

        [HideInDocumentation]
        public void OnPointerExit(PointerEventData eventData) {
            // TMPro dropdowns create a top-level gameobject called Blocker to close the dropdown, which makes it
            // so that the accordion would fade out if the mouse isn't over the dropdown (including where the
            // user clicked to open the dropdown), so this makes it so that the blocker is considered part of this accordion.
            // That way when the dropdown is open the accordion will be fully visible the whole time. 
            // TMPro will still delete the blocker like normal when the dropdown is closed
            if (eventData.pointerCurrentRaycast.gameObject &&
                eventData.pointerCurrentRaycast.gameObject.name == "Blocker") {
                eventData.pointerCurrentRaycast.gameObject.transform.SetParent(transform);
                return;
            }

            isMouseOver = false;
            StopAllCoroutines();
            StartCoroutine(ChangeAlpha(Input.GetMouseButton(1) ? cameraMoveTransparency : cameraStillTransparency));
        }

        /// <summary>
        /// Ensures the height of the accordion is under the height of the screen by closing panels
        /// </summary>
        private void EnsureAccordionHeight() {
            if (remainingHeight < 0) {
                // Handle there only being one open panel
                if (openPanelIndices.Count == 1) {
                    panels[openPanelIndices.First()].SetMaxHeight(screenHeight / canvas.scaleFactor - (panels.Length - 1) * titleHeight);
                } else if (openPanelIndices.Count > 1) {
                    // The least recently opened panel is the first in the list,
                    // since we add them to the end when they're opened
                    // We'll close it, which will call this function again
                    // and it'll keep going until the accordion fits
                    panels[openPanelIndices.First()].Toggle();
                }
            }
        }

        /// <summary>
        /// Creates a coroutine to transition the accordion from the current transparency to the target
        /// </summary>
        /// <param name="target">The target transparency to transition to</param>
        /// <returns>A coroutine</returns>
        private IEnumerator ChangeAlpha(float target) {
            while (true) {
                float currAlpha = canvasGroup.alpha;
                if (Mathf.Abs(currAlpha - target) <= transitionSpeed) {
                    // We made it to our target alpha
                    canvasGroup.alpha = target;
                    break;
                }

                // Move closer to our target alpha
                float newAlpha = currAlpha;
                if (currAlpha > target) {
                    newAlpha -= transitionSpeed;
                } else {
                    newAlpha += transitionSpeed;
                }
                // Apply it to our canvas group
                canvasGroup.alpha = newAlpha;

                // Wait until next frame
                yield return null;
            }
        }
    }
}
