using System.Collections.Generic;

namespace Armine.Model.Type
{
    public sealed partial class UnityComponent
    {
        #region Members
        private System.Type type;
        private Dictionary<string, object> fields;
        private Dictionary<string, object> properties;
        #endregion
    }
}
