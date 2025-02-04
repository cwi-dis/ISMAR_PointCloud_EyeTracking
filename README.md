# ISMAR_PointCloud_EyeTracking
The official repo for QAVA-DPC: Eye-Tracking Based Quality Assessment and Visual Attention Dataset for Dynamic Point Cloud in 6 DoF ISMAR 2023
# Visual Saliency Map 
Generated visual saliency map per user can be downloaded via this link: [Visual Saliancy Map](https://zenodo.org/records/10996417)  
Name Convension of files:
001_A: uer_session  
H1_C2_R2_191: stimuli name_codec_distortion level_rotation_degree  
4246452_rafa_084.txt filename explanation: timestamp_point cloud name_frame_number  
## Contents
The VisualSaliencyMap folder includes:
- HeatValue:
This subfolder contains the heat values for each frame in a dynamic point cloud sequence. Each point's heat value is saved in a text file, with values ranging from 0 to 1.

- HeatValuewithPointCloud:
This subfolder provides visualizations of all heat values for each frame. The heat values are overlaid on top of the point cloud for each frame in all dynamic point cloud sequences.

# Raw Gaze data for 40 users
In this folder, it includes all the experimental data related to the eye-tracking (in the json file) and the original opinion scores (in two txt files) of each user. It can be downloaded from: [GazeData](https://zenodo.org/records/10996417)  
user_001 : user_userindex
001_A.txt: userindex_session.txt  
20230317-2301_001_A.json:date_userindex_session.json

# Calculated Quality Scores
You can find the calculated Mean Opinion Scores (Mos) and DMOS in the MOS/mos.csv and MOS/dmos.csv file.


# Visualization:
This is the video of the H5_C0_R0_BackView  

![Video Visualization](https://github.com/cwi-dis/ISMAR_PointCloud_EyeTracking/blob/main/video/H5_C0_R0_BackView-ezgif.com-crop.gif)   



and H5_C0_R0_FrontView.  

![Video Visualization](https://github.com/cwi-dis/ISMAR_PointCloud_EyeTracking/blob/main/video/H5_C0_R0_FrontView-ezgif.com-crop.gif)  

# Quick Start
## Device Specifications
- Processor	Intel(R) Core(TM) i7-9700 CPU @ 3.00GHz   3.00 GHz
- Installed RAM	32,0 GB
- Device ID	D415874E-183F-4E30-B8B7-FA373C373E84
- Product ID	00329-10333-35181-AA552
- System type	64-bit operating system, x64-based processor
## How to run it in Unity



### Conditions of use

If you wish to use any of the provided material in your research, we kindly ask you to cite our paper.
- BibTex
```
@INPROCEEDINGS{10316522,
  author={Zhou, Xuemei and Viola, Irene and Alexiou, Evangelos and Jansen, Jack and Cesar, Pablo},
  booktitle={2023 IEEE International Symposium on Mixed and Augmented Reality (ISMAR)}, 
  title={QAVA-DPC: Eye-Tracking Based Quality Assessment and Visual Attention Dataset for Dynamic Point Cloud in 6 DoF}, 
  year={2023},
  volume={},
  number={},
  pages={69-78},
  keywords={Point cloud compression;Measurement;Visualization;Solid modeling;Head-mounted displays;Gaze tracking;Inspection;Volumetric video;Dynamic point cloud;Visual saliency;Visual attention;Subjective quality assessment;Objective quality metrics;Eye tracking;6DoF},
  doi={10.1109/ISMAR59233.2023.00021}}
```
## About 
The QAVA-DPC Dataset is maintained by the Distributed & Interactive Systems (DIS) research group at Centrum Wiskunde & Informatica (CWI).

Contact the authors
- Xuemei Zhou: xuemei.zhou@cwi.nl
