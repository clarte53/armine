using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model.Type
{
    /// <summary>
    /// Interface of handled references to UnityEngine.Object.
    /// </summary>
    public sealed partial class UnityReference
    {
        enum SupportedType
        {
            NONE,
            GAMEOBJECT,
            COMPONENT,
            MESH,
            MATERIAL,
            TEXTURE
        }

        #region Members
        private UnityEngine.Object unityValue;
        private Action<object> unitySetCallback;
        #endregion

        #region Getter / Setter
        public Action<object> UnitySetCallback
        {
            set
            {
                unitySetCallback = value;
            }
        }
        #endregion

        #region Import
        public static UnityReference FromUnity(Scene scene, UnityEngine.Object v)
        {
            UnityReference reference = null;

            if(v != null)
            {
                reference = new UnityReference();

                reference.type = v.GetType();
                reference.id = (uint) v.GetInstanceID();
                reference.unityValue = v;

                scene.AddUnityReference(reference);
            }

            return reference;
        }
        #endregion

        #region Export
        public void ToUnity(Scene scene)
        {
            UnityEngine.Object unity_object = null;

            SupportedType object_type = GetSupportedType();

            switch(object_type)
            {
                case SupportedType.GAMEOBJECT:
                case SupportedType.COMPONENT:
                    List<GameObject> parts;

                    if(scene.IdMapping.Id2Go.TryGetValue(id, out parts) && parts != null && part < parts.Count)
                    {
                        unity_object = parts[(int) part];

                        if(object_type == SupportedType.COMPONENT)
                        {
                            unity_object = (unity_object as GameObject).GetComponent(type);
                        }
                    }
                    break;
                case SupportedType.MESH:
                    if(scene.meshes != null && id < scene.meshes.Length)
                    {
                        unity_object = scene.meshes[id].ToUnity();
                    }
                    break;
                case SupportedType.MATERIAL:
                    if(scene.materials != null && id < scene.materials.Length)
                    {
                        unity_object = scene.materials[id].ToUnity(scene);
                    }
                    break;
                case SupportedType.TEXTURE:
                    if(scene.textures != null && id < scene.textures.Length)
                    {
                        unity_object = scene.textures[id].ToUnity();
                    }
                    break;
                default:
                    Debug.LogWarningFormat("Unhandled reference to unity object of type '{0}'", type);
                    break;
            }

            if(unity_object != null && unitySetCallback != null)
            {
                unitySetCallback(unity_object);
            }
        }
        #endregion

        #region Reference solver
        private SupportedType GetSupportedType()
        {
            if(typeof(GameObject).IsAssignableFrom(type))
            {
                return SupportedType.GAMEOBJECT;
            }
            else if(typeof(Component).IsAssignableFrom(type))
            {
                return SupportedType.COMPONENT;
            }
            else if(typeof(UnityEngine.Mesh).IsAssignableFrom(type))
            {
                return SupportedType.MESH;
            }
            else if(typeof(UnityEngine.Material).IsAssignableFrom(type))
            {
                return SupportedType.MATERIAL;
            }
            else if(typeof(Texture2D).IsAssignableFrom(type))
            {
                return SupportedType.TEXTURE;
            }
            else
            {
                return SupportedType.NONE;
            }
        }

        public void ResolveReference(Scene.Mapping ids, Dictionary<UnityEngine.Mesh, int> meshes, Dictionary<UnityEngine.Material, int> materials, Dictionary<Texture2D, int> textures)
        {
            try
            {
                switch(GetSupportedType())
                {
                    case SupportedType.GAMEOBJECT:
                        ResolveReference(ids, unityValue as GameObject);
                        break;
                    case SupportedType.COMPONENT:
                        ResolveReference(ids, (unityValue as Component).gameObject);
                        break;
                    case SupportedType.MESH:
                        ResolveReference(meshes, unityValue as UnityEngine.Mesh);
                        break;
                    case SupportedType.MATERIAL:
                        ResolveReference(materials, unityValue as UnityEngine.Material);
                        break;
                    case SupportedType.TEXTURE:
                        ResolveReference(textures, unityValue as Texture2D);
                        break;
                    default:
                        Debug.LogWarningFormat("Unhandled reference to unity object '{0}' of type '{1}'.", unityValue.name, type);
                        break;
                }
            }
            catch(ArgumentException)
            {
                Debug.LogWarningFormat("Unhandled reference to unity object '{0}' of type '{1}'. Reference to external asset not supported.", unityValue.name, type);
            }
        }

        private void ResolveReference(Scene.Mapping mapping, GameObject value)
        {
            if(value != null)
            {
                uint object_id;
                int object_part;

                List<GameObject> parts;

                if(mapping.Go2Id.TryGetValue(value, out object_id) && mapping.Id2Go.TryGetValue(object_id, out parts))
                {
                    object_part = parts.FindIndex(x => value == x);

                    if(object_part >= 0)
                    {
                        id = object_id;
                        part = (uint) object_part;

                        resolved = true;
                    }
                    else
                    {
                        throw new ArgumentException();
                    }
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        private void ResolveReference<T>(Dictionary<T, int> mapping, T value)
        {
            if(value != null)
            {
                int object_id;

                if(mapping.TryGetValue(value, out object_id) && object_id >= 0)
                {
                    id = (uint) object_id;
                    part = 0;

                    resolved = true;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }
        #endregion
    }
}
