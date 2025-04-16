using UnityEngine;
using UnityEditor; // Required for Editor scripts and Undo

public class PhysicsSetupValidator
{
    private const string PlayerTag = "Player";
    private const string GroundTag = "Ground";

    [MenuItem("Custom Tools/Validate Player/Ground Physics Setup")]
    static void ValidatePhysicsSetup()
    {
        int fixesMade = 0;
        int warnings = 0;

        // --- Find Player ---
        GameObject player = GameObject.FindWithTag(PlayerTag);
        if (player == null)
        {
            Debug.LogError($"Physics Validator: Could not find a GameObject tagged '{PlayerTag}'. Please tag your player object correctly.");
            return; // Stop if no player found
        }
        else
        {
            Debug.Log($"--- Checking Player ({player.name}) ---");
            fixesMade += CheckPlayerObject(player);
        }

        // --- Find Ground Objects ---
        GameObject[] groundObjects = GameObject.FindGameObjectsWithTag(GroundTag);
        if (groundObjects.Length == 0)
        {
            Debug.LogWarning($"Physics Validator: Could not find any GameObjects tagged '{GroundTag}'. Ensure your ground/tile objects are tagged correctly.");
            warnings++;
            // Continue checking player even if ground is missing
        }
        else
        {
            Debug.Log($"--- Checking {groundObjects.Length} Ground Object(s) ---");
            foreach (GameObject ground in groundObjects)
            {
                fixesMade += CheckGroundObject(ground);
            }
        }

        // --- Check Layer Collision (if both player and at least one ground object were found) ---
        if (player != null && groundObjects.Length > 0)
        {
            warnings += CheckLayerCollision(player, groundObjects[0]); // Check layers using the first ground object found
        }

        // --- Final Report ---
        EditorUtility.DisplayDialog("Physics Validation Complete",
            $"Validation finished.\n\n" +
            $"Objects Checked:\n" +
            $"- Player ('{PlayerTag}'): {(player != null ? "Found" : "NOT FOUND")}\n" +
            $"- Ground ('{GroundTag}'): {groundObjects.Length} Found\n\n" +
            $"Automatic Fixes Made: {fixesMade}\n" +
            $"Warnings Issued: {warnings}\n\n" +
            $"Please review the Console window for detailed logs and any warnings (especially regarding Layer Collisions).",
            "OK");

         // Optional: Ping objects that had issues or were fixed if needed (more complex to track)
    }

    // --- Helper: Check Player ---
    static int CheckPlayerObject(GameObject player)
    {
        int fixes = 0;

        // 1. Check for Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        Rigidbody2D rb2D = player.GetComponent<Rigidbody2D>(); // Also check for 2D

        if (rb == null && rb2D == null)
        {
            Debug.LogWarning($"{player.name}: Missing Rigidbody (or Rigidbody2D). Adding Rigidbody component. You might need Rigidbody2D for 2D physics.", player);
            // Add Rigidbody by default for 3D. User might need to change if it's 2D.
            Undo.AddComponent<Rigidbody>(player);
            fixes++;
        }
        // Optional: Check Rigidbody settings like 'Is Kinematic' if needed, but usually default is fine.

        // 2. Check for Collider
        Collider col = player.GetComponent<Collider>(); // 3D base class
        Collider2D col2D = player.GetComponent<Collider2D>(); // 2D base class

        if (col == null && col2D == null)
        {
            Debug.LogWarning($"{player.name}: Missing Collider (or Collider2D). Adding CapsuleCollider. Adjust size/type if needed.", player);
            // Add a default CapsuleCollider. Might not be the best fit but better than none.
            Undo.AddComponent<CapsuleCollider>(player);
            fixes++;
            // Re-get the collider after adding it for subsequent checks
            col = player.GetComponent<Collider>();
        }

        // 3. Check if Collider is Enabled (check both 3D and 2D if they exist)
        if (col != null && !col.enabled)
        {
            Debug.LogWarning($"{player.name}: Collider ({col.GetType().Name}) was disabled. Enabling it.", player);
            Undo.RecordObject(col, "Enable Player Collider");
            col.enabled = true;
            fixes++;
        }
        if (col2D != null && !col2D.enabled)
        {
            Debug.LogWarning($"{player.name}: Collider2D ({col2D.GetType().Name}) was disabled. Enabling it.", player);
            Undo.RecordObject(col2D, "Enable Player Collider2D");
            col2D.enabled = true;
            fixes++;
        }

        // 4. Check if Collider is Trigger (check both 3D and 2D)
        if (col != null && col.isTrigger)
        {
            Debug.LogWarning($"{player.name}: Collider ({col.GetType().Name}) was set to 'Is Trigger'. Disabling 'Is Trigger' for physical collision.", player);
            Undo.RecordObject(col, "Disable Player IsTrigger");
            col.isTrigger = false;
            fixes++;
        }
        if (col2D != null && col2D.isTrigger)
        {
             Debug.LogWarning($"{player.name}: Collider2D ({col2D.GetType().Name}) was set to 'Is Trigger'. Disabling 'Is Trigger' for physical collision.", player);
             Undo.RecordObject(col2D, "Disable Player IsTrigger");
            col2D.isTrigger = false;
            fixes++;
        }

        return fixes;
    }

