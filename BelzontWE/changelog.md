# v1.0.0r6 (15-DEC-25)

- Fixed uploader for assets layouts. The folder K45_WE shall be present at same folder where the main prefab is located.  

## FROM v1.0.0r5 (14-DEC-25)

- Fixed loading K45_WE folder contents for assets
- Now prevents UI erasing font name data if it was not changed
- Fix for null reference exception due WEDisposalJob issue
- Fixed more locations when atlases were disposed incorrectly
- KNOWN ISSUE: The WE camera that goes close to edited node seems to be broken on some custom assets. It's under investigation.

## FROM v1.0.0r4 (08-DEC-25)

- Fix for atlas exporing erasing it from the game (causing rendering issues)

## FROM v1.0.0r3 (04-DEC-25)

- Fix for atlas visualization in UI erasing it from the game (causing NullPointerException on UI)

## FROM v1.0.0r2 (04-DEC-25)

- Possible fix for vanilla bug that prevented WE UI to be loaded
- Fixing issue for sometimes text get broken in some situations

## FROM v1.0.0r0 (04-DEC-25)

- Allowing non-assembly mods (i.e. Assets) to register WE stuff: (beta feature, may change in future)
 - Need to add files into a folder "K45_WE" at same level of the prefab root
 - Subfolder structure is the same from the WE Module template project ( https://github.com/klyte45/CS2-WEModuleTemplate/tree/master/_BaseModule/Resources ), but fonts are not supported
- Setting up new limits for WE stuff:
 - Maximum atlas size (with all images filling it) will be lowered from 16384x16384 to 4096x4096
 - Maximum image size for the atlas (each entry) will be lowered from 2048x2048 to 1000x1000 - except mod integrations that will allow 4094x4094 when using byte arrays signature
- Added support to local variables: variables starting with "!" will not be passed to children node trees
- Added support to multilevel variables: variables starting with "$" (like array index variable "$idx") will not erase older values if more than once at variable stack. It will generate numbered entries for deeper stack values (ex: "$idx_0", "$idx_1", ...). The latest value will still being presented on original variable name.
- Fixed minor issues related to cross-reference renderings