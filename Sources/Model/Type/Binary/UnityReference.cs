using CLARTE.Serialization;

namespace Armine.Model.Type
{
    /// <summary>
    /// Interface of handled references to UnityEngine.Object.
    /// </summary>
    public sealed partial class UnityReference : IBinarySerializable
    {
        public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
        {
            string raw_type;

            uint read = serializer.FromBytes(buffer, start, out raw_type);

            type = System.Type.GetType(raw_type);

            read += serializer.FromBytes(buffer, start + read, out resolved);

            if(resolved)
            {
                read += serializer.FromBytes(buffer, start + read, out id);
                read += serializer.FromBytes(buffer, start + read, out part);
            }
            else
            {
                int instance_id;

                // This deserialization should never be used in normal case. Serialization of unresolved reference
                // is only usefull to compute the unique hash of each node to group them together. Deserialization
                // is not needed for this process.
                read += serializer.FromBytes(buffer, start + read, out instance_id);
            }

            return read;
        }

        public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
        {
            uint written = serializer.ToBytes(ref buffer, start, string.Format("{0}, {1}", type.ToString(), type.Assembly.GetName().Name));

            written += serializer.ToBytes(ref buffer, start + written, resolved);

            if(resolved)
            {
                written += serializer.ToBytes(ref buffer, start + written, id);
                written += serializer.ToBytes(ref buffer, start + written, part);
            }
            else
            {
                // We must create meaningfull unique serialization even when references are not resolved
                // because serialization of this script is used to compute the unique hash of a transform
                // to detect nodes that must be grouped together. By default, before reference resolution,
                // id is set to the execution specific object id.
                written += serializer.ToBytes(ref buffer, start + written, id);
            }

            return written;
        }
    }
}
