﻿// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Nito.AsyncEx;
using Voting.Basis.EventSignature.Exceptions;

namespace Voting.Basis.EventSignature;

/// <summary>
/// A contest cache which stores <see cref="ContestCacheEntry"/>.
/// Thread-safety is in the responsibility of the caller. The caller can acquire a write lock by calling BatchWrite().
/// </summary>
public class ContestCache : IDisposable
{
    private readonly Dictionary<Guid, ContestCacheEntry> _cacheEntries = new();
    private readonly AsyncReaderWriterLock _lock = new AsyncReaderWriterLock();

    /// <summary>
    /// Gets the contest cache entry. The caller should have a write lock per <see cref="BatchWrite"/> to ensure thread-safety.
    /// </summary>
    /// <param name="contestId">Contest id.</param>
    /// <returns>The reference of the contest cache entry.</returns>
    public ContestCacheEntry? Get(Guid contestId)
    {
        return _cacheEntries.GetValueOrDefault(contestId);
    }

    /// <summary>
    /// Gets all contest cache entries. The caller should have a write lock per <see cref="BatchWrite"/> to ensure thread-safety.
    /// </summary>
    /// <returns>A collection of references of the contest cache entries.</returns>
    public IEnumerable<ContestCacheEntry> GetAll()
    {
        return _cacheEntries.Values;
    }

    /// <summary>
    /// Adds a contest cache entry to the cache. The caller should have a write lock per <see cref="BatchWrite"/> to ensure thread-safety.
    /// </summary>
    /// <param name="entry">Contest cache entry.</param>
    /// <exception cref="ContestMemoryCacheException">If the contest id was already added.</exception>
    public void Add(ContestCacheEntry entry)
    {
        if (!_cacheEntries.TryAdd(entry.Id, entry))
        {
            throw new ContestCacheException($"Attempt to add {entry.Id}, but it was already added");
        }
    }

    /// <summary>
    /// Removes a contest cache entry. The caller should have a write lock per <see cref="BatchWrite"/> to ensure thread-safety.
    /// Does nothing when the element is not found.
    /// </summary>
    /// <param name="contestId">Contest id.</param>
    public void Remove(Guid contestId)
    {
        _cacheEntries.Remove(contestId);
    }

    /// <summary>
    /// Acquire a write lock and returns a disposable which releases the write lock.
    /// </summary>
    /// <returns>A disposable which releases the write lock.</returns>
    public IDisposable BatchWrite()
    {
        return _lock.WriterLock();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void Clear()
    {
        DisposeKeys();
        _cacheEntries.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            using var cacheWrite = BatchWrite();
            Clear();
        }
    }

    private void DisposeKeys()
    {
        foreach (var entry in _cacheEntries.Values)
        {
            entry.KeyData?.Key.Dispose();
            entry.KeyData = null;
        }
    }
}