    // --- Helper: Check Ground ---
    static int CheckGroundObject(GameObject ground)
    {
        int fixes = 0;
        bool is3D = true; // Assume 3D unless specific 2D components are found

        // 1. Check for erroneous Rigidbody (static ground shouldn't have one)
        Rigidbody rb = ground.GetComponent<Rigidbody>();
        Rigidbody2D rb2D = ground.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.LogWarning($"{ground.name}: Found unnecessary Rigidbody on static ground. Removing it.", ground);
            Undo.DestroyObjectImmediate(rb); // Use DestroyImmediate in Editor scripts
            fixes++;
        }
        if (rb2D != null)
        {
            Debug.LogWarning($"{ground.name}: Found unnecessary Rigidbody2D on static ground. Removing it.", ground);
             Undo.DestroyObjectImmediate(rb2D);
            fixes++;
            is3D = false; // Found a 2D component
        }

        // 2. Check for Mesh Collider (Primary for 3D ground tiles) or other colliders
        MeshCollider meshCol = ground.GetComponent<MeshCollider>();
        Collider genericCol = ground.GetComponent<Collider>(); // Check for Box, Sphere etc.
        Collider2D col2D = ground.GetComponent<Collider2D>(); // Check for 2D

        if (meshCol == null && genericCol == null && col2D == null)
        {
            // Attempt to add MeshCollider if a MeshFilter exists (common for imported tiles)
            MeshFilter mf = ground.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Debug.LogWarning($"{ground.name}: Missing Collider. Adding MeshCollider based on existing MeshFilter.", ground);
                meshCol = Undo.AddComponent<MeshCollider>(ground);
                // meshCol.sharedMesh = mf.sharedMesh; // Should be assigned automatically if added this way
                fixes++;
            } else if (is3D) {
                 Debug.LogWarning($"{ground.name}: Missing Collider and no suitable MeshFilter found to add a MeshCollider automatically.", ground);
            } else {
                 Debug.LogWarning($"{ground.name}: Missing Collider2D.", ground);
                 // Could try adding a default like BoxCollider2D here if desired
            }
        }

        // 3. Check Mesh Collider specific settings (if it exists)
        if (meshCol != null)
        {
            if (!meshCol.enabled)
            {
                Debug.LogWarning($"{ground.name}: MeshCollider was disabled. Enabling it.", ground);
                Undo.RecordObject(meshCol, "Enable Ground MeshCollider");
                meshCol.enabled = true;
                fixes++;
            }
            if (meshCol.isTrigger)
            {
                Debug.LogWarning($"{ground.name}: MeshCollider was set to 'Is Trigger'. Disabling 'Is Trigger'.", ground);
                 Undo.RecordObject(meshCol, "Disable Ground IsTrigger");
                meshCol.isTrigger = false;
                fixes++;
            }
            if (meshCol.sharedMesh == null)
            {
                // Try to assign from MeshFilter again if it's missing
                MeshFilter mf = ground.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) {
                     Debug.LogWarning($"{ground.name}: MeshCollider was missing its Mesh. Assigning from MeshFilter.", ground);
                     Undo.RecordObject(meshCol, "Assign Mesh to Ground MeshCollider");
                     meshCol.sharedMesh = mf.sharedMesh;
                     fixes++;
                } else {
                     Debug.LogError($"{ground.name}: MeshCollider exists but has no Mesh assigned, and no suitable MeshFilter found!", ground);
                }
            }
            // We generally want Convex OFF for static ground meshes
            if (meshCol.convex)
            {
                 Debug.LogWarning($"{ground.name}: MeshCollider 'Convex' property is enabled. This is usually incorrect for static ground. Disabling 'Convex'.", ground);
                 Undo.RecordObject(meshCol, "Disable Ground Convex");
                 meshCol.convex = false;
                 fixes++;
            }
        }
         // Add similar checks for genericCol and col2D (enabled, isTrigger) if needed

        return fixes;
    }

     // --- Helper: Check Layer Collision ---
    static int CheckLayerCollision(GameObject player, GameObject ground) {
        int warnings = 0;
        int playerLayer = player.layer;
        int groundLayer = ground.layer;

        // LayerMask.LayerToName returns "" if layer is invalid/unused
        string playerLayerName = LayerMask.LayerToName(playerLayer);
        string groundLayerName = LayerMask.LayerToName(groundLayer);

        if (string.IsNullOrEmpty(playerLayerName)) {
            Debug.LogWarning($"Player '{player.name}' is on an invalid Layer ({playerLayer}). Assign it to a valid layer.", player);
            warnings++;
        }
        if (string.IsNullOrEmpty(groundLayerName)) {
             Debug.LogWarning($"Ground '{ground.name}' is on an invalid Layer ({groundLayer}). Assign it to a valid layer.", ground);
            warnings++;
        }

        // If layers are valid, check the collision matrix
        if (!string.IsNullOrEmpty(playerLayerName) && !string.IsNullOrEmpty(groundLayerName))
        {
             bool ignoreCollision = Physics.GetIgnoreLayerCollision(playerLayer, groundLayer); // Use Physics2D for 2D projects

            if (ignoreCollision) {
                 Debug.LogError($"CRITICAL WARNING: Physics layers '{playerLayerName}' (Player) and '{groundLayerName}' (Ground) are set to IGNORE collisions! Go to Edit -> Project Settings -> Physics (or Physics 2D) -> Layer Collision Matrix and CHECK the box where these two layers intersect.", player);
                 warnings++;
            } else {
                 Debug.Log($"Layer Collision Check: '{playerLayerName}' and '{groundLayerName}' are correctly set to collide.");
            }
        }
        return warnings;
    }
}
