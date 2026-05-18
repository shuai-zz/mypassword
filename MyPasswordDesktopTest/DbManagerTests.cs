using System.Collections.Generic;
using System.IO;
using MyPasswordDesktop.Core;
using MyPasswordDesktop.Core.Data;
using MyPasswordDesktop.Core.Entities;
using MyPasswordDesktop.Util;

namespace MyPasswordDesktopTest
{
    /// <summary>Ported from <c>desktop/.../core/DbManagerTest.java</c>.</summary>
    public class DbManagerTests
    {
        private const int PbeIterations = 100_000;

        private string _dbFile;
        private DbManager _dbManager;

        [SetUp]
        public void SetUp()
        {
            _dbFile = Path.Combine(Path.GetTempPath(), "test-mypassword-" + Path.GetRandomFileName() + ".db");
            // ensure the file does not exist so DbManager runs the init schema
            if (File.Exists(_dbFile))
            {
                File.Delete(_dbFile);
            }
            _dbManager = new DbManager(_dbFile);
        }

        [TearDown]
        public void TearDown()
        {
            _dbManager?.Close();
            foreach (string f in Directory.GetFiles(Path.GetDirectoryName(_dbFile),
                         Path.GetFileNameWithoutExtension(_dbFile) + "*"))
            {
                try { File.Delete(f); } catch { /* ignore */ }
            }
        }

        [Test]
        public void TestQueryAppVersion()
        {
            Assert.That(_dbManager.QueryAppVersion(), Is.EqualTo(1));
        }

        [Test]
        public void TestQueryDataVersion()
        {
            Assert.That(_dbManager.QueryDataVersion(), Is.EqualTo(0));
            _dbManager.IncDataVersion();
            Assert.That(_dbManager.QueryDataVersion(), Is.EqualTo(1));
        }

        [Test]
        public void TestInitConfig()
        {
            Assert.That(_dbManager.QueryVaultConfig(), Is.Null);

            byte[] dek = EncryptUtils.GenerateKey();
            char[] password = "password".ToCharArray();
            byte[] pbeSalt = EncryptUtils.GenerateSalt();
            byte[] pbeKey = EncryptUtils.DerivePbeKey(password, pbeSalt, PbeIterations);
            byte[] iv = EncryptUtils.GenerateIV();
            byte[] encryptedDek = EncryptUtils.Encrypt(dek, pbeKey, iv);

            var vc = new VaultConfig
            {
                id = 1,
                b64_encrypted_dek = Base64Utils.B64(encryptedDek),
                b64_encrypted_dek_iv = Base64Utils.B64(iv),
                b64_pbe_salt = Base64Utils.B64(pbeSalt),
                pbe_iterations = PbeIterations,
            };
            _dbManager.Insert(vc);

            VaultConfig stored = _dbManager.QueryVaultConfig();
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored.id, Is.EqualTo(1));
            Assert.That(stored.pbe_iterations, Is.EqualTo(PbeIterations));
            Assert.That(stored.b64_encrypted_dek, Is.EqualTo(vc.b64_encrypted_dek));
        }

        [Test]
        public void TestInsertItems()
        {
            List<Item> items = _dbManager.QueryForList<Item>("");
            Assert.That(items, Is.Empty);

            for (int i = 1; i <= 10; i++)
            {
                var item = new Item
                {
                    item_type = ItemType.LOGIN,
                    id = IdUtils.NextId(),
                    deleted = false,
                    b64_encrypted_data = "{ \"title\": \"data-" + i + "\" }",
                    b64_encrypted_data_iv = "iv-" + i,
                    updated_at = 1_000_000 + i,
                };
                _dbManager.Insert(item);
            }

            items = _dbManager.QueryForList<Item>("");
            Assert.That(items, Has.Count.EqualTo(10));
        }
    }
}
