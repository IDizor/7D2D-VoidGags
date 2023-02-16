# 7D2D-VoidGags
7 Days to Die game modlet.

This mod is the set of the useful fixes and features.

All features are optional and can be disabled in the config file.

## Features

The features list / `VoidGags.config` file content:
```
{
  // Air drop supply crate should contain at least one item.
  "AirDropIsNeverEmpty": true,

  // Moves right-clicked item to the top of the crafting queue.
  "CraftingQueueRightClickToMove": true,

  // Experience reward for killing zombies depends on their max health and armor rate. Plus a little random.
  "ExperienceRewardByMaxHP": true,
  "ExperienceRewardByMaxHP_Multiplier": 1.0,

  // Use helmet light mod by default when pressing F. Instead of weapon light mod or laser pointer mod.
  "HelmetLightByDefault": true,

  // Allows to pickup slightly damaged items. Percentage is the alloved remainig item health. Note: Items will be fully repaired when placed again.
  "PickupDamagedItems": true,
  "PickupDamagedItems_Percentage": 80,

  // Repairs current weapon/tool in hands by mouse wheel click.
  "MouseWheelClickFastRepair": true,

  // New items to repair are always placed to the top of the crafting queue.
  "RepairHasTopPriority": true,

  // Saves the inventory locked slots count.
  // (in case you have any mod that displays inventory locked slots count on UI)
  // (not applicable for Undead Legacy mod)
  "SaveLockedSlotsCount": true,

  // Makes the scrapping process in inventory faster, depending on the Salvage Operations perk level.
  "ScrapTimeAndSalvageOperations": true
}
```
