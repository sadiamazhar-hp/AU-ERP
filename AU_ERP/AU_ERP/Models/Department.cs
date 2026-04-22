namespace AU_ERP.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public ICollection<ApplicationUserDepartment> UserDepartments { get; set; } = new List<ApplicationUserDepartment>();
    }
}
