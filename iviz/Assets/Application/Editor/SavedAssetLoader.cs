﻿using System;
using System.Collections.Generic;
using System.IO;
using Iviz.Displays;
using Iviz.Resources;
using UnityEditor;
using UnityEngine;
using LightType = Iviz.Sdf.LightType;

namespace Iviz.Editor
{
    public class SavedAssetLoader : UnityEditor.Editor
    { 
        [MenuItem("Iviz/Import Saved Assets To Unity")]
        public static void CreateAllAssets()
        {
            ExternalResourceManager manager = Resource.External;
            IReadOnlyList<Uri> resourceUris = manager.GetListOfModels();

            Debug.Log("SavedAssetLoader: Transferring " + resourceUris.Count + " assets...");
            foreach (Uri uri in resourceUris)
            {
                CreateAsset(uri, manager);
            }
            
            CreateRobots(manager);
            AssetDatabase.Refresh();
            
            Debug.Log("SavedAssetLoader: Done!");

            // ugly workaround
            GameObject managerNode = GameObject.Find("External Resources");
            if (managerNode != null)
            {
                DestroyImmediate(managerNode);
            }

            /*
            string packagePath = "/Users/akzeac/Shared/aws-robomaker-hospital-world";
            string localPath = "/worlds/hospital.world";
            
            string xmlData = File.ReadAllText(packagePath + localPath);
            Sdf.SdfFile sdf = Sdf.SdfFile.Create(xmlData);
            
            var modelPaths = Sdf.SdfFile.CreateModelPaths(packagePath);
            Sdf.SdfFile newSdf = sdf.ResolveIncludes(modelPaths);
            
            CreateWorld(newSdf.Worlds[0]);
            */

        }


        static void CreateRobots(ExternalResourceManager manager)
        {
            string unityDirectory = "Resources/Package/iviz/robots";
            string absolutePath = UnityEngine.Application.dataPath + "/" + unityDirectory;
            Directory.CreateDirectory(absolutePath);

            Dictionary<string, string> resourceFile = new Dictionary<string, string>();
            foreach (string robotName in manager.GetRobotNames())
            {
                string filename = ExternalResourceManager.SanitizeForFilename(robotName).Replace(".", "_");
                resourceFile[robotName] = filename;
            }

            //string text = JsonConvert.SerializeObject(resourceFile, Formatting.Indented);
            //File.WriteAllText(absolutePath + "/../resources.txt", text);
            Debug.LogWarning("SavedAssetLoader: Not writing robot resource files.");
            
            foreach (string robotName in manager.GetRobotNames())
            {
                manager.TryGetRobot(robotName, out string robotDescription);
                string filename = ExternalResourceManager.SanitizeForFilename(robotName).Replace(".", "_");
                File.WriteAllText(absolutePath + "/" + filename + ".txt", robotDescription);
            }
        }


