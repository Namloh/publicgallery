using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace galerie.Pages
{
    public class PrivacyModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<PrivacyModel> _logger;
        private ApplicationDbContext _context;
        public ICollection<StoredFile> Files { get; set; }

        public List<Gallery> Galleries { get; set; }
        [BindProperty]
        public Gallery Gallery { get; set; }

        public bool SignedIn { get; set; }
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        [TempData]
        public string NotLoggedInMessage { get; set; }
        public PrivacyModel(ILogger<PrivacyModel> logger, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _environment = env;
        }

        public IActionResult OnGet()
        {
            SignedIn = User.Identity.IsAuthenticated;
            if (SignedIn)
            {
                var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
               
                Files = _context.Files.AsNoTracking().Include(f => f.Uploader).Include(f => f.Thumbnail).ToList();
                Galleries = _context.Galleries.AsNoTracking()
                    .Include(i => i.Images).ThenInclude(u => u.Uploader)
                    .Include(u => u.User)
                    .Where(u => u.UserId == userId)
                    .ToList();
                
               
                foreach(var g in Galleries)
                {
                    if(g.GalleryName == "Your First gallery")
                    {
                        foreach (var f in Files)
                        {
                            if (f.IsDefault)
                            {
                                g.Images.Add(f);
                            }
                        }
                    }
                }
                return Page();
            }
            else
            {
                NotLoggedInMessage = "You must have an account to access your gallery. Please log in or register to continue.";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            
        }

        public async Task<IActionResult> OnGetThumbnail(string filename, ThumbnailType type = ThumbnailType.Square)
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
        public async Task<IActionResult> OnGetVisibility(string filename)
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
                return RedirectToPage();
            }
            return NotFound("no record for this file");
        }
        public async Task<IActionResult> OnGetGalleryVisibility(int galleryId)
        {
            Gallery gal = await _context.Galleries
            .Where(g => g.GalleryId == galleryId)
            .Include(i => i.Images)
            .FirstOrDefaultAsync();
            if (gal != null)
            {
                if (gal.IsPublic)
                {
                    foreach (var i in gal.Images)
                    {
                        i.IsPublic = !gal.IsPublic;
                    }
                    gal.IsPublic = false;
                }
                else
                {
                    foreach (var i in gal.Images)
                    {
                        i.IsPublic = !gal.IsPublic;
                    }
                    gal.IsPublic = true;
                }
                await _context.SaveChangesAsync();
                return RedirectToPage();
            }
            return NotFound("no record for this file");
        }

        public IActionResult OnGetDownload(string filename)
        {
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename);
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if (fileRecord != null) // je soubor v databázi?
                {
                    return PhysicalFile(fullName, fileRecord.ContentType, fileRecord.OriginalName);
                    // vrať ho zpátky pod původním názvem a typem
                }
                else
                {
                    ErrorMessage = "There is no record of such file.";
                    return RedirectToPage();
                }
            }
            else
            {
                ErrorMessage = "There is no such file.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetDelete(string filename)
        {
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename);
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if (fileRecord != null) // je soubor v databázi?
                {
                    _context.Files.Remove(fileRecord);
                    System.IO.File.Delete(fullName);
                    await _context.SaveChangesAsync();
                    return RedirectToPage();
                    // vrať ho zpátky pod původním názvem a typem
                }
                else
                {
                    ErrorMessage = "There is no record of such file.";
                    return RedirectToPage();
                }
            }
            else
            {
                ErrorMessage = "There is no such file.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostNewGallery()
        {
            var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
            Gallery = new Gallery { GalleryBackgroundColor = Gallery.GalleryBackgroundColor, GalleryName = Gallery.GalleryName, UserId = userId};
            _context.Galleries.Add(Gallery);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}