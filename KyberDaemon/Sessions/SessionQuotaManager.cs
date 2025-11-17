using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace KyberDaemon.Sessions;

internal sealed class SessionQuotaManager : IDisposable
{
    private readonly SemaphoreSlim _globalSemaphore;
    private readonly ConcurrentDictionary<string, UserQuota> _userQuotas;
    private readonly int _maxSessionsPerUser;
    private readonly int _maxGlobalSessions;
    private bool _disposed;

    public SessionQuotaManager(int maxGlobalSessions, int maxSessionsPerUser)
    {
        if (maxGlobalSessions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxGlobalSessions), "Le nombre maximum de sessions doit être positif");
        }

        _maxGlobalSessions = maxGlobalSessions;
        _globalSemaphore = new SemaphoreSlim(maxGlobalSessions, maxGlobalSessions);
        _userQuotas = new ConcurrentDictionary<string, UserQuota>(StringComparer.OrdinalIgnoreCase);
        _maxSessionsPerUser = Math.Max(1, maxSessionsPerUser);
    }

    public int MaxSessions => _maxGlobalSessions;
    public int MaxSessionsPerUser => _maxSessionsPerUser;
    public int ActiveSessions => _maxGlobalSessions - _globalSemaphore.CurrentCount;

    public int GetActiveSessions(string username)
        => _userQuotas.TryGetValue(username, out var quota) ? quota.Current : 0;

    public async ValueTask<IDisposable?> TryAcquireGlobalAsync(CancellationToken cancellationToken)
    {
        if (!await _globalSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new ReleaseHandle(() => _globalSemaphore.Release());
    }

    public async ValueTask<IDisposable?> TryAcquireUserAsync(string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Le nom d'utilisateur est obligatoire", nameof(username));
        }

        var quota = _userQuotas.GetOrAdd(username, _ => new UserQuota(_maxSessionsPerUser));
        if (!await quota.TryAcquireAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new ReleaseHandle(quota.Release);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _globalSemaphore.Dispose();

        foreach (var quota in _userQuotas.Values)
        {
            quota.Dispose();
        }

        _userQuotas.Clear();
    }

    private sealed class UserQuota : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _current;

        public UserQuota(int limit)
        {
            _semaphore = new SemaphoreSlim(limit, limit);
        }

        public int Current => Volatile.Read(ref _current);

        public async ValueTask<bool> TryAcquireAsync(CancellationToken token)
        {
            if (await _semaphore.WaitAsync(0, token).ConfigureAwait(false))
            {
                Interlocked.Increment(ref _current);
                return true;
            }

            return false;
        }

        public void Release()
        {
            var remaining = Interlocked.Decrement(ref _current);
            if (remaining < 0)
            {
                Interlocked.Exchange(ref _current, 0);
            }

            _semaphore.Release();
        }

        public void Dispose() => _semaphore.Dispose();
    }

    private sealed class ReleaseHandle : IDisposable
    {
        private readonly Action _onDispose;
        private int _disposed;

        public ReleaseHandle(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _onDispose();
            }
        }
    }
}
