using System.IO;
using UnityEditor;

namespace com.aoyon.scenemeshutils
{
    public static class AssetPathUtility
    {
        private const string BASEPATH = "Assets/SceneMeshUtils";
        
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