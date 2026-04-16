using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    [Table("Sales_Schema")]
    public class SalesSchemaRow
    {
        [Key]
        [Column("ConditionTypeID")]
        public int ConditionTypeID { get; set; }

        [Required]
        [MaxLength(32)]
        public string ConditionType { get; set; } = "";

        [Required]
        public string ConditionDescription { get; set; } = "";

        /// <summary>Walk-In or Dealer</summary>
        [Required]
        [MaxLength(32)]
        public string SalesType { get; set; } = "";
    }
}
