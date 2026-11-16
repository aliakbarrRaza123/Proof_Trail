using Microsoft.Data.SqlClient;
namespace ProofTrail.Data;

// One connection per request. Registration uses one transaction across Identity and institution inserts.
public sealed class SqlDatabase(IConfiguration configuration) : IDisposable
{
    private SqlConnection? _connection;
    public SqlTransaction? Transaction { get; private set; }
    public string ConnectionString => configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is missing.");

    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_connection == null) _connection = new SqlConnection(ConnectionString);
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken);
        return _connection;
    }
    public async Task BeginAsync()
    {
        if (Transaction != null) throw new InvalidOperationException("Transaction already started.");
        Transaction = (SqlTransaction)await (await OpenAsync()).BeginTransactionAsync();
    }
    public async Task CommitAsync()
    {
        if (Transaction == null) throw new InvalidOperationException("No transaction.");
        await Transaction.CommitAsync();
        await Transaction.DisposeAsync();
        Transaction = null;
    }
    public async Task RollbackAsync()
    {
        if (Transaction == null) return;
        await Transaction.RollbackAsync();
        await Transaction.DisposeAsync();
        Transaction = null;
    }
    public void Dispose() { Transaction?.Dispose(); _connection?.Dispose(); }
}
