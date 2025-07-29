namespace SaleManagementRewrite.Entities.Enum;

[Flags] 
public enum UserRole
{
    Customer = 1,
    Seller = 2,
    Admin = 4, //de dung thuat toan or bit (customer + seller) = admin
}