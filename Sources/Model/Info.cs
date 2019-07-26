using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model
{
	/// <summary>
	/// Component used to store import info and options of a resulting hierarchy.
	/// </summary>
	[Serializable]
	public class Info : MonoBehaviour, ISerializationCallbackReceiver
	{
		/// <summary>
		/// Mapping between gameobject and unique identifier, comppored of object ID and sub-part in object ID.
		/// </summary>
        [Serializable]
        public struct Mapping
        {
			/// <summary>
			/// GameObject to map.
			/// </summary>
            public GameObject go;

			/// <summary>
			/// Unique ID of object in scene.
			/// </summary>
            public uint id;

			/// <summary>
			/// Unique ID of gameobject in object.
			/// </summary>
            public int part;
        }

		#region Members
		/// <summary>
		/// Name of the imported file.
		/// </summary>
		public string filename;

		/// <summary>
		/// Number of vertices in the imported geometries.
		/// </summary>
		public int vertices;

		/// <summary>
		/// Number of triangles in the imported geometries.
		/// </summary>
		public int faces;

		/// <summary>
		/// Duration of import.
		/// </summary>
        [NonSerialized]
        public TimeSpan duration;

		/// <summary>
		/// Unique mapping between each imported gameobject and object and sub-part IDs.
		/// </summary>
        [NonSerialized]
        public Dictionary<uint, List<GameObject>> ids;

		[SerializeField]
		private string durationStr;

		[SerializeField]
		private List<Mapping> idsValues;
		
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		/// <summary>
		/// Assimp options used for import.
		/// </summary>
		public Option.Import options;
		#endif
		#endregion
		
		#region Constructors
		/// <summary>
		/// Initialize the info stucture.
		/// </summary>
		/// <param name="file">Name of the imported file.</param>
		/// <param name="loading_duration">Duration of import.</param>
		/// <param name="vertices_loaded">Number of vertices in the imported geometries.</param>
		/// <param name="faces_loaded">Number of triangles in the imported geometries.</param>
		/// <param name="id_mapping">Unique mapping between each imported gameobject and object and sub-part IDs.</param>
		public void Init(string file, TimeSpan loading_duration, int vertices_loaded, int faces_loaded, Dictionary<uint, List<GameObject>> id_mapping)
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
		/// <summary>
		/// Callback executed before serialization to prepare data
		/// </summary>
		public void OnBeforeSerialize()
		{
			if(ids != null)
			{
				if(idsValues == null)
				{
					idsValues = new List<Mapping>(ids.Count);
				}

				idsValues.Clear();

				for(uint i = 0; i < ids.Count; i++)
				{
                    for(int j = 0; j < ids[i].Count; j++)
                    {
                        idsValues.Add(new Mapping { go = ids[i][j], id = i, part = j });
                    }
				}
			}

			durationStr = duration.Ticks.ToString();
		}

		/// <summary>
		/// Callback executed after deserialization to restore data
		/// </summary>
		public void OnAfterDeserialize()
		{
			if(idsValues != null)
			{
				ids = new Dictionary<uint, List<GameObject>>();
			
				for(uint i = 0; i < idsValues.Count; i++)
				{
                    Mapping mapping = idsValues[(int) i];

                    List<GameObject> list;

                    if(!ids.TryGetValue(mapping.id, out list))
                    {
                        list = new List<GameObject>();

                        ids[mapping.id] = list;
                    }

                    list.Insert(mapping.part, mapping.go);
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
