using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;


namespace
MacroAttributeGuards
{


public class
MethodGuard
{
    

internal
MethodGuard(MethodBase method)
{
    type = method.DeclaringType;
    this.method = method;
    this.property = FindPropertyFromSetter(method);
    implementedInterfaces = type.GetInterfaces();
}


/// <summary>
/// The method being guarded
/// </summary>
///
MethodBase
method;


/// <summary>
/// The property if the method is a property setter, otherwise null
/// </summary>
///
PropertyInfo
property;


/// <summary>
/// The type the method is a member of
/// </summary>
///
Type
type;


/// <summary>
/// Interfaces implemented by the type
/// </summary>
///
Type[]
implementedInterfaces;


/// <summary>
/// Guard an argument value according to all <see cref="ValidationAttribute"/>s on the parameter and all
/// its ancestors (or, in the case of a property setter, the property and all its ancestors)
/// </summary>
///
/// <remarks>
/// <para>
/// Ancestors include base classes and implemented interfaces.
/// </para>
/// </remarks>
///
/// <param name="argumentExpression">
/// A lambda expression referring to the argument value
/// </param>
///
/// <returns>
/// This <see cref="MethodGuard"/>, so more <see cref="Argument{T}(Expression{Func{T}})"/> calls can be chained
/// </returns>
///
/// <example>
/// <code>
/// void SomeMethod(object arg1, object arg2)
/// {
///     MethodBase.GetCurrentMethod().Guard()
///         .Argument(() => arg1)
///         .Argument(() => arg2);
/// }
/// </code>
/// </example>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1006:DoNotNestGenericTypesInMemberSignatures")]
public MethodGuard
Argument<T>(Expression<Func<T>> argumentExpression)
{
    if (argumentExpression == null) throw new ArgumentNullException(nameof(argumentExpression));

    //
    // Look up the parameter corresponding to the argument expression
    //
    var parameter = GetParameterFromArgumentExpression(method, argumentExpression);

    //
    // Check assumption that property setters only have 'value' parameters
    //
    if (property != null && parameter.Name != "value")
    {
        throw new InvalidOperationException("Guarding a property setter argument but it isn't named 'value'");
    }

    //
    // Find validation attributes decorating the parameter (or property, if the method is a setter) and all of
    // its ancestors in base classes and implemented interfaces
    //
    IEnumerable<ValidationAttribute> validationAttributes =
        property != null
            ? FindValidationAttributesForProperty(property, implementedInterfaces)
            : FindValidationAttributesForParameter(method, parameter, implementedInterfaces);

    //
    // Retrieve the argument value using the argument expression
    //
    T value = argumentExpression.Compile()();

    //
    // Build a natural language reference to the parameter value for use in exception messages
    //
    var valueDescriptor =
        property != null
            ? string.Format(CultureInfo.InvariantCulture, "The new {0} value", property.Name)
            : string.Format(CultureInfo.InvariantCulture, "The {0} argument", parameter.Name);

    //
    // Guard the argument value according to each validation attribute
    //
    foreach (var attribute in validationAttributes)
    {
        GuardFromAttribute(value, parameter.Name, valueDescriptor, attribute);
    }

    //
    // Return the same MethodGuard so more .Argument() calls can be chained
    //
    return this;
}


static void
GuardFromAttribute<T>(
    T                   value,
    string              paramName,
    string              valueDescriptor,
    ValidationAttribute attribute)
{
    var message =
        string.Format(
            CultureInfo.InvariantCulture,
            "{0} is invalid: {1}",
            valueDescriptor,
            attribute.FormatErrorMessage(paramName));

    if (attribute is RequiredAttribute && value == null)
    {
        throw new ArgumentNullException(paramName, message);
    }

    if (!attribute.IsValid(value))
    {
        throw new ArgumentException(message, paramName);
    }
}


static IEnumerable<ValidationAttribute>
FindValidationAttributesForParameter(
    MethodBase          method,
    ParameterInfo       parameter,
    IEnumerable<Type>   implementedInterfaces)
{
    var parameters =
        Enumerable.Empty<ParameterInfo>()
            .Concat(new[] {parameter})
            .Concat(
                GetImplementedInterfaceMethods(method, implementedInterfaces)
                    .Select(m => m.GetParameters().Single(p => p.Name == parameter.Name)));
    
    return parameters.SelectMany(p => p.GetCustomAttributes<ValidationAttribute>(true));
}


static IEnumerable<ValidationAttribute>
FindValidationAttributesForProperty(
    PropertyInfo        property,
    IEnumerable<Type>   implementedInterfaces)
{
    var properties =
        Enumerable.Empty<PropertyInfo>()
            .Concat(new [] {property})
            .Concat(
                GetImplementedInterfaceMethods(property.SetMethod, implementedInterfaces)
                    .Select(setter => GetPropertyFromSetter(setter)));

    return properties.SelectMany(p => p.GetCustomAttributes<ValidationAttribute>(true));
}


static IEnumerable<MethodInfo>
GetImplementedInterfaceMethods(
    MethodBase method,
    IEnumerable<Type> implementedInterfaces)
{
    return implementedInterfaces
        .Select(i => GetImplementedInterfaceMethod(method, i))
        .Where(m => m != null);
}


static MethodInfo
GetImplementedInterfaceMethod(MethodBase method, Type iface)
{
    var map = method.DeclaringType.GetInterfaceMap(iface);
    for (int i=0; i<map.InterfaceMethods.Length; i++)
    {
        if (map.TargetMethods[i] == method)
        {
            return map.InterfaceMethods[i];
        }
    }
    return null;
}


static PropertyInfo
GetPropertyFromSetter(MethodBase setter)
{
    var pi = FindPropertyFromSetter(setter);
    if (pi == null) throw new ArgumentException("Not a property setter", nameof(setter));
    return pi;
}


static PropertyInfo
FindPropertyFromSetter(MethodBase setter)
{
    return
        setter.DeclaringType.GetProperties(
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        .SingleOrDefault(p => p.SetMethod == setter);
}


static ParameterInfo
GetParameterFromArgumentExpression<T>(
    MethodBase method,
    Expression<Func<T>> argumentExpression)
{
    var fieldExpression = argumentExpression.Body as MemberExpression;
    if (fieldExpression == null) throw NotAnArgumentReference();
    var member = fieldExpression.Member;
    if (member == null) throw NotAnArgumentReference();
    var name = member.Name;
    var parameter = method.GetParameters().Where(p => p.Name == name).FirstOrDefault();
    if (parameter == null) throw NotAnArgumentReference();
    return parameter;
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Usage",
    "CA2208:InstantiateArgumentExceptionsCorrectly",
    Justification = "Callers are expected to have an argumentExpression parameter")]
static ArgumentException
NotAnArgumentReference()
{
    return new ArgumentException("Not a reference to an argument", "argumentExpression");
}


}
}
