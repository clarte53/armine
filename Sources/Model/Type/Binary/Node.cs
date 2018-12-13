using CLARTE.Serialization;

namespace Armine.Model.Type
{
	public partial class Node : IBinarySerializable
	{
		public partial class GraphicMesh : IBinarySerializable
		{
			public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
			{
				uint read = serializer.FromBytes(buffer, start, out meshIndex);
				read += serializer.FromBytes(buffer, start + read, out materialsIndexes);

				return read;
			}

			public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
			{
				uint written = serializer.ToBytes(ref buffer, start, meshIndex);
				written += serializer.ToBytes(ref buffer, start + written, materialsIndexes);

				return written;
			}
		}

		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			int hide_flags;

			uint read = serializer.FromBytes(buffer, start, out name);
			read += serializer.FromBytes(buffer, start + read, out tag);
			read += serializer.FromBytes(buffer, start + read, out layer);
			read += serializer.FromBytes(buffer, start + read, out active);
			read += serializer.FromBytes(buffer, start + read, out hide_flags);
			read += serializer.FromBytes(buffer, start + read, out position);
			read += serializer.FromBytes(buffer, start + read, out rotation);
			read += serializer.FromBytes(buffer, start + read, out scale);
			read += serializer.FromBytes(buffer, start + read, out children);
			read += serializer.FromBytes(buffer, start + read, out meshes);
			read += serializer.FromBytes(buffer, start + read, out metadata, true);

			hideFlags = (UnityEngine.HideFlags) hide_flags;

			return read;
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			uint written = serializer.ToBytes(ref buffer, start, name);
			written += serializer.ToBytes(ref buffer, start + written, tag);
			written += serializer.ToBytes(ref buffer, start + written, layer);
			written += serializer.ToBytes(ref buffer, start + written, active);
			written += serializer.ToBytes(ref buffer, start + written, (int) hideFlags);
			written += serializer.ToBytes(ref buffer, start + written, position);
			written += serializer.ToBytes(ref buffer, start + written, rotation);
			written += serializer.ToBytes(ref buffer, start + written, scale);
			written += serializer.ToBytes(ref buffer, start + written, children);
			written += serializer.ToBytes(ref buffer, start + written, meshes);
			written += serializer.ToBytes(ref buffer, start + written, metadata, true);

			return written;
		}
	}
}
