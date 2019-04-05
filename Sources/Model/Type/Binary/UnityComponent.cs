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

                read += serializer.FromBytes(buffer, start + read, out fields);
                read += serializer.FromBytes(buffer, start + read, out properties);

                return read;
            }

            public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
            {
                uint written = 0;

                written += serializer.ToBytes(ref buffer, start + written, fields);
                written += serializer.ToBytes(ref buffer, start + written, properties);

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
