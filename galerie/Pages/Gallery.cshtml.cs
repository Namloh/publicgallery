using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace galerie.Pages
{


    public class GalleryModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<GalleryModel> _logger;
        private ApplicationDbContext _context;

        public ICollection<StoredFile> Files { get; set; }
        public Gallery Gallery { get; set; }
        public bool Public { get; set; }
        public int Count { get; set; }
        public bool SignedIn { get; set; }
        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        [TempData]
        public string NotLoggedInMessage { get; set; }

        public string GallerySort { get; set; }


        public GalleryModel(ILogger<GalleryModel> logger, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _environment = env;
        }
        [Authorize]
        public async Task<IActionResult> OnGet(int galleryId, string sortOrder)
        {
            
            SignedIn = User.Identity.IsAuthenticated;
            if (SignedIn)
            {

                var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                if (galleryId != 0)
                {
                    
                    Files = _context.Files.AsNoTracking().Include(f => f.Uploader).Include(f => f.Thumbnail).ToList();
                    Gallery = _context.Galleries.AsNoTracking().Include(i => i.Images.OrderByDescending(i => i.ExifDateTaken)).ThenInclude(u => u.Uploader).Where(g => g.GalleryId == galleryId).FirstOrDefault();

                    GallerySort = sortOrder == "gallery_desc" ? "gallery" : "gallery_desc";
                    IQueryable<StoredFile> imagesIQ = from s in _context.Files.Where(f => f.GalleryId == Gallery.GalleryId) select s;
                    switch (sortOrder)
                    {
                        case "gallery":
                            imagesIQ = imagesIQ.OrderByDescending(s => s.ExifDateTaken);
                            break;
                        case "gallery_desc":
                            imagesIQ = imagesIQ.OrderBy(s => s.ExifDateTaken);
                            break;

                        default:
                            imagesIQ = imagesIQ.OrderByDescending(s => s.ExifDateTaken);
                            break;
                    }
                    Gallery.Images = await imagesIQ.AsNoTracking().ToListAsync();

                    if (Gallery.UserId != userId)
                    {
                        return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
                    }
                }
                else
                {
                    ErrorMessage = "No such gallery";
                    return RedirectToPage("/Privacy");
                }

                if (Gallery.GalleryName == "Your First Gallery")
                {
                    foreach (var f in Files)
                    {
                        if (f.IsDefault)
                        {
                            Gallery.Images.Add(f);
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
        public async Task<IActionResult> OnGetPublic(int galleryId, string sortOrder)
        {
            if (galleryId != 0)
            {
                Gallery = _context.Galleries.AsNoTracking().Include(g => g.User).Include(i => i.Images.Where(i => i.IsPublic == true).OrderByDescending(i => i.UploadedAt)).ThenInclude(u => u.Uploader).Where(g => g.GalleryId == galleryId).FirstOrDefault();
                Files = _context.Files.AsNoTracking().Include(f => f.Uploader).Include(f => f.Thumbnail).ToList();

                if (Gallery.User == null)
                {
                    return NotFound();
                }

                if (Gallery.GalleryName == "Your First Gallery")
                {
                    foreach (var f in Files)
                    {
                        if (f.IsDefault)
                        {
                            Gallery.Images.Add(f);
                        }
                    }

                }
                GallerySort = sortOrder == "gallery_desc" ? "gallery" : "gallery_desc";
                IQueryable<StoredFile> imagesIQ = from s in _context.Files.Where(f => f.GalleryId == Gallery.GalleryId) select s;
                switch (sortOrder)
                {
                    case "gallery":
                        imagesIQ = imagesIQ.OrderByDescending(s => s.ExifDateTaken);
                        break;
                    case "gallery_desc":
                        imagesIQ = imagesIQ.OrderBy(s => s.ExifDateTaken);
                        break;

                    default:
                        imagesIQ = imagesIQ.OrderByDescending(s => s.ExifDateTaken);
                        break;
                }
                Gallery.Images = await imagesIQ.AsNoTracking().ToListAsync();
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
            string userId;
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename.Replace("-", string.Empty));
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if(User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value == null && fileRecord.IsPublic == false)
                {
                    ErrorMessage = "Restricted access";
                    return RedirectToPage(new { galleryId = galleryId });
                }
                else
                {
                    userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                    if (fileRecord.UploaderId != userId && fileRecord.IsPublic == false)
                    {
                        ErrorMessage = "Restricted access";
                        return RedirectToPage(new { galleryId = galleryId });
                    }
                }
              
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
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", filename.Replace("-", string.Empty));
            var dashes = Path.Combine(_environment.ContentRootPath, "Uploads", filename);
            if (System.IO.File.Exists(fullName)) // existuje soubor na disku?
            {
                var fileRecord = _context.Files.Find(Guid.Parse(filename));
                if (fileRecord != null) // je soubor v datab·zi?
                {
                    _context.Files.Remove(fileRecord);
                    System.IO.File.Delete(fullName);
                    System.IO.File.Delete(fullName + ".chunkcomplete");
                    System.IO.File.Delete(fullName + ".chunkstart");
                    System.IO.File.Delete(fullName + ".metadata");
                    System.IO.File.Delete(fullName + ".uploadlength");
                    System.IO.File.Delete(dashes);
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
