using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    public class ApplicationUserDepartment
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        /// <summary>
        /// Comma-separated <see cref="PlantsSample.PlantID"/> values when <see cref="Department"/> is Store or Sales.
        /// </summary>
        public string? PlantID { get; set; }

        [ForeignKey(nameof(PlantID))]
        public virtual PlantsSample? Plant { get; set; }
    }
}
