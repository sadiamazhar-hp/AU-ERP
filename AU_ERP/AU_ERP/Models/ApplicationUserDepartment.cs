using System.ComponentModel.DataAnnotations.Schema;

namespace AU_ERP.Models
{
    public class ApplicationUserDepartment
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = null!;

        /// <summary>Required when <see cref="Department"/> is Store; selects which plant this user’s Store access applies to.</summary>
        public string? PlantID { get; set; }

        [ForeignKey(nameof(PlantID))]
        public virtual PlantsSample? Plant { get; set; }
    }
}
