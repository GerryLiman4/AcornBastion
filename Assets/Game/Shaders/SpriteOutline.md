# Sprite outline

Add **SpriteOutline** to the GameObject containing the **SpriteRenderer** (usually `Model`). Its material reference is assigned automatically when adding it in the Editor. Set **Outline Color** and **Outline Width** on the component; width is measured in the source sprite's pixels. Disable the component to remove the outline.

`SpriteOutline.mat` and `FUR/Sprite Outline` draw the border on a temporary padded quad. Keep the original SpriteRenderer material assigned: the component draws the outline alongside it. This supports tight sprite meshes without changing sprites, textures, pivots, or import settings. No material copies are made per object.

The outline follows frame animation, sprite tint opacity, flip X/Y, object transforms, and the source sorting layer/order. For pivot sorting, its rendering bounds are centered at the sprite pivot. The component cleans up its temporary mesh and renderer when disabled.

The shared material provides **Comic Paper Style**, **Paper Tint**, **Printed Dot Color**, **Dot Spacing**, **Dot Radius**, **Printed Dot Density**, **Paper Grain**, and **Paper Edge Roughness**. The print is procedural and anchored to the sprite pivot, with antialiasing and no animated noise. Disable Comic Paper Style for the original solid-color border. A wider outline (4–5 sprite pixels) makes the small printed dots easier to see. Creep variants inherit this wider paper border from BaseEnemy.

Designed for the project's Built-in render pipeline and ordinary Simple SpriteRenderers, including sprite sheets and rectangular sprite atlases. SpriteMask interactions, sliced/tiled sprites, and SpriteSkin deformation are outside this component's scope. Each enabled outline adds one mesh draw and a nine-sample alpha edge pass. Existing game prefabs retain their current materials until the component is added.
