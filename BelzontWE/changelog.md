# Alpha 3 (0.0.0r3)
- Added support to formulae in any Appearance window field (float and color fields)
- Added new functions and reorganized existing from WEBuiltinFn class. Some examples are related to get the current renter of a building or what they are selling.
- Fixed lod from placeholders - now it will only be computed if the size set for them has the lod currently active
- Fixed issues related to atlases stored in savegame
- Fixed static layouts attached to objects not being serialized along savegame - but due an issue when loading WE layouts after loading a game, it was deactivated in this build. 
- Fixed issue with fonts after loading a savegame from another game session