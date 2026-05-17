using Microsoft.Data.SqlClient;

namespace AU_ERP.Validation;

/// <summary>Maps SQL Server FK/reference failures on delete to user-facing text.</summary>
public static class ReferenceConstraintDeleteMessage
{
    public static bool IsReferenceConstraint(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is SqlException sql && (sql.Number == 547 || sql.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        return false;
    }

    public static string EntityStillInUse(string entityNounPhrase) =>
        $"This {entityNounPhrase} cannot be deleted because it is still in use elsewhere. Remove or reassign those references, then try again.";

    /// <returns>Friendly FK message when the exception chain indicates REFERENCE constraint / error 547; otherwise <see cref="Exception.Message"/> chain.</returns>
    public static string MapDeleteFailure(Exception ex, string entityNounPhrase) =>
        IsReferenceConstraint(ex) ? EntityStillInUse(entityNounPhrase) : (ex.InnerException?.Message ?? ex.Message);
}
