using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

namespace Armine.Model
{
	/// <summary>
	/// Component used to store metadata associated with a given gameobject.
	/// </summary>
	[Serializable]
	public class Metadata : MonoBehaviour, ISerializationCallbackReceiver
	{
		#region Members
		[NonSerialized]
		private static readonly DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>));

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

		#region Serialization callbacks
		/// <summary>
		/// Callback executed before serialization to prepare data
		/// </summary>
		public void OnBeforeSerialize()
		{
			serializedData = null;

			if(data != null)
			{
				using(MemoryStream stream = new MemoryStream())
				{
					serializer.WriteObject(stream, data);

					serializedData = stream.ToArray();
				}
			}
		}

		/// <summary>
		/// Callback executed after deserialization to restore data
		/// </summary>
		public void OnAfterDeserialize()
		{
			data = null;

			if(serializedData != null && serializedData.Length > 0)
			{
				using(MemoryStream stream = new MemoryStream(serializedData))
				{
					data = (Dictionary<string, object>) serializer.ReadObject(stream);
				}
			}
		}
		#endregion
	}
}
