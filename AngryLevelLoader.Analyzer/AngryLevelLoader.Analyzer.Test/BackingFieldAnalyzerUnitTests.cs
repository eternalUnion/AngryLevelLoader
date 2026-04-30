using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = AngryLevelLoader.Analyzer.Test.CSharpCodeFixVerifier<
	AngryLevelLoader.Analyzer.BackingFieldUsageAnalyzer,
	AngryLevelLoader.Analyzer.BackingFieldAnalyzerCodeFixProvider>;

namespace AngryLevelLoader.Analyzer.Test
{
	[TestClass]
	public class BackingFieldAnalyzerUnitTests
	{
		[TestMethod]
		public async Task TestWithProperty()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;

class Program
{
    private int _backingField;
    public int BackingField
    {
        get => _backingField;
        set => _backingField = value;
    }

    void Main()
    {
        [|_backingField|] = 0;
    }
}
",
// Output:
@"
using System;

class Program
{
    private int _backingField;
    public int BackingField
    {
        get => _backingField;
        set => _backingField = value;
    }

    void Main()
    {
        BackingField = 0;
    }
}
");
		}

		[TestMethod]
		public async Task TestCorrectPropertyOnly()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;

class Program
{
    private int _backingField;
    public int BackingField
    {
        get => _backingField;
        set => _backingField = value;
    }

    public int OtherBackingField
    {
        get => [|_backingField|];
        set => [|_backingField|] = value;
    }
}
",
// Output:
@"
using System;

class Program
{
    private int _backingField;
    public int BackingField
    {
        get => _backingField;
        set => _backingField = value;
    }

    public int OtherBackingField
    {
        get => BackingField;
        set => BackingField = value;
    }
}
");
		}

        [TestMethod]
        public async Task TestNoPropertyAllow()
        {
            await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;

class Program
{
    private int _backingField;
    
    void Main()
    {
        _backingField = 0;
    }
}
",
// Output:
@"
using System;

class Program
{
    private int _backingField;
    
    void Main()
    {
        _backingField = 0;
    }
}
");
        }

		[TestMethod]
		public async Task TestFieldUsedAsExpression()
		{
			await VerifyCS.VerifyCodeFixAsync(
// Input:
@"
using System;

class Program
{
    private int _backingField;
    public int BackingField
    {
        get => _backingField;
        set => _backingField = value;
    }

    void Foo(int i) {}
    
    void Main()
    {
        Foo([|_backingField|]);
    }
}
",
// Output:
@"
using System;

class Program
{
    private int _backingField;
    public int BackingField
    {
        get => _backingField;
        set => _backingField = value;
    }

    void Foo(int i) {}
    
    void Main()
    {
        Foo(BackingField);
    }
}
");
			}
	}
}
