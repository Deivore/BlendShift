#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;

public class LBM_JSON_Importer : AssetPostprocessor
{
    [System.Serializable]
    public class LBM_Asset
    {
        [System.Serializable]
        public class cycles_Asset
        {
            public int reverse;
            public int rate;
            public int low;
            public int high;

            public static explicit operator Vector4(cycles_Asset cycles)
            {
                return new Vector4(cycles.reverse, cycles.rate, cycles.low, cycles.high);
            }

            public static List<Vector4> ListCast(List<cycles_Asset> cycles)
            {
                List<Vector4> vectors = new List<Vector4>();
                foreach(cycles_Asset cycle in cycles)
                {
                    vectors.Add((Vector4)cycle);
                }
                return vectors;
            }

            public static Vector4[] ArrayCast(List<cycles_Asset> cycles)
            {
                Vector4[] vectors = new Vector4[20];
                for (int i = 0; i < cycles.Count; i++)
                {
                    vectors[i]= (Vector4)(cycles[i]);
                }
                return vectors;
            }
        }

        //Unity doesn't currently support serializable multi-dimensional collections so we need this ridiculous thing.
        [System.Serializable]
        public class ColorTriple
        {
            public int R;
            public int G;
            public int B;
        }

        public string filename;
        public int width;
        public int height;
        public List<ColorTriple> colors;
        public List<cycles_Asset> cycles;
        public List<int> pixels;

        public static LBM_Asset CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LBM_Asset>(jsonString);
        }

