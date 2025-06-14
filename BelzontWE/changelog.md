# v0.3.2r1 (14-JUN-25)

- Now delaying the prefab layouts loading to prevent unnecessary loading when WE modules weren't finished loading.
- Hiddening debug messages when debug mode is not active.

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
