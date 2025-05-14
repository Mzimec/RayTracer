# Readme

## First Check

I created mine gitlab repository for this project. And hopefully successfully set up all the properties for it and set up upstream
For my second step I set up argument parsing using CommandLineParser library and reading from json config files.
Fo third step I updated the HDR generating project given to us in /src to work with my argument parsing and then I added simple function to create some simple pattern to the image.

Configuration Info:
* Config files are in json format.
* There is one config file named config.json with parameters: width = 800, height = 600, file name = output.pmf

The program can take following arguments:
* `--config`	: defining the name of the config file.
* `--width`		: defining width in pixels of the final image.
* `--height`	: defining height in pixels of the final image.
* `--output`	: defining name of the output file.

If there is no config file given, then will be used default one with values: width = 600, height = 450, file name = output.pfm

## Second Check

I created finally a prototype of renderer, while trying to use reasonable OOP programing.
Firstly I setted up my Perspective Camera.
Secondly I created three types of solids: Spheres, Planes and Cylinders.
Then I implemented BRDF using Phong method.
Created three types of light sources: Ambient, Directional and Point.
Lastly I finaly summ all of theese components into raytracer.

Currently I already added to my raytracer shadowmapping, reflections and refractions. Although they are still kind of experimental. Feel free to try them and comment on them.

Current ouptut format is .pmf.

Now onto Configuration Info:
* Config files are in json format.
* There is one scene in config file named config.json.

The program can take following command line arguments which overrides values in config file:
* `--config` 		:	defining the name of the config file. This is required argument.
* `--width`			:	defining width in pixels of the final image.
* `--height`		:	efining height in pixels of the final image.
* `--output`		:	defining name of the output file.
* `--render_type`	:	defining type of rendering (0 for basic, 1 with shadowmaps, 2 with reflections, 3+ with refraction)
* `--help` 			: 	for showing description for all the argument keywords.

## Third Check

I already made the renderer using OOP and had already implemented hard shadows and reflections in last version.
In this version I switch Phongs model for specular light for Blinn-Phongs model. There will be option to switch between them.
I added more clever way of saving. Either to PFM or to HDR based on the sufix of the output name.
The command line arguments are the same as in the previous check.

