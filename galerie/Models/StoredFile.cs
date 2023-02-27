using FileUpload.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace galerie.Models
{
    public class StoredFile
    {
        [Key]
        public Guid Id { get; set; } // identifikátor souboru a název fyzického souboru
        [ForeignKey("UploaderId")]
        public User Uploader { get; set; } // kdo soubor nahrál
        [Required]
        public string UploaderId { get; set; } // identifikátor uživatele, který soubor nahrál
        [Required]
        public DateTime UploadedAt { get; set; } // datum a čas nahrání souboru
        [Required]
        public string OriginalName { get; set; } // původní název souboru
        [Required]
        public string ContentType { get; set; } // druh obsahu v souboru (MIME type)
        public string Description { get; set; }
        public bool IsDefault { get; set; } // je stock?
        public bool IsPublic { get; set; } // je verejny?
        public DateTime ExifDateTaken { get; set; }
        public ThumbnailBlob Thumbnail { get; set; }
        public Guid? ThumbnailId { get; set; }
        public int? GalleryId { get; set; }
        
        public Gallery Gallery { get; set; }
    }
}
