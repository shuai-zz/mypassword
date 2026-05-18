using System;
using System.Collections.Generic;
using System.IO;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Rpc;

namespace MyPasswordDesktopTest
{
    /// <summary>Ported from <c>desktop/.../core/VaultManagerTest.java</c>.</summary>
    public class VaultManagerTests
    {
        private string _dbFile;
        private VaultManager _vaultManager;
        private byte[] _key;

        [SetUp]
        public void SetUp()
        {
            _dbFile = Path.Combine(Path.GetTempPath(), "test-mypassword-" + Path.GetRandomFileName() + ".db");
            if (File.Exists(_dbFile))
            {
                File.Delete(_dbFile);
            }
            _vaultManager = new VaultManager(new DbManager(_dbFile));
            _key = _vaultManager.InitVault("password");
        }

        [TearDown]
        public void TearDown()
        {
            _vaultManager?.Close();
            foreach (string f in Directory.GetFiles(Path.GetDirectoryName(_dbFile),
                         Path.GetFileNameWithoutExtension(_dbFile) + "*"))
            {
                try { File.Delete(f); } catch { /* ignore */ }
            }
        }

        // ── vault lifecycle ─────────────────────────────────────────────────

        [Test]
        public void TestIsInitialized()
        {
            Assert.That(_vaultManager.IsInitialized(), Is.True);
        }

        [Test]
        public void TestInitVaultTwiceThrows()
        {
            Assert.Throws<InvalidOperationException>(() => _vaultManager.InitVault("password"));
        }

        [Test]
        public void TestUnlockVaultWithCorrectPassword()
        {
            byte[] unlocked = _vaultManager.UnlockVault("password".ToCharArray());
            Assert.That(unlocked, Is.Not.Null);
            Assert.That(unlocked, Is.EqualTo(_key));
        }

        [Test]
        public void TestUnlockVaultWithWrongPassword()
        {
            Assert.That(_vaultManager.UnlockVault("BadPassword".ToCharArray()), Is.Null);
        }

        // ── create & get ────────────────────────────────────────────────────

        [Test]
        public void TestCreateAndGetLoginItem()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("GitHub", "alice", "s3cret"));

            Assert.That(created, Is.InstanceOf<LoginItemData>());
            var result = (LoginItemData)created;
            Assert.That(result.id, Is.Not.Zero);
            Assert.That(result.data.title, Is.EqualTo("GitHub"));
            Assert.That(result.data.username, Is.EqualTo("alice"));
            Assert.That(result.data.password, Is.EqualTo("s3cret"));
            Assert.That(result.item_type, Is.EqualTo(ItemType.LOGIN));
            Assert.That(result.deleted, Is.False);
            Assert.That(result.favorite, Is.False);

