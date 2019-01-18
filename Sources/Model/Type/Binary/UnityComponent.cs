using System.Collections.Generic;
using CLARTE.Serialization;

namespace Armine.Model.Type
{
    public sealed partial class UnityComponent : IBinarySerializable
    {
        public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
        {
            uint size;
            string key;
            object value;
            string raw_type;

            // Handle component type
            uint read = serializer.FromBytes(buffer, start, out raw_type);

            type = System.Type.GetType(raw_type);

            // Handle member fields
            read += serializer.FromBytes(buffer, start + read, out size);

            if(size > 0)
            {
                fields = new Dictionary<string, object>();
            }

            for(uint i = 0; i < size; ++i)
            {
                read += serializer.FromBytes(buffer, start + read, out key);
                read += serializer.FromBytesDynamic(buffer, start + read, out value);

                fields.Add(key, value);
            }

            // Handle properties
            read += serializer.FromBytes(buffer, start + read, out size);

            if(size > 0)
            {
                properties = new Dictionary<string, object>();
            }

            for(uint i = 0; i < size; ++i)
            {
                read += serializer.FromBytes(buffer, start + read, out key);
                read += serializer.FromBytesDynamic(buffer, start + read, out value);

                properties.Add(key, value);
            }

            return read;
        }

        public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
        {
            uint count, count_offset;

            // Handle component type
            uint written = serializer.ToBytes(ref buffer, start, string.Format("{0}, {1}", type.ToString(), type.Assembly.GetName().Name));

            // Handle member fields
            count = 0;
            count_offset = start + written;

            written += serializer.ToBytes(ref buffer, count_offset, count); // placeholder

            if(fields != null)
            {
                foreach(KeyValuePair<string, object> pair in fields)
                {
                    if(!string.IsNullOrEmpty(pair.Key) && pair.Value != null)
                    {
                        written += serializer.ToBytes(ref buffer, start + written, pair.Key);
                        written += serializer.ToBytesDynamic(ref buffer, start + written, pair.Value);

                        count++;
                    }
                }

                // Replace the count by the correct value
                serializer.ToBytes(ref buffer, count_offset, count);
            }

            // Handle properties
            count = 0;
            count_offset = start + written;

            written += serializer.ToBytes(ref buffer, count_offset, count); // placeholder

            if(properties != null)
            {
                foreach(KeyValuePair<string, object> pair in properties)
                {
                    if(!string.IsNullOrEmpty(pair.Key) && pair.Value != null)
                    {
                        written += serializer.ToBytes(ref buffer, start + written, pair.Key);
                        written += serializer.ToBytesDynamic(ref buffer, start + written, pair.Value);

                        count++;
                    }
                }

                // Replace the count by the correct value
                serializer.ToBytes(ref buffer, count_offset, count);
            }

            return written;
        }
    }
}
