# Text Event Toolkit

## Setup:
Install the package.

Set up code to trigger events.

Write action methods.

Create events in the editor window

### Triggering events:
Events can be created by calling `TextEvent.CreateRandom()`, `TextEvent.CreateRandomWithLabel(string)` or `CreateFromIndex(int)`.
Once an event is created it can be presented to the player by calling `EnterEvent()`

#### Example
```C#
TextEvent.CreateRandom().EnterEvent();
```
or
```C#
TextEvent.CreateRandomWithLabel("Combat").EnterEvent();
```

### Action system:
Methods can be exposed to the editor window by decorating them with the TextEventAction atribute, passing in a friendly name for the action.
int, float bool and string arguments are supported, the name and default values of the arguments are also exposed to the editor window. These methods can be
set in the editor window to be triggered with a result.

#### Example:

```C#
using DevonMillar.TextEvents;

public class TextEventActions
{
    static Player player;

    [TextEventAction("Damage player")]
    public static void DamagePlayer(float damage = 5.0f)
    {
        player.Damage(damage);
    }
    [TextEventAction("Give item")]
    public static void GiveItem(string item)
    {
        player.GiveItem(item);
    }
    [TextEventAction("Add status effect")]
    public static void AddStatusEffect(string effect)
    {
        player.AddStatusEffect(effect);
    }
    [TextEventAction("Remove status effect")]
    public static void RemoveStatusEffect(string effect)
    {
        player.RemoveStatusEffect(effect);
    }
}

```
Actions can now be added to results in the editor window

![image](https://user-images.githubusercontent.com/76901281/227108543-d871eefb-c659-44b0-882e-266998e613a9.png)

## Designing events
The text event editor can be opened by clicking the "Text Event Editor" button in the Tools menu.

### Structure of an event: 
Events contain choices, choices contain any number of results. Results contain any number of actions.
A result is chosen at random when a choice is picked by the player.

Event > Choice > Result > Actions
#### Choices
Choices are the options that are given to the player in the event. Choices can contain any number of results. Choices can be folded in the editor
![image](https://user-images.githubusercontent.com/76901281/227114492-1e80ba16-7580-461f-9da7-efb6bee279f3.png)

#### Results
One child result is chosen at random when a choice is picked by the player. 

Each result has a "Chance" variable that affects the chance of it being selected.
A result can contain it's own text and any number of actions which will be executed if chosen. Results text is shown to the player if picked. If a result has no text the event will be exited as soon as it is chosen. Results can also contain other choices for branching choice events.
![image](https://user-images.githubusercontent.com/76901281/227114377-fbc11e27-1345-4f80-91b4-73550df6aad6.png)

### Actions
Actions are the effects that a result has. These are set up per project. They are executed when the parent result is chosen.
![image](https://user-images.githubusercontent.com/76901281/227114036-05e0de49-5d82-4564-a99d-81290fb24739.png)

#### The editor window
![image](https://user-images.githubusercontent.com/76901281/227111877-8d4df190-2ad2-4489-b4ce-6da0836b79fd.png)

### Limitations:
Action methods being removed from the codebase while they are in use will throw when trying to trigger the action

The editor tool may need to be closed and reopened on scirpt compilation.

Action methods must be public and static, and their arguments only support int, float, bool, string
