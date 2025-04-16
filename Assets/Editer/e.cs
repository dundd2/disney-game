using UnityEngine;
using UnityEditor; // Required for Editor scripts
using System.IO;   // Required for file operations (Directory, File, Path)
using System.Text.RegularExpressions; // Required for filename parsing

public class TileBatchProcessor
{
    // Define the menu item path
    [MenuItem("Custom Tools/Process Tiles (Import, Place, Rotate, Add Collider)")]
    static void ProcessTiles()
    {
        // --- Configuration ---
        // Adjust these values if needed
        string targetAssetBaseFolder = "Assets/ImportedTiles"; // Where to store imported assets in Unity
        string tileNamePrefix = "Tile_"; // Expected prefix for tile OBJ files
        string coordinatePattern = @"([+-]?\d+)_([+-]?\d+)"; // Regex pattern to capture X and Z coordinates after the prefix
        Vector3 positionOffset = Vector3.zero; // Add any global position offset if needed
        Vector3 rotationCorrection = new Vector3(-90f, 0f, 0f); // Default rotation correction (e.g., for Z-up to Y-up)
        string[] textureExtensions = { ".jpg", ".png", ".tga" }; // Add other possible texture extensions if needed
        // --- End Configuration ---

        // 1. Ask user to select the root folder containing all tile subfolders
        string sourceRootPath = EditorUtility.OpenFolderPanel("Select Root Folder Containing Tile Data", "", "");
        if (string.IsNullOrEmpty(sourceRootPath))
        {
            Debug.LogWarning("Operation cancelled by user.");
            return; // User cancelled the folder selection
        }

        // 2. Ensure the target base directory exists in Assets
        if (!Directory.Exists(targetAssetBaseFolder))
        {
            Directory.CreateDirectory(targetAssetBaseFolder);
            AssetDatabase.Refresh(); // Refresh AssetDatabase to recognize the new folder
            Debug.Log($"Created target asset folder: {targetAssetBaseFolder}");
        }

        // 3. Find all .obj files recursively within the selected source folder
        string[] objFiles = Directory.GetFiles(sourceRootPath, "*.obj", SearchOption.AllDirectories);

        if (objFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("No OBJ Files Found", $"No '.obj' files were found within the selected directory:\n{sourceRootPath}", "OK");
            return;
        }

        int processedCount = 0;
        int errorCount = 0;

        // Start progress bar
        EditorUtility.DisplayProgressBar("Processing Tiles", "Starting...", 0f);

        try // Use a try-finally block to ensure the progress bar is cleared
        {
            for (int i = 0; i < objFiles.Length; i++)
            {
                string objFilePath = objFiles[i];
                FileInfo objFileInfo = new FileInfo(objFilePath);
                string baseName = Path.GetFileNameWithoutExtension(objFileInfo.Name); // e.g., "Tile_+147_+131"
                string sourceDirectory = objFileInfo.DirectoryName;

                // Update progress bar
                float progress = (float)(i + 1) / objFiles.Length;
                EditorUtility.DisplayProgressBar("Processing Tiles", $"Processing: {baseName}", progress);

                // 4. Parse filename for coordinates
                if (!baseName.StartsWith(tileNamePrefix))
                {
                    Debug.LogWarning($"Skipping file (doesn't start with '{tileNamePrefix}'): {objFileInfo.Name}");
                    continue;
                }

                string coordinatePart = baseName.Substring(tileNamePrefix.Length); // e.g., "+147_+131"
                Match match = Regex.Match(coordinatePart, coordinatePattern);

                if (!match.Success || match.Groups.Count < 3)
                {
                    Debug.LogError($"Failed to parse coordinates from filename: {baseName}. Expected pattern like '{tileNamePrefix}X_Y.obj'");
                    errorCount++;
                    continue;
                }

                float xCoord, zCoord;
                if (!float.TryParse(match.Groups[1].Value, out xCoord) || !float.TryParse(match.Groups[2].Value, out zCoord))
                {
                    Debug.LogError($"Failed to convert parsed coordinates to numbers for: {baseName}");
                    errorCount++;
                    continue;
                }

                // 5. Define and create the specific target folder within Assets for this tile
                string targetTileFolder = Path.Combine(targetAssetBaseFolder, baseName);
                if (!Directory.Exists(targetTileFolder))
                {
                    Directory.CreateDirectory(targetTileFolder);
                    // No need to refresh here, ImportAsset will handle it
                }

                // 6. Copy related files (.obj, .mtl, textures) to the target Unity folder
                bool objCopied = CopyFileToAssets(objFilePath, targetTileFolder, objFileInfo.Name);
                if (!objCopied)
                {
                    errorCount++;
                    continue; // Skip if OBJ couldn't be copied/imported
                }

                // Copy MTL file (if exists)
                string mtlFileName = baseName + ".mtl";
                string sourceMtlPath = Path.Combine(sourceDirectory, mtlFileName);
                CopyFileToAssets(sourceMtlPath, targetTileFolder, mtlFileName);

                // Copy Texture files (if they exist) - handles common extensions and _X suffixes
                foreach (string ext in textureExtensions)
                {
                    // Simple case: TextureName.jpg
                    string texFileName = baseName + ext;
                    string sourceTexPath = Path.Combine(sourceDirectory, texFileName);
                    CopyFileToAssets(sourceTexPath, targetTileFolder, texFileName);

                    // Case with suffix: TextureName_1.jpg, TextureName_2.jpg etc.
                    // Look for files matching pattern BaseName_*.ext
                    string searchPattern = $"{baseName}_*{ext}";
                    string[] suffixedTextures = Directory.GetFiles(sourceDirectory, searchPattern);
                    foreach(string suffixedTexPath in suffixedTextures)
                    {
                         CopyFileToAssets(suffixedTexPath, targetTileFolder, Path.GetFileName(suffixedTexPath));
                    }
                }

                // --- Asset Processing and Instantiation ---

                // 7. Load the imported OBJ asset from the AssetDatabase
                string importedObjAssetPath = Path.Combine(targetTileFolder, objFileInfo.Name).Replace("\\", "/"); // Use forward slashes for Unity paths
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(importedObjAssetPath);

                if (prefab == null)
                {
                    Debug.LogError($"Failed to load imported asset at path: {importedObjAssetPath}. Import might have failed earlier.");
                    errorCount++;
                    continue;
                }

                // 8. Instantiate the prefab into the current scene
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance == null)
                {
                    Debug.LogError($"Failed to instantiate prefab: {importedObjAssetPath}");
                    errorCount++;
                    continue;
                }

                // 9. Set Position based on parsed coordinates and offset
                // Assuming parsed Y coordinate maps to Unity's Z axis
                instance.transform.position = new Vector3(xCoord, 0f, zCoord) + positionOffset;

                // 10. Apply Rotation Correction
                instance.transform.rotation = Quaternion.Euler(rotationCorrection);

                // 11. Add Mesh Collider
                if (instance.GetComponent<MeshCollider>() == null)
                {
                    MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        MeshCollider meshCollider = instance.AddComponent<MeshCollider>();
                        // meshCollider.sharedMesh = meshFilter.sharedMesh; // Usually assigned automatically
                        meshCollider.isTrigger = false; // Ensure it's a solid collider
                        Debug.Log($"Added Mesh Collider to {instance.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"Cannot add Mesh Collider to {instance.name}: Missing MeshFilter or Mesh.");
                    }
                }
                else
                {
                     Debug.Log($"{instance.name} already has a Mesh Collider.");
                }


                // 12. Set instance name and mark dirty
                instance.name = baseName; // Set a meaningful name in the hierarchy
                EditorUtility.SetDirty(instance); // Mark the instance as changed (important for scene saving)

                processedCount++;
            } // End foreach obj file loop
        }
        finally // Ensure the progress bar is cleared
        {
            EditorUtility.ClearProgressBar();
        }