        static void CreateAsset(Uri assetUri, ExternalResourceManager manager)
        {
            const string basePath = "Assets/Resources/Package/";
            string uriPath = assetUri.Host + Uri.UnescapeDataString(assetUri.AbsolutePath);

            string relativePath = Path.GetDirectoryName(uriPath);
            if (!string.IsNullOrEmpty(relativePath) && Path.DirectorySeparatorChar == '\\')
            {
                relativePath = relativePath.Replace('\\', '/');
            }            
            
            string filename = Path.GetFileNameWithoutExtension(uriPath);
            string filenameWithExtension = Path.GetFileName(uriPath);

            string topPath = basePath + relativePath;
            string innerPath = topPath + "/" + filename;
            string unityDirectory = "Resources/Package/" + relativePath + "/" + filename;

            string absolutePath = UnityEngine.Application.dataPath + "/" + unityDirectory;

            if (Directory.Exists(absolutePath))
            {
                Debug.Log("SavedAssetLoader: Skipping " + assetUri + "...");
                return;
            }
            
            Directory.CreateDirectory(absolutePath);
            
            if (!manager.TryGet(assetUri, out Info<GameObject> resourceInfo, null))
            {
                throw new FileNotFoundException(assetUri.ToString());
            }
            
            GameObject obj = resourceInfo.Object;             

            MeshTrianglesResource[] resources = obj.GetComponentsInChildren<MeshTrianglesResource>();

            HashSet<Mesh> writtenMeshes = new HashSet<Mesh>();
            foreach (var resource in resources)
            {
                MeshFilter filter = resource.GetComponent<MeshFilter>();
                if (writtenMeshes.Contains(filter.sharedMesh))
                {
                    continue;
                }

                if (filter.sharedMesh.vertexCount != 0)
                {
                    Unwrapping.GenerateSecondaryUVSet(filter.sharedMesh);
                }

                writtenMeshes.Add(filter.sharedMesh);
                string meshPath = AssetDatabase.GenerateUniqueAssetPath(innerPath + "/mesh.mesh");
                AssetDatabase.CreateAsset(filter.sharedMesh, meshPath);
            }

            Material baseMaterial = UnityEngine.Resources.Load<Material>("Materials/Standard");

            Dictionary<(Color, Color, Texture2D), Material> writtenColors = new Dictionary<(Color, Color, Texture2D), Material>();
            foreach (var resource in resources)
            {
                MeshRenderer renderer = resource.GetComponent<MeshRenderer>();
                Color color = resource.Color;
                Color emissive = resource.EmissiveColor;
                Texture2D texture = resource.Texture;
                
                if (writtenColors.TryGetValue((color, emissive, texture), out Material material))
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                material = Instantiate(baseMaterial);
                material.color = color;
                material.mainTexture = texture;
                renderer.sharedMaterial = material;

                writtenColors.Add((color, emissive, texture), material);
                string materialPath = AssetDatabase.GenerateUniqueAssetPath(innerPath + "/material.mat");
                AssetDatabase.CreateAsset(material, materialPath);

                if (texture == null)
                {
                    continue;
                }

                if (AssetDatabase.Contains(texture))
                {
                    material.mainTexture = texture;
                    continue;
                }

                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(absolutePath + "/texture.png", bytes);
                AssetDatabase.Refresh();

                string texturePath = innerPath + "/texture.png";
                Texture2D newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                material.mainTexture = newTexture;
            }


            foreach (var resource in resources)
            {
                BoxCollider collider = resource.GetComponent<BoxCollider>();
                collider.center = resource.LocalBounds.center;
                collider.size = resource.LocalBounds.size;
                collider.enabled = false;
                
                //DestroyImmediate(resource);
                resource.enabled = false;
            }

            foreach (var marker in obj.GetComponentsInChildren<AggregatedMeshMarkerResource>())
            {
                //DestroyImmediate(marker);
                marker.enabled = false;
            }

            Debug.Log("Writing to " + topPath + "/" + filenameWithExtension + ".prefab");
            PrefabUtility.SaveAsPrefabAssetAndConnect(obj, topPath + "/" + filenameWithExtension + ".prefab", InteractionMode.UserAction);

            //DestroyImmediate(obj);
        }

        
        static void CreateWorld(Sdf.World world)
        {
            GameObject worldObject = new GameObject("World:" + world.Name);
            
            foreach (Sdf.Model model in world.Models)
            {
                CreateModel(model)?.transform.SetParent(worldObject.transform, false);
            }

            foreach (Sdf.Light source in world.Lights)
            {
                GameObject lightObject = new GameObject("Light:" + source.Name);
                lightObject.transform.parent = worldObject.transform;
                Light light = lightObject.AddComponent<Light>();
                light.color = source.Diffuse.ToColor();
                light.lightmapBakeType = LightmapBakeType.Mixed;
                light.shadows = source.CastShadows ? LightShadows.Soft : LightShadows.None;
                lightObject.transform.SetLocalPose(source.Pose.ToPose());
                light.range = 20;
                switch (source.Type)
                {
                    default:
                        light.type = UnityEngine.LightType.Point;
                        break;
                    case LightType.Spot:
                        light.type = UnityEngine.LightType.Spot;
                        light.transform.LookAt(light.transform.position + source.Direction.ToVector3());
                        light.spotAngle = (float)source.Spot.OuterAngle * Mathf.Rad2Deg;
                        light.innerSpotAngle = (float) source.Spot.InnerAngle * Mathf.Rad2Deg;
                        break;
                    case LightType.Directional:
                        light.type = UnityEngine.LightType.Directional; 
                        light.transform.LookAt(light.transform.position + source.Direction.ToVector3());
                        break;
                }
            }
        }
        static GameObject CreateModel(Sdf.Model model)
        {
            GameObject modelObject = new GameObject("Model:" + model.Name);
            Pose pose = model.Pose?.ToPose() ?? UnityEngine.Pose.identity;
            Pose includePose = model.IncludePose?.ToPose() ?? UnityEngine.Pose.identity;
            modelObject.transform.SetLocalPose(includePose.Multiply(pose));

            if (model.Models == null || model.Links == null)
            {
                // invalid
                return modelObject;
            }
            
            foreach (Sdf.Model innerModel in model.Models)
            {
                CreateModel(innerModel)?.transform.SetParent(modelObject.transform, false);
            }

            foreach (Sdf.Link link in model.Links)
            {
                CreateLink(link)?.transform.SetParent(modelObject.transform, false);
            }
            
            return modelObject;
        }
        