        //ISerializationCallbackReceiver
    }

    private static string type = ".lbm";//"LBM.json";
    private static EditorApplication.CallbackFunction _importDelegate;
    private static List<string> _assetsMarkedForImport = new List<string>();

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (var path in importedAssets)
        {
            if (path.LastIndexOf('.') == path.ToLower().IndexOf(type.ToLower()))
            {
                if (_importDelegate == null)
                {
                    _importDelegate = new EditorApplication.CallbackFunction(ImportLBM);
                }
                _assetsMarkedForImport.Add(path);
                EditorApplication.update = Delegate.Combine(EditorApplication.update, _importDelegate) as EditorApplication.CallbackFunction;
            
                //ImportLBM(path);
            }
        }
    }

    private static void ImportLBM()
    {
        EditorApplication.update = Delegate.Remove(EditorApplication.update, _importDelegate as EditorApplication.CallbackFunction) as EditorApplication.CallbackFunction;

        try
        {

            foreach (string assetPath in _assetsMarkedForImport)
            {
                //get folder location
                string assetPathLower = assetPath.ToLower();
                string folder = assetPathLower.Substring(0, assetPathLower.IndexOf(type.ToLower()));

                if (Launchlbm2json(assetPathLower))
                {
                    AssetDatabase.Refresh();

                    LBM_Asset lbm = JsonUtility.FromJson<LBM_Asset>(File.ReadAllText(assetPathLower + ".json"));
                    if (lbm.width == 0 || lbm.height == 0 || lbm.pixels.Count != lbm.width * lbm.height)
                    {
                        throw new System.Exception("Bad LBM.json file!");
                    }

                    //Get the name
                    string filename = lbm.filename.Substring(0, lbm.filename.LastIndexOf('.') - 1);

                    //---------------
                    //Make the indexed image
                    var indexedTexture = new Texture2D(lbm.width, lbm.height, TextureFormat.ARGB32, false);
                    for (int i = 0; i < lbm.height; i++)
                    {
                        for (int j = 0; j < lbm.width; j++)
                        {
                            int index = i * lbm.width + j;
                            indexedTexture.SetPixel(j, lbm.height - i - 1, new Color(lbm.pixels[index] / 255f, lbm.pixels[index] / 255f, lbm.pixels[index] / 255f, 1.0f));
                        }
                    }
                    indexedTexture.filterMode = FilterMode.Point;
                    indexedTexture.Apply();

                    //byte[] Ibytes = indexedTexture.EncodeToPNG();
                    //File.WriteAllBytes(folder + "_indices.png", Ibytes);
                    File.WriteAllBytes(folder + "_indices.png", indexedTexture.EncodeToPNG());
                    AssetDatabase.Refresh();

                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(folder + "_indices.png"); //returns null
                                                                                                                  //TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(folder + "_indices.png"); //returns null
                    importer.spritePixelsPerUnit = 1; //null pointer crash
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();

                    //---------------
                    //Make the palette image
                    var paletteTexture = new Texture2D(256, 1);
                    paletteTexture.filterMode = FilterMode.Point;
                    for (int i = 0; i < 256; i++)
                    {
                        paletteTexture.SetPixel(i, 0, new Color(lbm.colors[i].R / 255f, lbm.colors[i].G / 255f, lbm.colors[i].B / 255f, 1.0f));
                    }
                    paletteTexture.Apply();
                    byte[] Pbytes = paletteTexture.EncodeToPNG();
                    File.WriteAllBytes(folder + "_palette.png", Pbytes);
                    //Object.DestroyImmediate(paletteTexture);
                    //AssetDatabase.CreateAsset(paletteTexture, folder + "_palette.asset");
                    AssetDatabase.Refresh();

                    //Set palette importer info
                    importer = (TextureImporter)TextureImporter.GetAtPath(folder + "_palette.png");
                    importer.spritePixelsPerUnit = 1;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.filterMode = FilterMode.Point;
                    importer.isReadable = true;
                    importer.SaveAndReimport();

                    //---------------
                    //Mash 'em up with a material+shader that uses the cycles args.
                    Texture2D loadedPalette = (Texture2D)AssetDatabase.LoadAssetAtPath(folder + "_palette.png", typeof(Texture2D));
                    Texture2D loadedTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(folder + "_indices.png", typeof(Texture2D));
                    Shader Blendshift = Shader.Find("ColorCycling/BlendShift");
                    Material cyclingMaterial = new Material(Blendshift);

                    //Add shader values for this file
                    cyclingMaterial.SetTexture(Shader.PropertyToID("_Palette"), loadedPalette);
                    cyclingMaterial.SetTexture(Shader.PropertyToID("_MainTex"), loadedTexture);
                    cyclingMaterial.SetInt("_NumCycles", lbm.cycles.Count);
                    //cyclingMaterial.SetVectorArray("_Cycles", LBM_Asset.cycles_Asset.ArrayCast( lbm.cycles));

                    AssetDatabase.CreateAsset(cyclingMaterial, folder + "_mat.mat");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    //Create the actual image, attach materials and textures, attach a SetCycles and set its array
                    UnityEngine.Object ImagePrefab = PrefabUtility.CreateEmptyPrefab(folder + ".prefab");

                    GameObject CyclingGO = new GameObject();
                    SpriteRenderer cyclingSprite = CyclingGO.AddComponent<SpriteRenderer>();
                    cyclingSprite.sprite = (Sprite)AssetDatabase.LoadAssetAtPath(folder + "_indices.png", typeof(Sprite));
                    cyclingSprite.material = (Material)AssetDatabase.LoadAssetAtPath(folder + "_mat.mat", typeof(Material));
                    SetCycles cycleSetter = CyclingGO.AddComponent<SetCycles>();
                    cycleSetter.cycles = LBM_Asset.cycles_Asset.ArrayCast(lbm.cycles);

                    PrefabUtility.ReplacePrefab(CyclingGO, ImagePrefab, ReplacePrefabOptions.ConnectToPrefab);
                    GameObject.DestroyImmediate(CyclingGO);
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log(e);
        }
        finally
        {
            _assetsMarkedForImport.Clear();
        }
    }

    static bool Launchlbm2json(string path)
    {
        // For the example

        // Use ProcessStartInfo class

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.CreateNoWindow = false;
        startInfo.UseShellExecute = true;
        startInfo.FileName = System.Environment.CurrentDirectory + "/Assets/BlendShift/lbm2json.exe";
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.Arguments = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1) + path;

        try
        {
            // Start the process with the info we specified.
            // Call WaitForExit and then the using statement will close.
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
            return true;
        }
        catch( Exception e)
        {
            UnityEngine.Debug.Log(e);
            return false;
        }
    }

}

#endif