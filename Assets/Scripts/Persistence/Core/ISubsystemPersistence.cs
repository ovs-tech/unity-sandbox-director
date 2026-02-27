using System;

namespace Systems.Persistence.Core
{
    public enum PersistenceTarget
    {
        Embedded, // Inside game-data.json
        External  // Separate file/folder
    }

    public interface ISubsystemPersistence
    {
        // Namespace (subfolder) used for this subsystem within a save folder
        string Namespace { get; }

        // Persistent name (filename without extension) used for this subsystem
        string PersistentName { get; }

        // Where this subsystem should be persisted
        PersistenceTarget Target { get; }

        // Return a serializable object representing current state (will be serialized by IDataService)
        object GetSaveData();

        // Load data previously produced by GetSaveData; object will be of type DataType
        void LoadData(object data);

        // The concrete type returned by GetSaveData and expected by LoadData
        Type DataType { get; }
    }
}
