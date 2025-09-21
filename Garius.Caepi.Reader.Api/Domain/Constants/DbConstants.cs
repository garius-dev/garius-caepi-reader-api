namespace Garius.Caepi.Reader.Api.Domain.Constants
{
    public static class DbConstants
    {
        public static class SystemRoles
        {
            public const string Developer = "Developer";
            public const string SuperAdmin = "SuperAdmin";
            public const string Owner = "Owner";
            public const string Admin = "Admin";
            public const string Basic = "User";

            public static readonly IReadOnlyList<string> SuperUserRoles = new List<string> { Owner, SuperAdmin, Developer };
        }

        public static class Roles
        {
            public const string Read = "Permissions.Roles.Read";
            public const string Create = "Permissions.Roles.Create";
            public const string Update = "Permissions.Roles.Update";
            public const string Delete = "Permissions.Roles.Delete";
            public const string Manage = "Permissions.Roles.Manage";
        }

        public static List<string> GetAllPermissions()
        {
            var allPermissions = new List<string>();
            var nestedTypes = typeof(DbConstants).GetNestedTypes();

            foreach (var type in nestedTypes)
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                allPermissions.AddRange(fields.Select(fi => fi.GetValue(null)?.ToString() ?? string.Empty));
            }

            return allPermissions.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        }
    }
}