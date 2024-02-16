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

-----

### Overview

- Initial Screen

사진첨부

- Object Detection Result Screen

사진첨부

- Outlier Detection Result Screen

사진첨부
  

-----

### System Design

- Structure of VIDEX interface (with M-V-VM pattern)
![image](https://github.com/nth221/videx/assets/64348852/8fd4c014-51bb-41de-ac8c-70e2bcf9cd3f)

In VIDEX, interface consisted of C# WPF M-V-VM pattern. The model plays a role in storing and importing data, and declares and uses classes of data to be stored in the database. View is a place that displays the screen that the user sees and processes user input. VIDEX includes SettingView, where users import videos and set up settings, ObjectDetectionView, and AnomalyDetectionView, which show results for object detection and anomaly detection. ViewModel detects events occurring in View and performs business logic suitable for those events. In VIDEX, most of the major tasks are performed in viewmodel, and the data necessary for logic is retrieved from the model and updated to view. 

- Multi-Thread Pipeline     
![image](https://github.com/nth221/videx/assets/64348852/d49d0a61-2f4e-4a9e-b7f3-5e394660ec80)
</br>

- Dataflow Diagram  
![DFD_VIDEX-Page-1 drawio (3)](https://github.com/nth221/videx/assets/64348852/8f9181cb-186f-4b2b-a869-185c5dd55041)
</br>

-----

### Characteristic Function

- Object Statistics Using OxyChart UI  
![Chart_check_](https://github.com/nth221/videx/assets/125935704/afaef400-d67a-4b85-9a78-f21959f7e829)
</br>

- Object Filter On/Off  
![Filter ONOFF2](https://github.com/nth221/videx/assets/125935704/860a8e2a-03dc-4eec-8c01-ca0986c65b4e)
</br>

- Outlier Checking View  
![Outlier_check_](https://github.com/nth221/videx/assets/125935704/bfb9fa1a-4d87-473e-bde4-839049b77b94)
</br>


