using UnityEditor;

namespace com.aoyon.scenemeshutils
{
    public static class AssetPathUtility
    {
        public static string GenerateMeshPath(string root_name, string mesh_name)
        {
            // Assets/ModuleCreator/{root_name}/Mesh/newMesh.asset
            string folderpath = $"Assets/ModuleCreator/{root_name}/Mesh";
            CreateParent(folderpath);

            string fileName = mesh_name;
            string fileExtension = "asset";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderpath + "/" + fileName + "." + fileExtension);
        }

        public static string GeneratePrefabPath(string root_name, string mesh_name)
        {
            // Assets/ModuleCreator/{root_name}/mesh_name.prefab
            string folderpath = $"Assets/ModuleCreator/{root_name}";
            CreateParent(folderpath);

            string fileName = $"{mesh_name}_MA";
            string fileExtension = "prefab";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderpath + "/" + fileName + "." + fileExtension);
        }

        public static string GenerateTexturePath(string root_name, string mesh_name)
        {
            // Assets/ModuleCreator/{root_name}/Texture/newMesh.asset
            string folderpath = $"Assets/ModuleCreator/{root_name}/Texture";
            CreateParent(folderpath);

            string fileName = mesh_name;
            string fileExtension = "png";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderpath + "/" + fileName + "." + fileExtension);
        }


        public static void CreateParent(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string parentFolder = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string folder = folders[i];
                    string newPath = parentFolder + "/" + folder;
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(parentFolder, folder);
                    }
                    parentFolder = newPath;
                }
            }
        }
    }
}