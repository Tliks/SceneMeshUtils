using UnityEditor;

public static class AssetPathUtility
{
    public static string GenerateMeshPath(string root_name)
    {
        // Assets/ModuleCreator/{root_name}/Mesh/newMesh.asset

        string base_path = $"Assets/ModuleCreator";
        if (!AssetDatabase.IsValidFolder(base_path))
        {
            AssetDatabase.CreateFolder("Assets", "ModuleCreator");
            AssetDatabase.Refresh();
        }

        string folderPath = $"{base_path}/{root_name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(base_path, root_name);
            AssetDatabase.Refresh();
        }

        string folderPath1 = $"{folderPath}/Mesh";
        if (!AssetDatabase.IsValidFolder(folderPath1))
        {
            AssetDatabase.CreateFolder(folderPath, "Mesh");
            AssetDatabase.Refresh();
        }

        string fileName = "newMesh";
        string fileExtension = "asset";
        
        return AssetDatabase.GenerateUniqueAssetPath(folderPath1 + "/" + fileName + "." + fileExtension);
    }

    public static string GeneratePrefabPath(string root_name, string mesh_name)
    {
        // Assets/ModuleCreator/{root_name}/mesh_name.prefab

        string base_path = $"Assets/ModuleCreator";
        if (!AssetDatabase.IsValidFolder(base_path))
        {
            AssetDatabase.CreateFolder("Assets", "ModuleCreator");
            AssetDatabase.Refresh();
        }

        string folderPath = $"{base_path}/{root_name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(base_path, root_name);
            AssetDatabase.Refresh();
        }

        string fileName = $"{mesh_name}_MA";
        string fileExtension = "prefab";
        
        return AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + fileName + "." + fileExtension);
    }
}