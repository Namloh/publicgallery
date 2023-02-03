using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace galerie.Models
{
    public class Gallery
    {
       

        public int GalleryId { get; set; }
        public string GalleryName { get; set; } = "Your First Gallery";

        public string GalleryBackgroundColor { get; set; } = "rgb(34, 42, 54)";
        public bool IsPublic { get; set; } // je verejna?
        public string UserId { get; set; }
        [Required]
        public User User { get; set; }

        public List<StoredFile> Images { get; set; } = new List<StoredFile>();
        
    }
}
