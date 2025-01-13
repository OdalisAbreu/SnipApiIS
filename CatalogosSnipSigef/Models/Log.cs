using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CatalogosSnipSigef.Models
{
    [Table("trd_logs")] // Especifica el nombre de la tabla
    public class Log
    {
        [Key]
        public int id_log { get; set; }
        public string type { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public DateTime date { get; set; }
        public int user_id { get; set; }

        public string? ip { get; set; }

        public string? end_point { get; set; }
        public string? input {  get; set; }

        public string? output { get; set; }

        public string? method { get; set; }
    }
}
