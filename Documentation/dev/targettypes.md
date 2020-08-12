# Target Types

Targets are a way to display a desired location in 3D space for the patient to try to place their tongue tip near. Different types of targets can handle determining where that 3D location is in different ways. This document will discuss how to add a new target type to OptiSpeech 2.

# Implement [TargetController](../api/Optispeech.Targets.TargetController.yml)

The main part of the target is the controller, this is the class that determines the position of the target and contains any information this type of target uses. It has the following methods that'll need to be implemented:

- [`Vector3 GetTargetPosition(long currTime)`](../api/Optispeech.Targets.TargetController.yml#Optispeech_Targets_TargetController_GetTargetPosition_System_Int64_) - Return where the target should be at the given point in time in milliseconds.
- [`long GetCycleDuration()`](../api/Optispeech.Targets.TargetController.yml#Optispeech_Targets_TargetController_GetCycleDuration) - Return how long it takes for the target to return to its initial position, in milliseconds. If the target doesn't move, this should just be 0.

If you have any settings (apart from target transparency and radius, which are in the base class) for your target, you'll also need to override these methods:

- [`void ApplyConfigFromString(string config)`](../api/Optispeech.Targets.TargetController.yml#Optispeech_Targets_TargetController_ApplyConfigFromString_System_String_) - Take a tab-separated values string and read this target's settings from it. The string will always be generated via this controller's `ToString()` method so you have complete control over how its formatted.
- [`string ToString()`](../api/Optispeech.Targets.TargetController.yml#Optispeech_Targets_TargetController_ToString) - Writes this target's settings to a tab-separated values string.

Click each link to see more specific information on implementing each function.

Additionally, you can override [`void UpdateTarget(Vector3 targetPosition, SensorData? tongueTipSensor)`](../api/Optispeech.Targets.TargetController.yml#Optispeech_Targets_TargetController_UpdateTarget_UnityEngine_Vector3_System_Nullable_Optispeech_Data_SensorData__) if you'd like to customize how the target gets updated every frame.

You can set default values for everything like you would with any other MonoBehaviour. Using the `ToString` and `ApplyConfigFromString` methods, values will automatically be loaded and saved as needed.

> [!TIP]
> The base class already has implementations for target transparency and radius, so you don't need to add those yourself! Just call `base.ApplyConfigFromString(config)` and `base.ToString()` as appropriate. You can use the `TargetController.NUM_BASE_CONFIG_VALUES` constant to look up how many tab separated values are written in `TargetController.ToString()`.
>
> If you choose *not* to use those functions, you can disregard the fact that the settings strings are supposed to be tab-separated values. That is, if you never call `base.ApplyConfigFromString(config)` or `base.ToString()` then you can safely format the strings however you please

If any part is confusing, it's recommended to read through any of the existing target controller implementations to use as a reference.

# Implement [TargetConfig](../api/Optispeech.Targets.TargetConfig.yml)

The config script handles all the UI elements for updating the targets' values, when that target is selected in the target's panel. Any additional information stored in the `TargetController` implementation should have a corresponding UI element to update it here. Hooking the UI elements up will require overriding the appropriate methods out of the following:

- [`void Init(TargetsPanel panel, TargetController controller)`](../api/Optispeech.Targets.TargetConfig.yml) - This function is called when the target is opened in the targets panel. The config object is instantiated and then this function is called. You should setup listeners on each UI element to update `controller`, and set the elements' current values to the ones present in `controller`. You'll probably want to cast `controller` to the implementation of `TargetController` you made in the previous step. Whenever changing a value on `controller`, make sure to call [`panel.SaveTargetsToPrefs();`](../api/Optispeech.Targets.TargetsPanel.yml#Optispeech_Targets_TargetsPanel_SaveTargetsToPrefs) so the new value is saved.
- [`void SetInteractable(bool interactable)`](../api/Optispeech.Targets.TargetConfig.yml#Optispeech_Targets_TargetConfig_SetInteractable_System_Boolean_) - The targets are only sometimes allowed to be configured by the user, so this function is called to update whether the UI elements should be interactable or not.
- [`void OnOpen()`](../api/Optispeech.Targets.TargetConfig.yml#Optispeech_Targets_TargetConfig_OnOpen) - This is called whenever the target config is opened/the panel is opened while this target is selected
- [`void OnClose()`](../api/Optispeech.Targets.TargetConfig.yml#Optispeech_Targets_TargetConfig_OnClose) - This is called whenever the target config is closed/the panel is closed while this target is selected

Make sure to call `base.Init(panel, controller)` and `base.SetInteractable(interactable)` in their respective overrides!

If any part is confusing, it's recommended to read through any of the existing target config implementations to use as a reference.

# Create a [TargetDescription](../api/Optispeech.Targets.TargetDescription.yml)

This is a scriptable object that will tell the program about this new target type. It must be inside a folder called `Target Type Descriptions` inside of any `Resources` folder in `Assets`. To create the [TargetDescription](../api/Optispeech.Targets.TargetDescription.yml), go to `Optispeech > Target Type` in the create menu. The create menu is accessed through the plus sign in the project panel or by right clicking in a folder and hovering over `Create`. You must then specify the name of the target type and give it two prefabs.

The first prefab is for the target controller. The only requirement for this prefab is that it include the implementation of [TargetController](../api/Optispeech.Targets.TargetController.yml) made in the first step, and have a mesh renderer somewhere in the prefab to represent the target. It can contain anything else you need for this target type, although that's uncommon. 

The second prefab is for the target config. The only requirement for this prefab is that it include the implementation of [TargetConfig](../api/Optispeech.Targets.TargetConfig.yml) made in the previous step. It should also include any UI elements the script needs, naturally. 

To make this easier, its recommended to make variants of the "base" target prefabs. Those are located in the `Prefabs > Targets` folder, and you can create the variants by right clicking them and selecting `Create > Prefab Variant`.

Once this is setup, the target type should be detected and appear in the add target dropdown in the targets panel.

# Add documentation for the new target type

Make sure to add a section to the [Targets Panel](../api/Optispeech.Targets.TargetsPanel.yml) documentation describing how this new target type works, and how to configure it!
