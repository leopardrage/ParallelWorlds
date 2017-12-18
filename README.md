# ParallelWorlds

Parallel Worlds is a multiplayer online game built on top of the [Merry Fragmas 3.0](https://unity3d.com/learn/tutorials/topics/multiplayer-networking/merry-fragmas-30-multiplayer-fps-foundation) tutorial series from Unity Live Sessions. Besides the classic FPS mechanics, players can switch between two different worlds (a brighter one and a darker one) by pressing the right mouse button. This project has been developed with Unity3D 2017.2.0f3.

Please note that this is just a tech demo of my knowledge in graphics programming and gameplay programming on Unity, not a full game, so I mainly focused on the world swapping feature, leaving the rest of the game features to future development and polishing tasks.

## How to launch the game

Here are the instructions to test the game on the same machine using a standalone build as a host and the Unity editor as a client:
1. Open the project with Unity, **File->Build & Run**. Choose filename for your standalone build and hit **Save**
2. Once done building, press **Play!** in the ParallelWorlds Configuration window;
3. Once the game is running, type a name for your game room under **CREATE A GAME** and press **CREATE**;
4. (Optional) choose a color and a name for the hosting player;
5. Back on Unity, open scene **Lobby**, in Assets/_Scenes and press **play** on the editor;
6. Once the game is running, press **LIST SERVERS** and hit **JOIN** next to the game room you created.
7. (Optional) choose a color and a name for the client player;
8. In the lobby, press the **JOIN** button;
9. On the standalone build, in the lobby, press the **JOIN** button;

## Techincal Notes:
1. Both universes (as the worlds are called in code) exist at the same: the main scene (corrisponding to universe A) load additively universe B on awake. The universe swapping logic, located in the **PlayerUniverse** component, manages each player's universe state and handles the transition process between universes. *See comments on top of the PlayerUniverse class for more details*.
2. The swap effect on the local player side has been made by animating the **camera FOV** and by adding a custom post-processing **vignette** effect combined with a **color saturation** through a **vertex/fragment shader**.
3. The swap effect on the remote players has been made using a **surface shader** that progressively dissolve (clip) the model's fragments based on a uniform parameter and a dedicated texture. Also, a colored edge is shown based on a LUT.
