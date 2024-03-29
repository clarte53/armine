﻿using CLARTE.Serialization;

namespace Armine.Model.Type
{
	public partial class Scene : IBinarySerializable
	{
		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
            IBinarySerializable node;

			uint read = serializer.FromBytes(buffer, start, out node);
			read += serializer.FromBytes(buffer, start + read, out meshes);
			read += serializer.FromBytes(buffer, start + read, out materials);
			read += serializer.FromBytes(buffer, start + read, out textures);

            root_node = (Node) node;

            return read;
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			uint written = serializer.ToBytes(ref buffer, start, root_node);
			written += serializer.ToBytes(ref buffer, start + written, meshes);
			written += serializer.ToBytes(ref buffer, start + written, materials);
			written += serializer.ToBytes(ref buffer, start + written, textures);

			return written;
		}
	}
}
