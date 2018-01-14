using System;
using System.Reflection;


namespace
MacroAttributeGuards
{


public static class
MethodBaseExtensions
{


/// <summary>
/// Begin guarding arguments
/// </summary>
///
public static MethodGuard
Guard(this MethodBase method)
{
    if (method == null) throw new ArgumentNullException(nameof(method));
    return new MethodGuard(method);
}


}
}
