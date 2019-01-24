using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using CLARTE.Serialization;

namespace Armine.Model.Type
{
    public sealed partial class UnityComponent
    {
        private partial interface IBackend
        {
            void FromUnity(Scene scene, Component component, System.Type type);
            void ToUnity(Scene scene, Component component, System.Type type);
        }

        private partial class BackendGeneric
        {
            #region Import
            public void FromUnity(Scene scene, Component component, System.Type type)
            {
                fields = new Dictionary<string, object>();
                properties = new Dictionary<string, object>();

                FieldInfo[] class_fields = type.GetFields(flags);

                foreach(FieldInfo member in class_fields)
                {
                    if(DoSerialization(member, () => member.IsPublic))
                    {
                        AddValue(scene, fields, type, member.FieldType, member.Name, member.GetValue(component));
                    }
                }

                PropertyInfo[] class_properties = type.GetProperties(flags);

                foreach(PropertyInfo property in class_properties)
                {
                    if(property.GetIndexParameters().Length == 0)
                    {
                        MethodInfo getter = property.GetGetMethod(true);
                        MethodInfo setter = property.GetSetMethod(true);

                        if(DoSerialization(property, () => DoSerialization(getter) && DoSerialization(setter)))
                        {
                            AddValue(scene, properties, type, property.PropertyType, property.Name, property.GetValue(component, null));
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("Unsupported indexed property '{0}' in component of type '{1}'. Property will not be serialized.", property.Name, type);
                    }
                }
            }
            #endregion

            #region Export
            public void ToUnity(Scene scene, Component component, System.Type type)
            {
                if(fields != null)
                {
                    foreach(KeyValuePair<string, object> pair in fields)
                    {
                        FieldInfo member = type.GetField(pair.Key, flags);

                        if(member != null)
                        {
                            SetValue(scene, pair.Value, x => member.SetValue(component, x));
                        }
                        else
                        {
                            Debug.LogErrorFormat("Missing field '{0}' in component of type '{1}'. Field will not be deserialized.", pair.Key, type);
                        }
                    }
                }

                if(properties != null)
                {
                    foreach(KeyValuePair<string, object> pair in properties)
                    {
                        PropertyInfo property = type.GetProperty(pair.Key, flags);

                        if(property != null)
                        {
                            SetValue(scene, pair.Value, x => property.SetValue(component, x, null));
                        }
                        else
                        {
                            Debug.LogErrorFormat("Missing property '{0}' in component of type '{1}'. Property will not be deserialized.", pair.Key, type);
                        }
                    }
                }
            }
            #endregion

            #region Utility methods
            private static bool DoSerialization(MethodInfo method)
            {
                if(method != null)
                {
                    var attributes = method.GetCustomAttributes(true);

                    return (method.IsPublic || attributes.Any(x => x is SerializeField)) && !attributes.Any(x => x is NonSerializedAttribute);
                }

                return false;
            }

            private static bool DoSerialization(MemberInfo member, Func<bool> public_callback)
            {
                if(member != null)
                {
                    var attributes = member.GetCustomAttributes(true);

                    return (public_callback() || attributes.Any(x => x is SerializeField)) && !attributes.Any(x => x is NonSerializedAttribute);
                }

                return false;
            }

            private static void AddValue(Scene scene, Dictionary<string, object> dictionnary, System.Type component_type, System.Type type, string name, object value)
            {
                if(value is UnityEngine.Object)
                {
                    dictionnary.Add(name, UnityReference.FromUnity(scene, value as UnityEngine.Object));
                }
                else
                {
                    try
                    {
                        Binary.GetSupportedType(type);

                        dictionnary.Add(name, value);
                    }
                    catch(ArgumentException)
                    {
                        Debug.LogWarningFormat("Unsupported value '{0}' of type '{1}' in component of type '{2}'. Value will not be serialized.", name, type, component_type);
                    }
                }
            }

            private void SetValue(Scene scene, object value, Action<object> set_callback)
            {
                if(value != null && value is UnityReference)
                {
                    UnityReference reference = value as UnityReference;

                    reference.UnitySetCallback = set_callback;

                    scene.AddUnityReference(reference);
                }
                else
                {
                    set_callback(value);
                }
            }
            #endregion
        }

        private partial class BackendBinarySerializable
        {
            #region Import
            public void FromUnity(Scene scene, Component component, System.Type type)
            {
                Binary.Buffer buffer = null;

                try
                {
                    buffer = Module.Import.Binary.serializer.GetBuffer(1024);

                    uint written = Module.Import.Binary.serializer.ToBytes(ref buffer, 0, component as IBinarySerializable);

                    serialized = new byte[written];

                    Array.Copy(buffer.Data, serialized, written);
                }
                finally
                {
                    if(buffer != null)
                    {
                        buffer.Dispose();
                    }
                }
            }
            #endregion

            #region Export
            public void ToUnity(Scene scene, Component component, System.Type type)
            {
                using(Binary.Buffer buffer = Module.Import.Binary.serializer.GetBufferFromExistingData(serialized))
                {
                    Module.Import.Binary.serializer.FromBytesOverwrite(buffer, 0, component as IBinarySerializable);
                }
            }
            #endregion
        }

        #region Members
        private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        #endregion

        #region Import
        public static UnityComponent FromUnity(Scene scene, Component component)
        {
            return scene.GetUnityComponent(component, FromUnityCreator);
        }

        private static UnityComponent FromUnityCreator(Scene scene, Component component)
        {
            UnityComponent result = new UnityComponent();

            result.type = component.GetType();
            result.CreateBackend();

            if(component is ISerializationCallbackReceiver)
            {
                ((ISerializationCallbackReceiver) component).OnBeforeSerialize();
            }

            result.backend.FromUnity(scene, component, result.type);

            return result;
        }
        #endregion

        #region Export
        public void ToUnity(Scene scene, GameObject go)
        {
            Component component = go.GetComponent(type);

            if(component == null)
            {
                component = go.AddComponent(type);
            }

            if(component != null)
            {
                backend.ToUnity(scene, component, type);

                if(component is ISerializationCallbackReceiver)
                {
                    ((ISerializationCallbackReceiver) component).OnAfterDeserialize();
                }
            }
            else
            {
                Debug.LogErrorFormat("Can not add component of type '{0}' to GameObject '{1}'. Component will not be deserialized.", type, go.name);
            }
        }
        #endregion
    }
}
