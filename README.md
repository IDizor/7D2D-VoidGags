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

  // Allows to pickup slightly damaged items. Percentage is the allowed remaining item health. Note: Items will be fully repaired when placed again.
  "PickupDamagedItems": true,
  "PickupDamagedItems_Percentage": 80,

  // Repairs current weapon/tool in hands by mouse wheel click.
  "MouseWheelClickFastRepair": true,

  // New items to repair are always placed to the top of the crafting queue.
  "RepairHasTopPriority": true,

  // Allows to lock backpack/vehicle slots to prevent sorting and moving them with special buttons.
  // Highlights locked slots. Remembers the locked slots count if the game is restarted.
  // Added button to auto-spread the loot to nearby containers.
  // (not applicable for Undead Legacy mod)
  "LockedSlotsSystem": true,

  // Makes the scrapping process in inventory faster, depending on the Salvage Operations perk level.
  "ScrapTimeAndSalvageOperations": true,

  // Auto-open console on first exception only. All further exceptions will not open the console.
  "PreventConsoleErrorSpam": true,

  // Arrows and bolts make noise and attract enemies. Can wake sleeping zombies.
  "ArrowsBoltsDistraction": true,

  // Thrown grenades rolling on the ground attract zombies in the same way like a thrown rock. Can wake sleeping zombies.
  "RocksGrenadesDistraction": true,

  // How zombies get know where the player is when a grenade or dynamite explodes?
  // Not damaged zombies will check the explosion location, and not the player location.
  "ExplosionAttractionFix": true,

  // Dig with a shovel or pickaxe through the grass/plants.
  "DigThroughTheGrass": true,

  // Less fog when flying at high altitude for better vision.
  "LessFogWhenFlying": true,

  // Zombies can wake each other and share their intention to attack or to check some location.
  "SocialZombies": false,

  // Bullets, iron arrows and bolts can break weak blocks and hit objects behind.
  "PiercingShots": true
}
```
