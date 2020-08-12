using Optispeech.Documentation;
using Optispeech.Profiles;
using UnityEngine;

namespace Optispeech.Display {

	/// <summary>
	/// Sets up the camera to be controlled by the user when holding down right click, noclip-style.
	/// Modified from <see href="https://wiki.unity3d.com/index.php/FlyCam_Extended">FlyCam Extended</see>
	/// </summary>
	public class CameraController : MonoBehaviour {

		/// <summary>
		/// The amount the camera will rotate as the mouse moves
		/// </summary>
		[SerializeField]
		private float cameraSensitivity = 90;
		/// <summary>
		/// How quickly the camera moves vertically
		/// </summary>
		[SerializeField]
		private float climbSpeed = 4;
		/// <summary>
		/// How quickly the camera moves when using WASD while not holding shift
		/// </summary>
		[SerializeField]
		private float normalMoveSpeed = 10;
		/// <summary>
		/// How quickly the camera moves when using WASD while holding ctrl
		/// </summary>
		[SerializeField]
		private float slowMoveFactor = 0.25f;
		/// <summary>
		/// How quickly the camera moves when using WASD while holding shift
		/// </summary>
		[SerializeField]
		private float fastMoveFactor = 3;

		/// <summary>
		/// Whether or not we're currently moving, used to save the camera position
		/// and rotation to the active profile once the user stops moving
		/// </summary>
		private bool isMoving = false;

		[HideInDocumentation]
		private void Start() {
			LoadCameraSettings(ProfileManager.Instance.ActiveProfile);
			ProfileManager.Instance.onProfileChange.AddListener(LoadCameraSettings);
		}

		[HideInDocumentation]
		private void Update() {
			if (Input.GetMouseButtonDown(1))
				Cursor.lockState = CursorLockMode.Locked;
			if (Input.GetMouseButtonUp(1))
				Cursor.lockState = CursorLockMode.None;

			if (!Input.GetMouseButton(1)) {
				if (isMoving) {
					// Save camera settings to profile after moving
					ProfileManager.Profile profile = ProfileManager.Instance.ActiveProfile;
					profile.cameraPos = transform.position;
					profile.cameraRot = transform.localEulerAngles;
					ProfileManager.Instance.UpdateProfile(profile);
					isMoving = false;
				}
				return;
			}

			isMoving = true;

			Vector3 viewDir = transform.localEulerAngles;
			// Moving the mouse horizontally affects our angle around the y axis
			float rotationX = viewDir.y + Input.GetAxisRaw("Mouse X") * cameraSensitivity * Time.deltaTime;
			// Moving the mouse vertically affects our angle around the x axis
			float rotationY = viewDir.x - Input.GetAxisRaw("Mouse Y") * cameraSensitivity * Time.deltaTime;
			// Keep it between 0-90 or 270-360 (to avoid flipping the screen)
			if (rotationY > 180 && rotationY < 270) rotationY = 270;
			else if (rotationY < 180 && rotationY > 90) rotationY = 90;
			transform.localEulerAngles = new Vector3(rotationY, rotationX, 0);

			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
				transform.position += transform.forward * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
				transform.position += transform.right * (normalMoveSpeed * fastMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
			} else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
				transform.position += transform.forward * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Vertical") * Time.deltaTime;
				transform.position += transform.right * (normalMoveSpeed * slowMoveFactor) * Input.GetAxis("Horizontal") * Time.deltaTime;
			} else {
				transform.position += transform.forward * normalMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
				transform.position += transform.right * normalMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
			}

			if (Input.GetKey(KeyCode.Q)) { transform.position -= transform.up * climbSpeed * Time.deltaTime; }
			if (Input.GetKey(KeyCode.E)) { transform.position += transform.up * climbSpeed * Time.deltaTime; }
		}

		/// <summary>
		/// Loads the camera position and rotation from the given profile
		/// </summary>
		/// <param name="profile">The profile to apply the camera position and rotation from</param>
		private void LoadCameraSettings(ProfileManager.Profile profile) {
			transform.position = profile.cameraPos;
			transform.localEulerAngles = profile.cameraRot;
		}
	}
}
