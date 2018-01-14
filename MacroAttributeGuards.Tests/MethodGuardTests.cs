using System;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace
MacroAttributeGuards.Tests
{


[TestClass]
public class
MethodGuardTests
{


[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void
Argument_Null_Throws_ArgumentNullException()
{
    MethodBase.GetCurrentMethod().Guard().Argument<object>(null);
}


[TestMethod]
public void
Argument_Required_NonNull_Passes()
{
    Required(new object());
}


[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void
Argument_Required_Null_Throws_ArgumentNullException()
{
    Required(null);
}


[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void
Argument_Picks_Up_ValidationAttribute_From_Implemented_Interface()
{
    new TestClass().TestMethod(null);
}


[TestMethod]
[ExpectedException(typeof(ArgumentException))]
public void
Argument_Picks_Up_ValidationAttribute_From_Base_Class()
{
    new TestClass().TestMethod("too short");
}


[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void
Argument_Picks_Up_ValidationAttribute_From_Explicitly_Implemented_Interface()
{
    ((ITestInterface)new ExplicitImplementationTestClass()).TestMethod(null);
}


[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void
Argument_Picks_Up_ValidationAttribute_From_Implemented_Interface_Property()
{
    new TestClass().TestProperty = null;
}


[TestMethod]
[ExpectedException(typeof(ArgumentException))]
public void
Argument_Picks_Up_ValidationAttribute_From_Base_Class_Property()
{
    new TestClass().TestProperty = "too short";
}


[TestMethod]
[ExpectedException(typeof(ArgumentNullException))]
public void
Argument_Picks_Up_ValidationAttribute_From_Explicitly_Implemented_Interface_Property()
{
    ((ITestInterface)new ExplicitImplementationTestClass()).TestProperty = null;
}


static void
Required([Required] object param)
{
    MethodBase.GetCurrentMethod().Guard().Argument(() => param);
}


interface
ITestInterface
{
    void
    TestMethod([Required] string param);


    [Required]
    string
    TestProperty
    {
        get; set;
    }
}


class
TestBaseClass
    : ITestInterface
{
    public virtual void
    TestMethod([MinLength(10)] string param1)
    {
        throw new Exception("Wrong TestProperty");
    }


    [MinLength(10)]
    public virtual string
    TestProperty
    {
        get; set;
    }
}


class
TestClass
    : TestBaseClass
{
    public override void
    TestMethod(string param)
    {
        MethodBase.GetCurrentMethod().Guard().Argument(() => param);
        base.TestMethod(param);
    }


    public override string
    TestProperty
    {
        get
        {
            throw new Exception("Wrong TestProperty");
        }

        set
        {
            MethodBase.GetCurrentMethod().Guard().Argument(() => value);
            base.TestProperty = value;
        }
    }
}


class
ExplicitImplementationTestClass
    : ITestInterface
{
    public void
    TestMethod(string param)
    {
        throw new Exception("Wrong TestProperty");
    }


    public string
    TestProperty
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            throw new NotImplementedException();
        }
    }


    void
    ITestInterface.TestMethod(string param)
    {
        MethodBase.GetCurrentMethod().Guard().Argument(() => param);
    }


    string
    ITestInterface.TestProperty
    {
        get
        {
            throw new NotImplementedException();
        }
        set
        {
            MethodBase.GetCurrentMethod().Guard().Argument(() => value);
        }
    }
}


}
}
