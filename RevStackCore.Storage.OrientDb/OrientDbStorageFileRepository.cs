using System;
using System.Collections.Generic;
using System.Linq;
using RevStackCore.OrientDb;
using RevStackCore.Storage.Model;
using RevStackCore.Storage.Repository;

namespace RevStackCore.Storage.OrientDb
{
    public class OrientDbStorageFileRepository : IStorageFileRepository
    {
        private readonly OrientDbRepository<StorageFile, int> _repository;
        private readonly OrientDbRepository<StorageFolder, int> _storageFolderRepository;

        public OrientDbStorageFileRepository(OrientDbRepository<StorageFile, int> repository,
            OrientDbRepository<StorageFolder, int> storageFolderRepository)
        {
            _repository = repository;
            _storageFolderRepository = storageFolderRepository;
        }

        public IStorageFile Add(IStorageFile entity)
        {
            var storageFile = new StorageFile();
            storageFile.Path = entity.Path;

            storageFile = _repository.Add(storageFile);

            int lastIndex = storageFile.Path.LastIndexOf('/');

            if (lastIndex != -1)
            {
                int id = storageFile.Id;
                var path = storageFile.Path.Substring(0, lastIndex);
                RecurseForCreateFolderPath(path, id, storageFile);
            }

            return storageFile;
        }

        public void Delete(IStorageFile entity)
        {
            //remove from folder 
            RemoveFromCurrentFolder(entity);
            _repository.Delete((StorageFile)entity);
        }

        public IStorageFile Get(int id)
        {
            return _repository.Find(c => c.Id == id).SingleOrDefault();
        }

        public IEnumerable<IStorageFile> Get()
        {
            return _repository.Get();
        }

        public IStorageFile Update(IStorageFile entity)
        {
            //remove from current folder 
            RemoveFromCurrentFolder(entity);

            entity = _repository.Update((StorageFile)entity);

            //recurse folders
            int lastIndex = entity.Path.LastIndexOf('/');
            if (lastIndex != -1)
            {
                var path = entity.Path.Substring(0, lastIndex);
                RecurseForCreateFolderPath(path, entity.Id, entity);
            }

            return entity;
        }

        #region "private"

        private void RemoveFromCurrentFolder(IStorageFile entity)
        {
            List<StorageFolder> folders = _storageFolderRepository.Get().Where(c => c.Files.Any(t => t.Id == entity.Id)).ToList();

            int lastIndex = entity.Path.LastIndexOf('/');
            var path = entity.Path;

            if (lastIndex != -1)
            {
                path = entity.Path.Substring(0, lastIndex);
            }

            //remove from folder if new path differs
            if (folders.Any() && folders[0].Path != path)
            {
                StorageFolder currentFolder = folders[0];
                List<IStorageFile> childFiles = currentFolder.Files;
                int index = -1;

                foreach (IStorageFile file in childFiles)
                {
                    if (file.Id == entity.Id)
                        index = childFiles.FindIndex(c => c.Id == file.Id);
                }

                if (index != -1)
                    childFiles.RemoveAt(index);

                currentFolder.Files = childFiles;

                _storageFolderRepository.Update(currentFolder);
            }
        }

        private void RecurseForCreateFolderPath(string path, int childId, IStorageEntity obj)
        {
            //check for root folder
            if (string.IsNullOrEmpty(path))
                path = "/";

            IList<StorageFolder> array = _storageFolderRepository.Find(c => c.Path == path).ToList();

            if (!array.Any())
            {

                //folder does not exist so insert and move up
                StorageFolder folder = new StorageFolder();
                folder.Path = path;
                if (obj != null)
                {
                    if (obj.GetType() == typeof(StorageFile))
                    {
                        folder.Files.Add((StorageFile)obj);
                    }

                    if (obj.GetType() == typeof(StorageFolder))
                    {
                        folder.Folders.Add((StorageFolder)obj);
                    }
                }

                folder = _storageFolderRepository.Add(folder);

                if (!string.IsNullOrEmpty(path.Trim()))
                {
                    int lastIndex = path.LastIndexOf('/');
                    if (lastIndex != -1)
                    {
                        path = path.Substring(0, lastIndex);
                        RecurseForCreateFolderPath(path, folder.Id, folder);
                    }
                }
            }
            else
            {
                StorageFolder j_obj = array[0];
                List<IStorageFile> files = j_obj.Files;
                var id = j_obj.Id;

                if (id != childId)
                {
                    var list = new List<StorageFile>();
                    foreach (StorageFile file in files)
                    {
                        list.Add(file);
                    }

                    if (!list.Where(c => c.Id == childId).Any())
                        files.Add((StorageFile)obj);
                }

                j_obj.Files = files;

                _storageFolderRepository.Update(j_obj);
            }
        }

        #endregion
    }
}
