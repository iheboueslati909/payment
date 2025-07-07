using RabbitMQ.Client;
namespace Eventify.Payment.Api.Infrastructure.Messaging;
public interface IRabbitMqConnectionChecker
{
    Task EnsureConnectionIsAvailableAsync(CancellationToken cancellationToken = default);
}

public class RabbitMqConnectionChecker : IRabbitMqConnectionChecker
{
    private readonly IConfiguration _configuration;

    public RabbitMqConnectionChecker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnsureConnectionIsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory();
        Console.WriteLine("Checking RabbitMQ connection..." , _configuration["RabbitMQ:Uri"]);
        factory.Uri = new Uri(_configuration["RabbitMQ:Uri"] ?? throw new InvalidOperationException("RabbitMQ:Uri configuration is missing."));
        try
        {
            IConnection connection = await factory.CreateConnectionAsync(); // throws if can't connect
            if (!connection.IsOpen)
                throw new Exception("RabbitMQ connection is not open.");

            return;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to connect to RabbitMQ on startup", ex);
        }
    }
}
