#  v0.2.0r1 (19-MAR-25)
- Added layout item postion pivot select support
- Fixing some missing icons on UI
- Fixing max width (cm) of the text can't be reset to 0 if already set once

## FROM v0.2.0r0
- ADDED DECAL IMAGES SUPPORT - texts not supported for now
- Support to default subtemplates on WE Modules - will load default layout files from them and replace if applicable at all layouts from that module
- Support to replace subtemplates with city savegame WE layouts
- Added material debug window - available when mod is at debug logging level or more verbose by clicking a button at the WE Tool toolbox
- Fixed z-gap of child templates from their parents
- Fixed issue with cut/copy and paste not targeting the right prefab when pasting in another template tree
- Fixed issue with copy and paste not cloning fully the new instance
- Fixed bug that prevented layouts loaded from XML or city layouts to be deleted
- Added error image when the text have none of its glyphs supported by the font
- Final solution against WHY NULL error exploding at i18n Everywhere
- Fixed issue related to using more than one source template for the same model preventing fonts and atlases replacement from modules
- Fixed edge case where an outdated prefab template gets the GUID from updated prefab template, preventing it to get the modifications
- Fixed atlases names being truncated to 32 bytes when referring modules atlases
- Code cleanup to speed up the rendering code