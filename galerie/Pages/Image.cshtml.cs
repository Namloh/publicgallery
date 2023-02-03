using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace galerie.Pages
{
    public class ImageModel : PageModel
    {
        private ApplicationDbContext _context;
        private ILogger<ImageModel> _logger;
        private IWebHostEnvironment _environment;

        public ImageModel(ApplicationDbContext context, 
            ILogger<ImageModel> logger, 
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public StoredFile Input { get; set; }
        public void OnGet(Guid id)
        {
            Input = _context.Files.Find(id);
        }

        public IActionResult OnGetFile(Guid id)
        {
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", id.ToString());
            if (System.IO.File.Exists(fullName))
            {
                var fileRecord = _context.Files.Find(id);
                if (fileRecord != null)
                {
                    return PhysicalFile(fullName, fileRecord.ContentType, fileRecord.OriginalName);
                }
            }
            return NotFound();
        }
    }
}
