# Weather preview

Open `Assets/Scenes/WeatherPreview.unity` and enter Play Mode. Use the five buttons at the top to compare Morning, Afternoon, Sunset, Night, and Rain.

Select Main Camera and change **Preset** on Weather Post Process to preview a look in Edit Mode. The five WeatherProfile assets in this folder hold the exposure, contrast, saturation, color filters, split toning, haze, vignette, and rain settings. Rain adds falling streaks; the other profiles clear them.

The camera uses one Built-in image-effect pass. Screen Space Overlay canvases remain outside that pass. Rain is capped at 600 particles and stops when another look is selected. No render-pipeline or package changes are required.

The original `Assets/Scenes/Level.unity` is separate and does not contain this weather setup.
