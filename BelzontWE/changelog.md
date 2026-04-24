# v2.0.0r0 (29-APR-26)
- **Virtual texture startup**: WE now waits for the game's Virtual Texturing system before loading layouts, preventing blank or missing image atlases after the 1.5.7 game update
- **Image atlas pipeline**: atlases now use BC7 cache data and VT streaming (optional but recommended), which reduces VRAM pressure, makes unchanged atlas reloads much faster, shrinks city save data, and raises supported image limits to **2046 per image and 8196 per atlas**
- **GameProp layouts**: a new GameProp content type lets WE nodes spawn real in-game props, including array-instanced props that follow the parent object's transforms
- **GameProp authoring tools**: the editor now offers a formula-enabled prefab field, a searchable localized prop picker, and automatic inheritance of parent WE variables for spawned prop layouts
- **Formula authoring**: module-check expressions now support logical operators, module fonts can be referenced directly, and train-like vehicles expose a raw car number formula
- **Safer prop editing**: spawned GameProp entities are excluded from WE selection so they cannot be picked or edited as regular layout targets
- **Fixes**: missing emissive, control, and normal maps now fall back safely, the default route destination name is corrected, formula evaluation blocks unsupported generic methods, and the camera no longer flies out of the game space on zeroed layouts

## FROM v1.2.0r0 (08-APR-26)
- XZ plane camera view corrected: the top of the screen now corresponds to positive Z direction
- Emissive light effects are now rectangle or cube-shaped based on the geometry of the layout item, instead of using point light emission
- Reduced overall slowdown when loading a city with many active layouts (1000+)
- **Array instance navigation**: when editing a layout with array instancing enabled, keyboard shortcuts (← → for X axis, ↑ ↓ for Y axis, Numpad +/- for Z axis) and ± buttons in the tool options panel let you cycle through instances; camera follows the selected instance; controls are hidden when all axes are set to 1
- **Keyboard tree navigation**: Page Up / Page Down moves between visible items; Space toggles fold; Home collapses all; End expands all; Delete key triggers the confirmation dialog before removing an item
- FIXED: Camera clamps to terrain when editing a layout
- FIXED: Pasting a layout as sibling always pasted in the second level instead of the same level as the target item
- FIXED: Font text generation stalling under heavy load (1000+ active layouts)
- FIXED: Template usage counter was always showing zero in the WE window
- FIXED: Changing font quality or initial texture size settings did not re-render existing text, leaving garbled glyphs
- FIXED: Pasting or cloning a layout item did not auto-select the newly created item
- FIXED: Text cache entries not cleared when the font atlas version changed
- FIXED: Crash when using WE alongside other mods that also patch the game's debug system

## FROM v1.1.0r1 (29-MAR-26)
- Added point lights support: set emissive on a WE text mesh and toggle the "Use Global Illumination" checkbox on to enable point light emission from that element
- New `WEAttachedFn` formula functions for sidewalk-relative offset calculations
- Added formulae for indirect value getter: reference another formula's result value indirectly within an expression
- Added waypoints indexing formulae: access waypoint indexes within formula expressions
- Added support to render WE layouts on unmeshed entities that belong to a meshed entity and have a transform component
- New mod logo - CS1 classic style
- Added support for Z-reversed white cube geometry
- Plane movement offset directions now match the current view orientation
- Camera position for XZ planes corrected
- FIXED: UI for WE tool options when using decal shader becomes messy
- FIXED: Layouts failed to load when more than 10,000 chunks were waiting to be processed
