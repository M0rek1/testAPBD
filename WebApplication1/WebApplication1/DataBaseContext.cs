namespace WebApplication1;

using System.Data.SqlClient;

public class DataBaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqlConnection _connection;

    public DataBaseContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public SqlConnection Connection
    {
        get
        {
            if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }
    }

    public void Dispose()
    {
        if (_connection.State == System.Data.ConnectionState.Open)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}