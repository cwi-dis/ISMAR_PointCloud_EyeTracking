## CWIPC Simple Samples

These samples are part of the `cwipc_unity` package, <https://github.com/cwi-dis/cwipc_unity>. You are free to use, modify, etc.

These samples do not cater for VR use, see the `VR` sample set for that. And those samples are actually also more powerful for non-VR use.

## Scenes

- `SimplePointCloudViewer` shows a point cloud stream using the `cwipc_display` prefab and a camera. By default a synthetic point cloud is shown but this can be changed.
- `TwoUsers` runs an interactive session with two users (on two machines). You specify the hostnames of the two machines, but by default both machines are `localhost` so you are actually viewing yourself. Default representation is the synthetic point cloud, but again this can be changed to a RealSense camera, Azure Kinect camera or prerecorded point cloud source. Look in the `cwipc_avatar_self_simple` prefab.
- `TwoUsersTiled` is similar to `TwoUsers` but allows for transmitting point clouds as tiled streams (which can lead to better performance due to parallel encoding and decoding). Structure is similar to `TwoUsers`.
- `ProcessedPointcloudViewer` is like SimplePointCloudViewer, but every point cloud read is projected onto a unity sphere around its centroid (turning everything into a ball, with colors preserved). Not very useful, but shows how you can access individual points. But be warned that this is not very efficient.

## Prefabs

There are four prefabs: 2 for the simple session and 2 for the tiled session. Both are implemented for the self-user (which captures, encodes, transmits and displays self-view) and for the other-user (which receives, decoder and displays).

## Scripts

The scripts contain:

- sample character controllers for the self-user and other-user,
- sample session controller for the simple session and the tiled session,
- sample orchestration which allows the two session controllers on the two machines in the session to exchange information.
- sample reader that performs operations on individual points in the cloud.