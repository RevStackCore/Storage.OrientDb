using System;
using System.Collections.Generic;
using System.Linq;
using RevStackCore.OrientDb;
using RevStackCore.Storage.Model;
using RevStackCore.Storage.Repository;

namespace RevStackCore.Storage.OrientDb
{
    public class OrientDbStorageFolderRepository : IStorageFolderRepository
    {
        private readonly OrientDbRepository<StorageFolder, int> _repository;
        private readonly OrientDbRepository<StorageFile, int> _fileRepository;
        public OrientDbStorageFolderRepository(OrientDbRepository<StorageFolder, int> repository,
            OrientDbRepository<StorageFile, int> fileRepository)
        {
            _repository = repository;
            _fileRepository = fileRepository;
        }

        public IStorageFolder Add(IStorageFolder entity)
        {
            var storageFolder = new StorageFolder();
            storageFolder.Path = entity.Path;
            storageFolder.Files = entity.Files;

            storageFolder = _repository.Add(storageFolder);

            //recurse folders
            int lastIndex = storageFolder.Path.LastIndexOf('/');
            if (lastIndex != -1)
            {
                var path = storageFolder.Path.Substring(0, lastIndex);
                RecurseForCreateFolderPath(path, storageFolder.Id, storageFolder);
            }

            return storageFolder;
        }

        public void Delete(IStorageFolder entity)
        {
            List<IStorageFile> files = entity.Files;

            foreach (IStorageFile file in files)
            {
                _fileRepository.Delete((StorageFile)file);
            }

            List<IStorageFolder> folders = entity.Folders;

            foreach (IStorageFolder folder in folders)
            {
                _repository.Delete((StorageFolder)folder);
            }

            _repository.Delete((StorageFolder)entity);
        }

        public IStorageFolder Get(int id)
        {
            return _repository.Find(c => c.Id == id).SingleOrDefault();
        }

        public IEnumerable<IStorageFolder> Get()
        {
            return _repository.Get();
        }

        public IStorageFolder Update(IStorageFolder entity)
        {
            var currentFolder = _repository.Find(c => c.Id == entity.Id).SingleOrDefault();

            if (currentFolder != null && currentFolder.Path != entity.Path)
            {
                List<IStorageFile> files = entity.Files;

                foreach (IStorageFile file in files)
                {
                    var filePath = "";
                    var oPath = "";

                    var obj = _fileRepository.Find(c => c.Id == file.Id).SingleOrDefault();
                    filePath = obj.Path;
                    oPath = obj.Path;

                    int index = filePath.LastIndexOf('/');
                    string fileName = filePath;

                    if (index != -1)
                        fileName = filePath.Substring(index + 1);

                    oPath = entity.Path + "/" + fileName;

                    var f = (StorageFile)file;
                    f.Path = oPath;
                    _fileRepository.Update(f);
                }

                List<IStorageFolder> folders = entity.Folders;

                foreach (IStorageFolder folder in folders)
                {
                    var filePath = "";
                    var oPath = "";

                    var obj = Get(folder.Id);
                    filePath = obj.Path;
                    oPath = obj.Path;

                    int index = filePath.LastIndexOf('/');
                    string fileName = filePath;

                    if (index != -1)
                        fileName = filePath.Substring(index + 1);

                    oPath = entity.Path + "/" + fileName;

                    var f = (StorageFolder)folder;
                    f.Path = oPath;
                    _repository.Update(f);
                }

            }

            entity = _repository.Update((StorageFolder)entity);

            return entity;
        }

        #region "private"
        
        private void RecurseForCreateFolderPath(string path, int childId, IStorageFolder obj)
        {
            //check for root folder
            if (string.IsNullOrEmpty(path))
                path = "/";

            IList<StorageFolder> array = _repository.Find(c => c.Path == path).ToList();

            if (!array.Any())
            {
                //folder does not exist so insert and move up
                StorageFolder folder = new StorageFolder();
                folder.Path = path;
                folder.Folders.Add(obj);
                folder = _repository.Add(folder);

                if (!string.IsNullOrEmpty(path.Trim()))
                {
                    int lastIndex = path.LastIndexOf('/');
                    if (lastIndex != -1)
                    {
                        var id = folder.Id;
                        path = path.Substring(0, lastIndex);
                        RecurseForCreateFolderPath(path, id, folder);
                    }
                }
            }
            
        }

        #endregion
    }
}
