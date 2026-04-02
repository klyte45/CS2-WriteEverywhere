# Placeholder — the Managed/ directory is normally the game's
# Cities2_Data/Managed folder containing Unity and CS2 DLLs.
#
# In CI this folder is intentionally empty: GAME_DLLS_AVAILABLE will NOT
# be defined, so game-engine-dependent code paths are excluded via
#   #if GAME_DLLS_AVAILABLE … #endif
# and any test decorated with [Category("GameDlls")] is skipped.
#
# To run game-DLL-dependent tests locally, point CSII_MANAGEDPATH at
# your real game installation's Managed/ folder.
