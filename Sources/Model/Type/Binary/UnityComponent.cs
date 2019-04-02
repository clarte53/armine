using System.Collections.Generic;
using CLARTE.Serialization;

namespace Armine.Model.Type
{
    public sealed partial class UnityComponent : IBinarySerializable
    {
        private partial interface IBackend : IBinarySerializable
        {

        }

        private partial class BackendGeneric
        {
            #region IBinarySerializable implementation
            public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
            {
                uint read = 0;

                read += ReadDictionary(serializer, buffer, start + read, ref fields);
                read += ReadDictionary(serializer, buffer, start + read, ref properties);

                return read;
            }

            public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
            {
                uint written = 0;

                written += WriteDictionary(serializer, ref buffer, start + written, fields);
                written += WriteDictionary(serializer, ref buffer, start + written, properties);

                return written;
            }
            #endregion

            #region Helper methods
            private static uint ReadDictionary(Binary serializer, Binary.Buffer buffer, uint start, ref Dictionary<string, object> dictionary)
            {
                uint size;
                string key;
                object value;

                uint read = serializer.FromBytes(buffer, start, out size);

                if(size > 0)
                {
                    dictionary = new Dictionary<string, object>();
                }

                for(uint i = 0; i < size; ++i)
                {
                    read += serializer.FromBytes(buffer, start + read, out key);
                    read += serializer.FromBytes(buffer, start + read, out value);

                    dictionary.Add(key, value);
                }

                return read;
            }

            private static uint WriteDictionary(Binary serializer, ref Binary.Buffer buffer, uint start, Dictionary<string, object> dictionary)
            {
                uint count = 0;

                uint written = serializer.ToBytes(ref buffer, start, count); // placeholder

                if(dictionary != null)
                {
                    foreach(KeyValuePair<string, object> pair in dictionary)
                    {
                        if(!string.IsNullOrEmpty(pair.Key) && pair.Value != null)
                        {
                            written += serializer.ToBytes(ref buffer, start + written, pair.Key);
                            written += serializer.ToBytes(ref buffer, start + written, pair.Value);

                            count++;
                        }
                    }

                    // Replace the count by the correct value
                    serializer.ToBytes(ref buffer, start, count);
                }

                return written;
            }
            #endregion
        }

        private partial class BackendBinarySerializable
        {
            #region IBinarySerializable implementation
            public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
            {
                return serializer.FromBytes(buffer, start, out serialized);
            }

            public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
            {
                return serializer.ToBytes(ref buffer, start, serialized);
            }
            #endregion
        }

        #region IBinarySerializable implementation
        public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
        {
            uint read = 0;

            read += serializer.FromBytes(buffer, start + read, out type);

            CreateBackend();

            read += backend.FromBytes(serializer, buffer, start + read);

            return read;
        }

        public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
        {
            uint written = 0;

            written += serializer.ToBytes(ref buffer, start + written, type);
            written += backend.ToBytes(serializer, ref buffer, start + written);

            return written;
        }
        #endregion
    }
}
