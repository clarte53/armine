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
        #region Members
        private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static Dictionary<Component, UnityComponent> components = new Dictionary<Component, UnityComponent>();
        #endregion

        #region Utility methods
        public static void ClearMapping()
        {
            components.Clear();
        }
        #endregion

        #region Import
        public static UnityComponent FromUnity(Component component)
        {
            UnityComponent result = null;

            if(component != null && !components.TryGetValue(component, out result))
            {
                System.Type type = component.GetType();

                result = new UnityComponent();

                result.type = type;
                result.fields = new Dictionary<string, object>();
                result.properties = new Dictionary<string, object>();

                components[component] = result;                

                FieldInfo[] members = type.GetFields(flags);

                foreach(FieldInfo member in members)
                {
                    if(DoSerialization(member, () => member.IsPublic))
                    {
                        try
                        {
                            Binary.GetSupportedType(member.FieldType);

                            result.fields.Add(member.Name, member.GetValue(component));
                        }
                        catch(ArgumentException)
                        {
                            Debug.LogWarningFormat("Unsupported field '{0}' of type '{1}' in component of type '{2}'. Field will not be serialized.", member.Name, member.FieldType, type);
                        }
                    }
                }

                PropertyInfo[] properties = type.GetProperties(flags);

                foreach(PropertyInfo property in properties)
                {
                    if(property.GetIndexParameters().Length == 0)
                    {
                        MethodInfo getter = property.GetGetMethod(true);
                        MethodInfo setter = property.GetSetMethod(true);

                        if(DoSerialization(property, () => DoSerialization(getter) && DoSerialization(setter)))
                        {
                            try
                            {
                                Binary.GetSupportedType(property.PropertyType);

                                result.properties.Add(property.Name, property.GetValue(component, null));
                            }
                            catch(ArgumentException)
                            {
                                Debug.LogWarningFormat("Unsupported property '{0}' of type '{1}' in component of type '{2}'. Property will not be serialized.", property.Name, property.PropertyType, type);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarningFormat("Unsupported indexed property '{0}' in component of type '{1}'. Property will not be serialized.", property.Name, type);
                    }
                }
            }

            return result;
        }
        #endregion

        #region Export
        public void ToUnity(GameObject go)
        {
            Component component = go.GetComponent(type);

            if(component == null)
            {
                component = go.AddComponent(type);
            }

            if(component != null)
            {
                if(fields != null)
                {
                    foreach(KeyValuePair<string, object> pair in fields)
                    {
                        FieldInfo member = type.GetField(pair.Key, flags);

                        if(member != null)
                        {
                            member.SetValue(component, pair.Value);
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
                            property.SetValue(component, pair.Value, null);
                        }
                        else
                        {
                            Debug.LogErrorFormat("Missing property '{0}' in component of type '{1}'. Property will not be deserialized.", pair.Key, type);
                        }
                    }
                }
            }
            else
            {
                Debug.LogErrorFormat("Can not add component of type '{0}' to GameObject '{1}'. Component will not be deserialized.", type, go.name);
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
        #endregion
    }
}
