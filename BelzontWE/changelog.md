# v0.5.0r1 (26-JUL-25)

- Fixed issue when the first child of a placeholder layout is a decal.
- Minor i18n fixes.

## FROM v0.5.0r0 (26-JUL-25)

- Added text decals support
- All decals support was revisited and now have fine controls for metal and normal effect strength.
- Fixed behavior that was impeding formulaes that requires Unity non-thread safe data to work properly
- Added missing i18n entries

## FROM v0.4.1r0 (20-JUL-25)

- Complete review of all functions related to road naming. Check the new WERoadFn class to see the new functions available. Old functions were **removed** and may break existing layouts/modules.
- Added alert when an image fails to be loaded into an atlas
- Fixed custom meshes from modules not being properly listed on UI

## FROM v0.4.0r1 (18-JUL-25)

- Added new bridge for custom mesh management
- Added a new tag `hideMesh` for prefab layouts: `<hideMesh>N</hideMesh>` - `N` shall be a number indicating a mesh index from the original prefab that will be hidden, it's the same number shown at editor.

## FROM v0.4.0 (13-JUL-25)

- Added support to custom meshes import:
  - Add meshes as `obj` files at objMeshes folder. They shall have **vertices, normals, uv and triangles**, and must contain just one mesh
  - The custom meshes are available **only for Image type nodes**. Select the atlas and image and then the mesh it will use
  - The exported xml with custom meshes will have a new attribute `mesh` on the `imageMesh` node. It will point the mesh name (and like atlases names, names containing `:` will point to mods meshes when it becomes supported)
  - Formulas for switching meshes are not available *at this moment*
  - Modules can't export nor register meshes into WE *at this moment*
  - You can't import a mesh to current city savegame *at this moment*
  - There are no limitations on sizes for meshes - **use it at your own risk**
  - The feature development shall continue in next versions
- Added time and date functions on the new class `WECalendarFn`
- Fixed behavior for when a mesh doesn't have emission texture. Instead of a white texture, the main texture will be sent instead as emissive.
- Fixed meshes leaks. You may notice a slight reduction on game RAM memory usage.
- Fixed error when reloading all sprites from Options menu
- Fixed fonts replacements not being saved when the replacement font name matches the original module font name