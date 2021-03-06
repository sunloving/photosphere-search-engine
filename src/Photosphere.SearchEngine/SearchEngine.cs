﻿using System.Collections.Generic;
using System.Linq;
using Photosphere.SearchEngine.Events;
using Photosphere.SearchEngine.FileIndexing;
using Photosphere.SearchEngine.FileSupervision;
using Photosphere.SearchEngine.Scheduling;
using Photosphere.SearchEngine.Searching;
using Photosphere.SearchEngine.Utils;

namespace Photosphere.SearchEngine
{
    internal class SearchEngine : ISearchEngine
    {
        private readonly IFileIndexer _indexer;
        private readonly IIndexEjector _indexEjector;
        private readonly IFileSupervisor _supervisor;
        private readonly ISearcher _searcher;
        private readonly IScheduler _scheduler;

        public SearchEngine(
            EventReactor eventReactor,
            IFileIndexer indexer,
            IIndexEjector indexEjector,
            IFileSupervisor supervisor,
            ISearcher searcher,
            IScheduler scheduler)
        {
            _indexer = indexer;
            _indexEjector = indexEjector;
            _supervisor = supervisor;
            _searcher = searcher;
            _scheduler = scheduler;

            eventReactor.Register(EngineEvent.PathWatchingStarted, a => PathWatchingStarted?.Invoke(a));
            eventReactor.Register(EngineEvent.PathWatchingEnded, a => PathWatchingEnded?.Invoke(a));
            eventReactor.Register(EngineEvent.FileIndexingStarted, a => FileIndexingStarted?.Invoke(a));
            eventReactor.Register(EngineEvent.FileIndexingEnded, a => FileIndexingEnded?.Invoke(a));
            eventReactor.Register(EngineEvent.FileRemovingStarted, a => FileRemovingStarted?.Invoke(a));
            eventReactor.Register(EngineEvent.FileRemovingEnded, a => FileRemovingEnded?.Invoke(a));
            eventReactor.Register(EngineEvent.FileUpdateInitiated, a => FileUpdateInitiated?.Invoke(a));
            eventReactor.Register(EngineEvent.FileUpdateFailed, a => FileUpdateFailed?.Invoke(a));
            eventReactor.Register(EngineEvent.PathChanged, a => FilePathChanged?.Invoke(a));
            eventReactor.Register(EngineEvent.IndexCleanUpFailed, a => IndexCleanUpFailed?.Invoke(a));

            _scheduler.Start();
        }

        public event SearchEngineEventHandler PathWatchingStarted;
        public event SearchEngineEventHandler PathWatchingEnded;
        public event SearchEngineEventHandler FileIndexingStarted;
        public event SearchEngineEventHandler FileIndexingEnded;
        public event SearchEngineEventHandler FileRemovingStarted;
        public event SearchEngineEventHandler FileRemovingEnded;
        public event SearchEngineEventHandler FileUpdateInitiated;
        public event SearchEngineEventHandler FileUpdateFailed;
        public event SearchEngineEventHandler FilePathChanged;
        public event SearchEngineEventHandler IndexCleanUpFailed;

        public IEnumerable<string> PathesUnderIndex => _supervisor.WatchedPathes;

        public bool Add(string path)
        {
            var fullPath = FileSystem.GetFullPath(path);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }
            if (!FileSystem.IsExistingPath(fullPath))
            {
                return false;
            }
            if (_supervisor.IsUnderWatching(fullPath))
            {
                return false;
            }

            _indexer.Index(fullPath);
            _supervisor.Watch(fullPath);
            return true;
        }

        public bool Remove(string path)
        {
            var fullPath = FileSystem.GetFullPath(path);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return false;
            }
            if (!FileSystem.IsExistingPath(fullPath))
            {
                return false;
            }
            if (!_supervisor.IsUnderWatching(fullPath))
            {
                return false;
            }

            _supervisor.Unwatch(fullPath);
            _indexEjector.Eject(fullPath);
            return true;
        }

        public IEnumerable<WordEntry> Search(string query, bool wholeWord = false)
        {
            return string.IsNullOrWhiteSpace(query)
                ? Enumerable.Empty<WordEntry>()
                : _searcher.Search(query, wholeWord);
        }

        public IEnumerable<string> SearchFiles(string query, bool wholeWord = false)
        {
            return string.IsNullOrWhiteSpace(query)
                ? Enumerable.Empty<string>()
                : _searcher.SearchFiles(query, wholeWord);
        }

        public void Dispose()
        {
            _supervisor?.Dispose();
            _scheduler?.Dispose();
        }
    }
}