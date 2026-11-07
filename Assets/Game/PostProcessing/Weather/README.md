# Weather preview

Open `Assets/Scenes/WeatherPreview.unity` and enter Play Mode. Use the five buttons at the top to compare Morning, Afternoon, Sunset, Night, and Rain.

Select Main Camera and change **Preset** on Weather Post Process to preview a look in Edit Mode. Each look is a URP Volume Profile in the `Volumes` folder, using Color Adjustments, Lift Gamma Gain, Split Toning, and Vignette overrides. Edit those assets to change a look. The five WeatherProfile assets in this folder link each look to its Volume Profile and hold the rain settings. Rain adds falling streaks; the other profiles clear them.

Weather Post Process swaps the profile on the scene's `Weather Volume`. Screen Space Overlay canvases are not affected by post processing. Rain is capped at 600 particles and stops when another look is selected.

The original `Assets/Scenes/Level.unity` is separate and does not contain this weather setup.