        // 13. Final Refresh and Report
        AssetDatabase.SaveAssets(); // Save any changes made to imported assets (like meta files)
        AssetDatabase.Refresh();    // Force Unity to recognize all changes

        string summaryMessage = $"Batch processing complete.\nSuccessfully processed: {processedCount}\nErrors/Skipped: {errorCount}";
        Debug.Log(summaryMessage);
        EditorUtility.DisplayDialog("Processing Complete", summaryMessage, "OK");
    }

    // Helper function to copy a file and import it into Assets
    // Returns true if successful or file already exists, false on error
    static bool CopyFileToAssets(string sourceFilePath, string targetFolderInAssets, string targetFileName)
    {
        if (!File.Exists(sourceFilePath))
        {
            // Don't log an error for missing optional files like MTL or textures
            if (!sourceFilePath.EndsWith(".mtl") && !IsTextureExtension(sourceFilePath))
            {
                 Debug.LogWarning($"Source file not found, skipping copy: {sourceFilePath}");
            }
            return false; // Indicate file wasn't found/copied
        }

        string targetFilePath = Path.Combine(targetFolderInAssets, targetFileName);
        string targetAssetPath = targetFilePath.Replace("\\", "/"); // Unity uses forward slashes

        try
        {
            // Check if file already exists in target to avoid unnecessary copy/import
            if (!File.Exists(targetFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath);
                AssetDatabase.ImportAsset(targetAssetPath); // Import the newly copied asset
                // Debug.Log($"Copied and imported: {targetAssetPath}");
            } else {
                // Optional: Force reimport if needed, but usually not necessary unless source changed
                // AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
                // Debug.Log($"File already exists, skipped copy: {targetAssetPath}");
            }
            return true; // Success
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error copying/importing file '{sourceFilePath}' to '{targetFilePath}': {ex.Message}");
            return false; // Indicate error
        }
    }

     // Helper to check if a file path has a common texture extension
    static bool IsTextureExtension(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".tga" || ext == ".bmp" || ext == ".psd";
    }
}
