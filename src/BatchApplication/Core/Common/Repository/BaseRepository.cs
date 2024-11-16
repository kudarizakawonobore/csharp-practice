using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BatchApplication.Core.Common.Repository;

public abstract class BaseRepository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly ILogger _logger;
    protected readonly RepositoryOptions _options;

    protected BaseRepository(
        DbContext context,
        ILogger logger,
        RepositoryOptions options)
    {
        _context = context;
        _logger = logger;
        _options = options;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                return await _context.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity with ID {Id}", id);
                throw;
            }
        });
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                return await _context.Set<TEntity>().ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all entities");
                throw;
            }
        });
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                var result = await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return result.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding entity");
                throw;
            }
        });
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                _context.Set<TEntity>().Update(entity);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity");
                throw;
            }
        });
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                var entity = await GetByIdAsync(id, cancellationToken);
                if (entity != null)
                {
                    _context.Set<TEntity>().Remove(entity);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity with ID {Id}", id);
                throw;
            }
        });
    }

    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, retryCount))
            {
                retryCount++;
                _logger.LogWarning(ex, "Retry attempt {RetryCount} of {MaxRetries}", 
                    retryCount, _options.RetryOptions.MaxRetryAttempts);
                
                await Task.Delay(_options.RetryOptions.RetryDelay);
            }
        }
    }

    protected virtual bool ShouldRetry(Exception ex, int retryCount)
    {
        return retryCount < _options.RetryOptions.MaxRetryAttempts &&
               (ex is DbUpdateException || ex is TimeoutException);
    }
}
