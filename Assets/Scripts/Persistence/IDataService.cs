using System.Collections.Generic;

namespace Systems.Persistence {
    public interface IDataService {
        // Backwards-compatible game-level save/load
        void Save(GameData data, bool overwrite = true);
        GameData Load(string name);
        void Delete(string name);
        void DeleteAll();
        IEnumerable<string> ListSaves();

        // File-per-namespace within a save folder
        void Save<T>(T data, string saveName, string ns, bool overwrite = true);
        T Load<T>(string saveName, string ns);
        IEnumerable<string> ListFiles(string saveName);
        void DeleteFile(string saveName, string ns);
    }
}