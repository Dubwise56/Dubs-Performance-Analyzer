# How to Use

## Lingo
![Lingo](About/Identification.PNG)

## Some Basic Statistics
Rimworld's update cycle is broken up into units called *ticks*, this is the basis for the speed at which things change in your game. When you adjust the game speed in the bottom right, you increase the amount of ticks the game tries to execute per second. At 1x it tries to reach 60tps, 2x 180tps, 3x 360, and the dev mode 4x, or the Smart Speed mod, 900. These numbers get doubled when all player controlled pawns on a map are sleeping.

With a little math, we can see that to reach 60 fps, we need each frame to take less than <img src="https://render.githubusercontent.com/render/math?math=16 {2\over3}ms"> per *update* to reach 60fps. This means at `3x` speed, to reach 60 fps, each tick needs to be *on average* below <img src="https://render.githubusercontent.com/render/math?math=2 {7\over9}ms">.Any spikes in *tick* will likely be detriment to FPS. However, if the spikes are irregular, it may not slow down gameplay.

The game will always try to run at 60 updates per second, this means, that ticks are independent to updates. Which is true, there can be multiple ticks in a single frame. This is the true difference between the Tick category and the other categories. The entries which are shown in the Tick category can update more or less frequently given your game speed. Keeping in mind, if each tick takes to long, the game will automatically throttle your TPS to stabilise your FPS.

When taking into account numbers from the analyzer, recognise which Category the reading is coming from. An average of ~2ms in the tick category, when adjusted for higher speeds, is worse than an average of ~3ms in the category section. 

## Reading the Display
If your issue is inconsistent FPS with a semi stable TPS, you should be focusing on the **max** value for logs. As spikes can effect your FPS without consistently dragging down your TPS.

If your issue is steady yet low FPS/TPS, you should be focusing on the **average** value for the logs. 

When looking at logs in the row format, the coloured bar indicates the percentage makeup of in the current entry.   

Reading the graph

## Tick vs GUI vs Update
Dumbed down version

## Finding the mod a method is from by using the side panel

# Basic Troubleshooting

## Common Offenders

- Pawn.Tick
- etc

# Advanced Usage

## Linking Analyzer to Dnspy
In the analyzer settings there is a box you can fill in, which will 'link' dnspy to analyzer, this will allow you to directly open methods from inside Analyzer from any mod in dnspy.

Provide the absolute path to (and including) the dnspy.exe. This allows it to be accessed via the command line.

[Add Gif going from in game -> dnspy]

## Internal Profiling
You can right click the logs themselves, this will allow you to internal profile the method, which will show the methods which comprise the method you are profiling, and the time it takes to execute them.

[Add image]

## Custom Profiling
Using the analyzer you have a variety of ways in which you can profile methods. These are displayed on the main dev page

[Add image]

You can patch:
- Method (A method - non generic)
- Type (All the methods inside a type)
- Patches on a Method (All the Harmony Patches which effect a method)
- Patches on a Type (All the Harmony Patches which effect the methods of a type)
- Method Internals (Internal method profiling on a given method)
- Mod/Assembly (Patch all the methods implemented in an assembly)

# For Modders

## Using the Analyzer.xml
Using xml file allows you to pre-make a tab in the analyzer for your mod, this simply prevents you having to repatch the same methods to profile when developing a mod. The file format looks like
```xml
<?xml version="1.0" encoding="utf-8"?>
<Analyzer>
    <tabName>
        <Types>
            <li>Verse.Pawn</li>
        </Types>
        <Methods>
            <li>Verse.Thing:Tick</li>
        </Methods>
    </tabName>
</Analyzer>
```
This will create a tab called `tabName` which will be profiling all the methods in the type `Verse.Pawn`, and the method `Verse.Thing:Tick`

This file should be placed in the root directory of your mod, if you wish to hide it from users you can strip in from a release build. However its a good way to counter arguments that your mod doesn't perform well.

## Predicted Overhead
The act of profiling obviously takes time, and this is no different here, but lets look at what is done between the profiler starting, and it ending. 

When you call `Foo`, the basic execution if it has been patched will look like

```
Execute Prefixes...
Foo()
Execute Postfixes...
```
Analyzer aims to insert a Prefix just before the execution of Foo, and a postfix just after. This uses the harmony priority system, and thus is fallible to other modders using high harmony priority on their patches.

