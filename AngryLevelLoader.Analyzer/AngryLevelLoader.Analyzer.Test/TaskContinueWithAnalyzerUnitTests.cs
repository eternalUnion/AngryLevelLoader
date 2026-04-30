using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using VerifyCS = AngryLevelLoader.Analyzer.Test.CSharpCodeFixVerifier<
	AngryLevelLoader.Analyzer.TaskContinueWithAnalyzer,
	AngryLevelLoader.Analyzer.TaskContinueWithAnalyzerCodeFixProvider>;

namespace AngryLevelLoader.Analyzer.Test
{
	[TestClass]
	public class TaskContinueWithAnalyzerUnitTest
	{
		[TestMethod]
		public async Task TestWithNoArg()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        [|task.ContinueWith((t) => {})|];
    }
}
",
// Output:
@"
using System;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        task.ContinueWith((t) => {}, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
");
		}

		[TestMethod]
		public async Task TestWithNullArg()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        task.ContinueWith((t) => {}, [|null|]);
    }
}
",
// Output:
@"
using System;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        task.ContinueWith((t) => {}, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
");
		}

		[TestMethod]
		public async Task TestWithIncorrectArg()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        task.ContinueWith((t) => {}, [|TaskScheduler.Default|]);
    }
}
",
// Output:
@"
using System;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        task.ContinueWith((t) => {}, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
");
		}

		[TestMethod]
		public async Task TestWithIncorrectSignature()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        [|task.ContinueWith((t) => {}, new CancellationToken())|];
    }
}
",
// Output:
@"
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    void Main()
    {
        Task task = Task.Delay(0);
        task.ContinueWith((t) => {}, new CancellationToken(), TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
    }
}
");
		}
	}
}
