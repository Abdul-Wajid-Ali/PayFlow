using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PayFlow.Infrastructure.Options;
using RabbitMQ.Client;

namespace PayFlow.Infrastructure.Messaging.Connection
{
    public class RabbitMqConnectionManager : IRabbitMqConnectionProvider, IHostedService
    {
        private IConnection? _connection;
        private readonly RabbitMqOptions _options;

        public RabbitMqConnectionManager(IOptions<RabbitMqOptions> options)
            => _options = options.Value;

        //1: Expose the connection with a null guard — throws if accessed before StartAsync completes
        public IConnection Connection => _connection
        ?? throw new InvalidOperationException("RabbitMQ connection not initialized.");

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //2: Build the connection factory from validated options
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                ConsumerDispatchConcurrency = 1, // process messages sequentially per channel
                AutomaticRecoveryEnabled = true, // reconnect if the connection drops
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10) // delay between reconnection attempts
            };

            //3: Establish the single shared connection to RabbitMQ
            _connection = await factory.CreateConnectionAsync(cancellationToken);

            //4: Declare exchanges, queues, and bindings on the broker
            await RabbitMqTopologyInitializer.InitializeAsync(_connection, cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            //5: Gracefully close the connection on application shutdown
            if (_connection is not null)
                await _connection.CloseAsync(cancellationToken);
        }
    }
}