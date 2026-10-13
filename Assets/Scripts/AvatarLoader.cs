using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using GLTFast; 

public class AvatarLoader : MonoBehaviour
{
    private List<string> avatarFiles = new List<string> {
        "6984ec706f67100ea7e02465.glb",
        "tiedye.glb",
        "67e20a7fc5f8c4a77988b853.glb"
    };

    private GameObject currentAvatar;

    async void Start()
    {
        await LoadAvatar(0);
    }

    public async Task LoadAvatar(int index)
    {
        if (currentAvatar != null) Destroy(currentAvatar);

        // 1. Build a direct local path
        string filePath = Path.Combine(Application.streamingAssetsPath, "Models", avatarFiles[index]);

        Debug.Log("Checking local path: " + filePath);

        // 2. Read the file directly as bytes (bypasses WebRequests/404 errors completely)
        if (File.Exists(filePath))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            
            var gltf = new GltfImport();
            // Load using the raw byte array
            bool success = await gltf.Load(fileBytes); 

            if (success) 
            {
                currentAvatar = new GameObject("LoadedAvatar");
                currentAvatar.transform.SetParent(this.transform, false);
                await gltf.InstantiateMainSceneAsync(currentAvatar.transform);
                Debug.Log("SUCCESS! Model loaded via direct bytes.");
            } 
            else 
            {
                Debug.LogError("glTFast failed to parse the byte data.");
            }
        }
        else
        {
            Debug.LogError("CRITICAL: File does not exist at path: " + filePath);
        }
    }
}