# v0.3.2r13 (09-JUL-25)
- Added support to Traditional Chinese language (Thanks to **Source Design**, from forums)
- Fixed an issue that prevented additional languages to work on WE options section at options menu

## FROM v0.3.2r12 (07-JUL-25)

- Fixed non-square image decals being wrongly sized
- Fixed instances count not being updated with formulas

## FROM v0.3.2r11 (06-JUL-25)

- New formulae calculation method, reducing game lagging while moving the camera (specially when rendering the layout for the first time)

## FROM v0.3.2r10 (26-JUN-25)

- Fixing issue related to serializing illegit components from WE each time the game get saved

## FROM v0.3.2r9 (21-JUN-25)

- Allowing matrix transform nodes to be used as children for prefab default templates.
- Added more integration functions

## FROM v0.3.2r8 (19-JUN-25)

- Fixing a major issue that was causing weird shadows from behind the image meshes (special thanks: Sully)
- Fixing unable to setup ZFlip if the z axis scale got to zero somehow
- Added function to get convoy id

## FROM v0.3.2r5 (18-JUN-25)

- Added option to flip Z axis orientation - useful to create backfaces for planes/images
- Adding more route-related functions

## FROM v0.3.2r4 (16-JUN-25)

- Fixed deserialization error when using glass shader on a layout node saved along city.
- Now properly saving decal flags for glass shaders saved along city.

## FROM v0.3.2r3 (15-JUN-25)

- Added a title line with the self name and the prefab name of current editing layout in the hierarchy view.
- Added new button on bottom bar of hierarchy view allowing to select between upgrades of current building.

## FROM v0.3.2r2 (14-JUN-25)

- Added a new layout node type: White Cube. It's same as White Texture, but 3D (with depth). It also have an option to move the reference of next node to the front face - visually equals to White Texture.

## FROM v0.3.2r1 (14-JUN-25)

- Now delaying the prefab layouts loading to prevent unnecessary loading when WE modules weren't finished loading.
- Hiding some debug messages when debug mode is not active.

## FROM v0.3.2r0 (11-JUN-25)

- Fixes 1.3 patch
- Added support to load image from modules passing byte arrays
- Added support to logical operators on formulaes
- Added new text node type: Matrix Transform. It allows to apply a matrix transformation to all children nodes. It accept formulaes for scale, rotation, and translation.
- Max width meters field now support formulaes 
- Added support to metadata entries on layout XMLs. Useful for custom modules to store data to be retrieved via bridge.
- Atlases optimizations:
  - Now font atlases block less frames when adding new mapped characters, reducing slowdown.
  - Now texture atlases will grow by 2 times firstly by width and then by height, instead of growing by 2 times both dimensions each iteration. This may reduce the final size of the atlas.
