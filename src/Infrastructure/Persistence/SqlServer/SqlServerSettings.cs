namespace DDDExample.Infrastructure.Persistence.SqlServer;

public class SqlServerSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = "mydb";
}
