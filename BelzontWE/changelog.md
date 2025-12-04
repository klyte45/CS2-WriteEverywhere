# v1.0.0r2 (04-DEC-25)

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