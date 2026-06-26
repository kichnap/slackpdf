using PDFtoImage;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace SlackPDF.Services;

public class ThumbnailService : IDisposable
{
    private readonly string _cacheDir;
    private readonly ConcurrentDictionary<string, BitmapSource> _memCache = new();
    private readonly SemaphoreSlim _semaphore = new(4, 4);
    private bool _disposed;

    public ThumbnailService(string? cacheDir = null)
    {
        _cacheDir = cacheDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SlackPDF", "thumbcache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<BitmapSource?> GetThumbnailAsync(
        string filePath, int pageIndex, int width = 120, int height = 170,
        CancellationToken ct = default)
    {
        string key = $"{filePath}|{pageIndex}|{width}x{height}";
        if (_memCache.TryGetValue(key, out var cached))
            return cached;

        string cacheFile = Path.Combine(_cacheDir,
            $"{Convert.ToHexString(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key)))}.png");

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_memCache.TryGetValue(key, out cached))
                return cached;

            BitmapSource? bmp;
            if (File.Exists(cacheFile))
            {
                bmp = LoadFromFile(cacheFile);
            }
            else
            {
                bmp = await Task.Run(() => RenderPage(filePath, pageIndex, width, height), ct);
                if (bmp != null)
                    await Task.Run(() => SaveToFile(bmp, cacheFile), ct);
            }

            if (bmp != null)
            {
                bmp.Freeze();
                _memCache[key] = bmp;
            }
            return bmp;
        }
        catch
        {
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static BitmapSource? RenderPage(string filePath, int pageIndex, int width, int height)
    {
        try
        {
            var options = new RenderOptions(Dpi: 72);
            var images = Conversion.ToImages(filePath, null, options);
            using var skBitmap = images.Skip(pageIndex).FirstOrDefault();
            if (skBitmap == null) return null;

            using var scaled = skBitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
            using var image = SKImage.FromBitmap(scaled);
            using var data = image.Encode(SKEncodedImageFormat.Png, 85);

            var ms = new MemoryStream(data.ToArray());
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource? LoadFromFile(string path)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch { return null; }
    }

    private static void SaveToFile(BitmapSource bmp, string path)
    {
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = File.Create(path);
            encoder.Save(fs);
        }
        catch { }
    }

    public void ClearCache()
    {
        _memCache.Clear();
        try
        {
            foreach (var f in Directory.GetFiles(_cacheDir, "*.png"))
                File.Delete(f);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _semaphore.Dispose();
    }
}
