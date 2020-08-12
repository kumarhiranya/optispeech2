using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Diagnostics;
using System;
using System.Linq;

namespace Optispeech.Documentation {

    /// <summary>
    /// Automatically checks if docfx is installed when building the project and, if so, uses it to build
    /// the documentation and places it inside the output folder so it can be hosted by the <see cref="DocumentationServer"/>
    /// </summary>
    public class DocumentationBuilder : IPostprocessBuildWithReport {

        [HideInDocumentation]
        public int callbackOrder { get { return 0; } }
        
        /// <summary>
        /// Callback that runs when the project finishes building,
        /// at which point it attempts to build the documentation and place it in the output folder
        /// </summary>
        /// <param name="report"></param>
        public void OnPostprocessBuild(BuildReport report) {
            string sourcePath = Path.Combine(Application.dataPath, "..", "docs");
            string destPath = Path.Combine(report.summary.outputPath, "..", "docs");
            try {
                // Find docfx path
                var enviromentPath = Environment.GetEnvironmentVariable("PATH");

                var paths = enviromentPath.Split(';');
                // TODO linux support
                var exePath = paths.Select(x => Path.Combine(x, "docfx.exe"))
                                   .Where(x => File.Exists(x))
                                   .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(exePath)) {
                    UnityEngine.Debug.LogError($"Couldn't generate documentation because docfx wasn't found in path. You can fix this by installing it and adding docfx.exe to your path");
                    return;
                }
                // Remove old documenation
                FileUtil.DeleteFileOrDirectory(destPath);
                // Generate documentation
                Process process = Process.Start("docfx.exe", Path.Combine(Application.dataPath, "..", "Documentation", "docfx.json"));
                process.WaitForExit();
                // Copy to build folder
                FileUtil.CopyFileOrDirectory(sourcePath, destPath);
            } catch (Exception e) {
                UnityEngine.Debug.LogError($"Exception occured while generating documentation.\n{e}");
            }
        }
    }
}
