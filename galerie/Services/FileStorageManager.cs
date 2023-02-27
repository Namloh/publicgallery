using galerie.Data;
using galerie.Models;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats;
using Image = SixLabors.ImageSharp.Image;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System.Globalization;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using FileUpload.Models;
using Microsoft.AspNetCore.Mvc;

namespace galerie.Services
{
    public class FileStorageManager
    {
        private readonly ILogger<FileStorageManager> _logger;
        private readonly ApplicationDbContext _context;
        private IWebHostEnvironment _environment;
        private readonly int _size = 400;

        [TempData]
        public string ErrorMessage { get; private set; }
        [TempData]
        public string SuccessMessage { get; set; }

        public FileStorageManager(ILogger<FileStorageManager> logger, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _environment = environment;
        }

        public async Task StoreTus(ITusFile file, CancellationToken token)
        {
            _logger.Log(LogLevel.Debug, "Storing file", file.Id);
            Dictionary<string, Metadata> metadata = await file.GetMetadataAsync(token);
            string? filename = metadata.FirstOrDefault(m => m.Key == "filename").Value.GetString(System.Text.Encoding.UTF8);
            string? filetype = metadata.FirstOrDefault(m => m.Key == "filetype").Value.GetString(System.Text.Encoding.UTF8);
            string? description = metadata.FirstOrDefault(m => m.Key == "description").Value.GetString(System.Text.Encoding.UTF8);
            bool isDefault = bool.Parse(metadata.FirstOrDefault(m => m.Key == "isDefault").Value.GetString(System.Text.Encoding.UTF8));
            string userId = metadata.FirstOrDefault(m => m.Key == "userId").Value.GetString(System.Text.Encoding.UTF8);
            int galleryId = int.Parse(metadata.FirstOrDefault(m => m.Key == "galleryId").Value.GetString(System.Text.Encoding.UTF8));

           
            var f = new StoredFile
            {
                Id = Guid.Parse(file.Id),
                OriginalName = filename,
                UploadedAt = DateTime.Now,
                ExifDateTaken = DateTime.Now,
                ContentType = filetype,
                IsPublic = false,
                IsDefault = isDefault,
                Description = description,
                UploaderId = userId,
                GalleryId = galleryId,

            };

            if (f.ContentType.StartsWith("image"))
            {


                using Stream content = await file.GetContentAsync(token);
                content.Seek(0, SeekOrigin.Begin);


                MemoryStream ims = new MemoryStream();
                MemoryStream oms = new MemoryStream();
                content.CopyTo(ims);
                IImageFormat format;

                Image img = Image.Load(ims.ToArray(), out format);
                var result = img.Metadata;
                DateTime imageDatetime;
                if (result.ExifProfile == null ||
                    result.ExifProfile.GetValue(ExifTag.DateTimeOriginal) == null ||
                    result.ExifProfile.GetValue(ExifTag.DateTimeOriginal).Value == null ||
                    DateTime.TryParseExact(result.ExifProfile.GetValue(ExifTag.DateTimeOriginal).Value, "yyyy:MM:dd HH:mm:ss",
                                           CultureInfo.InvariantCulture, DateTimeStyles.None, out imageDatetime) == false)
                {
                    //nemáme datum z exifu
                    f.ExifDateTaken = f.UploadedAt; //nebo DateTime.Now()
                }
                else
                {
                    //bereme datum z Exifu fotky
                    f.ExifDateTaken = DateTime.ParseExact(result.ExifProfile.GetValue(ExifTag.DateTimeOriginal).Value, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                }
                Image image = Image.Load(ims.ToArray(), out format);

                int largestSize = Math.Max(image.Height, image.Width);

                if (image.Width > 3840 && image.Height > 2160 || (image.Height > 3840 && image.Width > 2160))
                {
                    ErrorMessage = "Image too big!";
                }
                if (image.Width > image.Height)
                {
                    image.Mutate(x => x.Resize(0, _size));
                }
                else
                {
                    image.Mutate(x => x.Resize(_size, 0));
                }
                image.Mutate(x => x.Crop(new Rectangle((image.Width - _size) / 2, (image.Height - _size) / 2, _size, _size)));
                image.Save(oms, format);
                f.Thumbnail = new ThumbnailBlob()
                {
                    Id = f.Id,
                    Blob = oms.ToArray()
                };

                _context.Files.Add(f);
                await _context.SaveChangesAsync();
                var f1 = Path.Combine(_environment.ContentRootPath, "Uploads", f.Id.ToString());

                using (var fileStream = new FileStream(f1, FileMode.Create))
                {
                    await content.CopyToAsync(fileStream);
                    SuccessMessage = "File uploaded succesfully!";

                };
                
            }
            else
            {
                ErrorMessage = "Not an image!";
            }

        }

        public async Task<ICollection<StoredFile>> ListAsync()
        {
            return await _context.Files.ToListAsync();
        }

        public async Task<StoredFile> CreateAsync(StoredFile fileRecord)
        {
            _context.Files.Add(fileRecord);
            await _context.SaveChangesAsync();
            return fileRecord;
        }
    }
}
