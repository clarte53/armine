using System;
using System.Collections.Generic;
using UnityEngine;
using CLARTE.Serialization;

namespace Armine.Model
{
	/// <summary>
	/// Component used to store metadata associated with a given gameobject.
	/// </summary>
	[Serializable]
	public class Metadata : MonoBehaviour, IBinarySerializable, ISerializationCallbackReceiver
	{
		#region Members
		/// <summary>
		/// Dictionary containing the metadata as key / value pairs.
		/// </summary>
		[NonSerialized]
		public Dictionary<string, object> data;

		[SerializeField]
		private byte[] serializedData;
		#endregion

		#region Public methods
		/// <summary>
		/// Initialize the metadata dictionary.
		/// </summary>
		public void Initialize()
        {
            data = new Dictionary<string, object>();
        }
		#endregion

		#region IBinarySerializable implementation
		/// <summary>
		/// Deserialize data from byte array.
		/// </summary>
		/// <param name="serializer">Serializer to use.</param>
		/// <param name="buffer">Buffer to get the data from.</param>
		/// <param name="start">Start offset in the buffer where to get the data.</param>
		/// <returns></returns>
		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			return serializer.FromBytes(buffer, start, out data);
		}

		/// <summary>
		/// Serialize data to a byte array.
		/// </summary>
		/// <param name="serializer">Serializer to use.</param>
		/// <param name="buffer">Buffer where to serialaze the data.</param>
		/// <param name="start">Start offset where to put the serialized data in the buffer.</param>
		/// <returns></returns>
		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			return serializer.ToBytes(ref buffer, start, data);
		}
		#endregion

		#region ISerializationCallbackReceiver implementation
		/// <summary>
		/// Callback executed before serialization to prepare data
		/// </summary>
		public void OnBeforeSerialize()
		{
			serializedData = null;

			if(data != null)
			{
				serializedData = Module.Import.Binary.serializer.Serialize(data);
			}
		}

		/// <summary>
		/// Callback executed after deserialization to restore data
		/// </summary>
		public void OnAfterDeserialize()
		{
			if(serializedData != null && serializedData.Length > 0)
			{
				data = (Dictionary<string, object>) Module.Import.Binary.serializer.Deserialize(serializedData);
			}

			serializedData = null;
		}
		#endregion
	}
}
