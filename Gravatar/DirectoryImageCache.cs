using System;
using System.Drawing;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Gravatar
{
    public interface IImageCache
    {
        /// <summary>
        /// Adds the image to the cache from the supplied stream.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        /// <param name="imageStream">The stream which contains the image.</param>
        Task AddImageAsync(string imageFileName, Stream imageStream);

        /// <summary>
        /// Clears the cache by deleting all images.
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// Deletes the specified image from the cache.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        Task DeleteImageAsync(string imageFileName);

        /// <summary>
        /// Retrieves the image from the cache.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        /// <param name="defaultBitmap">The default image to return 
        /// if the requested image does not exist in the cache.</param>
        Task<Image> GetImageAsync(string imageFileName, Bitmap defaultBitmap);
    }

    public class DirectoryImageCache : IImageCache
    {
        private const int DefaultCacheDays = 30;
        private readonly string _cachePath;
        private readonly int _cacheDays;
        private readonly IFileSystem _fileSystem;


        public DirectoryImageCache(string cachePath, int cacheDays, IFileSystem fileSystem)
        {
            _cachePath = cachePath;
            _fileSystem = fileSystem;
            _cacheDays = cacheDays;
            if (_cacheDays < 1)
            {
                _cacheDays = DefaultCacheDays;
            }
        }

        public DirectoryImageCache(string cachePath, int cacheDays)
            : this(cachePath, cacheDays, new FileSystem())
        {
        }


        /// <summary>
        /// Adds the image to the cache from the supplied stream.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        /// <param name="imageStream">The stream which contains the image.</param>
        public async Task AddImageAsync(string imageFileName, Stream imageStream)
        {
            if (!_fileSystem.Directory.Exists(_cachePath))
            {
                _fileSystem.Directory.CreateDirectory(_cachePath);
            }

            try
            {
                string file = Path.Combine(_cachePath, imageFileName);
                using (var output = new FileStream(file, FileMode.Create))
                {
                    byte[] buffer = new byte[1024];
                    int read;

                    if (imageStream == null)
                    {
                        return;
                    }
                    while ((read = await imageStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read);
                    }
                }
            }
            catch
            {
                // do nothing
            }
        }

        /// <summary>
        /// Clears the cache by deleting all images.
        /// </summary>
        public async Task ClearAsync()
        {
            if (!_fileSystem.Directory.Exists(_cachePath))
            {
                return;
            }
            foreach (var file in _fileSystem.Directory.GetFiles(_cachePath))
            {
                try
                {
                    await Task.Run(() => _fileSystem.File.Delete(file));
                }
                catch
                {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Deletes the specified image from the cache.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        public async Task DeleteImageAsync(string imageFileName)
        {
            string file = Path.Combine(_cachePath, imageFileName);
            if (!_fileSystem.File.Exists(file))
            {
                return;
            }
            try
            {
                await Task.Run(() => _fileSystem.File.Delete(file));
            }
            catch
            {
                // do nothing
            }
        }

        /// <summary>
        /// Retrieves the image from the cache.
        /// </summary>
        /// <param name="imageFileName">The image file name.</param>
        /// <param name="defaultBitmap">The default image to return 
        /// if the requested image does not exist in the cache.</param>
        public async Task<Image> GetImageAsync(string imageFileName, Bitmap defaultBitmap)
        {
            string file = Path.Combine(_cachePath, imageFileName);
            try
            {
                if (HasExpired(file))
                {
                    return null;
                }
                return await Task.Run(() =>
                {
                    using (Stream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        return Image.FromStream(fileStream);
                    }
                });
            }
            catch
            {
                return null;
            }
        }


        private bool HasExpired(string fileName)
        {
            var file = _fileSystem.FileInfo.FromFileName(fileName);
            if (!file.Exists)
            {
                return true;
            }
            return file.LastWriteTime < DateTime.Now.AddDays(-_cacheDays);
        }
    }
}
