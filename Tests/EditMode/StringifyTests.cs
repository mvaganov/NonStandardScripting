using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NonStandard.Extension;
using NonStandard.Data;
using NonStandard.Data.Parse;

public class StringifyTests
{
    public class TestClass {
        public string testString = "hello world";
        public int testInt = 5;
        public float testFloat = 3.14f;
        internal string testHidden = "secrets";
        public bool testTrue = true;
        public bool testFalse = false;
        public int testAnotherInt = -10;
        public float testAnotherFloat = -1.41f;
        public string Hidden {
            get => testHidden;
            set => testHidden = value;
        }
    }

    [Test]
    public void TestStringify() {
        // Assign
        TestClass tc = new TestClass();

        // Act
        string s = tc.Stringify();
        //Debug.Log(s);

        // Assert
        Assert.True(s.StartsWith("{"), "starts with a curly brace");
        Assert.True(s.EndsWith("}"), "starts with a curly brace");
        // test that each variable is there, and has the correct value
        Assert.True(s.Contains(nameof(TestClass.testString)), nameof(TestClass.testString) + " found");
        Assert.False(s.Contains(nameof(TestClass.testHidden)), nameof(TestClass.testString) + " hidden");
    }
    [Test]
    public void TestSerializeDeserialize() {
        // Assign
        TestClass tc = new TestClass {
            testAnotherFloat = -1.5f,
            testAnotherInt = -2,
            testFalse = true,
            testFloat = 4.5f,
            testInt = 5,
            testTrue = false,
            testString = "testing 123"
        };
        // Act
        string serialized = tc.Stringify();
        Tokenizer t = new Tokenizer();
        CodeConvert.TryParse(serialized, out TestClass clone, null, t);
        if (t.HasError()) {
            Debug.LogError(t.GetErrorString());
        }
        //Debug.Log(serialized + "\n--\n" + clone.Stringify());
        // Assert
        Assert.True(clone.Stringify() == serialized, "deserialized class stringifies to the same string");
        // test that each member of the clone is the same as the original
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator EmptyEnumeratorTest()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
