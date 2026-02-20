using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Systems.PlacementSystem.Sockets;

namespace PlacementSystem.Tests
{
    /// <summary>
    /// Tests for the socket snapping system.
    /// </summary>
    public class SocketSystemTests
    {
        private GameObject _socketGameObject;
        private GameObject _snapManagerGameObject;
        private SocketType _testSocketType;

        [SetUp]
        public void SetUp()
        {
            _socketGameObject = new GameObject("TestSocket");
            _snapManagerGameObject = new GameObject("SnapManager");
            _testSocketType = ScriptableObject.CreateInstance<SocketType>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_socketGameObject != null)
                Object.DestroyImmediate(_socketGameObject);
            if (_snapManagerGameObject != null)
                Object.DestroyImmediate(_snapManagerGameObject);
            if (_testSocketType != null)
                Object.DestroyImmediate(_testSocketType);
        }

        [Test]
        public void SocketType_CreatesSuccessfully()
        {
            // Assert
            Assert.IsNotNull(_testSocketType, "SocketType should be created");
        }

        [Test]
        public void Socket_InitialState_IsNotOccupied()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();

            // Assert
            Assert.IsFalse(socket.IsOccupied, "New socket should not be occupied");
        }

        [Test]
        public void Socket_CanSetOccupied()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();

            // Act
            socket.IsOccupied = true;

            // Assert
            Assert.IsTrue(socket.IsOccupied, "Socket should be marked as occupied");
        }

        [Test]
        public void Socket_CanAccept_WithMatchingType_ReturnsTrue()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            // Use reflection to set the private field for testing
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket, _testSocketType);

            // Act
            bool result = socket.CanAccept(_testSocketType);

            // Assert
            Assert.IsTrue(result, "Socket should accept matching socket type");
        }

        [Test]
        public void Socket_CanAccept_WhenOccupied_ReturnsFalse()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket, _testSocketType);
            socket.IsOccupied = true;

            // Act
            bool result = socket.CanAccept(_testSocketType);

            // Assert
            Assert.IsFalse(result, "Occupied socket should not accept new objects");
        }

        [Test]
        public void Socket_Clear_ResetsOccupiedStatus()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            socket.IsOccupied = true;

            // Act
            socket.Clear();

            // Assert
            Assert.IsFalse(socket.IsOccupied, "Clear should reset occupied status");
        }

        [Test]
        public void SnapManager_RegisterSocket_Succeeds()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();
            var socket = _socketGameObject.AddComponent<Socket>();

            // Act
            Assert.DoesNotThrow(() => snapManager.RegisterSocket(socket));
        }

        [Test]
        public void SnapManager_UnregisterSocket_Succeeds()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();
            var socket = _socketGameObject.AddComponent<Socket>();
            snapManager.RegisterSocket(socket);

            // Act
            Assert.DoesNotThrow(() => snapManager.UnregisterSocket(socket));
        }

        [Test]
        public void SnapManager_FindNearestSocket_NoSockets_ReturnsNull()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();

            // Act
            Socket result = snapManager.FindNearestSocket(Vector3.zero, _testSocketType, 10f);

            // Assert
            Assert.IsNull(result, "Should return null when no sockets available");
        }

        [Test]
        public void SnapManager_GetSocketsOfType_FiltersCorrectly()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();
            var socket1 = _socketGameObject.AddComponent<Socket>();
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket1, _testSocketType);
            
            snapManager.RegisterSocket(socket1);

            // Act
            var sockets = snapManager.GetSocketsOfType(_testSocketType);

            // Assert
            Assert.AreEqual(1, sockets.Count, "Should find one socket of the specified type");
        }

        [Test]
        public void SnapManager_ClearAllSockets_ClearsOccupiedStatus()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();
            var socket = _socketGameObject.AddComponent<Socket>();
            socket.IsOccupied = true;
            snapManager.RegisterSocket(socket);

            // Act
            snapManager.ClearAllSockets();

            // Assert
            Assert.IsFalse(socket.IsOccupied, "All sockets should be cleared");
        }

        [Test]
        public void SnapManager_RefreshSocketCache_DoesNotThrow()
        {
            // Expect log message
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex("SnapManager: Cached \\d+ sockets"));

            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();

            // Act & Assert
            Assert.DoesNotThrow(() => snapManager.RefreshSocketCache());
        }

        // Additional Socket Tests

        [Test]
        public void Socket_CanAccept_WithNullSocketType_ReturnsFalse()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket, _testSocketType);

            // Act
            bool result = socket.CanAccept(null);

            // Assert
            Assert.IsFalse(result, "Socket should reject null socket type");
        }

        [Test]
        public void Socket_CanAccept_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket, _testSocketType);

            var differentSocketType = ScriptableObject.CreateInstance<SocketType>();

            // Act
            bool result = socket.CanAccept(differentSocketType);

            // Assert
            Assert.IsFalse(result, "Socket should reject different socket type");

            // Cleanup
            Object.DestroyImmediate(differentSocketType);
        }

        [Test]
        public void Socket_SocketType_ReturnsAssignedType()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket, _testSocketType);

            // Act
            var result = socket.SocketType;

            // Assert
            Assert.AreEqual(_testSocketType, result, "SocketType property should return assigned value");
        }

        [Test]
        public void Socket_CanAccept_WhenSocketTypeIsNull_ReturnsFalse()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            // Leave _socketType as null (default)

            // Act
            bool result = socket.CanAccept(_testSocketType);

            // Assert
            Assert.IsFalse(result, "Socket with null type should reject all requests");
        }

        [Test]
        public void Socket_Clear_WhenNotOccupied_DoesNotThrow()
        {
            // Arrange
            var socket = _socketGameObject.AddComponent<Socket>();
            socket.IsOccupied = false;

            // Act & Assert
            Assert.DoesNotThrow(() => socket.Clear());
            Assert.IsFalse(socket.IsOccupied, "Should remain not occupied");
        }

        [Test]
        public void SnapManager_RegisterSocket_NullSocket_DoesNotThrow()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();

            // Act & Assert
            Assert.DoesNotThrow(() => snapManager.RegisterSocket(null));
        }

        [Test]
        public void SnapManager_UnregisterSocket_NullSocket_DoesNotThrow()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();

            // Act & Assert
            Assert.DoesNotThrow(() => snapManager.UnregisterSocket(null));
        }

        [Test]
        public void SnapManager_FindNearestSocket_WithNullType_ReturnsNull()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();
            var socket = _socketGameObject.AddComponent<Socket>();
            snapManager.RegisterSocket(socket);

            // Act
            Socket result = snapManager.FindNearestSocket(Vector3.zero, null, 10f);

            // Assert
            Assert.IsNull(result, "Should return null when searching for null type");
        }

        [Test]
        public void SnapManager_GetSocketsOfType_WithNullType_ReturnsEmptyList()
        {
            // Arrange
            var snapManager = _snapManagerGameObject.AddComponent<SnapManager>();
            var socket = _socketGameObject.AddComponent<Socket>();
            var socketTypeField = typeof(Socket).GetField("_socketType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            socketTypeField.SetValue(socket, _testSocketType);
            snapManager.RegisterSocket(socket);

            // Act
            var sockets = snapManager.GetSocketsOfType(null);

            // Assert
            Assert.AreEqual(0, sockets.Count, "Should return empty list for null type");
        }

        [Test]
        public void SocketType_DisplayName_ReturnsCorrectValue()
        {
            // Arrange
            var socketType = ScriptableObject.CreateInstance<SocketType>();
            var displayNameField = typeof(SocketType).GetField("_displayName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            displayNameField.SetValue(socketType, "TestSocket");

            // Act
            var result = socketType.DisplayName;

            // Assert
            Assert.AreEqual("TestSocket", result, "DisplayName should return assigned value");

            // Cleanup
            Object.DestroyImmediate(socketType);
        }

        [Test]
        public void SocketType_GizmoColor_ReturnsCorrectValue()
        {
            // Arrange
            var socketType = ScriptableObject.CreateInstance<SocketType>();
            var colorField = typeof(SocketType).GetField("_gizmoColor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            colorField.SetValue(socketType, Color.blue);

            // Act
            var result = socketType.GizmoColor;

            // Assert
            Assert.AreEqual(Color.blue, result, "GizmoColor should return assigned value");

            // Cleanup
            Object.DestroyImmediate(socketType);
        }
    }
}
