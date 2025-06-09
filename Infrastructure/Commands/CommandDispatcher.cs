using PaymentGateway.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace PaymentGateway.Infrastructure.Commands;

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService(handlerType);
        
        var method = handlerType.GetMethod("Handle");
        var result = await (Task<TResponse>)method!.Invoke(handler, new object[] { command, cancellationToken })!;
        
        return result;
    }
}
