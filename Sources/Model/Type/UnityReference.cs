namespace Armine.Model.Type
{
    /// <summary>
    /// Interface of handled references to UnityEngine.Object.
    /// </summary>
    public sealed partial class UnityReference
    {
        #region Members
        private System.Type type;
        private bool resolved;
        private uint id;
        private uint part;
        #endregion

        #region Constructors
        private UnityReference()
        {
            // To avoid invalid constructions
        }
        #endregion
    }
}
