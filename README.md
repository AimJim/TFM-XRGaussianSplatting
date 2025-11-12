This was made as the final project for my Master's degree (Simulation, Virtual Reality and Graphic Computing) at U-tad.

# VR_Unity_Gaussian_Renderer

Unity Renderer for 3D Gaussians

VR compatibility using OpenXR

The project offers two ways of representing a scene generated using 3D Gaussian Splatting. It can show a single scene where the camera has free movement or a number of smaller scenes where is posible to grab these.

## Single scene

In this representation, a single 3D Gaussian Splatting scene is represented. It is possible to navigate the scene freely using a VR headset.

## Multiple scenes

In this representation, a number of 3D Gaussian Splatting scenes are simultaneusly represented in a classic 3D modeled room.
The 3D Gaussian scenes correctly occlude their surrondings, occluding both other scenes and the 3D room. These scenes are also correctly ocluded by its surroundings.

## Credit

Some external libraries and assets have been used (Both the Ply reader and the shader have been modified):

3D room:
https://free3d.com/3d-model/living-room-997877.html

GPU Sorting:
https://github.com/b0nes164/GPUSorting

Ply reading:
https://github.com/3DBear/PlyImporter/tree/master

Shader:
https://jtstephens18.github.io/posts/Building-a-Gaussian-Splatting-Renderer-In-OpenGL
