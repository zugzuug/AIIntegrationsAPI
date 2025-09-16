using AIIntegrationsAPI.Abstractions;
using AIIntegrationsAPI.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AIIntegrationsAPI.Infrastructure
{
    public sealed class InMemoryChatSessionStore : IChatSessionStore
    {
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _opts =
            new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromHours(4) };

        public InMemoryChatSessionStore(IMemoryCache cache) => _cache = cache;

        public Task<IReadOnlyList<ChatMessage>> GetAsync(string sessionId, CancellationToken ct)
        {
            var list = _cache.GetOrCreate(sessionId, _ => new List<ChatMessage>())!;
            return Task.FromResult((IReadOnlyList<ChatMessage>)list);
        }

        public Task AppendAsync(string sessionId, IEnumerable<ChatMessage> newMessages, CancellationToken ct)
        {
            var list = _cache.GetOrCreate(sessionId, _ => new List<ChatMessage>())!;
            list.AddRange(newMessages);
            _cache.Set(sessionId, list, _opts);
            return Task.CompletedTask;
        }

        public Task ResetAsync(string sessionId, CancellationToken ct)
        {
            _cache.Remove(sessionId);
            return Task.CompletedTask;
        }
    }
}