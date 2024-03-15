# VIDEX
Video Indexing Tool with Object and Outlier detection
![스크린샷 2024-02-14 12 15 47](https://github.com/nth221/videx/assets/64348852/bee72b86-5916-4980-9834-c460de7a00a1)

![js](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![js](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![js](https://img.shields.io/badge/Python-14354C?style=for-the-badge&logo=python&logoColor=white)
![js](https://img.shields.io/badge/TensorFlow-FF6F00?style=for-the-badge&logo=tensorflow&logoColor=white)
![js](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)
![js](https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=visual%20studio&logoColor=white)


VIDEX is a novel video indexing tool designed to streamline the surveillance video review process. More specifically, VIDEX facilitates automatic object detection and outlier detection, enabling rapid summarization and easy access to critical events within the footage. By cataloging information about detected objects and anomalies in an indexed database, VIDEX ensures efficient retrieval and analysis of the video frames that require attention for review. This significantly cuts down the time and resources needed for a thorough examination of surveillance footage. VIDEX is developed by HAIL Lab at Handong University.
</br>

![objectdetection](https://github.com/nth221/videx/assets/125935704/15b849bf-19e9-4448-b88a-63c2f428044b)
![OutlierView](https://github.com/nth221/videx/assets/125935704/f66e0013-476c-40d4-bd8d-870351addacd)



## Overview

- Initial Screen

![Initial_Screen](https://github.com/nth221/videx/assets/125935704/592af790-275a-4154-a6ce-401a6bb0803e)

(White mode)  

![Initial_Screen_White](https://github.com/nth221/videx/assets/125935704/f1ff86c4-583f-4c3a-acc2-ad315f379605)


- Object Detection Result Screen

![Object_Result](https://github.com/nth221/videx/assets/125935704/2fe02f1a-c315-47ae-88e6-7673111c8a09)


- Outlier Detection Result Screen

![Outlier_Result](https://github.com/nth221/videx/assets/125935704/ac737543-b85d-4bd0-9ad5-bad381bb5b7f)





## System Design

- Structure of VIDEX interface (with M-V-VM pattern)
![image](https://github.com/nth221/videx/assets/64348852/8fd4c014-51bb-41de-ac8c-70e2bcf9cd3f)

  In VIDEX, interface consisted of C# WPF M-V-VM pattern. The model plays a role in storing and importing data, and declares and uses classes of data to be stored in the database. View is a place that displays the screen that the user sees and processes user input. VIDEX includes SettingView, where users import videos and set up settings, ObjectDetectionView, and AnomalyDetectionView, which show results for object detection and anomaly detection. ViewModel detects events occurring in View and performs business logic suitable for those events. In VIDEX, most of the major tasks are performed in viewmodel, and the data necessary for logic is retrieved from the model and updated to view. 

</br>

- Multi-Thread Pipeline     
![image](https://github.com/nth221/videx/assets/64348852/d49d0a61-2f4e-4a9e-b7f3-5e394660ec80)


  VIDEX effectively processes video data through a multithreading method. 

  In the process for object detection, entire video frame is divided by the number of threads allocated to object detection. Each thread is allocated as many as the number of divided frames, and independent work is possible because there are no frames shared with each other. Each thread executes a YOLOv5 model called through ONNX (Open Neural Network eXchange) for the video frames, and stores information such as class, frame, bounding box coordinates, and size for the detected object in the database. At the same time, the information is retrieved from the database and the view is updated.

  In the process for anomaly detection, we allocate threads as many as the number of detection methods to parallelize the process. Initially, we segment the input video into segments, which serve as detection units, and obtain spatio-temporal feature embeddings using a pre-trained 3D-CNN (also called through ONNX) on large-scale action recognition data. Then, each thread distinguishes anomaly embeddings from the entire embeddings using assigned non-parametric outlier detection methods. Through this approach, VIDEX leverages multithreading to maintain process parallelism and achieve improvements in speed.

</br>

- Dataflow Diagram
![DFD_VIDEX-Page-1 drawio (6)](https://github.com/nth221/videx/assets/64348852/5f48ef87-811f-4e09-a813-3875a4a2e3db)


  The figure above is a dataflow diagram of videx. Follow the flow to find out the functions of videx.

</br>



## Additional Function

- Object Statistics Using OxyChart UI  
![Chart_check_](https://github.com/nth221/videx/assets/125935704/afaef400-d67a-4b85-9a78-f21959f7e829)
</br>

- Object Filter On/Off  
![ObjectOn-off](https://github.com/nth221/videx/assets/125935704/0afa0559-1ddd-415f-8fee-44556bf022b5)

</br>


