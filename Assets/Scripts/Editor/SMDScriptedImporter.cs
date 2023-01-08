using UnityEngine;

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

// SMD file format importer
// Following specs at https://developer.valvesoftware.com/wiki/Studiomdl_Data

namespace SilentTools
{

[CustomEditor(typeof(SMDScriptedImporter))]
[CanEditMultipleObjects]
public class SMDScriptedImporterEditor : UnityEditor.AssetImporters.ScriptedImporterEditor
{
}

[UnityEditor.AssetImporters.ScriptedImporter(1, "smd")]
public class SMDScriptedImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public float m_Scale = 1;
    public bool m_Loop = true;
    public int m_RemoveFirstEntries = 1;

    [SerializeField] private float m_Version = 0;

    // For debugging
    [SerializeField] List<SMDFrame> frames;
    // [SerializeField] List<string> parsedLines;

    enum ParsingState
    {
        None,
        Nodes,
        Skeleton, 
        Triangles
    }

    [Serializable]
    class Node {
        public string name;
        public int parent;
    }

    [Serializable]
    class SMDTransform
    {
        public int node;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable]
    class SMDFrame
    {
        public int time;
        public List<SMDTransform> transforms;
    }

    class ClipTransformCurves
    {
        public AnimationCurve posX = new AnimationCurve();
        public AnimationCurve posY = new AnimationCurve();
        public AnimationCurve posZ = new AnimationCurve();

        public AnimationCurve rotX = new AnimationCurve();
        public AnimationCurve rotY = new AnimationCurve();
        public AnimationCurve rotZ = new AnimationCurve();
        public AnimationCurve rotW = new AnimationCurve();
    }

    Vector3 parseVector3(string x, string y, string z)
    {
        Vector3 vec = new Vector3();
        vec.x = float.Parse(x);
        vec.y = float.Parse(y);
        vec.z = float.Parse(z);
        return vec;
    }

    // https://forum.unity.com/threads/right-hand-to-left-handed-conversions.80679/
    public static Quaternion ConvertMayaRotationToUnity(Vector3 rotation) {
        Vector3 flippedRotation = new Vector3(rotation.x, -rotation.y, -rotation.z); // flip Y and Z axis for right->left handed conversion
        // convert XYZ to ZYX
        Quaternion qx = Quaternion.AngleAxis(flippedRotation.x, Vector3.right);
        Quaternion qy = Quaternion.AngleAxis(flippedRotation.y, Vector3.up);
        Quaternion qz = Quaternion.AngleAxis(flippedRotation.z, Vector3.forward);
        Quaternion qq = qz * qy * qx; // this is the order
        return qq;
    }

    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        var clip = new AnimationClip();

        var smdRawData = File.ReadAllText(ctx.assetPath);
        var smdLines = smdRawData.Split('\n');
        ParsingState state = ParsingState.None;
        List<Node> parsedNodes = new List<Node>();
        List<SMDFrame> parsedFrames = new List<SMDFrame>();
        List<string> curvePaths;

        foreach (string smdLine in smdLines)
        {
            var lineData = smdLine.Trim().Split(' ');

            if (lineData.Length > 0)
            {
                // Check if this is a comment. 
                if ((  lineData[0].Length > 2 && lineData[0].Substring(0, 2) == "//")
                    || lineData[0].Length > 1 && lineData[0][0] == '#'
                    || lineData[0].Length > 1 && lineData[0][0] == ';')
                {
                    // Ignore this line.
                    continue;
                }

                // Check if we're already parsing, otherwise do the "first" stuff.
                // Nodes identifies the node hierarchy in the file. 
                if (state == ParsingState.Nodes)
                {
                    if (lineData[0] == "end")
                    {
                        state = ParsingState.None;
                        continue;
                    }

                    if (int.Parse(lineData[0]) != parsedNodes.Count) 
                    {
                        Debug.LogError("Failed to read nodes!");
                        break;
                    }
                    parsedNodes.Add(new Node() 
                    { 
                        // Remove the quotes from around the name.
                        name = lineData[1].Substring(1, lineData[1].Length - 2), 
                        parent = int.Parse(lineData[2]) 
                    });
                }

                // Skeleton contains the animation data.
                if (state == ParsingState.Skeleton)
                {
                    if (lineData[0] == "end")
                    {
                        state = ParsingState.None;
                        continue;
                    }
                    
                    if (lineData[0] == "time")
                    {
                        parsedFrames.Add(new SMDFrame() 
                        { 
                            time = int.Parse(lineData[1]),
                            transforms = new List<SMDTransform>()
                        });
                    }
                    else 
                    {
                        int currentFrame = parsedFrames.Count;
                        if (currentFrame > 0)
                        {
                            parsedFrames[currentFrame-1].transforms.Add(new SMDTransform()
                            {
                                node = int.Parse(lineData[0]),
                                position = parseVector3(lineData[1], lineData[2], lineData[3]),
                                rotation = parseVector3(lineData[4], lineData[5], lineData[6]) * Mathf.Rad2Deg
                            });
                        }
                    }
                }

                if (state == ParsingState.Triangles)
                {
                    if (lineData[0] == "end")
                    {
                        state = ParsingState.None;
                        continue;
                    }
                }

                if (state == ParsingState.None)
                {
                    // Start of file
                    if (lineData[0] == "version")
                    {
                        m_Version = float.Parse(lineData[1]);
                    }
                    
                    if (lineData[0] == "nodes")
                    {
                        state = ParsingState.Nodes;
                    }
                    
                    if (lineData[0] == "skeleton")
                    {
                        state = ParsingState.Skeleton;
                    }
                    
                    if (lineData[0] == "triangles")
                    {
                        state = ParsingState.Triangles;
                    }
                }
            }
        }

