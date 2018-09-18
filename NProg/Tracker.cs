using System;
using System.Collections.Generic;
using System.Linq;

namespace NProg
{
	public class Tracker
	{
		private long _startTime = DateTime.UtcNow.Ticks;
		private long _endTime;
		private int _total;
		private int _started;
		private int _succeeded;
		private int _failed;

		private List<ProgressAction> _actions = new List<ProgressAction>();

		public Tracker(int itemCount) => _total = itemCount;
		public void Start() => _startTime = DateTime.UtcNow.Ticks;
		public void Stop() => _endTime = DateTime.UtcNow.Ticks;

		public void Every(Trigger trigger, Action<Progress> action) => _actions.Add(new ProgressAction { Trigger = trigger, Invoke = action, Recurring = true });
		public void On(Trigger trigger, Action<Progress> action) => _actions.Add(new ProgressAction { Trigger = trigger, Invoke = action, Recurring = false });
		public void Every(TimeSpan interval, Action<Progress> action) => Every(new Trigger(interval.Ticks, p => p.ElapsedTime.Ticks), action);
		public void OnComplete(Action<Progress> action) => On(_total.ItemsDone(), action);
		public void Now(Action<Progress> action) => action(GetProgress());

		private readonly object _lock = new object();

		public void ItemStarted() => ProcessTriggers(ref _started);
		public void ItemSucceeded() => ProcessTriggers(ref _succeeded);
		public void ItemFailed() => ProcessTriggers(ref _failed);

		public Progress GetProgress() => new Progress(_startTime, _endTime, _total, _started, _succeeded, _failed);

		private void ProcessTriggers(ref int fieldToIncrement) {
			var fired = new List<ProgressAction>();
			Progress prog;

			lock (_lock) {
				fieldToIncrement++;
				prog = GetProgress();
				fired = _actions.Where(act => act.Trigger.IsFired(prog)).ToList();

				foreach (var act in fired) {
					if (!act.Recurring)
						_actions.Remove(act);
				}
			}

			fired.ForEach(act => act.Invoke(prog));
		}

		private class ProgressAction
		{
			public Trigger Trigger { get; set; }
			public Action<Progress> Invoke { get; set; }
			public bool Recurring { get; set; }
		}
	}

	public class Trigger
	{
		private readonly long _targetNumber;
		private readonly Func<Progress, long> _getCurrentNumber;
		private long _nextNumber;

		public Trigger(long targetNumber, Func<Progress, long> getCurrentNumber) {
			_nextNumber = _targetNumber = targetNumber;
			_getCurrentNumber = getCurrentNumber;
		}

		public bool IsFired(Progress prog) {
			if (_getCurrentNumber(prog) >= _nextNumber) {
				_nextNumber += _targetNumber;
				return true;
			}
			return false;
		}
	}

	public static class TriggerBuilderExtensions
	{
		public static Trigger ItemsStarted(this int i) => new Trigger(i, p => p.ItemsStarted);
		public static Trigger ItemsDone(this int i) => new Trigger(i, p => p.ItemsDone);
		public static Trigger ItemsSucceeded(this int i) => new Trigger(i, p => p.ItemsSucceeded);
		public static Trigger ItemsFailed(this int i) => new Trigger(i, p => p.ItemsFailed);

		public static Trigger PercentStarted(this int i) => new Trigger(i, p => p.PercentStarted);
		public static Trigger PercentDone(this int i) => new Trigger(i, p => p.PercentDone);
		public static Trigger PercentSucceeded(this int i) => new Trigger(i, p => p.PercentSucceeded);
		public static Trigger PercentFailed(this int i) => new Trigger(i, p => p.PercentFailed);

		public static TimeSpan Seconds(this int i) => TimeSpan.FromSeconds(i);
		public static TimeSpan Minutes(this int i) => TimeSpan.FromMinutes(i);
		public static TimeSpan Hours(this int i) => TimeSpan.FromHours(i);
	}

	public class Progress
	{
		private readonly long _startTime;
		private readonly long _endTime;

		public int TotalItems { get; }
		public int ItemsStarted { get; }
		public int ItemsSucceeded { get; }
		public int ItemsFailed { get; }

		public int ItemsDone => ItemsSucceeded + ItemsFailed;
		public int ItemsInProgress => ItemsStarted - ItemsDone;
		public int ItemsRemaining => TotalItems - ItemsStarted;

		public int PercentStarted => 100 * ItemsStarted / TotalItems;
		public int PercentDone => 100 * ItemsDone / TotalItems;
		public int PercentSucceeded => 100 * ItemsSucceeded / TotalItems;
		public int PercentFailed => 100 * ItemsFailed / TotalItems;
		public int PercentInProgress => 100 * ItemsInProgress / TotalItems;
		public int PercentRemaining => 100 * ItemsRemaining / TotalItems;

		public float PercentStartedExact => (float)100 * ItemsStarted / TotalItems;
		public float PercentDoneExact => (float)100 * ItemsDone / TotalItems;
		public float PercentSucceededExact => (float)100 * ItemsSucceeded / TotalItems;
		public float PercentFailedExact => (float)100 * ItemsFailed / TotalItems;
		public float PercentInProgressExact => (float)100 * ItemsInProgress / TotalItems;
		public float PercentRemainingExact => (float)100 * ItemsRemaining / TotalItems;

		private long ElapsedTicks => (_endTime == 0 ? DateTime.UtcNow.Ticks : _endTime) - _startTime;

		public TimeSpan ElapsedTime => TimeSpan.FromTicks(ElapsedTicks);
		public int ElapsedSeconds => (int)ElapsedTime.TotalSeconds;
		public int ElapsedMinutes => (int)ElapsedTime.TotalMinutes;
		public int ElapsedHours => (int)ElapsedTime.TotalHours;

		public TimeSpan EstTotalTime => TimeSpan.FromTicks(ElapsedTicks * TotalItems / ItemsDone);
		public TimeSpan EstTimeRemaining => EstTotalTime - ElapsedTime;
		public DateTime EstEndTimeUtc => DateTime.UtcNow + EstTimeRemaining;
		public DateTime EstEndTimeLocal => DateTime.Now + EstTimeRemaining;

		public bool IsDone => ItemsDone == TotalItems;

		public Progress(long startTime, long endTime, int total, int started, int succeeded, int failed) {
			_startTime = startTime;
			_endTime = endTime;
			TotalItems = total;
			ItemsStarted = started;
			ItemsSucceeded = succeeded;
			ItemsFailed = failed;
		}
	}
}
