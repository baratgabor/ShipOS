[![Build Status](https://travis-ci.com/baratgabor/GridOS.svg?branch=master)](https://travis-ci.com/baratgabor/GridOS)

**Table of Contents**
<!-- Table of contents generated generated by http://tableofcontent.eu -->
  - [Summary](#summary)
  - [Architecture](#architecture)
  - [How can you use it](#how-can-you-use-it)
  - [Planned Features](#planned-features)
  - [Getting started using the framework](#getting-started-using-the-framework)
  - [Example module class with all interfaces implemented](#example-module-class-with-all-interfaces-implemented)
  - [Instantiating the framework and registering a module](#instantiating-the-framework-and-registering-a-module)

## Menu Demo

A short demonstration of the menu handling capabilities, which includes: traversing tree structures, automatic word-wrapping, prefix and suffix bullet characters based on item type, auto-scrolling, displaying navigation breadcrumbs, etc. This demo doesn't cover other existing capabilities, e.g. displaying automatically updating text.

The script has been updated to utilize the game's relatively new low-level graphics API, instead of the initial text-based implementation.

![GridOS Menu system demo](docs/GridOS_Menu_Demo.gif)

(Yes, it has an integrated log with customizable logging level. Every developer's dream. 😅)

## Summary
GridOS is a modular multitasking and command handling ingame script for Space Engineers. This script provides a framework for creating separate code modules to run on a single Programmable Block.

Additionally, it provides access to a highly flexible hierarchical menu system that implements intelligent automatic screen updates (when the underlying data changes), and supports showing different parts of the same menu hierarchy on different displays (additional displays can be added or removed dynamically at runtime).

From time to time expect some breaking changes to the consumer-facing interfaces. I don't think anyone besides me is using this script currently, so there doesn't seem to be any reason to be careful yet. I'm taking long breaks from the project, but I'll strive to produce a release version sometimes in the future.

## Current state, development focus: GUI

The script has been updated to a graphical interface from the initial text-based one, utilizing Space Engineer's new low-level sprite API. This makes it somewhat more presentable and increases its customizability. I basically implemented a simple GUI system, where the screen consists of controls, and each control describes its appearance via familiar properties like colors, margin, padding and text size, supporting multiple units, e.g. screen percentage, pixel, and em (relative to the base font size).

This is already quite flexible in some ways, but with this implementation controls are still essentially just text boxes, and I realized that a significantly wider range of GUI-capabilities could be well utilized. So the next step is to redesign the rendering logic to make each control draw itself. This will allow controls to contain shapes, or other nested controls, thus unlocking the ability to implement even layout elements, like stackpanels in WPF. After this feature is added, I can start implementing various utility controls, e.g. sliders and pop-ups.

Subsequently I plan to implement the ability to add, remove and customize LCD panels directly in the menu system, including selecting which controls should be visible on which LCDs. This would allow, for example, to display a certain inventory content on a certain LCD, or any combination of supported controls, without the entire menu (since the menu itself is also just a control).

One problem is that this project is already too complex for an in-game script (even if it runs well), and I constantly struggle with the script length limitation, especially considering that the custom code written by users will require space as well. Possibly I'll have to make this into a mod.

## Architecture

Functional overview of GridOS:

*(Note that this solution interprets concepts rather pragmatically, and some industry standard practices, like using abstractions and dependency injection everywhere, don't really add value in this limited/constrained runtime environment.)*

![GridOS components overview](docs/GridOS-diagram.png)

* **GridOS facade:** The main GridOS class (the one you're instantiating) is a simple facade or front controller that handles all features under the hood via a single entry point (`Main()`), with the help of multiple dispatcher units, namely:
	* **Command dispatcher** for routing and dispatching argument based commands and their parameters.
	* **Update Dispatcher** for executing the scheduled recurring runs of modules (if any).
	* **Display Orchestrator** for signaling views, at the end of execution, to pull and push updated content if any prior change notification was received.
	
* **Display stack:** The display subsystem of GridOS follows a loosely layered GUI design that uses the notion of `controllers`, `views`, `controls`, `models`, etc. A Display Orchestrator governs the list of Display Controllers, all of which represent a separate display stack that drives menu display on a specific ingame `TextSurface`.
	* **Efficient display population:** I reworked the menu display pipeline so that, instead of the traditional "render the full content, then limit it with a viewport" approach, it only processes and renders the menu lines it actually needs to display, and 'scrolling' is imitated by maintaining a line offset relative to the menu item in focus.
	* **Advanced update propagation:** Any displayable change in the menu system automatically propagates to the display in an intelligent manner. Each display stack has a menu model that maintains the currently open menu group, and transmits their change notifications to the menu control. *The menu control executes an update only if the change pertains to a menu item that is actually visible in the viewport.*
	* **Update aggregation:** The changes of individual menu items aren't just blindly pushed to the displays, because this would lead to multiple redraws per cycle if multiple items changed. Instead, the view simply notes that an update is required, and executes the update only once at the end of the execution cycle.
	* **No string allocations:** The entire processing pipeline – including word wrapping, item prefix/suffix addition, assembly of final content, etc. – is carefully designed to avoid string allocations. It uses cached `StringBuilders`, the temporary entities are all `structs`, and multiple pipeline methods stream `IEnumerables` instead of relying on collections (but collections are also cached everywhere of course).
	
* **Composite menu system:** The model part of the display system is mostly based on the Composite pattern, which means that it's a node-based hierarchical structure where nodes can contain additional nodes, creating a tree structure.
	* This node-based system is carefully planned with flexibility in mind:
		* Nodes don't have a defined parent, facilitating scenarios where nodes (e.g. commands) are reused in multiple parts of the tree structure. *(Just be careful not to create circular references.)*
		* All menu node types expose various events to facilitate the implementation event-driven functionality, including groups emitting aggregate notifications on behalf of their children (which in turn drives the update propagation of displays).

## How can you use it

GridOS basically uses the concept of 'modules'. You have to create a module (or multiple) and put your menu items, commands and update method inside. Then you can register an instance of your module via GridOS's `RegisterModule()` method.

- Each module is a separate class that implements the `IModule` interface. Implementing this interface is what makes you able to register the module in the `GridOS` instance. But `IModule` alone doesn't do anything; you need to indicate which features you want to use, by implementing any/all of the following interfaces:

  - `IUpdateSubscriber`: With this interface you can subscribe to recurring automatic execution, which simply requires setting an `UpdateFrequency` and declaring an `Update()` method. You can modify this frequency any time, and the system will adjust. The system dynamically changes the main frequency of the programmable block too, so it runs only when it is actually requested by at least one module.

  - `ICommandPublisher`: With this interface you can publish a list of commands. The methods linked to the commands will be executed when the programmable block receives the commands as an argument.
  
  - `IDisplayElementPublisher`: With this interface you can specify a display element to be shown in GridOS's display system. The display element can be a simple `DisplayElement`, for displaying non-interactive textual information; `DisplayCommand`, for displaying executable commands; and `DisplayGroup`, for creating a node that contains other nodes. This system is extremely flexible; you can create a fully custom hierarchy, and even change it during runtime, since most changes are designed to propage to the screen automatically. The `DisplayGroup` node type lets you subscribe to multiple events, e.g. `BeforeOpen`, so you can be notified when the group is about to be opened (for e.g. refreshing the information elements or command labels).

## Planned Features

- **Overhauled overall framework consumption model:** Instead of the static, compile-time implementation of `IUpdateSubscriber`, etc. interfaces, the framework will expose a service object through which modules can dynamically subscribe to, use, and unsubscribe from framework services during runtime.
- ~~**Shared caching layer:** Currently all composed display system instances (basically the views on LCDs) process and format the displayed content individually. A single-instance, shared caching layer will be added that will store all processed elements, and all "views" will request the processing of elements through this caching layer. So if view A already traversed certain parts of the menu tree, displaying the same parts on view B, view C, etc. will be significantly faster.~~ – Not entirely feasible due to differing line lengths on different displays (since word wrapping is the main processing step). It still could be feasible to implement caching only for displays that happen to use the same settings, but ROI is questionable.
- **Expanded selection of update frequencies:** E.g. 200 ticks, 1-2 minutes, etc. Currently the code modules can set only the vanilla update frequencies (`Update1`, `Update10`, `Update100`, and `None`).
- ~~**Screen Update Aggregation:** Currently all changes in the displayed menu content are directly propagated to the display, which means that if you update multiple display elements in the same execution cycle, you're incurring increasing runtime costs (due to the repetitive processing/formatting of content). An aggregation layer will be added that collects all updates in a given cycle, and applies them in one go at the end of the cycle.~~ – Implemented.
- **Load balancing:** Currently, if e.g. 10 modules are registered for the `Update100` tier, all of them will execute in the same Programmable Block invocation (in the same tick). I'm planning to introduce load-balancing, which will offset each module's running cycle. The tradeoff will be a higher base frequency of the Programmable Block itself. This feature will probably be switchable.
- **Interpreter:** I'm considering the possibility of building an interpreter into GridOS that can be used to define a menu tree - and possibly even commands - via a text field, without writing code modules. This would make it easier to use GridOS for simple informational menus, plus it would facilitate rapid use, and make the system available to non-programmer players.
- <s>**More screens to display, including a configuration screen:** Currently, GridOS' display capability is limited to displaying the menu of the registered commands a hierarchical menu. I'm planning to introduce multiple screens, for example a configuration or a status screen, or possibly giving the ability for each module to publish their own information/configuration screen.</s> Switched to hierarchical, composite node based tree stucture, where each group node serves as a "screen". Multi-display support added.
- **Communication and data sharing between modules:** Currently, the modules are completely separated, but I want to add built-in options for inter-module communication. E.g. a message bus, in which modules can subscribe to topics, and publish payloads on topics.
- **Persistent storage for modules:** At the moment no persistent storage access is available to modules.
- **Exception handling for each module:** The main system will be protected by module exceptions. Either by discarding the malfunctioning module, or by forcing the modules to implement a Reset() method for resetting themselves.

## Getting started using the framework

*(Skip this section if you have Visual Studio and MDK already set up, and it's obvious to you how to add a shared project to your solution.)*

**Environment**

If you wish to utilize this framework in your scripting projects, you should be using:
- [Visual Studio](https://www.visualstudio.com/downloads/), and
- [MDK plugin](https://github.com/malware-dev/MDK-SE).

*The MDK plugin is the only comfortable way currently available for merging multiple project files into a single Programmable Block script.*

**Adding the framework as a shared project**

This project is created as a *shared project* that you can (after cloning or downloading it) add to your existing solutions, by selecting **File > Open > Project/Solution > Add to solution**. Afterwards you need to add a reference to it by right-clicking the **References** node in your own project (in Solution Explorer), then selecting **Add Reference > Shared Projects**, and checking the checkbox in front of **GridOS**.

**Deploying the finished script**

After you've written your script against the framework, and wish to transfer it to SE, right-click the solution node in Solution Explorer, and select **Deploy All MDK Scripts**. This command merges all files, including both the files of the framework and your own project files, into a single script file placed into SE's script folder, ready for selecting it in-game from the list of scripts.

## Example module class with all interfaces implemented

The class below, after instantiating it, and registering it in the `GridOS` instance, will have its `Update()` cycle called according to its `UpdateFrequency` setting. The specified `CommandItem` will be executable from argument, and the specified `DisplayElements` will appear on the system's display.

```csharp
public class ExampleModule : IModule, ICommandPublisher, IUpdateSubscriber, IDisplayElementPublisher
{
    public string ModuleDisplayName { get; } = "Example Module";

    public ObservableUpdateFrequency Frequency { get; } = new ObservableUpdateFrequency(UpdateFrequency.Update100);

    public List<CommandItem> Commands => _commands;
    private List<CommandItem> _commands = new List<CommandItem>();

    public IDisplayElement DisplayElement => _displayElement;
    private DisplayGroup _displayElement = new DisplayGroup("Menu Group");
    private DisplayCommand _myDisplayCommand;

    // Inject your dependencies through the constructor
    public ExampleModule()
    {
        _commands.Add(new CommandItem(
            CommandName: "SomeCommand",
            Execute: ExecuteSomeCommand
        ));

        _displayElement.AddChild(new DisplayElement("This can be any information"));
        // Save reference if you want to modify it later
        _myDisplayCommand = new DisplayCommand("Do something", DoSomething);
        _displayElement.AddChild(_myDisplayCommand);
    }

    public void Update(UpdateType updateType)
    {
        // Do something at each update cycle, call other methods, etc.
        // Modify UpdateFrequency any time if needed
        Frequency.Set(UpdateFrequency.Update10);
    }

    private void ExecuteSomeCommand(CommandItem sender, string param)
    {
        // Do something when command is called via argument
    }

    private void DoSomething()
    {
        // Do something when display command is selected
        _myDisplayCommand.Label = "This will update on the display";
        _displayElement.AddChild(new DisplayElement("This is some new information, dynamically added."));
    }
}
```

## Instantiating the framework and registering a module
The following example shows the current, simplified instantiation of the framework, along with the registration of a single module. The framework uses multiple components as dependencies, but the instantiation of these happens internally, to facilitate ease of use.

```csharp
private GridOS gridOS;

public Program()
{
    IMyTextPanel gridOSDisplay = GridTerminalSystem.GetBlockWithName("GridOSDisplay") as IMyTextPanel;

    gridOS = new GridOS(this);

    // For using display capabilities (optional).
    // Multi-display supported: Multiple textpanels can be registered.
    // Each display has their own view state of the same hierarchical menu system.
    // Each display creates its own unique navigation commands in the internal command registry.
    // Currently the commands for the first registered display are: Display1Up, Display1Down, and Display1Select.
    // Additonal displays added use an incremented version of these commands (e.g. Display2Up, etc.).
    gridOS.RegisterTextPanel(gridOSDisplay);

    ExampleModule exampleModule = new ExampleModule();
    gridOS.RegisterModule(exampleModule);
}

public void Main(string argument, UpdateType UpdateType)
{
    // Simply transfer control to the system, passing all parameters
    gridOS.Main(argument, UpdateType);
}
```
