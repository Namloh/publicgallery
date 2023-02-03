using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net.Mime;

namespace galerie.Pages
{
    public class IndexModel : PageModel
    {
        private IWebHostEnvironment _environment;
        private readonly ILogger<IndexModel> _logger;
        private ApplicationDbContext _context;

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public ICollection<StoredFile> Files { get; set; }

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment, ApplicationDbContext context)
        {
            _environment = environment;
            _logger = logger;
            _context = context;
        }

        public void OnGet()
        {
            Files = _context.Files.AsNoTracking().Where(f => f.IsPublic).OrderByDescending(f => f.UploadedAt).Take(12).Include(f => f.Uploader).Include(f => f.Thumbnail).Include(g => g.Gallery).ToList();
            ErrorMessage = null;
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
    }
}