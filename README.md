# Mirror Lobby
**Open-source Boilerplate project** 

This repo contains an open-source Networked multiplayer Unity game project developed by Alexandria Pagram.

* The game loads with a main menu as its first scene.
* Players are given the option to create their own lobby or join an existing lobby [P2P connection].
* Rounds are timer-based.
* A basic score system exists.
* At the end of each round, the player with the highest score is displayed onscreen.
* Players can choose to start another round in the same lobby, or to exit the lobby and return to the main menu.
* There is some basic error-handling, such as network errors and what should happen when the Host leaves the game.

It uses the popular Mirror library for its multiplayer, a fork/extension of Unity's deprecated HLAPI. Currently, the game uses a peer-to-peer (P2P) connection - similar to Terraria and Don't Starve Together. A server-based system would be a nice-to-have option for players sometime in the future (if anyone has any ideas, message me!)

To understand how to sync movement and variables between clients, and to understand when and how to run methods serverside, please consult the Mirror or Unity HLAPI docs and join the Mirror discord community.

USE UNITY VERSION 2019.4.1f1
https://unity3d.com/unity/qa/lts-releases

## Using this project:

Please flag any bugs as issues on the github repository, or message me on discord and I'll add it to a Trello board. Feel free to submit PR's to fix any bugs or to extend the functionality of this project.

If you want to make your own game, I'd be delighted if you'd fork this project. Any PR's for this repository should be made with the intent to maintain or extend this 'boilerplate' lobby system for other users to easily fork.

## Background on the important files:

The player prefab has the Player.cs class at its root, as well as a network identity and flexnetworktransforms to link up any movement over the network.

There is a ScoreManager and a RoundSystem class that I've stripped back for you to customise.

The LobbyNetworkManager.cs is where my custom lobby networking code sits. It overrides Mirror's default NetworkManager.cs class. 

Some of the basics of this lobby system were created by using parts of Dapper Dino's lobby project and the Mirror example 'Room' project as references.

```
# Dapper Dino - Mirror Lobby tutorials
https://www.youtube.com/watch?v=5LhA4Tk_uvI&list=PLS6sInD7ThM1aUDj8lZrF4b4lpvejB2uB&ab_channel=DapperDino

# Mirror Networking Library
https://mirror-networking.com/
```
## Instructions - scenes and players:

**Menu scene** - the default scene & default Offline scene.

_Here is where a player enters their name, and decides whether to host or join a game._
##

**Lobby scene** - the default online scene.

_Here is where a player is taken once they become a part of a lobby. There is minimal GUI elements currently, as the lobby overlay is actually attached to the lobby player prefabs._
##

**Game scene** - where the game takes place.
##

'RoomPlayers' (type of LobbyPlayer) are persistent between rounds.

'GamePlayers' (type of Player) are created for each player once they join the Game scene. They are destroyed between rounds.
