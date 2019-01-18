using System.Collections.Generic;

namespace Armine.Model.Type
{
    public sealed partial class UnityComponent
    {
        private partial interface IBackend
        {

        }

        private partial class BackendGeneric : IBackend
        {
            #region Members
            private Dictionary<string, object> fields;
            private Dictionary<string, object> properties;
            #endregion
        }

        private partial class BackendBinarySerializable : IBackend
        {
            #region Members
            private byte[] serialized;
            #endregion
        }

        #region Members
        private System.Type type;
        private IBackend backend;
        #endregion

        #region Helper methods
        private void CreateBackend()
        {
            if(typeof(CLARTE.Serialization.IBinarySerializable).IsAssignableFrom(type))
            {
                backend = new BackendBinarySerializable();
            }
            else
            {
                backend = new BackendGeneric();
            }
        }
        #endregion
    }
}
