# RosBag to PsiStore Converter
Licensed under the MIT license.

*This is still work in progress & a lot stuff still to be added*

This project builds a tool that converts Ros Bag (version 2.0 only) to [Platform for Situated Intelligence](https://github.com/microsoft/psi) Store (a.k.a. PsiStore).

Some properties of the tool:
* [Coming] If the message has a header in the root level, the message's originating time is set to the header time & not the message publish time.
* Does not rely on any external Ros message definitions, the tool figures out the fields from the message definitions in the RosBag.
* Convert some common standard Ros messages into Psi formats (example: Sensor_msgs/Image -> Image) *Currently only some std_msgs implemented* 
* For standard messsages not implemented or custom ros messages, the tool deconstruct them into their [ros message built-in types](http://wiki.ros.org/msg)

## Installation

The tool depends on the [Platform for Situated Intelligence](https://github.com/microsoft/psi) which is installed upon first build.
Open `RosBagConverter.sln` and Build Solution.

## Usage

To use the tool, open the commandline tool and navigate to where the executable is. Our goal is to eventually provide the same functionalities as those in [rosbag tools](http://wiki.ros.org/bag_tools).

If you need some test data, try running through the [Recording and playing back data](http://wiki.ros.org/rosbag/Tutorials/Recording%20and%20playing%20back%20data) tutorial, then `RosBagConverter.exe convert -f <my_path>\turtle.bag -o <my_path> -n Turtle`

#### Info

`RosBagConverter.exe info`
This list out the topics in the given RosBag. 
```
  -f, --file    Required. Path to first Rosbag
```
Example:
```
RosBagConverter.exe info -f C:\Data\psi_test.bag

Ros Bag to PsiStore Converter
---------------------
Info for Bags
---------------------
Earliest Message Time:11/12/2019 10:28:14 PM
Latest Message Time:11/12/2019 10:28:15 PM
Name                                              Type
--------------------------------------------------------------
/cameras/head_camera/camera_info                  sensor_msgs/CameraInfo
/cameras/right_hand_camera/camera_info            sensor_msgs/CameraInfo
/robot/joint_names                                MotorControlMsgs/StringArray
/tf_static                                        tf2_msgs/TFMessage
/robot/accelerometer/left_accelerometer/state     sensor_msgs/Imu
/robot/accelerometer/right_accelerometer/state    sensor_msgs/Imu
/robot/joint_states                               sensor_msgs/JointState
/tf                                               tf2_msgs/TFMessage
/kinect2/hd/camera_info                           sensor_msgs/CameraInfo
/cameras/right_hand_camera/image                  sensor_msgs/Image
/kinect2/hd/image_color/compressed                sensor_msgs/CompressedImage
/cameras/head_camera/image                        sensor_msgs/Image
/kinect2/hd/image_depth_rect/compressed           sensor_msgs/CompressedImage
/kinect2/hd/points                                sensor_msgs/PointCloud2
```

#### Convert
`RosBagConverter.exe convert`
Converts the rosbag to a PsiStore. You can specified which topic to be converted
```
  -f, --file      Required. Either directory holding bag files, list of bag files or a single bag files

  -o, --output    Required. Path to where to store PsiStore

  -n, --name      Required. Name of the PsiStore

  -t, --topics    List of topics to be converted to PsiStore
```
Example:
```
RosBagConverter.exe convert -f C:\Data\psi_example1.bag -o C:\Data -n t1 -t \rosout
```
```
RosBagConverter.exe convert -f C:\Data\psi_example1.bag C:\Data\psi_example2.bag -o C:\Data -n t2 --topics /text /rosout
```
```
RosBagConverter.exe convert -f C:\Data -o C:\Data -n t3 --topics /text /rosout
```

## ChangeLog:
* 12/28
	* bug fixed to make sure it works
	* 30% faster on complex ROS BAGs by precalculating offsets of fields.
	* Showed message types in Info screen
* 11/21
	* Rewrite the RosBag component to lazily read from the ROS Bags.
	* Refactor code.
	* Allow the reading of multiple ros bag files as long as they are ordered correctly.

## TODO
1. Optimize code
	* Convert the RosBag Module into a ISourceComponent. 
1. Implement more standard message types (Sensor_msgs, Geometry_msgs, etc)
1. For message with headers, use the header time instead of the message publish time (Could also be an option to be toggled).
1. Figure out how to handle transformations that exist on the /tf and /tf_static topics. 
