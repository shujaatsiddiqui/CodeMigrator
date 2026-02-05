using CodeMigrator.Models;

namespace CodeMigrator.Analyzers;

/// <summary>
/// Interface for code analyzers that extract method metadata from source code.
/// </summary>
public interface ICodeAnalyzer
{
    /// <summary>
    /// Gets the name of this analyzer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the supported file patterns (e.g., "*.cs", "*.aspx.cs").
    /// </summary>
    IEnumerable<string> SupportedFilePatterns { get; }

    /// <summary>
    /// Determines if this analyzer can process the given file.
    /// </summary>
    bool CanAnalyze(string filePath);

    /// <summary>
    /// Analyzes a single source file and extracts method metadata.
    /// </summary>
    Task<IEnumerable<MethodMetadata>> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes a directory of source files.
    /// </summary>
    Task<IEnumerable<MethodMetadata>> AnalyzeDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes source code content directly.
    /// </summary>
    Task<IEnumerable<MethodMetadata>> AnalyzeContentAsync(string content, string fileName = "source.cs", CancellationToken cancellationToken = default);
}
