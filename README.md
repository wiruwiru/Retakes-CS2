[![GitHub Downloads](https://img.shields.io/github/downloads/b3none/cs2-retakes/total.svg?style=flat-square&label=Downloads)](https://github.com/b3none/cs2-retakes/releases/latest)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/b3none/cs2-retakes/plugin-build.yml?branch=master&style=flat-square&label=Latest%20Build)

# CS2 Retakes
CS2 implementation of retakes written in C# for CounterStrikeSharp. Based on the version for CS:GO by Splewis.

## Share the love
If you appreciate the project then please take the time to star the repository 🙏

![Star us](https://github.com/b3none/gdprconsent/raw/development/.github/README_ASSETS/star_us.png)

## Features / Roadmap
- [x] Bombsite selection
- [x] Per map configurations
- [x] Ability to add spawns
- [x] Spawn system
- [x] Temporary weapon allocation (hard coded)
- [x] Temporary grenade allocation (hard coded)
- [x] Equipment allocation
- [x] Queue manager (Queue system)
- [x] Team manager (with team switch calculations)
- [x] Retakes config file
- [x] Add translations
- [x] Improve bombsite announcement
- [x] Queue priority for VIPs
- [x] Add autoplant
- [x] Add a command to view the spawns for the current bombsite
- [x] Add a command to delete the nearest spawn
- [x] Implement better spawn management system
- [x] Add a release zip file without spawns too
- [x] Barrier system (temporary walls that block paths at round start)

## Installation
- Download the zip file from the [latest release](https://github.com/B3none/cs2-retakes/releases/latest), and extract the contents into your `addons/counterstrikesharp/plugins` directory.
- Download the latest shared plugin and put it into your `addons/counterstrikesharp/shared` directory.

## Recommendations
I also recommend installing these plugins for an improved player experience
- Instadefuse: https://github.com/B3none/cs2-instadefuse
- Retakes Zones (prevent silly flanks / rotations): https://github.com/oscar-wos/Retakes-Zones
- Clutch Announce: https://github.com/B3none/cs2-clutch-announce
- Instaplant (if not using autoplant): https://github.com/B3none/cs2-instaplant

## Allocators
Although this plugin comes with it's own weapon allocation system, I would recommend using **one** of the following plugins for a better experience:
- Yoni's Allocator: https://github.com/yonilerner/cs2-retakes-allocator
- NokkviReyr's Allocator: https://github.com/nokkvireyr/kps-allocator
- Ravid's Allocator: https://github.com/Ravid-A/cs2-retakes-weapon-allocator

## Configuration
When the plugin is first loaded it will create a `retakes_config.json` file in the plugin directory. This file contains all of the configuration options for the plugin:

### GameSettings
| Config                    | Description                                                                                                                             | Default | Min   | Max   |
|---------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|---------|-------|-------|
| MaxPlayers                | The maximum number of players allowed in the game at any time. (If you want to increase the max capability you need to add more spawns) | 9       | 2     | 10    |
| ShouldBreakBreakables     | Whether to break all breakable props on round start (People are noticing rare crashes when this is enabled).                            | false   | false | true  |
| ShouldOpenDoors           | Whether to open doors on round start (People are noticing rare crashes when this is enabled).                                           | false   | false | true  |
| EnableFallbackAllocation  | Whether to enable the fallback weapon allocation. You should set this value to false if you're using a standalone weapon allocator.     | true    | false | true  |

### QueueSettings
| Config                 | Description                                                                                                   | Default  | Min | Max |
|------------------------|---------------------------------------------------------------------------------------------------------------|----------|-----|-----|
| QueuePriorityFlag      | A comma separated list of CSS flags for queue priority.                                                       | @css/vip | n/a | n/a |
| QueueImmunityFlag      | A comma separated list of CSS flags for queue immunity (prevents being moved to spectator).                   | @css/vip | n/a | n/a |
| ShouldRemoveSpectators | When a player is moved to spectators, remove them from all retake queues. Ensures that AFK plugins work as expected. | true     | false | true |

### TeamSettings
| Config                                            | Description                                                                                                                                     | Default | Min   | Max   |
|---------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|---------|-------|-------|
| TerroristRatio                                    | The percentage of the total players that should be Terrorists.                                                                                  | 0.45    | 0     | 1     |
| RoundsToScramble                                  | The number of rounds won in a row before the teams are scrambled.                                                                               | 5       | -1    | 99999 |
| IsScrambleEnabled                                 | Whether to scramble the teams once the RoundsToScramble value is met.                                                                           | true    | false | true  |
| IsBalanceEnabled                                  | Whether to enable the default team balancing mechanic.                                                                                          | true    | false | true  |
| ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 | Whether to force even teams when the active players is a multiple of 10 or not. (this means you will get 5v5 @ 10 players / 10v10 @ 20 players) | true    | false | true  |
| ShouldPreventTeamChangesMidRound                  | Whether or not to prevent players from switching teams at any point during the round.                                                           | true    | false | true  |

### MapConfigSettings
| Config                             | Description                                                                                     | Default | Min   | Max  |
|------------------------------------|-------------------------------------------------------------------------------------------------|---------|-------|------|
| EnableBombsiteAnnouncementVoices   | Whether to play the bombsite announcement voices.                                               | false   | false | true |
| EnableBombsiteAnnouncementCenter   | Whether to display the bombsite in the center announcement box.                                 | true    | false | true |
| EnableFallbackBombsiteAnnouncement | Whether to enable the fallback bombsite announcement.                                           | true    | false | true |

### BarrierSettings
| Config             | Description                                                                                                          | Default | Min   | Max   |
|--------------------|----------------------------------------------------------------------------------------------------------------------|---------|-------|-------|
| IsBarrierEnabled   | Whether to enable the barrier system. Barriers are temporary walls that spawn at round start and disappear after a delay. | false   | false | true  |
| BarrierRemoveDelay | Time in seconds after freeze time ends before barriers are removed. Barriers spawn at round start on the active bombsite. | 3.0     | 0     | 99999 |

### BombSettings
| Config             | Description                                                         | Default | Min   | Max  |
|--------------------|---------------------------------------------------------------------|---------|-------|------|
| IsAutoPlantEnabled | Whether to enable auto bomb planting at the start of the round or not. | true    | false | true |

### DebugSettings
| Config      | Description                                                 | Default | Min   | Max  |
|-------------|-------------------------------------------------------------|---------|-------|------|
| IsDebugMode | Whether to enable debug output to the server console or not. | false   | false | true |

## Commands

### General Commands
| Command            | Arguments                         | Description                                                          | Permissions |
|--------------------|-----------------------------------|----------------------------------------------------------------------|-------------|
| !forcebombsite     | <A / B>                           | Force the retakes to occur from a single bombsite.                   | @css/root   |
| !forcebombsitestop |                                   | Clear the forced bombsite and return back to normal.                 | @css/root   |
| !scramble          |                                   | Scrambles the teams next round.                                      | @css/admin  |
| !scrambleteams     |                                   | Scrambles the teams next round (alias).                              | @css/admin  |
| !voices            |                                   | Toggles whether or not to hear the bombsite voice announcements.     |             |
| css_debugqueues    |                                   | **SERVER ONLY** Shows the current queue state in the server console. |             |

### Spawn Editor Commands
| Command            | Arguments                         | Description                                                          | Permissions |
|--------------------|-----------------------------------|----------------------------------------------------------------------|-------------|
| !showspawns        | <A / B>                           | Show the spawns for the specified bombsite.                          | @css/root   |
| !spawns            | <A / B>                           | Show the spawns for the specified bombsite (alias).                  | @css/root   |
| !edit              | <A / B>                           | Show the spawns for the specified bombsite (alias).                  | @css/root   |
| !addspawn          | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point for the bombsite spawns currently shown.  | @css/root   |
| !add               | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point (alias).                                  | @css/root   |
| !newspawn          | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point (alias).                                  | @css/root   |
| !new               | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point (alias).                                  | @css/root   |
| !removespawn       |                                   | Removes the nearest spawn point for the bombsite currently shown.    | @css/root   |
| !remove            |                                   | Removes the nearest spawn point (alias).                             | @css/root   |
| !deletespawn       |                                   | Removes the nearest spawn point (alias).                             | @css/root   |
| !delete            |                                   | Removes the nearest spawn point (alias).                             | @css/root   |
| !nearestspawn      |                                   | Teleports the player to the nearest spawn.                           | @css/root   |
| !nearest           |                                   | Teleports the player to the nearest spawn (alias).                   | @css/root   |
| !hidespawns        |                                   | Exits the spawn editing mode.                                        | @css/root   |
| !done              |                                   | Exits the spawn editing mode (alias).                                | @css/root   |
| !exitedit          |                                   | Exits the spawn editing mode (alias).                                | @css/root   |

### Barrier Editor Commands
| Command            | Arguments    | Description                                                                    | Permissions |
|--------------------|--------------|--------------------------------------------------------------------------------|-------------|
| !editbarriers      | <A / B>      | Enter barrier editing mode for a bombsite. Game pauses until editing is done. | @css/root   |
| !barriers          | <A / B>      | Enter barrier editing mode for a bombsite (alias).                            | @css/root   |
| !showbarriers      | <A / B>      | Show barriers for a bombsite without editing mode.                            | @css/root   |
| !viewbarriers      | <A / B>      | Show barriers for a bombsite without editing mode (alias).                    | @css/root   |
| !hidebarriers      |              | Exit barrier viewing mode.                                                     | @css/root   |
| !removebarrier     |              | Remove the nearest barrier to your position.                                   | @css/root   |
| !deletebarrier     |              | Remove the nearest barrier to your position (alias).                           | @css/root   |
| !testbarrier       |              | Spawn the barriers for the current bombsite to test their placement.          | @css/root   |
| !donebarriers      |              | Exit barrier editing mode and restart the map.                                 | @css/root   |
| !exitbarriers      |              | Exit barrier editing mode and restart the map (alias).                         | @css/root   |

**Note:** When in barrier editing mode, use the ping key (default: X) twice to mark two corners of a barrier.

### Map Config Commands
| Command            | Arguments          | Description                                    | Permissions |
|--------------------|--------------------|------------------------------------------------|-------------|
| !mapconfig         | <Config file name> | Forces a specific map config file to load.     | @css/root   |
| !setmapconfig      | <Config file name> | Forces a specific map config file to load (alias). | @css/root   |
| !loadmapconfig     | <Config file name> | Forces a specific map config file to load (alias). | @css/root   |
| !mapconfigs        |                    | Displays a list of available map configs.     | @css/root   |
| !viewmapconfigs    |                    | Displays a list of available map configs (alias). | @css/root   |
| !listmapconfigs    |                    | Displays a list of available map configs (alias). | @css/root   |

## Stay up to date
Subscribe to **release** notifications and stay up to date with the latest features and patches:

![image](https://github.com/B3none/cs2-retakes/assets/24966460/e288a882-0f1f-4e8c-b67f-e4c066af34ea)

## Credits
This was inspired by the [CS:GO Retakes project](https://github.com/splewis/csgo-retakes) written by [splewis](https://github.com/splewis).

## Server Hosting (Discounted)

Looking for reliable server hosting? [Dathost](https://dathost.net/r/b3none/cs2-server-hosting) offers top-tier performance, easy server management, and excellent support, with servers available in multiple regions across the globe. Whether you're in North America, Europe, Asia, or anywhere else, [Dathost](https://dathost.net/r/b3none/cs2-server-hosting) has you covered. Use [this link](https://dathost.net/r/b3none/cs2-server-hosting) to get **30% off your first month**. Click [here]( https://dathost.net/r/b3none/cs2-server-hosting) to get started with the discount!