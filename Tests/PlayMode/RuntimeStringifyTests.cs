using System.Collections;
using System.Collections.Generic;
using NonStandard.Extension;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RuntimeStringifyTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void EmptyTest()
    {
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator StringifyOverTime()
    {
        string s = this.Stringify();
        yield return new WaitForSeconds(.125f);
        string test = this.Stringify();
        Assert.True(s == test, s+" is the same as "+test);
        yield return new WaitForSeconds(.125f);
        test = this.Stringify();
        Assert.True(s == test, s + " is the same as " + test);
        yield return new WaitForSeconds(.125f);
        test = this.Stringify();
        Assert.True(s == test, s + " is the same as " + test);
        yield return new WaitForSeconds(.125f);
        test = this.Stringify();
        Assert.True(s == test, s + " is the same as " + test);
        yield return new WaitForSeconds(.125f);
        yield return null;
    }
}
