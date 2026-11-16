using Microsoft.Data.SqlClient;

namespace CakeStudio.Data;

public static class CheckoutLock
{
    public static async Task<T> RunAsync<T>(IConfiguration configuration,int customerId,Func<Task<T>> action,Func<T> whenBusy,CancellationToken cancellationToken)
    {
        await using var connection =
            new SqlConnection(configuration.GetConnectionString("DefaultConnection"));

        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @result INT;

            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;

            SELECT @result;
            """;

        command.Parameters.AddWithValue("@resource", $"CakeStudio:checkout:{customerId}");

        var result = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? -1);

        if (result < 0)
            return whenBusy();

        return await action();
    }
}