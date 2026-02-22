using System;

namespace Systems.Persistence.Core {
    public interface ISubsystemPersistence {
        // Namespace (filename without extension) used for this subsystem within a save folder
        string Namespace { get; }

        // Return a serializable object representing current state (will be serialized by IDataService)
        object GetSaveData();

        // Load data previously produced by GetSaveData; object will be of type DataType
        void LoadData(object data);

        // The concrete type returned by GetSaveData and expected by LoadData
        Type DataType { get; }
    }
}
