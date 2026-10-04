using Microsoft.Data.Sqlite;

namespace MyPasswordDesktopTest;

public class SqliteVersionTests
{
    [Test]
    public void NativeSqliteIncludesCve20256965Fix()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";
        var version = Version.Parse((string)command.ExecuteScalar()!);
        TestContext.Out.WriteLine($"Loaded native SQLite: {version}");
        Assert.That(version, Is.GreaterThanOrEqualTo(new Version(3, 50, 2)));
    }
}
