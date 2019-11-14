# RosBag to PsiStore Converter
Licensed under the MIT license.

*This is still work in progress & a lot stuff still to be added*

This project builds a tool that converts Ros Bag (version 2.0 only) to [Platform for Situated Intelligence](https://github.com/microsoft/psi) Store (a.k.a. PsiStore).

Some properties of the tool:
* [Coming] If the message has a header in the root level, the message's originating time is set to the header time & the message time will be the time the message was published. If the message does not have a header, both originate and message time are set to the message publish time.
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
```

#### Convert
`RosBagConverter.exe convert`
Converts the rosbag to a PsiStore. You can specified which topic to be converted
```
  -f, --file      Required. Path to first Rosbag

  -o, --output    Required. Path to where to store PsiStore

  -n, --name      Required. Name of the PsiStore

  -t, --topics    List of topics to be converted to PsiStore
```
Example:
```
RosBagConverter.exe convert -f C:\Data\psi_test_59_msgs.bag -o C:\Data -n t1 -t \rosout
```
```
RosBagConverter.exe convert -f C:\Data\psi_test_59_msgs.bag -o C:\Data -n t1 -t --topics /text /rosout
```

## TODO
1. Handle RosBag Files that are split into multiple files. Currently, you can just inspect each file individually.
1. Optimize code
	* Decrease the number of object creation by passing in file stream and offset instead of `byte[]`.
	* Make the array constructed lazly.
1. Implement more standard message types (Sensor_msgs, Geometry_msgs, etc)
1. Handle nested none-builtin types in message definitions.
1. For message with headers, use the header time instead of the message publish time (Could also be an option to be toggled).
1. Figure out how to handle transformations that exist on the /tf and /tf_static topics. 
