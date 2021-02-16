## follow-me
Unity project

Networked multiplayer Unity game project.

Uses the Mirror library for its multiplayer, a fork/extension of Unity's deprecated HLAPI.

To understand how to sync movement and variables between clients, and to understand when and how to run methods serverside, please consult the Mirror or Unity HLAPI docs and join the Mirror discord community.

USE UNITY VERSION 2019.4.1f1
https://unity3d.com/unity/qa/lts-releases

# PSA:

Please flag any bugs as issues on the github repository, or message me on discord and I'll add it to a Trello board. Feel free to submit PR's to fix any bugs or to extend the functionality of this project.

If you want to make your own game, I'd be delighted if you'd fork this project. Any PR's for this repository should be made with the intent to maintain or extend this 'boilerplate' lobby system for other users to easily fork.

# Background:

The player prefab has the Player.cs class at its root, as well as a network identity and flexnetworktransforms to link up any movement over the network.

There is a ScoreManager and a RoundSystem class that I've stripped back for you to customise.

The LobbyNetworkManager.cs is where my custom lobby networking code sits. It overrides Mirror's default NetworkManager.cs class. 

Some of the basics of this lobby system were created by using parts of Dapper Dino's lobby project and the Mirror example 'Room' project as references.

# Instructions:

Menu scene - the default scene & default Offline scene. 
Here is where a player enters their name, and decides whether to host or join a game.

Lobby scene - the default online scene
Here is where a player is taken once they become a part of a lobby. There is minimal GUI elements currently, as the lobby overlay is actually attached to the lobby player prefabs.

Game scene - where the game takes place.

'RoomPlayers' (type of LobbyPlayer) are persistent between rounds.
'GamePlayers' (type of Player) are created for each player once they join the Game scene. They are destroyed between rounds.
