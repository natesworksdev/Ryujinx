using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<Ryujinx.Analyzers.CatchClauseAnalyzer,
        Ryujinx.Analyzers.CatchClauseCodeFixProvider>;

namespace Ryujinx.Tests.Analyzers
{
    public class CatchClauseCodeFixProviderTests
    {
        private static readonly string _loggerTextPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Fixtures", "CatchClauseLogger.cs");

        private static readonly string _loggerText = File.ReadAllText(_loggerTextPath);

        [Fact]
        public async Task CatchWithoutDeclaration_LogException()
        {
            string text = _loggerText + @"
public class MyClass
{
    public void MyMethod1()
    {
        try
        {
            Console.WriteLine(""test"");
        }
        catch
        {
            // ignored
        }
    }
}
";

            string newText = _loggerText + @"
public class MyClass
{
    public void MyMethod1()
    {
        try
        {
            Console.WriteLine(""test"");
        }
        catch (System.Exception exception)
        {
            Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Application, $""Exception caught: {exception}"");
            // ignored
        }
    }
}
";

            var expected = Verifier.Diagnostic()
                .WithSpan(89, 9, 92, 10)
                .WithArguments("Exception");
            await Verifier.VerifyCodeFixAsync(text, expected, newText).ConfigureAwait(false);
        }

        [Fact]
        public async Task CatchWithoutIdentifier_LogException()
        {
            string text = _loggerText + @"
public class MyClass
{
    public void MyMethod2()
    {
        try
        {
            Console.WriteLine(""test"");
        }
        catch (NullReferenceException)
        {
            // ignored
        }
    }
}
";

            string newText = _loggerText + @"
public class MyClass
{
    public void MyMethod2()
    {
        try
        {
            Console.WriteLine(""test"");
        }
        catch (NullReferenceException exception)
        {
            Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Application, $""Exception caught: {exception}"");
            // ignored
        }
    }
}
";

            var expected = Verifier.Diagnostic()
                .WithSpan(89, 9, 92, 10)
                .WithArguments("NullReferenceException");
            await Verifier.VerifyCodeFixAsync(text, expected, newText).ConfigureAwait(false);
        }

        [Fact]
        public async Task LogWithoutCatchIdentifier_LogException()
        {
            string text = _loggerText + @"
public class MyClass
{
    public void MyMethod3()
    {
        try
        {
            Console.WriteLine(""test"");
        }
        catch (ArgumentException ex)
        {
            Ryujinx.Common.Logging.Logger.Info?.Print(Ryujinx.Common.Logging.LogClass.Application, ""test"");
        }
    }
}
";

            string newText = _loggerText + @"
public class MyClass
{
    public void MyMethod3()
    {
        try
        {
            Console.WriteLine(""test"");
        }
        catch (ArgumentException ex)
        {
            Ryujinx.Common.Logging.Logger.Error?.Print(Ryujinx.Common.Logging.LogClass.Application, $""Exception caught: {ex}"");
            Ryujinx.Common.Logging.Logger.Info?.Print(Ryujinx.Common.Logging.LogClass.Application, ""test"");
        }
    }
}
";

            var expected = Verifier.Diagnostic()
                .WithSpan(89, 9, 92, 10)
                .WithArguments("ArgumentException");
            await Verifier.VerifyCodeFixAsync(text, expected, newText).ConfigureAwait(false);
        }
    }
}
