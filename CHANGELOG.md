# ChangeLog:

## TODOs
1. Figure out slowdown and optimize code
1. Implement more standard message types (Sensor_msgs, Geometry_msgs, etc)
1. Figure out how to handle transformations that exist on the /tf and /tf_static topics. 


## ChangeLog:
#### [0.0.8] - 2020-06-23
	* Update the Psi Nugets to the newest verison - 0.12.53.2-beta
	* Reorganized Sample ROS Bags.
	* Added additional test for parsing more complex files.
#### [0.0.7] - 2020-04-15
	* Update the Psi Nugets to the newest verison - 0.11.82.2
	* Reorganized Repository:
		* Added UnitTests.
		* Add sample ROS Bags.
#### [0.0.6] - 2020-02-27
	* Added option `-s` to specify whether to use custom serializers.
	* Added option `-x`	to specify topics to be excluded when converting. 
	* Speed up of the conversion up to 600 % through:
		* Rewrote the reader to parse/store the message upon reading and creation.
		* Minor audit of code and removed redundant checks and loops. 
#### [0.0.5] - 2020-01-16
	* Added interface to add custom serializers.-
	* Added option `-r` to restamp the time.
#### [0.0.4] - 2020-01-13
	* Added serialization of sensor_msgs/CompressedImage to uncompressed Psi Images
	* Added ability to specify whether to use header time for selected stamped messages using `-h` command
#### [0.0.3] - 2020-01-09
	* Added a bunch of geometry_msgs definition.
	* fixed the problem of chain lookup of fields in serialization.
	* changed to .NET core to run on Linux machines.
	* Show number of message in Info screen
#### [0.0.2] - 2019-12-28
	* bug fixed to make sure it works
	* 30% faster on complex ROS BAGs by precalculating offsets of fields.
	* Showed message types in Info screen
#### [0.0.1] - 2019-11-21
	* Rewrite the RosBag component to lazily read from the ROS Bags.
	* Refactor code.
	* Allow the reading of multiple ros bag files as long as they are ordered correctly.