using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NProg;

namespace NProg.Demo
{
	class Program
	{
		static void Main(string[] args) {
			var workItems = Enumerable.Range(1, 2001).ToList();

			var tracker = new Tracker(workItems.Count);
			tracker.Every(100.ItemsDone(), LogCount);
			tracker.Every(25.PercentDone(), LogPercent);
			tracker.Every(10.Seconds(), LogSeconds);
			tracker.On(50.PercentDone(), prog => Console.WriteLine("hey we're half done!"));
			tracker.OnComplete(LogDone);

			tracker.Start();
			foreach (var item in workItems) {
				tracker.ItemStarted();
				try {
					ProcessItem(item);
					tracker.ItemSucceeded();
				}
				catch (Exception) {
					tracker.ItemFailed();
				}
			}
			Console.ReadLine();
		}

		private static void ProcessItem(int item) {
			Thread.Sleep(10);
			if (item == 999)
				throw new Exception("this one failed!");
		}

		private static void LogCount(Progress prog) {
			Console.WriteLine($"\t{prog.ItemsDone} of {prog.TotalItems} done ({prog.PercentDone}%)");
		}

		private static void LogPercent(Progress prog) {
			Console.WriteLine($"{prog.PercentDone}% done. estimated competion time: {prog.EstEndTimeLocal:h:mm:ss tt}");
		}

		private static void LogSeconds(Progress prog) {
			Console.WriteLine($"{prog.ElapsedSeconds} seconds elapsed, {prog.ItemsDone} of {prog.TotalItems} done");
		}

		private static void LogDone(Progress prog) {
			Console.WriteLine($"Done! Completed in {prog.ElapsedSeconds} seconds, {prog.ItemsSucceeded} succeeded, {prog.ItemsFailed} failed.");
		}
	}
}
