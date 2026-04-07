# CNetworkingSolution (CNS)

High level modular networking system for multiplayer games. Relies on a specific transport. After years of networking debates, this is the singularity.

## Features

Here is a list of some of the features it contains:
- Packet creation system (through the most up to date version of `NetPacket`) for easily creating and sending packets over the network
- Plug and play transport system where you can easily swap between between different low-level transports (CNet, LiteNetLib, Steamworks, etc.) and not have to make ANY changes to your codebase
  - You can also create your own transports by inheriting from the `NetTransport` abstract class
- Fully functioning lobby creation and joining system (lobby matchmaking)
  - You may need a [web server](https://github.com/Monstroe/CNSWebServer) to act as a lobby brokerage system/load balancer depending on how you're running your system
- Robust service and command system where you can send packets to different services, allowing you to change how you read the data based off what service and command was sent
  - This data can also be sent to specific networked objects (objects that have inherited from either `ServerObject` or `ClientObject`)
- Remote Procedure Calls (RPCs) allowing the server and its clients to directly call functions over the network
  - Scripts must inherit from either `ServerObject` or `ClientObject` to use this functionality
- Server-side event handling for easily updating state across the server
  - This state can then be synced on the clients with the different methodologies above
- Multiple samples with important game functions already synced, such as:
  - Player positions, rotations, and animations
  - SFX and VFX
  - Chat system
  - Fully functioning example project for testing
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

### Must be running Unity 6000.3 or newer

### Install

To install the package, you can choose one of the following ways:

1. Open the Unity Package Manager window (Window > Package Management > Package Manager)
2. Click the `+` button on the upper-left of the window, and select "Add package from git URL..."
3. Enter the following URL and click the `Add` button

```
https://github.com/Monstroe/CNetworkingSolution.git?path=/Assets/com.github-monstroe.cnetworkingsolution
```

### Extra Notes

1. When using some of the samples, ensure that "Active Input Handling" is set to "Both" in Player Settings.
2. When Unity Addressables imports, ensure you have defined `CNS_SFX`, `CNS_VFX`, `CNS_ServerPrefabs`, and `CNS_ClientPrefabs` as labels in your project. If you are using the samples, you may need to re-add some of the defined SFX/prefabs into their corresponding labels or the sample project will NOT function.

### Scripting Define Symbols

Each transport supported out of the box comes with it's own Scripting Define Symbol. Please see the following:

#### Transports
1. `CNS_TRANSPORT_LOCAL`: The local singleplayer transport, most projects will require this one
2. `CNS_TRANSPORT_CNET`: [CNet](https://github.com/Monstroe/CNet) transport
    - 2.5 `CNS_TRANSPORT_CNETRELAY`: [CNet](https://github.com/Monstroe/CNet) relay transport, used with a [relay server](https://github.com/Monstroe/CNSRelayServer)
3. `CNS_TRANSPORT_LITENETLIB`: [LiteNetLib](https://github.com/RevenantX/LiteNetLib) transport
    - 3.5 `CNS_TRANSPORT_LITENETLIBRELAY`: [LiteNetLib](https://github.com/RevenantX/LiteNetLib) relay transport, used with a [relay server](https://github.com/Monstroe/CNSRelayServer)
    - 3.5 `CNS_TRANSPORT_LITENETLIBBROADCAST`: [LiteNetLib](https://github.com/RevenantX/LiteNetLib) broadcast transport, used for UDP broadcasting
4. `CNS_TRANSPORT_STEAMRELAY`: [Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) transport

## Information

Assembly: Monstroe.CNetworkingSolution.Runtime

Namespace: CNetworkingSolution
