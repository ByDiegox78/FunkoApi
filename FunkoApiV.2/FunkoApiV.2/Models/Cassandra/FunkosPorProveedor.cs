using Cassandra.Mapping.Attributes;

namespace FunkoApiV._2.Models.Cassandra;

[Table("funkos_por_proveedor")]
public class FunkosPorProveedor {
    [Column("proveedor_id")]
    [PartitionKey]
    public string ProveedorId { get; set; }
    
    [Column("funkos_id")]
    [ClusteringKey]
    public long FunkosId { get; set; }
    
    [Column("nombre")]
    public string Nombre { get; set; }
    
    [Column("stock")]
    public int Stock { get; set; }
    [Column("precio")]
    public decimal Precio { get; set; }
    
    [Column("proveedor_nombre")]
    public string ProveedorNombre { get; set; }
}