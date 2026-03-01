using System.Collections.Generic;

namespace Systems.Persistence.Core
{
    public interface IDataService
    {
        // Backwards-compatible game-level save/load
        void Save<T>(T data, string saveName, bool overwrite = true);
        T Load<T>(string saveName);
        void Delete(string saveName);
        void DeleteAll();
        IEnumerable<string> ListSaves();

        // File-per-namespace within a save folder
        void Save<T>(T data, string saveName, string ns, string fileName = null, bool overwrite = true);
        T Load<T>(string saveName, string ns, string fileName = null);
        IEnumerable<string> ListFiles(string saveName, string ns);
        void DeleteFile(string saveName, string ns, string fileName = null);
    }
}