            AbstractItemData fetched = _vaultManager.GetItem(_key, result.id);
            Assert.That(fetched.id, Is.EqualTo(result.id));
        }

        [Test]
        public void TestCreateAndGetNoteItem()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewNote("My Note", "some content"));

            Assert.That(created, Is.InstanceOf<NoteItemData>());
            var result = (NoteItemData)created;
            Assert.That(result.data.title, Is.EqualTo("My Note"));
            Assert.That(result.data.content, Is.EqualTo("some content"));
        }

        [Test]
        public void TestGetItems()
        {
            _vaultManager.CreateItem(_key, NewLogin("Site1", "u1", "p1"));
            _vaultManager.CreateItem(_key, NewLogin("Site2", "u2", "p2"));
            _vaultManager.CreateItem(_key, NewNote("Note1", "content"));

            List<AbstractItemData> items = _vaultManager.GetItems(_key);
            Assert.That(items, Has.Count.EqualTo(3));
        }

        [Test]
        public void TestGetItemNotFound()
        {
            var ex = Assert.Throws<VaultException>(() => _vaultManager.GetItem(_key, 999L));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.DATA_NOT_FOUND));
        }

        // ── create validation ───────────────────────────────────────────────

        [Test]
        public void TestCreateItemNullFields()
        {
            var login = new LoginItemData { data = null };
            var ex = Assert.Throws<VaultException>(() => _vaultManager.CreateItem(_key, login));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.BAD_FIELD));
        }

        [Test]
        public void TestCreateItemBlankTitle()
        {
            var login = NewLogin("", "user", "pass");
            var ex = Assert.Throws<VaultException>(() => _vaultManager.CreateItem(_key, login));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.BAD_FIELD));
        }

        // ── update ──────────────────────────────────────────────────────────

        [Test]
        public void TestUpdateItem()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("Old Title", "alice", "pass"));

            LoginItemData updated = NewLogin("New Title", "bob", "newpass");
            updated.id = created.id;
            AbstractItemData result = _vaultManager.UpdateItem(_key, updated);

            Assert.That(result, Is.InstanceOf<LoginItemData>());
            var loginResult = (LoginItemData)result;
            Assert.That(loginResult.data.title, Is.EqualTo("New Title"));
            Assert.That(loginResult.data.username, Is.EqualTo("bob"));
            Assert.That(loginResult.id, Is.EqualTo(created.id));
        }

        [Test]
        public void TestUpdateItemNotFound()
        {
            LoginItemData login = NewLogin("Title", "user", "pass");
            login.id = 999L;
            var ex = Assert.Throws<VaultException>(() => _vaultManager.UpdateItem(_key, login));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.DATA_NOT_FOUND));
        }

        [Test]
        public void TestUpdateItemTypeMismatch()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("Title", "user", "pass"));

            NoteItemData note = NewNote("Title", "content");
            note.id = created.id;
            var ex = Assert.Throws<VaultException>(() => _vaultManager.UpdateItem(_key, note));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.BAD_FIELD));
        }

        // ── delete & restore ────────────────────────────────────────────────

        [Test]
        public void TestDeleteItem()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("Title", "user", "pass"));
            AbstractItemData deleted = _vaultManager.DeleteItem(_key, created.id);

            Assert.That(deleted.deleted, Is.True);
            Assert.That(deleted.id, Is.EqualTo(created.id));
        }

        [Test]
        public void TestDeleteItemIdempotent()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("Title", "user", "pass"));
            _vaultManager.DeleteItem(_key, created.id);
            AbstractItemData deleted2 = _vaultManager.DeleteItem(_key, created.id); // should not throw
            Assert.That(deleted2.deleted, Is.True);
        }

        [Test]
        public void TestDeleteItemNotFound()
        {
            var ex = Assert.Throws<VaultException>(() => _vaultManager.DeleteItem(_key, 999L));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.DATA_NOT_FOUND));
        }

        [Test]
        public void TestRestoreItem()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("Title", "user", "pass"));
            _vaultManager.DeleteItem(_key, created.id);
            AbstractItemData restored = _vaultManager.RestoreItem(_key, created.id);

            Assert.That(restored.deleted, Is.False);
            Assert.That(restored.id, Is.EqualTo(created.id));
        }

        [Test]
        public void TestRestoreItemIdempotent()
        {
            AbstractItemData created = _vaultManager.CreateItem(_key, NewLogin("Title", "user", "pass"));
            AbstractItemData restored = _vaultManager.RestoreItem(_key, created.id); // not deleted, should not throw
            Assert.That(restored.deleted, Is.False);
        }

        [Test]
        public void TestRestoreItemNotFound()
        {
            var ex = Assert.Throws<VaultException>(() => _vaultManager.RestoreItem(_key, 999L));
            Assert.That(ex.ErrorCode, Is.EqualTo(ErrorCode.DATA_NOT_FOUND));
        }

        // ── settings ────────────────────────────────────────────────────────

        [Test]
        public void TestGetSettingDefault()
        {
            Assert.That(_vaultManager.GetSetting("nonexistent", "default"), Is.EqualTo("default"));
            Assert.That(_vaultManager.GetSetting("nonexistent", 42L), Is.EqualTo(42L));
        }

        [Test]
        public void TestSetAndGetStringSetting()
        {
            _vaultManager.SetSetting("theme", "dark");
            Assert.That(_vaultManager.GetSetting("theme", "light"), Is.EqualTo("dark"));
        }

        [Test]
        public void TestSetAndGetLongSetting()
        {
            _vaultManager.SetSetting("lock.time", 600L);
            Assert.That(_vaultManager.GetSetting("lock.time", 0L), Is.EqualTo(600L));
        }

        [Test]
        public void TestUpdateSetting()
        {
            _vaultManager.SetSetting("theme", "dark");
            Assert.That(_vaultManager.GetSetting("theme", ""), Is.EqualTo("dark"));
            _vaultManager.SetSetting("theme", "light");
            Assert.That(_vaultManager.GetSetting("theme", ""), Is.EqualTo("light"));
        }

        [Test]
        public void TestGetLongSettingWithInvalidValue()
        {
            _vaultManager.SetSetting("bad", "not-a-number");
            Assert.That(_vaultManager.GetSetting("bad", 99L), Is.EqualTo(99L));
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static LoginItemData NewLogin(string title, string username, string password)
            => new()
            {
                data = new LoginFieldsData { title = title, username = username, password = password },
            };

        private static NoteItemData NewNote(string title, string content)
            => new() { data = new NoteFieldsData { title = title, content = content } };
    }
}
