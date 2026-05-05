using System.Collections.Generic;
using System.Threading.Tasks;
using SwiftDrop.Models;

namespace SwiftDrop.Services
{
    /// <summary>
    /// Contract for all SwiftDrop actions.
    /// </summary>
    public interface IActionService
    {
        string Name { get; }

        /// <summary>Executes on a single file/URL.</summary>
        Task<ActionResult> ExecuteAsync(string input);

        /// <summary>
        /// Executes on multiple files at once (batch mode).
        /// Default implementation calls ExecuteAsync per file.
        /// Override for actions that need all files together (e.g., ZIP).
        /// </summary>
        Task<ActionResult> ExecuteBatchAsync(IReadOnlyList<string> inputs)
        {
            // Default: just process the first file (subclasses override for real batch)
            return inputs.Count > 0
                ? ExecuteAsync(inputs[0])
                : Task.FromResult(ActionResult.Fail("No files provided"));
        }
    }
}