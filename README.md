# NProg

NProg is a general-purpose progress tracker for .NET. It is useful in batch processing applications where you want to periodically display, log, or otherwise process overall progress.

## Example

```C#
using NProg;

var tracker = new Tracker(workItems.Count);
tracker.Every(100.ItemsDone(), prog => Console.WriteLine($"{prog.ItemsDone} items done"));
tracker.Every(10.Seconds(), prog => Console.WriteLine($"{prog.ItemsDone} items done"));
tracker.On(50.PercentDone(), prog => Console.WriteLine("Half way done"));
tracker.OnComplete(prog => Console.WriteLine($"Finished in {prog.ElapsedSeconds} seconds"));

tracker.Start();
foreach (var item in workItems) {
	tracker.ItemStarted();
	try {
		ProcessItem(item);
		tracker.ItemSucceeded();
	}
	catch (Exception ex) {
		tracker.ItemFailed();
		// log exception
	}
}
```

## Setup

As the example shows, you set up a tracker by declaring the total item count and creating triggers and actions with the `Every` (periodic) and `On` (one-time) methods. Extension methoods on `int` help you create the triggers:

```C#
i.ItemsStarted()
i.ItemsDone()
i.ItemsSucceeded()
i.ItemsFailed()
i.PercentStarted()
i.PercentDone()
i.PercentSucceeded()
i.PercentFailed()
i.Seconds()
i.Minutes()
i.Hours()
```

The last argument to `Every` and `On` is an `Action<Progress>`. `Progress` provides a wealth of information useful for displaying, logging, etc:

```c#
int TotalItems { get; }
int ItemsStarted { get; }
int ItemsSucceeded { get; }
int ItemsFailed { get; }

int ItemsDone { get; }        // succeeded + failed
int ItemsInProgress { get; }  // started - done
int ItemsRemaining { get; }   // total - started
bool IsDone { get; }          // done == total

// rounded down
int PercentStarted { get; }
int PercentDone { get; }
int PercentSucceeded { get; }
int PercentFailed { get; }
int PercentInProgress { get; }
int PercentRemaining { get; }

// more precise
float PercentStartedExact { get; }
float PercentDoneExact { get; }
float PercentSucceededExact { get; }
float PercentFailedExact { get; }
float PercentInProgressExact { get; }
float PercentRemainingExact { get; }

// elapsed time
TimeSpan ElapsedTime { get; }
int ElapsedSeconds { get; }
int ElapsedMinutes { get; }
int ElapsedHours { get; }

// completion time estimates
TimeSpan EstTotalTime { get; }
TimeSpan EstTimeRemaining { get; }
DateTime EstEndTimeUtc { get; }
DateTime EstEndTimeLocal { get; }
```

## Run

Referring again to the example, `Start`, `ItemStarted`, `ItemSucceeded`, `ItemFailed`, and `Stop` are the main methods you will call on `Tracker` during the course of a batch job.

`Start` and `Stop` are optional. For time tracking, `Progress` defaults to when the `Tracker` was created and when the last item was completed for its elapsed time calculations, but you can call these methods if you want more precise control over those values.

`ItemStarted`, `ItemSucceeded`, `ItemFailed` are all thread-safe. In concurrent environments, you can call them on the same `Tracker` instance from multiple threads without conflicts such as the same trigger firing twice. `ItemStarted` is optional if you only want to track "done" items and not "started" or "in progress".
    
## Get it

This project is brand new and still in the experimental phase. It is not available on NuGet (yet), but for convenience it is completely contained in a single C# file that you can drop into your project. Get it here.

## Questions? Feedback?

Like it? Hate it? Found a bug? Got a feature request? Did I reinvent something that already exists? For time being I welcome any and all feedback under issues.
