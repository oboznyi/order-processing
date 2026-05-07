using Microsoft.EntityFrameworkCore;
using OrderProcessing.API.Domain.Messaging;
using OrderProcessing.API.Infrastructure.Persistence;

namespace OrderProcessing.API.Infrastructure.Persistence.Repositories;

public sealed class ProcessedMessageRepository : IProcessedMessageRepository
{
    private readonly AppDbContext _dbContext;

    public ProcessedMessageRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProcessedMessages.AnyAsync(
            x => x.MessageId == messageId && x.ConsumerName == consumerName,
            cancellationToken);
    }

    public void Add(ProcessedMessage message)
    {
        _dbContext.ProcessedMessages.Add(message);
    }
}
