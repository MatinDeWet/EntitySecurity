namespace EntitySecurity.Domain.Enums
{
    [Flags]
    public enum EntitySecurityRoleEnum
    {
        None = 0,
        Admin = 1,
        Developer = Admin | 2
    }
}
