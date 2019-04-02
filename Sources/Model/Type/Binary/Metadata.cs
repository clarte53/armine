using System.Collections.Generic;
using CLARTE.Serialization;

namespace Armine.Model.Type
{
	public partial class Metadata : IBinarySerializable
	{
		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			uint size;
			string key;
			object value;

			Initialize();

			uint read = serializer.FromBytes(buffer, start, out size);

			for(uint i = 0; i < size; ++i)
			{
				read += serializer.FromBytes(buffer, start + read, out key);
				read += serializer.FromBytes(buffer, start + read, out value);

				data.Add(key, value);
			}

			return read;
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			uint count = 0;

			uint written = serializer.ToBytes(ref buffer, start, count); // placeholder

			foreach(KeyValuePair<string, object> pair in data)
			{
				if(!string.IsNullOrEmpty(pair.Key))
				{
					written += serializer.ToBytes(ref buffer, start + written, pair.Key);
					written += serializer.ToBytes(ref buffer, start + written, pair.Value != null ? pair.Value : "");

					count++;
				}
			}

			// Replace the count by the correct value
			serializer.ToBytes(ref buffer, start, count);

			return written;
		}
	}
}
