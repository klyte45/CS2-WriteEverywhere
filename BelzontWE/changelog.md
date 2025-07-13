# v0.4.0 (13-JUL-25)

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