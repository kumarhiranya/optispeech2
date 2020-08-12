# Accuracy Panel

When a data source is active with at least one target, an accuracy panel will appear at the bottom center of the screen and show how accurately the tongue tip's sensor matches the targets.

Accuracy is calculated based on the radius of the targets. If the sensor is completely within the target's radius, it is 100% accurate. 0% accuracy is when the sensor is a second radius away from the target, and anywhere in between has a percent accuracy between 0 and 100. The target marker will change color to represent its individual accuracy at any given moment.

The accuracy percentages are shown over two different type of durations:

- One "cycle", which is the lowest common multiple of each target's duration. That is, if one takes 2 seconds and another takes 5, then one cycle would be 10 seconds. The duration of a target is how long it takes to get back to its initial position
- One sweep. During a sweep this will be the accuracy since the beginning of the sweep. Once the sweep ends it'll continue displaying the average accuracy of the entire sweep until another is started
