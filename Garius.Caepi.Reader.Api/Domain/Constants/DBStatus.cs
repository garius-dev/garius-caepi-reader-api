using System.Reflection;
using static Garius.Caepi.Reader.Api.Domain.Constants.SystemPermissions;

namespace Garius.Caepi.Reader.Api.Domain.Constants
{
    public class DBStatus
    {
        public class TenantStatus
        {
            public string Value { get; }
            private TenantStatus(string value) => Value = value;

            public static implicit operator string(TenantStatus role) => role.Value;

            public static readonly TenantStatus Active = new("ACTIVE");
            public static readonly TenantStatus Inactive = new("INACTIVE");
            public static readonly TenantStatus Suspended = new("SUSPENDED");
            public static readonly TenantStatus Pending = new("PENDING");

            public override string ToString() => Value;

            private static readonly Dictionary<string, TenantStatus> _instances;

            static TenantStatus()
            {
                _instances = typeof(TenantStatus)
                    .GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(TenantStatus))
                    .Select(f => (TenantStatus)f.GetValue(null)!)
                    .ToDictionary(s => s.Value, StringComparer.OrdinalIgnoreCase);
            }

            public static TenantStatus FromValue(string value)
            {
                if (_instances.TryGetValue(value, out var status))
                    return status;

                throw new ArgumentException($"Invalid tenant status: {value}");
            }
        }
    }
}
