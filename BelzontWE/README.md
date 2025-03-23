# IT'S BETA AND IT MAY BREAK YOUR GAME. USE AT YOUR OWN RISK!

This is the Cities Skylines 2 version of Write Everywhere ([CS1 mod version here](https://steamcommunity.com/sharedfiles/filedetails/?id=2887458944)). 
It keep some of the features from the CS1 version plus some new features that are only possible in CS2.

For detailed reference and samples, [look this repository](https://github.com/klyte45/CS2-WriteEverywhereFiles).

## Suggested usages

- Add texts in game from properties of any object of the game
- Add textured planes to simulate structures, like walls and signs
- Make color-animated planes with images (requires some coding)
- Add plates to the vehicles of the game with the style you wish.

This list isn't exhaustive and many more uses are already possible. Use your imagination!

## Requirements

This mod is simpler to understand when compared with its CS1 version, but it still being a mod with a lot of stuff that must be discovered by the users - specially at this moment.

More documentation will be added in the future.

## Write Everywhere modules

They are designed to add automatically new content into the Write Everywhere, like:

- Image atlases
- Default layouts to assets
- Importable layouts (into City Layouts tab)
- Fonts to be imported

Notice that fonts will always require manual install in the city (by adding them in the City Fonts tab). To make things easier, was added a button to list all mods that registered fonts/layouts in Write Everywhere 
at file picker dialog.

If you are interested to create your own modules, take a look at [this template repository](https://github.com/klyte45/CS2-WEModuleTemplate).

This feature was heavily inspired by the [Station Entrance Visuals](https://mods.paradoxplaza.com/mods/94028/Windows) by rodrigmatrix.

## Features detailing

### Image atlases

Image atlases are a group of images stored together in a folder:
- These images shall not be bigger than **2048x2048** pixels in size
- All images shall be in PNG format
- The images can have other special images like the asset textures of the game have.
  - The naming convention follow the same shown [at CS2 official Wiki page about asset creation](https://cs2.paradoxwikis.com/Asset_Creation_Guide) - except that the `_Base_Color` isn't used because it is the default image name.
  - EX: a normal image for the file `ABC.png` (a base color image) shall be named `ABC_Normal.png` to be loaded as normal map of the first image.
  - The same details of each image applies to WE planes.
- It's not required to all images to follow the texture good practices about sizing (always power of 2 size, not less than 512px each side, etc). All images in an Image Atlas are joined into an image that follow that rules.

### Fonts

- All fonts shall be in TTF format. Use converters online to use other format, converting that font as TTF.
- Some TTF files format are not supported due game limitations. When face this problem, try converting it to OTF then getting back to TTF and try again with this last one.

### Shaders

The shaders currently supported by WE are: **Default**, **Glass** and **Decal**
- The Default shader is the most used shader in the game, and shall be the right choice most of time
- The Glass shader is semitransparent, can be useful in many situations too
- The Decal shader only works for images currently. It projects into meshes that have the same decal flags in their material. The flags are editable at UI and they can be added also over WE surfaces using the shaders above.

### Variables

Variables are string key-value pairs setup at a WE component. Useful for recycling layouts that change very few stuff based on a parameter. They are inherited to all WE components at its descendant tree and can be overridden by their children at any time.
- To read variables in a custom C# method, add a `Dictionary<string,string>` as second parameter of the method, the first shall be from the type which the method will get used in the formulae pipeline.
- Both key and value are limited to 30 characters length.
- At moment, there's no use without custom functions (vanilla WE). The `WEParameterFn` class contains just debug methods for now.

## FAQ

First of all, read the [files repository](https://github.com/klyte45/CS2-WriteEverywhereFiles) to get the basic information.

- - **How can I find the properties of the objects to get the texts?**
  - If what you want is not in the builtin functions, so you will need to use modders tools like [Scene Explorer](https://mods.paradoxplaza.com/mods/74285/Windows) to inspect the objects you want to extract information from.
- **There's any Discord server I can ask for help with this mod?**
  - No, I have no specific Discord server for support. Leave your questions at Paradox forums and I will response as soon as I can.
  - You can ask for assistance in general Discord servers from CS2, may I be there but I can't promise I will be able to respond.
- **There are any videos showing how to use this mod?**
  - Not from me. Maybe fellow youtubers/streamers can learn stuff from this mod and make videos in the future (let me know if you do any to highlight here)

## Special thanks
I want to thank the people that helped me with CS2 modding since first days, specially mentioning **yenyang** and **krzychu**. Also thanks **Chameleon**, **Mimonsi** and **Nullpinter** for testing the mod during the development process.


