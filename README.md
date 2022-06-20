
# PrroBot
This is a MapAssist fork with some added botting features. 
I have started this project a couple of months ago and developed it in the very few hours of free time I have. 
About a week ago I (and a lot other MapAssist users) got banned, so my motivation to take this project any further is pretty much gone.
In its current state it is not very user friendly, a lot is hardcoded and not configurable via config files. My end goal was to make this a fully fledged bot, sadly this is currently not the case. 
I hope this project is helpful to anybody who is trying to do something similar. Maybe someone even picks up where I left and finishes this bot (but make it open source please).

**About 99.9% of the credit goes to the MapAssist contributers and jcageman** for his [pathing implementation](https://github.com/jcageman/Diablo2Clientless1.09/tree/master/src/D2NG.Navigation) that I modified to my needs.

The main branch is a mirror of the original MapAssist branch. I use this to pull the MapAssist changes into this repo. This branch should be left unchanged.
The bot is implemented in the PrroBot branch. Any changes should be done there.

There are a lot of //TODO's in the code marking some known bugs, possible improvements and feature ideas.

I will try to keep this fork up-to-date with major stable releases of MapAssist, so this bot may work with future D2R versions.

If you have any questions regarding compiling/installing this yourself, take a look at the [MapAssist Wiki](https://github.com/OneXDeveloper/MapAssist/wiki), this part is unchanged.

## Usage:

 - F9: Move to next area 
 - F10: Move to quest objective 
 - F11: Move to waypoint in this area 
 - F12: Start/Stop bot

## What this bot does:
- Interacts with the game via key and mouse events (no packet sending, function calling etc).
- Uses only MapAssist internal mechanics to read the game memory
- Uses potions automatically
- Exits the game if life drops below a certain threshold
- Capable of walking (in town) and teleporting (out of town, if teleport skill is available)
- Can move to next area, quest objective, waypoint, any npc, arbitrary point on the map
- Can interact with any game object like npcs, portals etc
- Can open and use town portals
- Town Chores
	- Pickup Corpse
	- Heal
	- Repair/Identify/Sell/Stash items
	- Revive merc
	- Refill potions (Mana, Life) at vendor
	- Refill potions (Rejuvs) from stash
- Pickit using the MapAssist item filter (configured via the ItemFilter.yaml)
- Stable implemented runs/scripts (usually 95%+ success rate):
	- Countess
	- Andariel
	- Summoner
	- Mephisto
	- Nihlathak
- Unstable implemented runs/scripts (somewhere around 50% success rate):
	- Diablo


## Limitations:
- Only works in fullscreen mode
- Only Hammerdin build implemented
- The runs are hardcoded. For example if you want to run Mephisto only, you have to change that in the PrroBot.cs file and recompile the project
- Many other configs are hardcoded (e.g. which stash tabs to use, which potions to use, which inventory slots to ignore when selling items)



# [MapAssist](https://mapassist.github.io)

> Status: DETECTED

> Do Not Use on B.net

Using this program on B.net will result in a permanent account suspension from D2R online.

MapAssist users reported being banned from D2R online services beginning June 14, 2022.

## Features

- Reveals the D2R map as a game overlay, includes:
  - Shows monsters, monster immunities, and distinguish different monster types with different colors
  - Lines to next area and other points of interest
    - Including the correct tomb in Canyon of Magi and the right direction to Summoner in Arcane Sanctuary
  - Waypoints
  - Party players, neutral players, hostile players, corpses
  - Player portals and red portal
  - Super chests
  - Shrines
  - Weapon and armor racks
- Built-in item filter with map icons and loot drop alerts
- Naturally boosts your magic find (strong anecdotal evidence)
- +300% chance to find Gem shrines (strong anecdotal evidence)
- Highly configurable with a built-in GUI
- Works online (user accepts all risks) and offline
- Multi-launcher compatible
- Active development (new features added regularly)
- Free technical support [on our Discord channel](https://discord.gg/uBftrtBE4j)

## Images

![MapAssist](https://user-images.githubusercontent.com/1294559/151440355-6cfd64d5-94e0-4942-b144-9224e16d15c6.png)
![MapAssist](https://user-images.githubusercontent.com/1294559/151440400-4c887af4-ca89-46cf-893b-9cdc8a1fcb5b.png)
![MapAssist](https://user-images.githubusercontent.com/1294559/151440395-baecf57f-d7bd-4cbe-b78f-201ce3e0f464.png)
![MapAssist](https://user-images.githubusercontent.com/1294559/151440410-452eada6-da24-458e-8c32-d86e18204642.png)
![MapAssist](https://user-images.githubusercontent.com/1294559/151440415-fe92d5b1-068d-4734-b355-f47cfa9931df.png)

## Disclosures

THE 'MapAssist' TOOL AND CONTENTS THEREIN WERE INTENDED FOR INFORMATIONAL AND LEARNING PURPOSES ONLY.

Feel free to contribute a pull request with new features or fixes, the code is under GPL so make sure to please follow the license.

The software is free and open source licensed under the GPLv3. You may download a copy of the source code and follow the instructions to build the software for yourself. In certain instances there may be direct links to precompiled versions of the software, you are free to use these as well. The best place to start looking is inside of the repository.

Use at your own risk! The MapAssist team makes no expression of warranty or claim about the safety in regards to the usage of this program. We have done our best to provide an open project for learning and educational purposes only. The contents of the official source code are known to not contain any malicious or disingenuous code. If you have additional information in regards to the safety of this program please do not hesitate to raise a concern.

All software projects are subject to their own licensing, and copyrights. All other product names and any registered and unregistered trademarks mentioned are used for identification purposes only and remain the exclusive property of their respective owners.
