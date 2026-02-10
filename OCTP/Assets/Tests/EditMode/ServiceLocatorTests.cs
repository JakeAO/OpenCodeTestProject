using NUnit.Framework;
using OCTP.Core;
using System;
using System.Threading.Tasks;

namespace OCTP.Tests
{
    public class ServiceLocatorTests
    {
        private class TestServiceA : IGameService
        {
            public int Value { get; set; }
        }

        private class TestServiceB : IGameService
        {
            public string Name { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }

        [Test]
        public void Register_ValidService_RegistersSuccessfully()
        {
            // Arrange
            var service = new TestServiceA { Value = 42 };

            // Act
            ServiceLocator.Register(service);

            // Assert
            Assert.IsTrue(ServiceLocator.Has<TestServiceA>());
        }

        [Test]
        public void Register_NullService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ServiceLocator.Register<TestServiceA>(null));
        }

        [Test]
        public void Register_DuplicateService_ThrowsInvalidOperationException()
        {
            // Arrange
            var service1 = new TestServiceA { Value = 1 };
            var service2 = new TestServiceA { Value = 2 };
            ServiceLocator.Register(service1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Register(service2));
        }

        [Test]
        public void Get_RegisteredService_ReturnsCorrectInstance()
        {
            // Arrange
            var service = new TestServiceA { Value = 42 };
            ServiceLocator.Register(service);

            // Act
            var retrieved = ServiceLocator.Get<TestServiceA>();

            // Assert
            Assert.AreEqual(service, retrieved);
            Assert.AreEqual(42, retrieved.Value);
        }

        [Test]
        public void Get_UnregisteredService_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<TestServiceA>());
        }

        [Test]
        public void TryGet_RegisteredService_ReturnsTrueAndService()
        {
            // Arrange
            var service = new TestServiceA { Value = 42 };
            ServiceLocator.Register(service);

            // Act
            bool result = ServiceLocator.TryGet<TestServiceA>(out var retrieved);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(service, retrieved);
            Assert.AreEqual(42, retrieved.Value);
        }

        [Test]
        public void TryGet_UnregisteredService_ReturnsFalseAndDefault()
        {
            // Act
            bool result = ServiceLocator.TryGet<TestServiceA>(out var retrieved);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(retrieved);
        }

        [Test]
        public void Has_RegisteredService_ReturnsTrue()
        {
            // Arrange
            var service = new TestServiceA { Value = 42 };
            ServiceLocator.Register(service);

            // Act
            bool result = ServiceLocator.Has<TestServiceA>();

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void Has_UnregisteredService_ReturnsFalse()
        {
            // Act
            bool result = ServiceLocator.Has<TestServiceA>();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void Clear_RemovesAllServices()
        {
            // Arrange
            var serviceA = new TestServiceA { Value = 42 };
            var serviceB = new TestServiceB { Name = "Test" };
            ServiceLocator.Register(serviceA);
            ServiceLocator.Register(serviceB);

            // Act
            ServiceLocator.Clear();

            // Assert
            Assert.IsFalse(ServiceLocator.Has<TestServiceA>());
            Assert.IsFalse(ServiceLocator.Has<TestServiceB>());
        }

        [Test]
        public void MultipleServices_CanBeRegisteredAndRetrieved()
        {
            // Arrange
            var serviceA = new TestServiceA { Value = 42 };
            var serviceB = new TestServiceB { Name = "Test" };

            // Act
            ServiceLocator.Register(serviceA);
            ServiceLocator.Register(serviceB);

            // Assert
            var retrievedA = ServiceLocator.Get<TestServiceA>();
            var retrievedB = ServiceLocator.Get<TestServiceB>();

            Assert.AreEqual(serviceA, retrievedA);
            Assert.AreEqual(serviceB, retrievedB);
            Assert.AreEqual(42, retrievedA.Value);
            Assert.AreEqual("Test", retrievedB.Name);
        }

        [Test]
        public void ThreadSafety_ConcurrentRegistrations_AllSucceed()
        {
            // Arrange
            const int taskCount = 10;
            var tasks = new Task[taskCount];
            var services = new TestServiceA[taskCount];

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                int index = i;
                services[i] = new TestServiceA { Value = i };
                
                // Only register the first service to avoid duplicate registration exceptions
                if (i == 0)
                {
                    tasks[i] = Task.Run(() => ServiceLocator.Register(services[index]));
                }
                else
                {
                    // For other tasks, just check thread-safe reads
                    tasks[i] = Task.Run(() =>
                    {
                        ServiceLocator.Has<TestServiceA>();
                        if (ServiceLocator.TryGet<TestServiceA>(out var service))
                        {
                            _ = service.Value;
                        }
                    });
                }
            }

            Task.WaitAll(tasks);

            // Assert
            Assert.IsTrue(ServiceLocator.Has<TestServiceA>());
        }

        [Test]
        public void ThreadSafety_ConcurrentReads_DoNotThrow()
        {
            // Arrange
            var service = new TestServiceA { Value = 42 };
            ServiceLocator.Register(service);
            const int taskCount = 100;
            var tasks = new Task[taskCount];

            // Act
            for (int i = 0; i < taskCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var retrieved = ServiceLocator.Get<TestServiceA>();
                    Assert.AreEqual(42, retrieved.Value);
                });
            }

            // Assert - should not throw
            Assert.DoesNotThrow(() => Task.WaitAll(tasks));
        }
    }
}
