using Microsoft.Data.Sqlite;
namespace RepairWorkshop.Data;
public sealed class Database
{
    public string FilePath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RepairWorkshop", "workshop.db");
    private string ConnectionString => new SqliteConnectionStringBuilder {DataSource=FilePath,Mode=SqliteOpenMode.ReadWriteCreate}.ToString();
    public Database()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        Execute(@"PRAGMA foreign_keys=ON;
CREATE TABLE IF NOT EXISTS Customers(Id INTEGER PRIMARY KEY, FullName TEXT NOT NULL, Phone TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Technicians(Id INTEGER PRIMARY KEY, FullName TEXT NOT NULL, Specialty TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS Parts(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Quantity INTEGER NOT NULL CHECK(Quantity>=0), Price REAL NOT NULL CHECK(Price>=0));
CREATE TABLE IF NOT EXISTS Orders(Id INTEGER PRIMARY KEY, CustomerId INTEGER NOT NULL REFERENCES Customers(Id), TechnicianId INTEGER REFERENCES Technicians(Id), DeviceType TEXT NOT NULL, Brand TEXT NOT NULL, Model TEXT NOT NULL, Fault TEXT NOT NULL, Status INTEGER NOT NULL DEFAULT 0 CHECK(Status BETWEEN 0 AND 4), Cost REAL NOT NULL DEFAULT 0 CHECK(Cost>=0), CreatedAt TEXT NOT NULL);
CREATE INDEX IF NOT EXISTS idx_orders_customer ON Orders(CustomerId);
CREATE INDEX IF NOT EXISTS idx_orders_status ON Orders(Status);");
    }
    public SqliteConnection Open() {var c=new SqliteConnection(ConnectionString);c.Open();using var pragma=c.CreateCommand();pragma.CommandText="PRAGMA foreign_keys=ON;";pragma.ExecuteNonQuery();return c;}
    public void Execute(string sql,params (string,object?)[] args)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=sql;
        foreach(var (name,value) in args)cmd.Parameters.AddWithValue(name,value??DBNull.Value);
        cmd.ExecuteNonQuery();
    }
    public System.Data.DataTable Query(string sql,params (string,object?)[] args)
    {
        using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=sql;
        foreach(var (name,value) in args)cmd.Parameters.AddWithValue(name,value??DBNull.Value);
        using var reader=cmd.ExecuteReader();var result=new System.Data.DataTable();result.Load(reader);return result;
    }
}
