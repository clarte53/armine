using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model
{
	[Serializable]
	public class Info : MonoBehaviour, ISerializationCallbackReceiver
	{
		#region Members
		public string filename;
		public TimeSpan duration;
		public int vertices;
		public int faces;
		public Dictionary<uint, GameObject> ids;

		[SerializeField]
		private string durationStr;

		[SerializeField]
		private List<GameObject> idsValues;
		
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		public Option.Import options;
		#endif
		#endregion
		
		#region Constructors
		public void Init(string file, TimeSpan loading_duration, int vertices_loaded, int faces_loaded, Dictionary<uint, GameObject> id_mapping)
		{
			filename = file;
			duration = loading_duration;
			vertices = vertices_loaded;
			faces = faces_loaded;
			ids = id_mapping;
			
			#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			options = new Option.Import();
			#endif
		}
		#endregion

		#region Serialization callback
		public void OnBeforeSerialize()
		{
			if(ids != null)
			{
				if(idsValues == null)
				{
					idsValues = new List<GameObject>(ids.Count);
				}

				idsValues.Clear();

				for(int i = 0; i < ids.Count; i++)
				{
					idsValues.Insert(i, ids[(uint) i]);
				}
			}

			durationStr = duration.Ticks.ToString();
		}
		
		public void OnAfterDeserialize()
		{
			if(idsValues != null)
			{
				ids = new Dictionary<uint, GameObject>();
			
				for(int i = 0; i != idsValues.Count; i++)
				{
					ids.Add((uint) i, idsValues[i]);
				}

				idsValues.Clear();
			}

			long ticks;

			if(long.TryParse(durationStr, out ticks))
			{
				duration = new TimeSpan(ticks);
			}
			
			durationStr = null;
		}
		#endregion
	}
}
