using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    [Table("Distribution_Channel")]
    public class DistributionChannel
    {
        [Key]
        [Column("DistributionChannelID")]
        public int DistributionChannelID { get; set; }

        [Required]
        [MaxLength(200)]
        public string DistributionChannelName { get; set; } = "";
    }
}
