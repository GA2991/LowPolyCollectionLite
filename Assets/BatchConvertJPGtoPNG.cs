using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BatchConvertImageToPNG : MonoBehaviour
{
    void Start()
    {
        ConvertAllTextures();
    }

    void ConvertAllTextures()
    {
        // Crear la carpeta si no existe
        string folderPath = Path.Combine(Application.dataPath, "ConvertedTextures");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"Carpeta creada: {folderPath}");
        }

        // Buscar archivos con extensión .jpg y .jpeg en el proyecto
        string[] jpgPaths = Directory.GetFiles(Application.dataPath, "*.jpg", SearchOption.AllDirectories);
        string[] jpegPaths = Directory.GetFiles(Application.dataPath, "*.jpeg", SearchOption.AllDirectories);
        string[] allTextures = jpgPaths.Concat(jpegPaths).ToArray();

        Dictionary<string, string> convertedTextures = new Dictionary<string, string>();

        foreach (string path in allTextures)
        {
            Texture2D texture = LoadImage(path);
            if (texture != null)
            {
                string newPath = SaveTextureAsPNG(texture, path, folderPath);
                convertedTextures[path] = newPath;

                #if UNITY_EDITOR
                AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Importado en Unity: {newPath}");
                #endif
            }
        }

        #if UNITY_EDITOR
        AssetDatabase.Refresh(); // Refrescar para que Unity reconozca los cambios
        #endif

        VerifyTexturesAreAssigned(convertedTextures);
        Debug.Log($"Conversión completa. Se convirtieron {convertedTextures.Count} texturas.");
    }

    Texture2D LoadImage(string path)
    {
        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        texture.name = Path.GetFileNameWithoutExtension(path);
        return texture;
    }

    string SaveTextureAsPNG(Texture2D texture, string oldPath, string folderPath)
    {
        string newPath = Path.Combine(folderPath, Path.GetFileName(oldPath).Replace(".jpg", ".png").Replace(".jpeg", ".png"));
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(newPath, pngData);
        Debug.Log($"Guardado PNG: {newPath}");
        return "Assets/ConvertedTextures/" + Path.GetFileName(newPath); // Ruta en Unity para asignación
    }

    void VerifyTexturesAreAssigned(Dictionary<string, string> convertedTextures)
    {
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.mainTexture != null) // Asegurar que no es null
                {
                    string oldName = material.mainTexture.name;
                    if (string.IsNullOrEmpty(oldName))
                    {
                        Debug.LogWarning("Material sin nombre de textura, saltando...");
                        continue;
                    }

                    string newPath = convertedTextures.Values.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == oldName);

                    if (!string.IsNullOrEmpty(newPath))
                    {
                        Debug.Log($"Intentando reasignar: {oldName} → {Path.GetFileName(newPath)}");

                        Texture2D newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
                        
                        if (newTexture != null)
                        {
                            material.mainTexture = newTexture;
                            Debug.Log($"Reasignado correctamente: {oldName} → {Path.GetFileName(newPath)}");
                        }
                        else
                        {
                            Debug.LogWarning($"Error al cargar la nueva textura PNG: {newPath}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No se encontró una nueva textura para: {oldName}");
                    }
                }
                else
                {
                    Debug.LogWarning("Material sin textura asignada, saltando...");
                }
            }
        }
    }
}
