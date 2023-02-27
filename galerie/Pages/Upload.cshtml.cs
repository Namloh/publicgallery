using FileUpload.Models;
using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using galerie.Services;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using System.Drawing.Imaging;
using System.Text;
using ExifLib;
using Microsoft.AspNetCore.Authorization;
using tusdotnet.Models;
using tusdotnet.Stores;
using tusdotnet.Interfaces;
using tusdotnet.Models.Configuration;

namespace galerie.Pages
{
    [Authorize]
    public class UploadModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private IConfiguration _configuration;
        private ApplicationDbContext _context;
        private FileStorageManager _tus;
        private readonly IAuthorizationService _authorizationService;
        private int _squareSize;
        private int _sameAspectRatioHeight;
        private int _size = 400;

        public string UserId { get; set; }
        public bool SignedIn { get; set; }
        public bool IsAdmin { get; set; }
        [BindProperty]
        public Gallery Gallery { get; set; }
        [BindProperty]
        public string FileDescription { get; set; }
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public string NotLoggedInMessage { get; set; }
        [BindProperty]
        public bool CheckedDeafault { get; set; }
        public IFormFile Upload { get; set; }
        

        public UploadModel(IWebHostEnvironment environment, ApplicationDbContext context, IConfiguration configuration, IAuthorizationService authorizationService, FileStorageManager tus)
        {
            _tus = tus;
            _environment = environment;
            _context = context;
            _configuration = configuration;
            _authorizationService = authorizationService;
            if (Int32.TryParse(_configuration["Thumbnails:SquareSize"], out _squareSize) == false) _squareSize = 64; // získej data z konfigurave nebo použij 64
            if (Int32.TryParse(_configuration["Thumbnails:SameAspectRatioHeigth"], out _sameAspectRatioHeight) == false) _sameAspectRatioHeight = 128;
        }



        public async Task<IActionResult> OnGetGallery(int id)
        {
            SignedIn = User.Identity.IsAuthenticated;
            if (SignedIn)
            {
                var isAdministrator = await _authorizationService.AuthorizeAsync(User, "Admin");
                if (isAdministrator.Succeeded) 
                { 
                    IsAdmin = true;
                }
                Gallery = _context.Galleries.Find(id);  
                var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                UserId = userId;


                return Page();
            }
            else
            {
                NotLoggedInMessage = "You must have an account to access your gallery. Please log in or register to continue.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
        }
        public IActionResult OnGetNewGallery()
        {
            SignedIn = User.Identity.IsAuthenticated;
            if (SignedIn)
            {
                return Page();
            }
            else
            {
                NotLoggedInMessage = "You must have an account to access your gallery. Please log in or register to continue.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
        }
        public async Task<IActionResult> OnPostNewGallery()
        {
            var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            Gallery = new Gallery { GalleryBackgroundColor = Gallery.GalleryBackgroundColor, GalleryName = Gallery.GalleryName, UserId = userId };
            if (Gallery.GalleryName == "Your First Gallery")
            {
                ErrorMessage = "Choose a different name...";
                return RedirectToPage();
            }
            _context.Galleries.Add(Gallery);
            await _context.SaveChangesAsync();
            SuccessMessage = "Gallery added succesfully";
            return RedirectToPage("/Privacy");
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
            return RedirectToPage("/Gallery", new { galleryId = id });
            /*
            var fileRecord = new StoredFile
            {
                Id = newId,
                OriginalName = Upload.FileName,
                UploaderId = userId,
                UploadedAt = DateTime.Now,
                ContentType = Upload.ContentType,
                IsDefault = CheckedDeafault,
                GalleryId = id,
                Description = FileDescription
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

                    if(image.Width > 3840 && image.Height > 2160 || (image.Height > 3840 && image.Width > 2160 )) 
                {
                    ErrorMessage = "Image too big!";
                    return RedirectToPage("/Upload", "Gallery", new { id = id }); 
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
                

                fileRecord.Thumbnail = new ThumbnailBlob()
                {
                    Id = newId,
                    Blob = oms.ToArray()
                };
            }
            else
            {
                ErrorMessage = "File is not an image!";
                return RedirectToPage("/Upload", "Gallery", new { id = id });
            }

            try
            {

                 _context.Files.Add(fileRecord);
                 await _context.SaveChangesAsync();
                 var file = Path.Combine(_environment.ContentRootPath, "Uploads", fileRecord.Id.ToString());


                 using (var fileStream = new FileStream(file, FileMode.Create))
                 {

                     await Upload.CopyToAsync(fileStream);
                     SuccessMessage = "File was uploaded succesfully.";
                 }
                


                var uploadPath = Path.Combine(_environment.ContentRootPath, "Uploads", fileRecord.Id.ToString());
                var fsm = _tus;
                using (var fileStream = new FileStream(uploadPath, FileMode.Create))
                {
                    await fsm.CreateAsync(fileRecord);
                    await Upload.CopyToAsync(fileStream);
                    SuccessMessage = "File was uploaded succesfully.";
                }

            }
            catch (Exception ex)
            {
                
                ErrorMessage = "File upload failed miserably ;(";
                
            }
            
            return RedirectToPage("/Gallery", new { galleryId = id});
            */
        }
       


        }
}
