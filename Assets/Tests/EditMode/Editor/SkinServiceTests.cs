using System.Collections.Generic;
using NUnit.Framework;
using UIModule.Data.ScriptableObjects;
using UIModule.UI.Services;
using UIModule.UI.Shop;
using UnityEngine;

namespace UIModule.Tests.EditMode.Editor
{
    public sealed class SkinServiceTests
    {
        private SkinDatabaseSO database;
        private FakeSaveService saveService;

        [SetUp]
        public void SetUp()
        {
            database = ScriptableObject.CreateInstance<SkinDatabaseSO>();
            database.skins = new List<SkinData>
            {
                new() { id = "default", displayName = "Default" },
                new() { id = "red", displayName = "Red" },
                new() { id = "blue", displayName = "Blue" }
            };

            saveService = new FakeSaveService();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(database);
        }

        [Test]
        public void Initialize_RestoresValidSavedIndex()
        {
            saveService.SetInt(SkinService.SelectedSkinIndexKey, 2);

            var service = new SkinService(database, saveService);
            service.Initialize();

            Assert.That(service.GetCurrentSkinIndex(), Is.EqualTo(2));
            Assert.That(service.GetCurrentSkin().displayName, Is.EqualTo("Blue"));
        }

        [Test]
        public void Initialize_InvalidSavedIndexFallsBackToZero()
        {
            saveService.SetInt(SkinService.SelectedSkinIndexKey, 50);

            var service = new SkinService(database, saveService);
            service.Initialize();

            Assert.That(service.GetCurrentSkinIndex(), Is.EqualTo(0));
            Assert.That(saveService.GetInt(SkinService.SelectedSkinIndexKey), Is.EqualTo(0));
        }

        [Test]
        public void Initialize_EmptyDatabaseReturnsNoCurrentSkin()
        {
            database.skins.Clear();

            var service = new SkinService(database, saveService);
            service.Initialize();

            Assert.That(service.GetCurrentSkinIndex(), Is.EqualTo(-1));
            Assert.That(service.GetCurrentSkin(), Is.Null);
        }

        [Test]
        public void SetCurrentSkinIndex_SavesSelection()
        {
            var service = new SkinService(database, saveService);
            service.Initialize();

            service.SetCurrentSkinIndex(1);

            Assert.That(service.GetCurrentSkinIndex(), Is.EqualTo(1));
            Assert.That(saveService.GetInt(SkinService.SelectedSkinIndexKey), Is.EqualTo(1));
            Assert.That(saveService.SaveCallCount, Is.EqualTo(2));
        }

        private sealed class FakeSaveService : ISaveService
        {
            private readonly Dictionary<string, int> ints = new();

            public int SaveCallCount { get; private set; }

            public bool HasKey(string key)
            {
                return ints.ContainsKey(key);
            }

            public int GetInt(string key, int defaultValue = 0)
            {
                return ints.TryGetValue(key, out var value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                ints[key] = value;
            }

            public void Save()
            {
                SaveCallCount++;
            }
        }
    }
}
