using CLARTE.Serialization;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Mesh : IBinarySerializable
	{
		public partial class SubMesh : IBinarySerializable
		{
			public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
			{
				int topo;

				uint read = serializer.FromBytes(buffer, start, out topo);
				read += serializer.FromBytes(buffer, start + read, out triangles);

				topology = (MeshTopology) topo;

				return read;
			}

			public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
			{
				uint written = serializer.ToBytes(ref buffer, start, (int) topology);
				written += serializer.ToBytes(ref buffer, start + written, triangles);

				return written;
			}
		}

		public uint FromBytes(Binary serializer, Binary.Buffer buffer, uint start)
		{
			uint read = serializer.FromBytes(buffer, start, out name);
			read += serializer.FromBytes(buffer, start + read, out submeshes);
			read += serializer.FromBytes(buffer, start + read, out vertices);
			read += serializer.FromBytes(buffer, start + read, out normals);
			read += serializer.FromBytes(buffer, start + read, out tangents);
			read += serializer.FromBytes(buffer, start + read, out uv1);
			read += serializer.FromBytes(buffer, start + read, out uv2);
			read += serializer.FromBytes(buffer, start + read, out colors);
			
			return read;
		}

		public uint ToBytes(Binary serializer, ref Binary.Buffer buffer, uint start)
		{
			uint written = serializer.ToBytes(ref buffer, start, name);
			written += serializer.ToBytes(ref buffer, start + written, submeshes);
			written += serializer.ToBytes(ref buffer, start + written, vertices);
			written += serializer.ToBytes(ref buffer, start + written, normals);
			written += serializer.ToBytes(ref buffer, start + written, tangents);
			written += serializer.ToBytes(ref buffer, start + written, uv1);
			written += serializer.ToBytes(ref buffer, start + written, uv2);
			written += serializer.ToBytes(ref buffer, start + written, colors);

			return written;
		}
	}
}
