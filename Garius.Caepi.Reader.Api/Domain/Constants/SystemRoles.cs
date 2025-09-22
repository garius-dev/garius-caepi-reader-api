namespace Garius.Caepi.Reader.Api.Domain.Constants
{
    
    public class SystemRoles
    {
        public string Value { get; }
        public bool IsSuperUser => SuperUserRoles.Contains(this);

        private SystemRoles(string value) => Value = value;

        public static implicit operator string(SystemRoles role) => role.Value;

        public static readonly SystemRoles Developer = new("Developer");
        public static readonly SystemRoles SuperAdmin = new("SuperAdmin");
        public static readonly SystemRoles Owner = new("Owner");
        public static readonly SystemRoles Admin = new("Admin");
        public static readonly SystemRoles User = new("User");


        //Pega os usuários com mais privilégios
        public static readonly IReadOnlyList<SystemRoles> SuperUserRoles = new List<SystemRoles> 
        { 
            Owner, 
            SuperAdmin, 
            Developer 
        };

        public static IReadOnlyList<SystemRoles> All { get; } =
            typeof(SystemRoles)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(SystemRoles))
                .Select(f => (SystemRoles)f.GetValue(null)!)
                .ToList();
        public override string ToString() => Value;
    }

    
}
