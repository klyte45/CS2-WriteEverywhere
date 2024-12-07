#  v0.1.1r2 (07-DEC-24)
- Added feature to setup fonts and atlases for layouts loaded from modules. You can export settings to use in other savegames.
  - It allows to replace any font or atlas used in a layout from prefab loaded from a WE module to an existing one in the game. 
  - As default, fonts from WE modules are all the default until get setup. Atlases from WE modules are the ones setted up in the module.
- Added feature to copy a atlas from a WE module to the local atlases folder - useful for editing them to replace the original
  - Export the atlas, edit them at `exportedAtlases` folder, then move it to `imageAtlases` folder, all relative to WE data folder at AppData
- Fixed: UI Crashing when trying to type the path address at file picker dialog