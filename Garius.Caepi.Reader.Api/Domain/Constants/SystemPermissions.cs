namespace Garius.Caepi.Reader.Api.Domain.Constants
{
    public class SystemPermissions
    {
        //### TENANTS
        public class TenantPermissions
        {
            public string Value { get; }
            private TenantPermissions(string value) => Value = value;

            public static implicit operator string(TenantPermissions role) => role.Value;

            public static readonly TenantPermissions Read = new("Permissions.Tenants.Read");
            public static readonly TenantPermissions Create = new("Permissions.Tenants.Create");
            public static readonly TenantPermissions Update = new("Permissions.Tenants.Update");
            public static readonly TenantPermissions Delete = new("Permissions.Tenants.Delete");
            public static readonly TenantPermissions Manage = new("Permissions.Tenants.Manage");

            public override string ToString() => Value;
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
