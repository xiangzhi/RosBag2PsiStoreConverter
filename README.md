﻿# RosBag to PsiStore Converter
Licensed under the MIT license.

*This is still work in progress & a lot stuff still to be added*

This project builds a tool that converts Ros Bag (version 2.0 only) to [Platform for Situated Intelligence](https://github.com/microsoft/psi) Store (a.k.a. PsiStore).

Some properties of the tool:
* [Coming] If the message has a header in the root level, the message's originating time is set to the header time & the message time will be the time the message was published. If the message does not have a header, both originate and message time are set to the message publish time.
* Does not rely on any external Ros message definitions, the tool figures out the fields from the message definitions in the RosBag.
* Convert some common standard Ros messages into Psi formats (example: Sensor_msgs/Image -> Image) *Currently only some std_msgs implemented* 
* For standard messsages not implemented or custom ros messages, the tool deconstruct them into their [ros message built-in types](http://wiki.ros.org/msg)

## Installation
1. To build the tool, you need the source version of [Platform for Situated Intelligence](https://github.com/microsoft/psi).
2. Add this project to the solution (Suggested location is Psi\Samples).
3. Edit ``psi\Sources\Runtime\Microsoft.Psi\Serialization\KnownSerializers.cs`` and change the method `GetHandler<T>()` from internal to public. You will also need to disable the warning.
```
#pragma warning disable SA1600 // Elements should be documented
        public SerializationHandler<T> GetHandler<T>()
#pragma warning restore SA1600 // Elements should be documented
```
4. Build the tool.

## Usage
To use the tool, open the commandline tool and navigate to where the executable is. Our goal is to eventually provide the same functionalities as those in [rosbag tools](http://wiki.ros.org/bag_tools).

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

## TODO & Limitations
1. Handle RosBag Files that are split into multiple files
1. Change the originate time in the file to the header time. (Figure out the way propagate originated time and latency)
1. Implemented Sensor_msgs and able to convert to images
1. Figure out how to deal with TF (transformations)
