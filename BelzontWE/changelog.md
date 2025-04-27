# v0.3.1r0 (27-APR-25)
- ADDED EDITOR SUPPORT! Now custom maps can pack custom assets, fonts and layouts to be distributed, it's also valid for WE Modules settings. 
  - Example of usage: Add the car plates custom design using the WE Module for vehicle plates positions, then the players can get the layout by default!
- ADDED SUPPORT FOR FALLBACK IMAGES ON ATLASES! Use the `_Fallback.png` inage file to show an image when an image with some given name isn't available at atlases - it could be a transparent image!
- Smarter default layout application: When setting up a layout to a spawner object prop (like the gas station prices pylons), any variation from that prefab will also inherit the layout setted up
- WE MODULES MAKERS: Reloading sprites now also reload images from all WE Modules currently active
- WE MODULES MAKERS: Added button to reload subtemplates from WE Modules currently loaded
- Minor UI fixes

## FROM v0.3.0r0 (13-APR-25)
- Added support to template placeholder arrays! It may be useful when making dynamic content (like reading lines passing at a station)
  - Limited to 256 instances, 100 items per axis row (only will render the first 256 of them)
  - Quantity of instances may be controlled manually or using formulae; values < 0 will fill the entire array size while any other number will be read literally (up to 256)
  - Can control format and axis order for growth
  - Can setup incomplete rows alignment (left, center, right or justified) based on full width of a completed row in any axis
  - Pivot alignment based on recent Pivot feature for axis X and Y, special axis Z pivot field on the new Instancing window
  - Use the variable `$idx` to retrieve the current index of the generated layout, at sublayouts formulaes
- Added support to conditional show/hide layout items via formulae
- Added basic math operations support: Add (+), Substract (-), Multiply (*) and Divide (÷)
- Added WEEffectFn class, with a method that returs 1 when night lights are active
- Now subtemplates can reference other subtemplates. AVOID CREATING CIRCULAR REFERENCES!
- Fixed subtemplates update process leaving garbage at entity ecosystem
- Optimizing formulaes and variables storage
- Variables strings now are cached to avoid parsing content every WE update cycle, speeding up performance
- Text generation process reviewed: now it may freeze a little longer but render all texts in a short time
- Adjusted text lods to consider the emission intensity, making bright items to be rendered from a greater distance