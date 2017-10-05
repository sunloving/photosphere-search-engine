﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jbta.SearchEngine.Utils;

namespace Jbta.SearchEngine.FileVersioning
{
    internal class FileVersionsCollection
    {
        private readonly ReaderWriterLockSlim _lock;

        public FileVersionsCollection()
        {
            _lock = new ReaderWriterLockSlim();
        }

        public ISet<FileVersion> Items { get; } = new HashSet<FileVersion>();

        public void Add(FileVersion fileVersion)
        {
            using (_lock.Exclusive())
            {
                Items.Add(fileVersion);
            }
        }

        public IReadOnlyCollection<FileVersion> GetList()
        {
            using (_lock.Shared())
            {
                return Items.ToList();
            }
        }

        public void UpdateFilePath(string newPath)
        {
            using (_lock.SharedIntentExclusive())
            {
                foreach (var fileVersion in Items)
                {
                    using (_lock.Exclusive())
                    {
                        fileVersion.Path = newPath;
                    }
                }
            }
        }

        public IEnumerable<FileVersion> RemoveDeadVersions()
        {
            using (_lock.SharedIntentExclusive())
            {
                var deadVersions = Items.Where(i => i.IsDead).ToList();
                foreach (var deadVersion in deadVersions)
                {
                    using (_lock.Exclusive())
                    {
                        Items.Remove(deadVersion);
                    }
                }
                return deadVersions;
            }
        }

        public void KillVersions()
        {
            using (_lock.Exclusive())
            {
                foreach (var version in Items)
                {
                    version.IsDead = true;
                }
            }
        }

        public bool All(Func<FileVersion, bool> predicate)
        {
            using (_lock.Shared())
            {
                return Items.All(predicate);
            }
        }
    }
}