﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NProg;

namespace NProg.Test
{
	[TestFixture]
	public class Tests
	{
		[Test]
		public void item_tracking_works() {
			var logCount = 0;
			var itemsStarted = 0;
			var percentDone = 0;

			var tracker = new Tracker(5001);

			// should be called 25 times
			tracker.Every(200.ItemsStarted(), prog => {
				itemsStarted += 200;
				Assert.AreEqual(itemsStarted, prog.ItemsStarted);
				Assert.AreEqual(1, prog.ItemsInProgress);
				Assert.AreEqual(itemsStarted - 1, prog.ItemsDone);
				Assert.AreEqual(itemsStarted - 1, prog.ItemsSucceeded);
				Assert.AreEqual(0, prog.ItemsFailed);
				Assert.AreEqual(5001 - itemsStarted, prog.ItemsRemaining);
				logCount++;
			});

			// should be called 4 times
			tracker.Every(25.PercentDone(), prog => {
				percentDone += 25;
				Assert.AreEqual(percentDone, prog.PercentDone);
				Assert.AreEqual(5000 * percentDone / 100 + 1, prog.ItemsDone);
				logCount++;
			});

			// should be called 1 time
			tracker.On(50.PercentDone(), _ => logCount++);

			// should be called 1 time
			tracker.OnComplete(prog => {
				Assert.AreEqual(5001, prog.ItemsDone);
				logCount++;
			});

			for (var i = 0; i < 5001; i++) {
				tracker.ItemStarted();
				// process item
				tracker.ItemSucceeded();
			}

			Assert.AreEqual(31, logCount);
		}

		[Test]
		public void time_tracking_works() {
			var count = 0;

			var tracker = new Tracker(9);
			tracker.Every(TimeSpan.FromMilliseconds(100), prog => count++);

			Thread.Sleep(200); // timer shouldn't fire yet

			tracker.Start();
			for (var i = 0; i < 10; i++) {
				Thread.Sleep(100);
			}
			tracker.Stop();

			Thread.Sleep(200); // timer should have stopped firing

			Assert.AreEqual(9, count);
			// 1000 ms and change should have elapsed, just check fo 9xx
			Assert.GreaterOrEqual(9, tracker.GetProgress().ElapsedTime.Milliseconds / 100);
		}

		[Test]
		public void tracker_is_thread_safe() {
			var counts = new List<int>();

			var tracker = new Tracker(10);
			tracker.Every(1.ItemsDone(), prog => counts.Add(prog.ItemsDone));

			Task.WaitAll(
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()),
				Task.Run(() => tracker.ItemSucceeded()));

			Assert.AreEqual(8, counts.Count);
			CollectionAssert.Contains(counts, 1);
			CollectionAssert.Contains(counts, 2);
			CollectionAssert.Contains(counts, 3);
			CollectionAssert.Contains(counts, 4);
			CollectionAssert.Contains(counts, 5);
			CollectionAssert.Contains(counts, 6);
			CollectionAssert.Contains(counts, 7);
			CollectionAssert.Contains(counts, 8);
		}

		[Test]
		public void now_runs_immediately() {
			var tracker = new Tracker(10);
			var nowRan = false;
			tracker.Now(prog => nowRan = true);
			Assert.IsTrue(nowRan);
		}

		[Test]
		public async Task async_tracking_works() {
			var tracker = new Tracker(10);
			var counter = 0;

			tracker.Every(2.ItemsDone(), async prog => {
				await Task.Delay(1000);
				counter++;
			});

			for (var i = 0; i < 10; i++)
				tracker.ItemSucceeded();

			// counter should get incremeted 5 times, but not until we await completion
			Assert.AreEqual(0, counter);
			await tracker.CompleteAsync();
			Assert.AreEqual(5, counter);
		}
	}
}
