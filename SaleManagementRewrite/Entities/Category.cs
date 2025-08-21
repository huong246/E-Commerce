using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SaleManagementRewrite.Entities;
[Table("Category")]
public class Category
{
    public Guid Id { get; set; }
    [MaxLength(1000)]
    public string Name { get; set; } = string.Empty;
    [JsonIgnore]
    public virtual ICollection<Item>  Items { get; set; }  = new HashSet<Item>();
}