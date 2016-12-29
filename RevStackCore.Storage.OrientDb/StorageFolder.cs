using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RevStackCore.OrientDb;
using RevStackCore.Storage.Model;

namespace RevStackCore.Storage.OrientDb
{
    public class StorageFolder : IStorageFolder
    {
        public StorageFolder()
        {
            Files = new List<IStorageFile>();
            Folders = new List<IStorageFolder>();
        }

        public int Id { get; set; }

        public string Path { get; set; }

        [JsonConverter(typeof(InterfaceArrayConverter<IStorageFile, StorageFile>))]
        public List<IStorageFile> Files { get; set; }

        [JsonConverter(typeof(InterfaceArrayConverter<IStorageFolder, StorageFolder>))]
        public List<IStorageFolder> Folders { get; set; }

        [JsonProperty(PropertyName = "@class")]
        public string _class
        {
            get { return this.GetType().Name; }
        }

        [JsonProperty(PropertyName = "@rid")]
        public string _rid { get; set; }
    }
}
