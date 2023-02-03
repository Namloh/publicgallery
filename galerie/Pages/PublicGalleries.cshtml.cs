using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace galerie.Pages
{
    public class PublicGalleriesModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<PrivacyModel> _logger;
        private ApplicationDbContext _context;

        public List<Gallery> Galleries { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public PublicGalleriesModel(ILogger<PrivacyModel> logger, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _environment = env;
        }

        public void OnGet()
        {
            Galleries = _context.Galleries.AsNoTracking()
                   .Include(i => i.Images).ThenInclude(u => u.Uploader)
                   .Include(u => u.User)
                   .Where(g => g.IsPublic == true)
                   .ToList();
            if(Galleries.Count == 0)
            {
                ErrorMessage = "There are no public galleries.";
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
        public IActionResult OnGetDownload(string filename)
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
                    return RedirectToPage();
                }
            }
            else
            {
                ErrorMessage = "There is no such file.";
                return RedirectToPage();
            }
        }
    }
}
