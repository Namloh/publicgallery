using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace galerie.Pages
{
    public class GalleryModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<GalleryModel> _logger;
        private ApplicationDbContext _context;


        public Gallery Gallery { get; set; }
        public bool Public { get; set; }
        public bool SignedIn { get; set; }
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        [TempData]
        public string NotLoggedInMessage { get; set; }


        public GalleryModel(ILogger<GalleryModel> logger, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _environment = env;
        }
       
        public IActionResult OnGet(int galleryId)
        {
            SignedIn = User.Identity.IsAuthenticated;
            if (SignedIn)
            {
                if(galleryId != 0)
                {
                    Gallery = _context.Galleries.AsNoTracking().Include(i => i.Images.OrderByDescending(i => i.UploadedAt)).ThenInclude(u => u.Uploader).Where(g => g.GalleryId == galleryId).FirstOrDefault();
                }
                else
                {
                    ErrorMessage = "No such gallery";
                    return RedirectToPage("/Privacy");
                }

                return Page();
            }
            else
            {
                NotLoggedInMessage = "You must have an account to access your gallery. Please log in or register to continue.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

        }
        public IActionResult OnGetPublic(int galleryId)
        {
          
            
            if (galleryId != 0)
            {
                Gallery = _context.Galleries.AsNoTracking().Include(i => i.Images.Where(i => i.IsPublic == true).OrderByDescending(i => i.UploadedAt)).ThenInclude(u => u.Uploader).Where(g => g.GalleryId == galleryId).FirstOrDefault();
            }
            else
            {
                ErrorMessage = "No such gallery";
                return RedirectToPage("/Privacy");
            }
            Public = true;
            return Page();
            

        }
        public async Task<IActionResult> OnGetThumbnail(string filename, int galleryId, ThumbnailType type = ThumbnailType.Square)
        {
            StoredFile file = await _context.Files
            .AsNoTracking()
            .Where(f => f.Id == Guid.Parse(filename))
            .Include(f => f.Thumbnail)
            .SingleOrDefaultAsync();
            if (file != null)
            {
                if (file.Thumbnail != null)
                {
                    return File(file.Thumbnail.Blob, file.ContentType);
                }
                return NotFound("no thumbnail for this file");
            }
            return NotFound("no record for this file");
        }
        public async Task<IActionResult> OnGetVisibility(string filename, int galleryId)
        {
            StoredFile file = await _context.Files
            .Where(f => f.Id == Guid.Parse(filename))
            .Include(f => f.Thumbnail)
            .SingleOrDefaultAsync();
            if (file != null)
            {
                if (file.IsPublic)
                {
                    file.IsPublic = false;
                }
                else
                {
                    file.IsPublic = true;
                }
                await _context.SaveChangesAsync();
                SuccessMessage = "Image status changed succesfully";
                return RedirectToPage( new {galleryId = galleryId});
            }
            return NotFound("no record for this file");
        }
        public IActionResult OnGetDownload(string filename, int galleryId)
        {
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename);
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if (fileRecord != null) // je soubor v datab·zi?
                {
                    return PhysicalFile(fullName, fileRecord.ContentType, fileRecord.OriginalName);
                    // vraù ho zp·tky pod p˘vodnÌm n·zvem a typem
                }
                else
                {
                    ErrorMessage = "There is no record of such file.";
                    return RedirectToPage(new { galleryId = galleryId });
                }
            }
            else
            {
                ErrorMessage = "There is no such file.";
                return RedirectToPage(new { galleryId = galleryId });
            }
        }

        public async Task<IActionResult> OnGetDelete(string filename, int galleryId)
        {
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename);
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if (fileRecord != null) // je soubor v datab·zi?
                {
                    _context.Files.Remove(fileRecord);
                    System.IO.File.Delete(fullName);
                    await _context.SaveChangesAsync();
                    SuccessMessage = "Image deleted succesfully";
                    return RedirectToPage(new { galleryId = galleryId });
                    // vraù ho zp·tky pod p˘vodnÌm n·zvem a typem
                }
                else
                {
                    ErrorMessage = "There is no record of such file.";
                    return RedirectToPage(new { galleryId = galleryId });
                }
            }
            else
            {
                ErrorMessage = "There is no such file.";
                return RedirectToPage(new { galleryId = galleryId });
            }
        }
    }
}
