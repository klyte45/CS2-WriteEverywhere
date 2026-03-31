# Formulae System: Built-in Functions Reference

> **Purpose**: Complete reference of all available built-in static functions that can be used in WE formulae, organized by category.

## Function Categories

All functions must be public static methods with one of two signatures:
1. `T Method(Entity reference)` — entity-only access
2. `T Method(Entity reference, Dictionary<string, string> variables)` — entity + variables access

---

## WEAttachedFn — Attachment Positioning

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetOffsetForNearestSidewalk` | `float3` | Entity | Offset vector to reach the nearest sidewalk edge from the given entity's position |

## WEBuildingFn — Building Data Queries

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetBuildingRoad` | `Entity` | Entity | The road entity adjacent to the building |
| `GetBuildingRoadNumber` | `string` | Entity | Address/road number as string |
| `GetBuildingMainRenter` | `Entity` | Entity | Primary renter entity of the building |

## WECalendarFn — Date & Time (Locale-Aware)

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetTimeStringWeLocale` | `string` | Entity, vars | Current in-game time, using `am`/`pm` variables for designators |
| `GetFormattedDateWeLocale` | `string` | Entity, vars | Current in-game date, using `dateFormat` variable for format string |

## WECityFn — City System Access

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetCityConfSystem` | `CityConfigurationSystem` | Entity | Access to city configuration (population, policies, etc.) |
| `GetCitySystem` | `CitySystem` | Entity | Access to the core city system |

## WEColorsFn — Color Utilities

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetContrastColor` | `Color` | Color input | Returns black or white depending on luminance of input |
| `CastColor` | `Color` | Color32 input | Convert Color32 to Color |
| `CastColor32` | `Color32` | Color input | Convert Color to Color32 |

## WEEffectsFn — Environmental Effects

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetNightLight01` | `float` | Entity | Returns 1.0 if night lighting is enabled, 0.0 otherwise |

## WEModuleFn — Module Integration

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `IsModuleEnabled` | `int` | Entity, vars | Returns 1 if the module named in `!module` variable is loaded, 0 otherwise |

## WENumberFormattingFn — Number Presentation

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `To4DigitsValue(float)` | `string` | float | Reduces number to 4 significant digits with suffix (k, M, G, etc.) |
| `To4DigitsValue(int)` | `string` | int | Same for int |
| `To4DigitsValue(long)` | `string` | long | Same for long |
| `To3DigitsValue(float)` | `string` | float | Reduces to 3 significant digits with suffix |
| `To3DigitsValue(int)` | `string` | int | Same for int |
| `To3DigitsValue(long)` | `string` | long | Same for long |

## WEParameterFn — Variable Access

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `PrintVariables` | `string` | Entity, vars | All variables as `key=value;key=value` |
| `RelVarStr1-8` | `string` | Entity, vars | Relative variable by index (reads `!!r1`...`!!r8` → key → value) |
| `RelVarInt1-8` | `int` | Entity, vars | Same as above, parsed to int (0 on failure) |

## WERenterFn — Trade & Commerce

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetTradeCost(Entity, vars)` | `TradeCost` | Entity, vars | Trade cost at index from `_tradeCost#` variable |
| `GetTradeCost0` | `TradeCost` | Entity | Trade cost at buffer index 0 |
| `GetTradeCost1` | `TradeCost` | Entity | Trade cost at buffer index 1 |
| `GetTradeCost2` | `TradeCost` | Entity | Trade cost at buffer index 2 |
| `GetTradeCost3` | `TradeCost` | Entity | Trade cost at buffer index 3 |
| `GetTradeCost4` | `TradeCost` | Entity | Trade cost at buffer index 4 |

## WERoadFn — Road Network Data

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetNodePropData` | `WENodeElementCache` | Entity | Cached node property data (position, adjacency) |
| `GetRoadSideSegmentForProp` | `WENetNodeInformation` | Entity | The road segment on the "side" of the prop |
| `GetRoadOwnSegmentForProp` | `WENetNodeInformation` | Entity | The road segment that "owns" the prop |
| `GetFromPropByTargetVar` | `WENetNodeInformation` | Entity, vars | Segment selected by `target` variable |
| `GetRoadAggregation` | `Entity` | Entity | The aggregated road entity |

## WERouteFn — Transport Routes

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetTransportLineNumber` | `string` | Entity | Line number string |
| `GetWaypointStaticDestinationEntity` | `Entity` | Entity | Destination entity for waypoint |
| `GetWaypointStaticDestinationName` | `string` | Entity | Name of destination |
| `GetNthWaypoint` | `Entity` | Entity, vars | Nth waypoint entity (index from `!wp#` variable) |

## WEUtitlitiesFn — General Utilities

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetEntityName` | `string` | Entity | Display name of entity via NameSystem |
| `GetMainMeshColor1` | `Color` | Entity | Primary mesh color from MeshColor buffer |
| `GetMainMeshColor2` | `Color` | Entity | Secondary mesh color |
| `GetMainMeshColor3` | `Color` | Entity | Tertiary mesh color |

## WEVehicleFn — Vehicle Data

| Method | Return | Params | Description |
|--------|--------|--------|-------------|
| `GetTargetDestinationStatic` | `string` | Entity | Static destination name |
| `GetTargetDestinationDynamic` | `string` | Entity | Dynamic/current destination name |
| `GetVehiclePlate` | `string` | Entity | Full 7-character license plate |
| `GetVehiclePlateLine1` | `string` | Entity | First line of plate (typically 3 chars) |
| `GetVehiclePlateLine2` | `string` | Entity | Second line of plate (typically 4 chars) |
| `GetTransportLineNumber` | `string` | Entity | Transport line number this vehicle serves |
| `GetSerialNumber` | `string` | Entity | Zero-padded serial number |
| `GetConvoyId` | `string` | Entity | Convoy identifier |

## Statistics

- **Total function classes**: 14
- **Total callable methods**: ~50+
- **Return types covered**: string, int, float, float3, Color, Entity, TradeCost, CitySystem, WENetNodeInformation, WENodeElementCache
- **Variable-dependent methods**: ~12 (those taking `Dictionary<string, string>`)
