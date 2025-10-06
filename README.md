# CNetworkingSolution

Higher level abstracted networking system for multiplayer games. Relies on a specific transport. After years of networking debates, this is the singularity.

## Features

Here is a list of some of the features it contains:
- Packet creation system (through the most up to date version of `NetPacket`) for easily creating and sending packets over the network
  - All custom packets can be stored in the `PacketBuilder` static class
- Plug and play transport system where you can easily swap between between different low-level transports (CNet, LiteNetLib, Steamworks, etc.) and not have to make ANY changes to your codebase
  - You can also create your own transports by inheriting from the `NetTransport` abstract class
- Fully functioning lobby creation and joining system
  - This can be run in multiple modes by changing Scripting Define Symbols, see more below
  - You may need a [web server](https://github.com/Monstroe/CNSWebServer) to act as a lobby brokerage system/load balancer depending on how you're running your system
- Robust service and command system where you can send packets to different services, allowing you to change how you read the data based off what service and command was sent
  - This data can also be sent to specific networked objects (objects that have inherited from either `ServerObject` or `ClientObject`)
- An example project with many important game functions already synced, such as:
  - Lobby settings
  - User settings
  - Player positions, rotations, and animations
  - Player interaction system with `ServerInteractable` and `ClientInteractable`
  - Spawning and destroying system with players and items
  - SFX and VFX
  - Event system
  - Chat system
- Various utility scripts for ease of use in projects, such as:
   - `Expand`
   - `Fade`
   - `FadeUI`
   - `FadeScreen`
   - `Hover`
   - `Rotate`
   - `LookAtCamera`
   - `SmoothLookAtCamera`

## Instructions

Here are various installation and setup instructions.

### Install

To install the package, you can choose one of the following ways:

1. Clone the repository and drag and drop the features you would like in your project
2. Clone the repository and start building out of the example project directly
3. Go to the 'Releases' tab and download the .unitypackage file (COMING SOON)

### Scripting Define Symbols

Networked games have differrent ways of syncing their users. I have tried to cover ALL these ways with Scripting Define Symbols. Please see what they all mean:

#### Server
1. `CNS_SYNC_SERVER_SINGLE`: One dedicated server that handles every user/lobby for your game
2. `CNS_SYNC_SERVER_MULTIPLE`: Multiple dedicated servers that all handle different users/lobbies for your game (this one will most likely require a [web server](https://github.com/Monstroe/CNSWebServer) lobby brokerage system/load balancer)
3. `CNS_SYNC_HOST`: Clients run their own lobbies where everyone connects to one user called the 'host' (this one will also likely require a web server)

#### Lobby
1. `CNS_LOBBY_SINGLE`: Every server only handles one lobby (this might be better for MMO style games)
2. `CNS_LOBBY_MULTIPLE`: Every server can handle multiple lobbies

#### Transports
1. `CNS_TRANSPORT_LOCAL`: The local singleplayer transport, most projects will require this one
2. `CNS_TRANSPORT_LITENETLIB`: [LiteNetLib](https://github.com/RevenantX/LiteNetLib) transport
3. `CNS_TRANSPORT_STEAMRELAY`: [Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) transport
4. `CNS_TRANSPORT_CNET`: [CNet](https://github.com/Monstroe/CNet) transport
