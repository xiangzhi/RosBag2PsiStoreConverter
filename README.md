# RosBag to PsiStore Converter
Licensed under the MIT license.

This project builds a tool that converts Ros Bag (version 2.0 only) to [Platform for Situated Intelligence](https://github.com/microsoft/psi) Store (a.k.a. PsiStore). Works on both Linux (Requires an upcoming fix in Psi) and Window machines.


Some properties of the tool:
* Does not need to prespecify message type or definitions. Does not rely on any external Ros message definitions OR the Psi Ros Stack. The tool will figure out the fields and types from the message definitions encoded in the RosBag.
* Convert some common standard Ros messages into Psi formats (example: Sensor_msgs/Image -> Image). A list of custom converters is specified in [MessageSerializers](MessageSerializers).
* For standard messages not implemented or custom constructed ros messages, the tool deconstructs them into their [ros message built-in types](http://wiki.ros.org/msg) through recursion.
* For `Stamped` messages specified in [MessageSerializers](MessageSerializers) with their own converters, use option `h` to use header stamp time instead of message publish time as originating time of message in Psi Stream. 
* To restamp the time to the pipeline start time, use the option `-r`. In this mode, the messages originating time will change to the start of the pipeline with the same original offsets between messages recorded in the Rosbag. *Note due to implementation issues, the start time might be earlier than the actual start time* 

## Installation & Build

The tool depends on the [Platform for Situated Intelligence](https://github.com/microsoft/psi) which is installed upon first build.
Open `RosBagConverter.sln` and Build Solution.

## Usage

To use the tool, open the commandline tool and navigate to where the executable is. Our goal is to eventually provide the same functionalities as those in [rosbag tools](http://wiki.ros.org/bag_tools).

Here are list of test RosBags:
* [Single RosBag with single message]()
* [Single RosBag with multiple messages including arrays]()
* [Single RosBag with custom Ros Message type]()

If you need want to build your own test data, try running through the [Recording and playing back data](http://wiki.ros.org/rosbag/Tutorials/Recording%20and%20playing%20back%20data) tutorial, then `RosBagConverter.exe convert -f <my_path>\turtle.bag -o <my_path> -n Turtle`

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
Name                                              Type                          Counts
--------------------------------------------------------------------------------------------
/cameras/head_camera/camera_info                  sensor_msgs/CameraInfo        1570
/cameras/right_hand_camera/camera_info            sensor_msgs/CameraInfo        1630
/robot/joint_names                                MotorControlMsgs/StringArray  1521
/tf_static                                        tf2_msgs/TFMessage            3667
/robot/accelerometer/left_accelerometer/state     sensor_msgs/Imu               1221
/robot/accelerometer/right_accelerometer/state    sensor_msgs/Imu               944
/robot/joint_states                               sensor_msgs/JointState        775
/tf                                               tf2_msgs/TFMessage            1845
/kinect2/hd/camera_info                           sensor_msgs/CameraInfo        174
/cameras/right_hand_camera/image                  sensor_msgs/Image             191
/kinect2/hd/image_color/compressed                sensor_msgs/CompressedImage   26
/cameras/head_camera/image                        sensor_msgs/Image             22
/kinect2/hd/image_depth_rect/compressed           sensor_msgs/CompressedImage   16
/kinect2/hd/points                                sensor_msgs/PointCloud2       14

```

#### Convert
`RosBagConverter.exe convert`
Converts the rosbag to a PsiStore. You can specified which topic to be converted
```
  -f, --file       Required. Path to first Rosbag

  -o, --output     Required. Path to where to store PsiStore

  -n, --name       Required. Name of the PsiStore

  -h               Use header time

  -r, --restamp    Re-stamp Starting Time to be relative to the beginning of this application

  -t, --topics     List of topics to be converted to PsiStore

  -x, --exclude	   List of topics to be excluded when converting to PsiStore

  --help           Display this help screen.

  --version        Display version information.

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
* TBD:
	* Added lab specific serializers.
	* Created helper method to convert header time.
* 2/27/2020
	* Added option `-s` to specify whether to use custom serializers.
	* Added option `-x`	to specify topics to be excluded when converting. 
	* Speed up of the conversion up to 600 % through:
		* Rewrote the reader to parse/store the message upon reading and creation.
		* Minor audit of code and removed redundant checks and loops. 
* 1/16/2020
	* Added interface to add custom serializers.-
	* Added option `-r` to restamp the time.
* 1/13/2020
	* Added serialization of sensor_msgs/CompressedImage to uncompressed Psi Images
	* Added ability to specify whether to use header time for selected stamped messages using `-h` command
* 1/9/2020
	* Added a bunch of geometry_msgs definition.
	* fixed the problem of chain lookup of fields in serialization.
	* changed to .NET core to run on Linux machines.
	* Show number of message in Info screen
* 12/28/2019
	* bug fixed to make sure it works
	* 30% faster on complex ROS BAGs by precalculating offsets of fields.
	* Showed message types in Info screen
* 11/21/2019
	* Rewrite the RosBag component to lazily read from the ROS Bags.
	* Refactor code.
	* Allow the reading of multiple ros bag files as long as they are ordered correctly.

## TODO
1. Figure out slowdown and optimize code
1. Implement more standard message types (Sensor_msgs, Geometry_msgs, etc)
1. Figure out how to handle transformations that exist on the /tf and /tf_static topics. 