In the prefix, the profiling state is found, which contains the stopwatch. In the postfix, the profiling state is accesed via the special harmony parameter `__state`, and the timer is stopped. This looks like
```csharp
[HarmonyPriority.Lowest]
public static void Prefix(MethodBase __originalMethod, ref Profiler __state)
{
    __state = Profiler.FindAndGetProfilerFor(__originalMethod); // dictionary lookup
    __state.Start(); // start our stopwatch
}

// execute Foo();

[HarmonyPriority.Highest]
public static void Postfix(Profiler __state)
{
    __state.Stop(); // hopefully inlined method
}
```

The overhead for this varies per user, obviously, but in general, it incurs cost on the speed of your program, but the actual noise in the profiling is kept to a minimum. Using the special `__state` parameter from harmony prevents a dictonary lookup being added to the time of your methods execution. 

## Technical Explanations

### Tick Vs Update Methods
In Analyzer there are two different types of 'updates'. One called `UpdateCycle` and the other is `FinishUpdateCycle`. The FinishUpdateCycle is called a varying amount of times per second, and it is responsible for spawning the thread where the logic is done on for your logs. This can be done up to 20 times a second (set inside the settings for the mod).

`UpdateCycle` however is determined by the Category you are in, and is responsible for the difference in speed of the graph when viewing an entry from the Tick category vs the Update one. Any category aside from Tick updates when `Root_Play.Update` completes. This symbolises one 'update' cycle within the game. Tick however, is updated every time `TickManager.DoSingleTick` completes. This can happen multiple times per update, which accounts for the update-speed difference.

This change will also effect the total time and number of calls for a method over a given time period. Because it *can* be split depending on your game speed. You can profile anything as a Tick method vs an Update method.

### Method Switching (Transpiler and Internal Profiling)

In order for transpiler profiling and internal method profiling to work, there needs to be a process to 'profile' internal components of a method. This is done by parsing the IL and finding all instructions which are either `Call` or `CallVirt` and measuring the time it takes for the methods called inside them to execute.

This process looks roughly like this
```csharp
public static int MyTargetMethod(int param1, bool param2)
{
    // ...
    CallFoo(param1, local2);
    // ...
}

public static void CallFoo(int param2, int local2)
{
    // ...
}

```
after modification

```csharp
public static int MyTargetMethod(int param1, bool param2)
{
    // ...
    CallFoo_runtimeReplacement(param1, local2);
    // ...
}

public static void CallFoo_runtimeReplacement(int param2, int local2)
{
    Stopwatch.Start();
    CallFoo(param2, local2);
    Stopwatch.End();
    return; // The value which is currently on the stack will be returned if applicable
}
```

The `Stopwatch.Start();` above is simplified because the process here is specific to how Analyzer collects data on methods, and irrelevant for the example.

The implementation used for internal method profiling is [here](https://github.com/simplyWiri/Dubs-Performance-Analyzer/blob/rework/Source/Refactored/Utility/InternalMethodUtility.cs) `todo: Make point to local point in repo`

### Transpiler Profiling

Transpiler profiling is done using a relatively simple approach. The original methods IL is compared to the current IL (after all transpilers have been applied) and a diff algorithm is applied. 

For each of the added IL instructions which are of the type `Call` or `CallVirt`, the method it calls is swapped out with a *profiling method* (as described above). 

The ***sum*** of these calls is considered the 'added' weight by the transpiler(s). This obviously does not handle all cases. I.e. Adding for loops / while loops using branches, inserting instructions in for loops etc. This will also collate all transpilers on a method, if you are trying to get accurate measurements on individual transpilers individually as a modder, I'd suggest only running the mod which adds it, or looking at the source code using a decompiler like dnspy. 

### Internal Method Profiling

Internal method profling is done by iterating through the IL of a method, and for each of the `Call` or `CallVirt` instructions, swapping the operand method out as described above.

Because this swaps out the instruction, instead of replacing the method, it will only profile the method when it is called from inside the original method you are internal profiling. Otherwise your profiling will pick up every single call to the method.
 
 <!-- It will often reveal that LINQ =
<img src="https://render.githubusercontent.com/render/math?math=\sqrt{evil}"> 
:) -->


## Edge Cases

### IEnumerable
If you are seeing spikes from relatively simple postfixes which deal with IEnumerables, keep in mind how they work. This section is only relevant when the IEnumerable is being frontloaded. If you are doing a Postfix wherein a parameter you are checking is an IEnumerable, and you are doing a call like `.Any()` or `.Select`, your postfix will be forcing the calculation for the enumerable, thus the time will be attributed to your postfix. As well, each IEnumerator generated from the IEnumerable will have to repeat the calculation, which is something you can avoid.
