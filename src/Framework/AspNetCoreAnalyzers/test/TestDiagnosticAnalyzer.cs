// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers;

public class TestDiagnosticAnalyzerRunner : DiagnosticAnalyzerRunner
{
    public TestDiagnosticAnalyzerRunner(DiagnosticAnalyzer analyzer)
    {
        Analyzer = analyzer;
    }

    public DiagnosticAnalyzer Analyzer { get; }

    public async Task<ClassifiedSpan[]> GetClassificationSpansAsync(TextSpan textSpan, params string[] sources)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, sources);

        var classifierHelperType = project.GetType().Assembly.GetType("Microsoft.CodeAnalysis.Classification.ClassifierHelper");
        var classificationOptionsType = project.GetType().Assembly.GetType("Microsoft.CodeAnalysis.Classification.ClassificationOptions");
        var classificationOptions = Activator.CreateInstance(classificationOptionsType);

        var method = classifierHelperType.GetMethod("GetClassifiedSpansAsync");
        //method

        var doc = project.Solution.GetDocument(project.Documents.First().Id);

        var resultTask = (Task<ImmutableArray<ClassifiedSpan>>)method.Invoke(obj: null, new object[] { doc, textSpan, classificationOptions, CancellationToken.None, false, false });
        var result = await resultTask;

        return result.ToArray();
    }

    public async Task<Diagnostic[]> GetDiagnosticsAsync(params string[] sources)
    {
        var project = CreateProjectWithReferencesInBinDir(GetType().Assembly, sources);

        var classifierHelperType = project.GetType().Assembly.GetType("Microsoft.CodeAnalysis.Classification.ClassifierHelper");
        var classificationOptionsType = project.GetType().Assembly.GetType("Microsoft.CodeAnalysis.Classification.ClassificationOptions");
        var classificationOptions = Activator.CreateInstance(classificationOptionsType);

        var method = classifierHelperType.GetMethod("GetClassifiedSpansAsync");
        //method

        var doc = project.Solution.GetDocument(project.Documents.First().Id);

        var resultTask = (Task<ImmutableArray<ClassifiedSpan>>)method.Invoke(obj: null, new object[] { doc, new TextSpan(0, 200), classificationOptions, CancellationToken.None, true, true });
        var result = await resultTask;
        //var languageService = project.Solution.Workspace.Services.GetLanguageServices("C#");

        //var project.GetType().Assembly.GetType("Microsoft.CodeAnalysis.Classification.IClassificationService");

        //var method = languageService.GetType().GetMethod("GetRequiredService");
        //method.MakeGenericMethod
        //languageService.GetRequiredService

        return await GetDiagnosticsAsync(project);
    }

    public static Project CreateProjectWithReferencesInBinDir(Assembly testAssembly, params string[] source)
    {
        // The deps file in the project is incorrect and does not contain "compile" nodes for some references.
        // However these binaries are always present in the bin output. As a "temporary" workaround, we'll add
        // every dll file that's present in the test's build output as a metadatareference.

        var project = DiagnosticProject.Create(testAssembly, source);
        foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            if (!project.MetadataReferences.Any(c => string.Equals(Path.GetFileNameWithoutExtension(c.Display), Path.GetFileNameWithoutExtension(assembly), StringComparison.OrdinalIgnoreCase)))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(assembly));
            }
        }

        return project;
    }

    public Task<Diagnostic[]> GetDiagnosticsAsync(Project project)
    {
        return GetDiagnosticsAsync(new[] { project }, Analyzer, Array.Empty<string>());
    }

    protected override CompilationOptions ConfigureCompilationOptions(CompilationOptions options)
    {
        return options.WithOutputKind(OutputKind.ConsoleApplication);
    }
}
