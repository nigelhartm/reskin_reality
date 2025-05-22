using System.IO;
using UnityEngine.Networking;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using System.Globalization;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine.Windows;
using File = System.IO.File;
using System;
using TMPro;
using UnityEngine.XR.Provider;
using UnityEngine.XR;

// API is still missing. To implement it just send the exported mesh to MeshyMesh and get the result back.
// The mesh is not exported in the correct format. So after import it needs to be adjusted to the room position, utilizing the Debug menu on left hand.

public class ReskinRoom : MonoBehaviour
{
    private MRUK mruk;
    [SerializeField] Material myMaterial;
    private GameObject effectMesh;
    private bool isLoadedSuccesful = false;
    private float lastTriggerTime = 0;
    private float buttonClickTimer = 0;
    private GameObject meshyMesh;
    [SerializeField] GameObject passthroughObject = null;
    [SerializeField] private GameObject debugGUI;
    private string[] texturePath = {"textures/scary",
                                    "textures/jungle",
                                    "textures/lava",};
    private int actTextureIndex = 3; // (passthrough texture)
    [SerializeField] private TMPro.TMP_Text debugText;

    void Start()
    {
        XRSettings.useOcclusionMesh = false; // prevent fisheye effect when taking video...


        Debug.Log("Start start");
        mruk = MRUK.Instance;
        mruk.SceneLoadedEvent.AddListener(sceneLoaded);
        Debug.Log("Start end");
    }

    // Called after the Scene Mesh got loaded
    void sceneLoaded()
    {
        Debug.Log("sceneLoaded start");
        // Get the Rooms global mesh
        MRUKRoom room = mruk.GetCurrentRoom();
        MRUKAnchor meshAnchor = room.GlobalMeshAnchor;
        Mesh mesh = meshAnchor.GlobalMesh;

        effectMesh = new GameObject("EffectMesh");
        effectMesh.transform.parent = meshAnchor.transform;
        effectMesh.transform.position = meshAnchor.transform.position;
        effectMesh.transform.rotation = meshAnchor.transform.rotation;
        effectMesh.SetActive(false);
        isLoadedSuccesful = true;
        //ExportMeshToOBJ(mesh, Path.Combine(Application.persistentDataPath, "ExportedMesh.obj"));
        loadMeshyMesh();
        Debug.Log("sceneLoaded end");
    }

    private void loadMeshyMesh() {
        Debug.Log("loadMeshyMesh start");
        MRUKRoom room = mruk.GetCurrentRoom();
        MRUKAnchor meshAnchor = room.GlobalMeshAnchor;
        //meshyMesh = Import(Application.streamingAssetsPath + "/model/ExportedMesh_0519084101_texture.obj");
        meshyMesh = Import("model2/ExportedMesh_0518180916_texture");
        meshyMesh.transform.parent = meshAnchor.transform;
        meshyMesh.transform.localPosition = meshAnchor.transform.position; // new Vector3(1.32f, -0.81f, -0.09f); kitchen
        meshyMesh.transform.rotation = meshAnchor.transform.rotation;
        meshyMesh.SetActive(false);
        Debug.Log("loadMeshyMesh end");
    }

