using System.IO;
using UnityEditor;

namespace com.aoyon.scenemeshutils
{
    public static class AssetPathUtility
    {
        private const string BASEPATH = "Assets/SceneMeshUtils";
        
        public static string GenerateMeshPath(string root_name, string mesh_name)
        {
            string folderpath = $"{BASEPATH}/{root_name}/Mesh";
            if (!Directory.Exists(folderpath)) Directory.CreateDirectory(folderpath);

            string fileName = mesh_name;
            string fileExtension = "asset";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderpath + "/" + fileName + "." + fileExtension);
        }

        public static string GeneratePrefabPath(string root_name, string mesh_name)
        {
            string folderpath =  $"{BASEPATH}/{root_name}/Prefab";;
            if (!Directory.Exists(folderpath)) Directory.CreateDirectory(folderpath);

            string fileName = $"{mesh_name}_MA";
            string fileExtension = "prefab";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderpath + "/" + fileName + "." + fileExtension);
        }

        public static string GenerateTexturePath(string root_name, string mesh_name)
        {
            string folderpath =  $"{BASEPATH}/{root_name}/Texture";;
            if (!Directory.Exists(folderpath)) Directory.CreateDirectory(folderpath);

            string fileName = mesh_name;
            string fileExtension = "png";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderpath + "/" + fileName + "." + fileExtension);
        }
    }
}