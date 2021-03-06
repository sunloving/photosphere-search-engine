﻿using Photosphere.SearchEngine.Events;
using Photosphere.SearchEngine.FileIndexing;
using Photosphere.SearchEngine.FileSupervision.FileSystemEventWatching;

namespace Photosphere.SearchEngine.FileSupervision.FileSystemPolling
{
    internal class PathRemover
    {
        private readonly IEventReactor _eventReactor;
        private readonly PathWatchersCollection _watchers;
        private readonly IIndexEjector _indexEjector;

        public PathRemover(
            IEventReactor eventReactor,
            PathWatchersCollection watchers,
            IIndexEjector indexEjector)
        {
            _eventReactor = eventReactor;
            _watchers = watchers;
            _indexEjector = indexEjector;
        }

        public void Remove(string path)
        {
            if (!_watchers.TryRemove(path))
            {
                return;
            }

            _eventReactor.React(EngineEvent.PathWatchingEnded, path);
            _indexEjector.Eject(path);
        }
    }
}