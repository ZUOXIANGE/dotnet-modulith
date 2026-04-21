using DotNetCore.CAP;
using DotNetModulith.Abstractions.Domain;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Orders.Application.Commands.ConfirmOrder;
using DotNetModulith.Modules.Orders.Domain;
using DotNetModulith.Modules.Orders.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Orders.Tests.Application;

public sealed class ConfirmOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldThrowBusinessException()
    {
        var repository = new InMemoryOrderRepository();
        var dispatcher = new NoopDomainEventDispatcher();
        var sut = CreateHandler(repository, dispatcher);

        var command = new ConfirmOrderCommand(OrderId.New());

        Func<Task> act = async () => _ = await sut.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<BusinessException>();
        exception.Which.Code.Should().Be(ApiCodes.Common.NotFound);
        exception.Which.HttpStatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_WhenOrderStateInvalid_ShouldThrowBusinessException()
    {
        var repository = new InMemoryOrderRepository();
        var dispatcher = new NoopDomainEventDispatcher();
        var sut = CreateHandler(repository, dispatcher);

        var order = OrderEntity.Create("customer-1", [new OrderLineData("P001", "Product 1", 1, 10m)]);
        order.Cancel("cancelled for test");
        await repository.AddAsync(order, TestContext.Current.CancellationToken);

        var command = new ConfirmOrderCommand(order.Id);

        Func<Task> act = async () => _ = await sut.Handle(command, TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<BusinessException>();
        exception.Which.Code.Should().Be(ApiCodes.Order.InvalidState);
        exception.Which.HttpStatusCode.Should().Be(400);
    }

    private static ConfirmOrderCommandHandler CreateHandler(
        IOrderRepository repository,
        IDomainEventDispatcher dispatcher)
    {
        return new ConfirmOrderCommandHandler(
            repository,
            dispatcher,
            NullLogger<ConfirmOrderCommandHandler>.Instance,
            new FusionCache(new FusionCacheOptions()),
            new OrdersDbContext(new DbContextOptionsBuilder<OrdersDbContext>().Options),
            new NullCapPublisher());
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly Dictionary<OrderId, OrderEntity> _orders = new();

        public Task<OrderEntity?> GetByIdAsync(OrderId id, CancellationToken ct = default)
            => Task.FromResult(_orders.GetValueOrDefault(id));

        public Task<IReadOnlyList<OrderEntity>> GetByCustomerIdAsync(string customerId, int limit, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<OrderEntity>)_orders.Values
                .Where(x => x.CustomerId == customerId)
                .Take(limit)
                .ToList());

        public Task<IReadOnlyList<OrderEntity>> GetPendingOrdersAsync(int limit, CancellationToken ct = default)
            => Task.FromResult((IReadOnlyList<OrderEntity>)_orders.Values
                .Where(x => x.Status == OrderStatus.Pending)
                .Take(limit)
                .ToList());

        public Task AddAsync(OrderEntity order, CancellationToken ct = default)
        {
            _orders[order.Id] = order;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OrderEntity order, CancellationToken ct = default)
        {
            _orders[order.Id] = order;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NoopDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(IAggregateRoot aggregateRoot, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class NullCapPublisher : ICapPublisher
    {
        public IServiceProvider ServiceProvider => null!;

        public ICapTransaction? Transaction { get; set; }

        public Task PublishAsync<T>(string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishAsync<T>(string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Publish<T>(string name, T? contentObj, string? callbackName = null)
        {
        }

        public void Publish<T>(string name, T? contentObj, IDictionary<string, string?> headers)
        {
        }

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, IDictionary<string, string?> headers)
        {
        }

        public void PublishDelay<T>(TimeSpan delayTime, string name, T? contentObj, string? callbackName = null)
        {
        }
    }
}
