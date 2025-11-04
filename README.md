# Unity Dev Console

A developer console with a developer panel UI that allows you to trigger functions during runtime just like in Valve or Minecraft
You can use it to trigger cheats for testing when ingame

It hooks into the usual application debug logger so it can display errors, warnings and normal debug log

Type "help" for quick start on how to use the dev console
Press the ? button for list of commands
Type "clearconsole" to clear the console 

## Features
- Dev Console Panel UI
- Input commands
- History (Click up or down arrow to shuffle through history command)
- Autocomplete (Has a ghost that allows you to autocomplete the command)
- Shows type of arguments during autocomplete
- Show a list of all commands available
- Colour coded according to warning, error and log type
- Command listing by page

## How to use
1. Import project or package into your project
2. Drag Dev Console Canvas prefab to scene
3. Go to any script and do two things: (Refer to example script for more info)
   A. Register script to developer console via code
   B. Add [ConsoleCmd] attribute to the function you want to register
5. Press the dev console icon at bottom left or "/" key to open up dev console
6. Input your commands
7. Profit??

## How to register script and functions

Make sure you register the script that you want to register function for

**To register script**
<img width="483" height="205" alt="image" src="https://github.com/user-attachments/assets/f691699d-9eec-435a-828c-196d7faf267b" />
Ensure you register and unregister the component script

**To register function**
<img width="465" height="147" alt="image" src="https://github.com/user-attachments/assets/b161a01c-bb50-4d98-bc2c-4288ba1b1122" />
You can choose to put description that will showup in the list of commands or choose to not put any descriptions too

Refer to ExampleDevConsoleScript.cs for more examples

## How do you input commands
Open up the developer console panel and type in your command following the structure:
<command> <parameter1> <parameter2> <parameter3> ... so on

**Separate each parameter by space**

### Parameter supported
string - <command> "hello world"
bool - <command> true
vector3 - <command> 1,1,1
vector2 - <command> 1,1
enum - <command> enumStringNotCaseSensitive
int/float - <command> 2.4

## Current Limitations
- Does not support registering multiple function with same name but different parameter (function overloading)

## Screenshots
<img width="1631" height="922" alt="image" src="https://github.com/user-attachments/assets/8987f285-013d-4386-8403-a716a56d6bb6" />
<img width="862" height="415" alt="image" src="https://github.com/user-attachments/assets/4f0a72ef-5e29-4c87-9a78-a71496e4b2a8" />
<img width="858" height="417" alt="image" src="https://github.com/user-attachments/assets/a6e8a9e6-22b2-4d45-8b3d-eac9c0da42f4
![Uploading image.png…]()