    private void Update()
    {
        if (OVRInput.Get(OVRInput.Button.Start) &&
            Time.time - lastTriggerTime > 0.5f) {
            debugGUI.SetActive(!debugGUI.activeSelf);
            debugGUI.transform.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand);
            lastTriggerTime = Time.time;
        }
    }

    public void actiavteSkin()
    {
        if (isLoadedSuccesful && (Time.time - buttonClickTimer) > 0.5f)
        {
            Debug.Log("activateSkin");
            meshyMesh.SetActive(!meshyMesh.activeSelf);
            passthroughObject.SetActive(!passthroughObject.activeSelf);
            buttonClickTimer = Time.time;
        }
    }

    public void activateSkin()
    {
        if (isLoadedSuccesful)
        {
            meshyMesh.SetActive(true);
            passthroughObject.SetActive(false);
        }
    }
    public void deactivateSkin()
    {
        if (isLoadedSuccesful)
        {
            meshyMesh.SetActive(false);
            passthroughObject.SetActive(true);
        }
    }

    public void changeTexture()
    {
        if (isLoadedSuccesful && (Time.time - buttonClickTimer) > 0.5f)
        {
            buttonClickTimer = Time.time;
            Debug.Log("changeTexture");

            Material currentMaterial = null;

            Debug.Log("actTextureIndex" + actTextureIndex);
            actTextureIndex++;
            Debug.Log("new index" + actTextureIndex);
            if (actTextureIndex > texturePath.Length)
            {
                actTextureIndex = 0;
                activateSkin();
            }
            if (actTextureIndex == texturePath.Length) { // (Passthrough)
                deactivateSkin();
                return;
            }
           Texture tex = Resources.Load<Texture>(texturePath[actTextureIndex]);

            currentMaterial = new Material(Shader.Find("Unlit/Texture"));
            currentMaterial.mainTexture = tex;

            if (currentMaterial != null) {
                meshyMesh.GetComponent<MeshRenderer>().material = currentMaterial ;
            }
            return;
        }
    }

    public static void ExportMeshToOBJ(Mesh mesh, string path)
    {
        Debug.Log("exportMeshToObj start");
        using (StreamWriter sw = new StreamWriter(path))
        {
            sw.WriteLine("# Exported from Unity");
            // Write vertices
            foreach (Vector3 v in mesh.vertices)
            {
                sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "v {0} {1} {2}", v.x, v.y, v.z));
            }

            /* Write normals (optional) if (mesh.normals.Length == mesh.vertices.Length){foreach (Vector3 n in mesh.normals){sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "vn {0} {1} {2}", n.x, n.y, n.z));}}*/
            /* Write UVs (optional) if (mesh.uv.Length == mesh.vertices.Length){foreach (Vector2 uv in mesh.uv){sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "vt {0} {1}", uv.x, uv.y));}}*/

            // Write faces (1-based indexing for OBJ!)
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                int i1 = mesh.triangles[i] + 1;
                int i2 = mesh.triangles[i + 1] + 1;
                int i3 = mesh.triangles[i + 2] + 1;
                sw.WriteLine(string.Format("f {0} {1} {2}", i1, i2, i3));
            }
        }
        Debug.Log("Mesh exported to: " + path);
        Debug.Log("exportMeshToObj end");
    }

    /*//Import before Resource path
     public static GameObject Import(string objPath)
    {
        Debug.Log("import start");
        string directory = Path.GetDirectoryName(objPath);
        string[] objLines = File.ReadAllLines(objPath);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        List<Vector2> finalUVs = new List<Vector2>();
        List<Vector3> finalVerts = new List<Vector3>();

        string mtlFile = null;

        Dictionary<string, Material> materials = new Dictionary<string, Material>();
        Material currentMaterial = null;

        foreach (string line in objLines)
        {
            string[] parts = line.Trim().Split(' ');
            if (parts.Length < 1) continue;

            switch (parts[0])
            {
                case "mtllib":
                    mtlFile = Path.Combine(directory, parts[1]);
                    break;

                case "v":
                    vertices.Add(ConvertBlenderToUnity(ParseVector3(parts)));
                    //vertices.Add(ParseVector3(parts));
                    break;

                case "vt":
                    uvs.Add(ParseVector2(parts));
                    break;

                case "vn":
                    normals.Add(ConvertBlenderToUnity(ParseVector3(parts)).normalized);
                    //normals.Add(ParseVector3(parts));
                    break;

                case "f":
                    for (int i = 1; i < 4; i++)
                    {
                        string[] subParts = parts[i].Split('/');
                        int vIndex = int.Parse(subParts[0]) - 1;
                        int vtIndex = int.Parse(subParts[1]) - 1;

                        finalVerts.Add(vertices[vIndex]);
                        finalUVs.Add(uvs[vtIndex]);
                        triangles.Add(finalVerts.Count - 1);
                    }
                    break;
            }
        }

        // Load material and texture
        if (!string.IsNullOrEmpty(mtlFile) && File.Exists(mtlFile))
        {
            string[] mtlLines = File.ReadAllLines(mtlFile);
            foreach (string line in mtlLines)
            {
                if (line.StartsWith("map_Kd"))
                {
                    string texName = line.Split(' ')[1].Trim();
                    string texPath = Path.Combine(directory, texName);

                    if (File.Exists(texPath))
                    {
                        byte[] data = File.ReadAllBytes(texPath);
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(data);

                        currentMaterial = new Material(Shader.Find("Unlit/Texture"));
                        currentMaterial.mainTexture = texture;
                        break;
                    }
                }
            }
        }

        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = "ImportedMesh";
        mesh.vertices = finalVerts.ToArray();
        mesh.uv = finalUVs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        GameObject obj = new GameObject("ImportedOBJ");
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = currentMaterial ?? new Material(Shader.Find("Standard"));
        Debug.Log("import end");
        return obj;
    }*/

    public GameObject Import(string objPath)
    {
        Debug.Log("import start");
        string directory = Path.GetDirectoryName(objPath);

        //string[] objLines = File.ReadAllLines(objPath);
        TextAsset objAsset = Resources.Load<TextAsset>(objPath);
        if (objAsset == null)
        {
            Debug.LogError("OBJ file not found in Resources " + objPath);
            debugText.text = "OBJ file not found in Resources " + objPath;
            return null;
        }
        else {
            Debug.Log("OBJ file found in Resources " + objPath);
            debugText.text = "OBJ file found in Resources " + objPath;
        }
        string[] objLines = objAsset.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        debugText.text += objLines[0];
        debugText.text += objLines[1];
        debugText.text += objLines[2];
        debugText.text += objLines[3];


        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        List<Vector2> finalUVs = new List<Vector2>();
        List<Vector3> finalVerts = new List<Vector3>();

        string mtlFile = null;

        Dictionary<string, Material> materials = new Dictionary<string, Material>();
        Material currentMaterial = null;

        foreach (string line in objLines)
        {
            string[] parts = line.Trim().Split(' ');
            if (parts.Length < 1) continue;

            switch (parts[0])
            {
                case "mtllib":
                    mtlFile = Path.Combine(directory, parts[1]);
                    break;

                case "v":
                    vertices.Add(ConvertBlenderToUnity(ParseVector3(parts)));
                    //vertices.Add(ParseVector3(parts));
                    break;

                case "vt":
                    uvs.Add(ParseVector2(parts));
                    break;

                case "vn":
                    normals.Add(ConvertBlenderToUnity(ParseVector3(parts)).normalized);
                    //normals.Add(ParseVector3(parts));
                    break;

                case "f":
                    for (int i = 1; i < 4; i++)
                    {
                        string[] subParts = parts[i].Split('/');
                        int vIndex = int.Parse(subParts[0]) - 1;
                        int vtIndex = int.Parse(subParts[1]) - 1;

                        finalVerts.Add(vertices[vIndex]);
                        finalUVs.Add(uvs[vtIndex]);
                        triangles.Add(finalVerts.Count - 1);
                    }
                    break;
            }
        }

        Texture texture = Resources.Load<Texture>(objPath);

        currentMaterial = new Material(Shader.Find("Unlit/Texture"));
        currentMaterial.mainTexture = texture;

        // Create mesh
        Mesh mesh = new Mesh();
        mesh.name = "ImportedMesh";
        mesh.vertices = finalVerts.ToArray();
        mesh.uv = finalUVs.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        GameObject obj = new GameObject("ImportedOBJ");
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = currentMaterial ?? new Material(Shader.Find("Standard"));
        Debug.Log("import end");
        return obj;
    }

    private static Vector3 ConvertBlenderToUnity(Vector3 v)
    {
        float scaleFix = 3.0f; // or 2.95f if precision needed
        return new Vector3(
            v.x * scaleFix,
            v.y * scaleFix,     // Blender Z  Unity Y
            v.z * scaleFix     // Blender Y  Unity -Z
        );
    }

    private static Vector3 ParseVector3(string[] parts)
    {
        return new Vector3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3]));
    }

    private static Vector2 ParseVector2(string[] parts)
    {
        return new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2]));
    }

    private static float ParseFloat(string s)
    {
        return float.Parse(s.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
    }

    public void debugChangeX(float value)
    {
        meshyMesh.transform.localPosition = new Vector3(value, meshyMesh.transform.localPosition.y, meshyMesh.transform.localPosition.z);
    }

    public void debugChangeY(float value)
    {
        meshyMesh.transform.localPosition = new Vector3(meshyMesh.transform.localPosition.x, value, meshyMesh.transform.localPosition.z);
    }

    public void debugChangeZ(float value)
    {
        meshyMesh.transform.localPosition = new Vector3(meshyMesh.transform.localPosition.x, meshyMesh.transform.localPosition.y, value);
    }
}