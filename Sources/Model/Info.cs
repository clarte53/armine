using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model
{
	[Serializable]
	public class Info : MonoBehaviour, ISerializationCallbackReceiver
	{
        [Serializable]
        public struct Mapping
        {
            public GameObject go;
            public uint id;
            public int part;
        }

		#region Members
		public string filename;
		public int vertices;
		public int faces;

        [NonSerialized]
        public TimeSpan duration;

        [NonSerialized]
        public Dictionary<uint, List<GameObject>> ids;

		[SerializeField]
		private string durationStr;

		[SerializeField]
		private List<Mapping> idsValues;
		
		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		public Option.Import options;
		#endif
		#endregion
		
		#region Constructors
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
