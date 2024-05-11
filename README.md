# AC Proximity Health Plugin
Proof-of-concept plugin that shows enemy health bar and health percentage for a given number of enemies within range.

## Disclaimer
I am not a C#/dotnet programmer! This plugin does not by any means represent best coding practices and is largely based on other example [Virindi](http://www.virindi.net/) plugins.

## Requirements
The plugin was developed back when Asheron's Call retail was still live. Most recently the plugin has been tested using [ACEmulator](https://emulator.ac/) by following the [how to play](https://emulator.ac/how-to-play/) instructions.

Once you can connect to a server you can download the [Virindi Plugins Bundle](http://www.virindi.net/plugins/updates/VirindiInstaller1008.zip) and then clone this repository. The actual plugin file that should be added to Decal [is located here](https://github.com/adpriebe/acproximityhealth/blob/master/bin/x86/Debug/ProximityHealth.dll).

## Configuration
Plugin usage should be fairly self-explanatory.

![plugin-screenshot](https://github.com/adpriebe/acproximityhealth/assets/1586316/99664df2-6c05-4652-8b3d-384025768684)

* Enabled: toggle whether target tracking is enabled ("Tracking" shows current number of targets being tracked)
* Max Distance: proximity from player targets must be in to be trackable
* Max Targets: total number of targets to track
* Update Frequency: How often target health should be updated (in millseconds).
