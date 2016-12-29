using System;
using Newtonsoft.Json;
using RevStackCore.Storage.Model;

namespace RevStackCore.Storage.OrientDb
{
    public class StorageFile : IStorageFile
    {
        public int Id { get; set; }
        public string Path { get; set; }
        [JsonProperty(PropertyName = "@class")]
        public string _class
        {
            get { return this.GetType().Name; }
        }
        [JsonProperty(PropertyName = "@rid")]
        public string _rid { get; set; }
    }
}
