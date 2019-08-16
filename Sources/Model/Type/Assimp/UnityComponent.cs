#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Collections.Generic;
using Assimp;
using CLARTE.Serialization;
using UnityEngine;

namespace Armine.Model.Type
{
	public sealed partial class UnityComponent
	{
		private readonly struct AssimpMetadataSerializationContext
		{
			#region Members
			public System.Type Type { get; }
			public Dictionary<string, object> Data { get; }
			#endregion

			#region Constructors
			public AssimpMetadataSerializationContext(System.Type type, Dictionary<string, object> data)
			{
				Type = type;
				Data = data;
			}
			#endregion

			#region Delegate implementation
			public uint Callback(Binary serializer, ref Binary.Buffer buffer)
			{
				uint written = 0;
				
				written += serializer.ToBytes(ref buffer, written, Type);
				written += serializer.ToBytes(ref buffer, written, Data);

				return written;
			}
			#endregion
		}

		#region Import
		public static UnityComponent FromAssimpMetadata(aiMetadata meta)
		{
			UnityComponent metadata = null;

			if(meta != null && meta.Keys != null)
			{
				uint size = meta.Keys.Size();

				if(meta.Values != null && meta.Values.Size() == size)
				{
					if(size > 0)
					{
						Dictionary<string, object> storage = new Dictionary<string, object>();

						for(uint i = 0; i < size; ++i)
						{
							using(aiString key = meta.Keys.Get(i))
							{
								using(aiMetadataEntry entry = meta.Values.Get(i))
								{
									object value = null;

									switch(entry.mType)
									{
										case aiMetadataType.AI_BOOL:
											value = entry.GetBool();
											break;
										case aiMetadataType.AI_INT32:
											value = entry.GetInt32();
											break;
										case aiMetadataType.AI_UINT64:
											value = entry.GetUInt64();
											break;
										case aiMetadataType.AI_FLOAT:
											value = entry.GetFloat();
											break;
										case aiMetadataType.AI_DOUBLE:
											value = entry.GetDouble();
											break;
										case aiMetadataType.AI_AISTRING:
											value = entry.GetString().C_Str();
											break;
										case aiMetadataType.AI_AIVECTOR3D:
											value = Assimp.Convert.AssimpToUnity.Vector3(entry.GetVector3D());
											break;
									}

									storage.Add(key.C_Str(), value);
								}
							}
						}

						metadata = new UnityComponent
						{
							type = typeof(Metadata)
						};

						metadata.CreateBackend();

						AssimpMetadataSerializationContext context = new AssimpMetadataSerializationContext(metadata.type, storage);

						((BackendBinarySerializable) metadata.backend).serialized = Module.Import.Binary.serializer.Serialize(context.Callback);
					}
				}
				else
				{
					Debug.LogError("The number of metadata keys and values does not match.");
				}
			}

			return metadata;
		}
		#endregion

		#region Export
		public aiMetadata ToAssimpMetadata()
		{
			aiMetadata assimp_meta = null;

			if(typeof(Metadata).IsAssignableFrom(type))
			{
				Dictionary<string, object> data = (Dictionary<string, object>) Module.Import.Binary.serializer.Deserialize(((BackendBinarySerializable) backend).serialized);

				if(data != null && data.Count > 0)
				{
					uint count = 0;

					// Get the number of valid metadata that can be exported
					foreach(KeyValuePair<string, object> pair in data)
					{
						if(!string.IsNullOrEmpty(pair.Key) && pair.Value != null)
						{
							count++;
						}
					}

					// Allocate memory for metadata
					assimp_meta = aiMetadata.Alloc(count);

					uint index = 0;

					// Export all valid metadata
					foreach(KeyValuePair<string, object> pair in data)
					{
						if(!string.IsNullOrEmpty(pair.Key) && pair.Value != null)
						{
							System.Type type = pair.Value.GetType();

							if(type == typeof(bool))
							{
								assimp_meta.SetBool(index++, pair.Key, (bool) pair.Value);
							}
							else if(type == typeof(int))
							{
								assimp_meta.SetInt32(index++, pair.Key, (int) pair.Value);
							}
							else if(type == typeof(ulong))
							{
								assimp_meta.SetUInt64(index++, pair.Key, (ulong) pair.Value);
							}
							else if(type == typeof(float))
							{
								assimp_meta.SetFloat(index++, pair.Key, (float) pair.Value);
							}
							else if(type == typeof(double))
							{
								assimp_meta.SetDouble(index++, pair.Key, (double) pair.Value);
							}
							else if(type == typeof(string))
							{
								assimp_meta.SetString(index++, pair.Key, new aiString((string) pair.Value).Unmanaged());
							}
							else if(type == typeof(Vector3))
							{
								assimp_meta.SetVector3D(index++, pair.Key, Assimp.Convert.UnityToAssimp.Vector3((Vector3) pair.Value).Unmanaged());
							}
							else
							{
								Debug.LogErrorFormat("Unsupported metadata of type '{0}'.", type);
							}
						}
					}
				}
			}

			return assimp_meta;
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
