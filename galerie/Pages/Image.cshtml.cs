using galerie.Data;
using galerie.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

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
        [Authorize]
        public IActionResult OnGet(Guid id)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Claims.Where(c => c.Type == ClaimTypes.NameIdentifier).FirstOrDefault().Value;
                Input = _context.Files.Find(id);
                if(Input.IsDefault == true || Input.IsPublic == true)
                {
                    return Page();
                }
                if (Input.UploaderId != userId)
                {
                    return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
                }
            }
            else
            {
                return  RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            return Page();
        }
        public void OnGetPublic(Guid id)
        {

            Input = _context.Files.Find(id);

        }

        public IActionResult OnGetFile(Guid id)
        {
            var fullName = Path.Combine(_environment.ContentRootPath, "Uploads", id.ToString().Replace("-", string.Empty));
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