        static GameObject CreateLink(Sdf.Link link)
        {
            GameObject linkObject = new GameObject("Link:" + link.Name);
            linkObject.transform.SetLocalPose(link.Pose?.ToPose() ?? UnityEngine.Pose.identity);

            foreach (Sdf.Visual visual in link.Visuals)
            {
                Sdf.Geometry geometry = visual.Geometry;

                GameObject visualObject = new GameObject
                (
                    name: visual.Name != null ? $"[Visual:{visual.Name}]" : "[Visual]"
                );
                visualObject.transform.SetParent(linkObject.transform, false);

                GameObject resourceObject = null;
                //bool isSynthetic = false;
                if (geometry.Mesh != null)
                {
                    Uri uri = geometry.Mesh.Uri.ToUri();

                    string path =
                        $"Assets/Resources/Package/{uri.Host}{Uri.UnescapeDataString(uri.AbsolutePath)}.prefab";

                    Debug.Log(path);

                    GameObject assetObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (assetObject == null)
                    {
                        throw new Exception();
                    }
                    
                    resourceObject = Instantiate(assetObject, visualObject.transform, false);
                    visualObject.transform.localScale = geometry.Mesh.Scale?.ToVector3().Abs() ?? Vector3.one;
                    //isSynthetic = true;
                }
                else if (geometry.Cylinder != null)
                {
                    resourceObject = Instantiate(Resource.Displays.Cylinder.Object, visualObject.transform, false);
                    visualObject.transform.localScale = new Vector3(
                        (float)geometry.Cylinder.Radius * 2,
                        (float)geometry.Cylinder.Length,
                        (float)geometry.Cylinder.Radius * 2);
                }
                else if (geometry.Box != null)
                {
                    resourceObject = Instantiate(Resource.Displays.Cube.Object, visualObject.transform, false);
                    visualObject.transform.localScale = geometry.Box.Scale?.ToVector3().Abs() ?? Vector3.one;
                }
                else if (geometry.Sphere != null)
                {
                    resourceObject = Instantiate(Resource.Displays.Sphere.Object, visualObject.transform, false);
                    visualObject.transform.localScale = (float)geometry.Sphere.Radius * Vector3.one;
                }

                if (resourceObject == null)
                {
                    continue; //?
                }
            }

            return linkObject;
        }

        

    }
}
