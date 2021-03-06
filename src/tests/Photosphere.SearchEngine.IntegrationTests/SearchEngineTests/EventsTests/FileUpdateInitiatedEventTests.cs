﻿using System.IO;
using System.Threading.Tasks;
using Photosphere.SearchEngine.IntegrationTests.Utils;
using Xunit;

namespace Photosphere.SearchEngine.IntegrationTests.SearchEngineTests.EventsTests
{
    public class FileUpdateInitiatedEventTests
    {
        [Fact]
        public async void FileUpdateInitiated_FileChanged_Raised()
        {
            var engine = SearchEngineFactory.New();
            var tcs = new TaskCompletionSource<bool>();

            using (var file = new TestFile("foo"))
            {
                var filePath = file.Path;

                engine.Add(filePath);
                await Task.Delay(100);

                engine.FileUpdateInitiated += args =>
                {
                    tcs.TrySetResult(Path.GetFullPath(filePath) == args.Path);
                };

                file.ChangeContent("bar");

                Assert.True(await tcs.Task);
            }
        }

        [Fact]
        public async void FileUpdateInitiated_FileInFolderChanged_Raised()
        {
            var engine = SearchEngineFactory.New();
            var tcs = new TaskCompletionSource<bool>();

            using (var folder = new TestFolder())
            {
                var folderPath = folder.Path;
                using (var file = new TestFile("foo", folderPath))
                {
                    var filePath = file.Path;

                    engine.Add(folderPath);
                    await Task.Delay(100);

                    engine.FileUpdateInitiated += args =>
                    {
                        tcs.TrySetResult(Path.GetFullPath(filePath) == args.Path);
                    };

                    file.ChangeContent("bar");

                    Assert.True(await tcs.Task);
                }
            }
        }
    }
}