        frames = parsedFrames;

        // Make the list of node paths in advance
        curvePaths =  new List<string>(parsedNodes.Count);
        foreach (Node n in parsedNodes)
        {
            List<string> thisPath = new List<string>();
            thisPath.Add(n.name);

            int parentID = n.parent;
            while (parentID > -1)
            {
                thisPath.Add(parsedNodes[parentID].name);
                parentID = parsedNodes[parentID].parent;
            }

            thisPath.Reverse();
            thisPath.RemoveRange(0, m_RemoveFirstEntries);

            curvePaths.Add(string.Join("/", thisPath));
        }

        // Parse the frames into Unity AnimationClips and AnimationCurves
        Dictionary<string, ClipTransformCurves> animationCurves = new Dictionary<string, ClipTransformCurves>();
        foreach (SMDFrame frame in frames)
        {
            // It seems like AddKey takes time in seconds, so divide frame by 60.
            float time = frame.time / 60.0f;

            foreach (SMDTransform transform in frame.transforms)
            {
                // Construct path
                // Path is at curvePaths[transform.node]

                // Construct curves
                // Each transform needs to set...
                // transform.localPosition x, y, z
                // transform.localRotation x, y, z, w (quaternions...)
                if (!animationCurves.ContainsKey(curvePaths[transform.node]))
                {
                    animationCurves.Add(curvePaths[transform.node], new ClipTransformCurves());
                } 
                
                animationCurves[curvePaths[transform.node]].posX.AddKey(time, transform.position.x * m_Scale * -1);
                animationCurves[curvePaths[transform.node]].posY.AddKey(time, transform.position.y * m_Scale);
                animationCurves[curvePaths[transform.node]].posZ.AddKey(time, transform.position.z * m_Scale);

                Quaternion thisRotation = new Quaternion();
                thisRotation = ConvertMayaRotationToUnity(transform.rotation);

                animationCurves[curvePaths[transform.node]].rotX.AddKey(time, thisRotation.x);
                animationCurves[curvePaths[transform.node]].rotY.AddKey(time, thisRotation.y);
                animationCurves[curvePaths[transform.node]].rotZ.AddKey(time, thisRotation.z);
                animationCurves[curvePaths[transform.node]].rotW.AddKey(time, thisRotation.w);
            }
        }

        foreach (KeyValuePair<string, ClipTransformCurves> entry in animationCurves)
        {
            clip.SetCurve(entry.Key, typeof(Transform), "localPosition.x", entry.Value.posX);
            clip.SetCurve(entry.Key, typeof(Transform), "localPosition.y", entry.Value.posY);
            clip.SetCurve(entry.Key, typeof(Transform), "localPosition.z", entry.Value.posZ);

            clip.SetCurve(entry.Key, typeof(Transform), "localRotation.x", entry.Value.rotX);
            clip.SetCurve(entry.Key, typeof(Transform), "localRotation.y", entry.Value.rotY);
            clip.SetCurve(entry.Key, typeof(Transform), "localRotation.z", entry.Value.rotZ);
            clip.SetCurve(entry.Key, typeof(Transform), "localRotation.w", entry.Value.rotW);
        }

        // WrapMode needs to be assigned to AnimationCurves, so this doesn't have an effect, 
        clip.wrapMode = m_Loop? WrapMode.Loop : WrapMode.Once;
        clip.EnsureQuaternionContinuity();

        ctx.AddObjectToAsset("main", clip);
        ctx.SetMainObject(clip);
    }
}
} // namespace SilentTools