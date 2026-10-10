namespace BannerService.Application.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

public class ArchitectureTests
{
    private const string ApplicationNamespace = "BannerService.Application";
    private static readonly string[] ForbiddenNamespaces =
    {
        "BannerService.Infrastructure.Data",
        "BannerService.Infrastructure.Repositories"
    };

    [Fact]
    public void ApplicationLayer_DoesNotDependOnDataAccess()
    {
        var assembly = typeof(BannerService.Domain.Interfaces.IDashboardService).Assembly;
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var violations = new List<string>();
        var applicationTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith(ApplicationNamespace, StringComparison.Ordinal))
            .Where(t => !t.Name.StartsWith("<", StringComparison.Ordinal));

        foreach (var type in applicationTypes)
        {
            var used = new List<Type>();
            if (type.BaseType != null) used.Add(type.BaseType);
            used.AddRange(type.GetInterfaces());
            used.AddRange(type.GetFields(all).Where(f => !f.Name.StartsWith("<", StringComparison.Ordinal)).Select(f => f.FieldType));
            used.AddRange(type.GetProperties(all).Select(p => p.PropertyType));
            foreach (var ctor in type.GetConstructors(all)) used.AddRange(ctor.GetParameters().Select(p => p.ParameterType));
            foreach (var method in type.GetMethods(all))
            {
                used.Add(method.ReturnType);
                used.AddRange(method.GetParameters().Select(p => p.ParameterType));
            }

            foreach (var dependency in used.SelectMany(Flatten).Distinct())
            {
                if (dependency.Namespace != null && ForbiddenNamespaces.Any(n => dependency.Namespace.StartsWith(n, StringComparison.Ordinal)))
                    violations.Add($"{type.FullName} -> {dependency.FullName}");
                else if (dependency.FullName == "Microsoft.EntityFrameworkCore.DbContext" || dependency.BaseType?.FullName == "Microsoft.EntityFrameworkCore.DbContext")
                    violations.Add($"{type.FullName} -> {dependency.FullName}");
            }
        }

        Assert.True(violations.Count == 0,
            "The Application layer must reach data through Domain repository interfaces, not Infrastructure.Data, Infrastructure.Repositories or a DbContext:" + Environment.NewLine + string.Join(Environment.NewLine, violations.Distinct()));
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        if (type.HasElementType && type.GetElementType() is { } element)
        {
            foreach (var inner in Flatten(element)) yield return inner;
            yield break;
        }

        yield return type;

        if (type.IsGenericType)
            foreach (var argument in type.GetGenericArguments())
                foreach (var inner in Flatten(argument)) yield return inner;
    }
}
