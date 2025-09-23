namespace Garius.Caepi.Reader.Api.Domain.Constants
{
    public class SystemPermissions
    {
        //### TENANTS
        public class TenantPermissions
        {
            public const string Read = "Permissions.Tenants.Read";
            public const string Create = "Permissions.Tenants.Create";
            public const string Update = "Permissions.Tenants.Update";
            public const string Delete = "Permissions.Tenants.Delete";
            public const string Manage = "Permissions.Tenants.Manage";
        }

        //### ---

        public static List<string> GetAllPermissions()
        {
            var allPermissions = new List<string>();
            var nestedTypes = typeof(SystemPermissions).GetNestedTypes();

            foreach (var type in nestedTypes)
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
                allPermissions.AddRange(fields.Select(fi => fi.GetValue(null)?.ToString() ?? string.Empty));
            }

            return allPermissions.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        }
    }
}
