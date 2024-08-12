# tshock-uhc
 Prevents players from regenerating HP 
 
***

### Setup
* Server Side Characters (SSC) must be enabled in `tshock/sscconfig.json`
    - Using SSC will require players to register/login when they join in order to play
    - This plugin will not work on players that are no logged in
    - If you want players to start with more than 100hp, change the `StartingHealth` property in `tshock/sscconfig.json`
    - Note that if the value is over 500, you will also need to edit `tshock/config.json` and set the `MaxHP` property to the same value or higher

* Install the plugin and start tshock, this will generate the config file in `tshock/UHC.json`

### Configure (optional)
* Close tshock, and edit the config file `tshock/UHC.json`
* The `HealingItems` property allows you to define items, which when used, will heal the player, and by what amount
    - By default, the config will have Life Crystals (item id 29) heal for 50 hp, and Demon Hearts (item id 3335) heal for 600 hp.
    - You can remove, edit, or add your own items to this list
* Save the config file. Note that you will always need to restart tshock after editing config file

### Commands
`/sethp <amount> <player>`
* Manually set UHC player hp (in case you need to heal someone)
* Exampe: /sethp 400 onusai
* Required permission: `tshock.admin` (can also be used in console)

### How to reset UHC for new game / world
1. Reset UHC plugin config
   * Either delete the `tshock/UHC.json` file or edit the file and delete all the players listed inside the `PlayerHP` property
2. Reset `tshock/tshock.sqlite`
   * Option 1:
       - Edit this file using a program like [DB Browser for SQLite](https://sqlitebrowser.org/)
       - Delete all records in the `tsCharacter` table
       - Optionally, if you also want to delete all user accounts, delete all records in the `Users` table
   * Option 2:
      - Delete the `tshock/tshock.sqlite` file
      - This is the easiest way, however it will also reset any accounts or permissions that you have set/modified
   * Suggestion:
      - The easiest approach would be to make a copy of the `tshock/tshock.sqlite` file before starting a game
      - Once the game is over and your ready to start a new one, replace the `tshock/tshock.sqlite` file with the copy you made before

***

[Download UHC.dll](https://github.com/onusai/tshock-uhc/raw/main/bin/Debug/net6.0/UHC.dll)
