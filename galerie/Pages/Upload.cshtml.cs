using FileUpload.Models;
using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using System.Drawing.Imaging;
using System.Text;
using ExifLib;

namespace galerie.Pages
{
    public class UploadModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private IConfiguration _configuration;
        private ApplicationDbContext _context;

        private int _squareSize;
        private int _sameAspectRatioHeight;
        private int _size = 300;


        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public string NotLoggedInMessage { get; set; }
        [BindProperty]
        public bool CheckedDeafault { get; set; }
        public IFormFile Upload { get; set; }
        [BindProperty(SupportsGet = true)]
        public int? GalId { get; set; }

        public UploadModel(IWebHostEnvironment environment, ApplicationDbContext context, IConfiguration configuration)
        {
            _environment = environment;
            _context = context;
            _configuration = configuration;
            if (Int32.TryParse(_configuration["Thumbnails:SquareSize"], out _squareSize) == false) _squareSize = 64; // získej data z konfigurave nebo použij 64
            if (Int32.TryParse(_configuration["Thumbnails:SameAspectRatioHeigth"], out _sameAspectRatioHeight) == false) _sameAspectRatioHeight = 128;
        }


        //ASI USELESS!!!
        public void OnGet(int? id)
        {
            GalId = id;
        }
        public async Task<IActionResult> OnPost()
        {
            ErrorMessage = "You can't upload images without an account!";
            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostGalleryAsync(int? id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                NotLoggedInMessage = "You must be logged in to upload images.";
                return Page();
            }
            var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            Guid newId = Guid.NewGuid();

            var fileRecord = new StoredFile
            {
                Id = newId,
                OriginalName = Upload.FileName,
                UploaderId = userId,
                UploadedAt = DateTime.Now,
                ContentType = Upload.ContentType,
                IsDefault = CheckedDeafault,
                GalleryId = id
            };
            string extension = System.IO.Path.GetExtension(Upload.FileName);
            if (Upload.ContentType.StartsWith("image"))
            {
                MemoryStream ims = new MemoryStream();
                MemoryStream oms = new MemoryStream();
                Upload.CopyTo(ims);
                IImageFormat format;
                Image image = Image.Load(ims.ToArray(), out format);
                
                    int largestSize = Math.Max(image.Height, image.Width);
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
                

                fileRecord.Thumbnail = new ThumbnailBlob()
                {
                    Id = newId,
                    Blob = oms.ToArray()
                };
            }

            try
            {
               
                _context.Files.Add(fileRecord);
                await _context.SaveChangesAsync();
                var file = Path.Combine(_environment.ContentRootPath, "Uploads", fileRecord.Id.ToString());
                /* ASI POOPOO !!!!!!!
                using (ExifReader reader = new ExifReader(file))
                {
                    // Extract the tag data using the ExifTags enumeration
                    DateTime datePictureTaken;
                    if (reader.GetTagValue<DateTime>(ExifTags.DateTimeDigitized, out datePictureTaken))
                    {
                        // Do whatever is required with the extracted information
                        fileRecord.ExifDateTaken = datePictureTaken;
                    }
                }
                */
                using (var fileStream = new FileStream(file, FileMode.Create))
                {
                    
                    await Upload.CopyToAsync(fileStream);
                    SuccessMessage = "File was uploaded succesfully.";
                }
            }
            catch (Exception ex)
            {
                
                ErrorMessage = "File upload failed miserably. ;(";
                
            }
            return RedirectToPage("/Privacy");
        }
       


    }
}
