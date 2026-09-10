using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using vehiculo.api.Events;
using vehiculo.api.Data;
using vehiculo.api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;

namespace vehiculo.api.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private RabbitMQ.Client.IConnection? _connection;

        public RabbitMQConsumer(
            IConfiguration configuration,
            ILogger<RabbitMQConsumer> logger,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"],
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"]
            };

            _connection = await factory.CreateConnectionAsync();
            var channel = await _connection.CreateChannelAsync();

            var queueName = _configuration["RabbitMQ:QueueName"] ?? "categoria_creado";

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var mensaje = Encoding.UTF8.GetString(body);
                    var evento = JsonSerializer.Deserialize<CategoriaCreadoEvento>(mensaje);

                    if (evento != null)
                    {
                        _logger.LogInformation("Evento recibido. IdCategoria: {IdCategoria}", evento.IdCategoria);

                        // 1. Crear el scope para acceder a la base de datos
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<VehiculoDBContext>();

                        // 2. Crear el nuevo vehículo con los campos de tu tabla (Omitiendo IdVehiculo)
                        var nuevoVehiculo = new Vehiculo
                        {
                            IdCategoria = evento.IdCategoria,
                            Marca = "Generado Automáticamente",
                            Modelo = "N/A",
                            Precio = 0.00m,
                            Stock = 1,
                            Estado = true
                        };

                        // 3. Guardar en SQL Server
                        db.Vehiculos.Add(nuevoVehiculo);
                        await db.SaveChangesAsync();

                        _logger.LogInformation("Vehículo creado exitosamente en base de datos.");
                    }

                    // Confirmar a RabbitMQ que el mensaje se procesó bien
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al guardar el vehículo.");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }

                await Task.Yield();
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

            _logger.LogInformation("RabbitMQ consumer iniciado. Conectado a {Host} y escuchando cola {Queue}.", factory.HostName, queueName);

            // Mantener el servicio en ejecución hasta que se solicite cancelación
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_connection != null)
                {
                    try
                    {
                        await _connection.CloseAsync();
                    }
                    catch { }

                    try
                    {
                        await _connection.DisposeAsync();
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cerrando conexión RabbitMQ");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}