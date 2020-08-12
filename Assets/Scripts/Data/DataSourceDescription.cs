using UnityEngine;

namespace Optispeech.Data {

    /// <summary>
    /// A scriptable object that contains information about a specific data source.
    /// The data source list will look for all instances of this scriptable object in
    /// any "Data Source Descriptions" folder inside of a Resources folder anywhere in Assets
    /// </summary>
    [CreateAssetMenu(menuName = "Optispeech/Data Source")]
    public class DataSourceDescription : ScriptableObject {
        /// <summary>
        /// The name of this data source, as it should appear in the data source list
        /// </summary>
        public string sourceName;
        /// <summary>
        /// The prefab that will be instantiated whenever this data source is the active one
        /// </summary>
        public DataSourceReader readerPrefab;

    }
}
