using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using UnityEngine;

namespace Armine.Model
{
	[Serializable]
	public class Metadata : MonoBehaviour, ISerializationCallbackReceiver
	{
		#region Members
		[NonSerialized]
		private static readonly DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>));

		[NonSerialized]
		public Dictionary<string, object> data;

		[SerializeField]
		private byte[] serializedData;
        #endregion

        public void Initialize()
        {
            data = new Dictionary<string, object>();
        }

        #region Serialization callbacks
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
