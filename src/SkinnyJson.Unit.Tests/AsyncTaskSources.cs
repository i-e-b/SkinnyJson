using System;
using System.Threading.Tasks;
using NUnit.Framework;
// ReSharper disable InconsistentNaming
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace SkinnyJson.Unit.Tests;

[TestFixture]
public class AsyncTaskSources
{
    [Test]
    public void completed_tasks_are_unpacked_when_passed_to_Freeze()
    {
        var obj        = new { One = "one", Two = "two" };
        var fromResult = Task.FromResult(obj);

        var result = Json.Freeze(fromResult);

        Assert.That(result, Is.EqualTo(fix("{'One':'one','Two':'two'}")));
    }

    [Test]
    public void pending_tasks_are_awaited_and_unpacked_when_passed_to_Freeze()
    {
        var obj = new { One = "one", Two = "two" };

        var result = Json.Freeze(TaskPack(obj));

        Assert.That(result, Is.EqualTo(fix("{'One':'one','Two':'two'}")));
    }

    [Test]
    public void slow_tasks_cause_an_exception_when_passed_to_Freeze()
    {
        var obj = new { One = "one", Two = "two", Three = SlowTask("three") };

        var result = Assert.Throws<Exception>(() => Json.Freeze(obj));

        Console.WriteLine(result);
        Assert.That(result.Message, Is.EqualTo("Failed to encode 'Three'"));
    }

    private async Task<object> TaskPack(object o)
    {
        await Task.Delay(1);
        return o;
    }

    private async Task<object> SlowTask(object o)
    {
        await Task.Delay(TimeSpan.FromSeconds(30));
        return o;
    }

    static string fix(string s) { return s.Replace("'", "\""); }
}