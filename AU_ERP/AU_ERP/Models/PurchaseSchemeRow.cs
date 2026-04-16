using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    [Table("Purchase_Scheme")]
    public class PurchaseSchemeRow
    {
        [Key]
        [Column("ConditionID")]
        public int ConditionID { get; set; }

        [Required]
        [MaxLength(32)]
        public string ConditionType { get; set; } = "";

        [Required]
        public string ConditionSchema { get; set; } = "";
    }
}
