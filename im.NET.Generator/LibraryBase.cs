﻿using System.Runtime.CompilerServices;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using im.NET.Generator.Logging;
using im.NET.Generator.Passes;

namespace im.NET.Generator;

public abstract class LibraryBase : ILibrary
{
    protected static void AddDefaultPasses(Driver driver)
    {
        driver.AddTranslationUnitPass(new ImEnumPass());
    }

    protected static TranslationUnit GetImGuiTranslationUnit(ASTContext ctx)
    {
        return ctx.TranslationUnits.Single(s => s.FileName == "imgui.h");
    }

    protected static void Ignore(ASTContext ctx, string? className, string? memberName, IgnoreType ignoreType)
    {
        switch (ignoreType)
        {
            case IgnoreType.Class:
                ctx.IgnoreClassWithName(className);
                return;
            case IgnoreType.Function:
                ctx.IgnoreFunctionWithName(memberName);
                return;
            case IgnoreType.Method:
                ctx.IgnoreClassMethodWithName(className, memberName);
                return;
            case IgnoreType.Property:
                ctx.FindCompleteClass(className).Properties.Single(s => s.Name == memberName).ExplicitlyIgnore();
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(ignoreType), ignoreType, null);
        }
    }

    protected static void PreprocessPasses(Driver driver)
    {
        // actually, we do want these, else we'll get pretty much nothing generated

        RemovePass<CheckIgnoredDeclsPass>(driver);

        // this is useless in our case, it also throws when adding our own comments

        RemovePass<CleanCommentsPass>(driver);
    }

    protected static void PushDeclarationUpstream(TranslationUnit unit, string @class)
    {
        var ns = unit.Namespaces.Single(s => s.Name == @class);

        unit.Declarations.AddRange(ns.Declarations);

        ns.Declarations.Clear();
    }

    private static void RemovePass<T>(Driver driver, [CallerMemberName] string memberName = null!) where T : TranslationUnitPass
    {
        var count = driver.Context.TranslationUnitPasses.Passes.RemoveAll(s => s is T);

        using (new ConsoleColorScope(null, ConsoleColor.Yellow))
        {
            Console.WriteLine($"Removed {count} passes of type {typeof(T)} in {memberName}");
        }
    }

    protected static void SetEnumerationsFlags(TranslationUnit unit)
    {
        foreach (var enumeration in unit.Enums)
        {
            if (enumeration.Name.Contains("Flags") is false)
            {
                continue;
            }

            if (enumeration.IsFlags)
            {
                continue;
            }

            enumeration.SetFlags();

            using (new ConsoleColorScope(null, ConsoleColor.Yellow))
            {
                Console.WriteLine($"Set enumeration as flags: {enumeration.Name}");
            }
        }
    }

    #region ILibrary Members

    public virtual void Setup(Driver driver)
    {
        var options = driver.Options;

        options.GeneratorKind = GeneratorKind.CSharp;
        options.GenerateFinalizers = true;
        options.GenerateDebugOutput = true;
        options.GenerateDefaultValuesForArguments = true;
        options.MarshalCharAsManagedChar = true;
        options.Verbose = true;
    }

    public abstract void SetupPasses(Driver driver);

    public abstract void Preprocess(Driver driver, ASTContext ctx);

    public abstract void Postprocess(Driver driver, ASTContext ctx);

    #endregion
}