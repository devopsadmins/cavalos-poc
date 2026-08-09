using System.ComponentModel.DataAnnotations.Schema;

namespace CavalosPOC.Models;

public class CavaloRegInfo
{
    /// <summary>
    /// Resultado do DECODE: Retorna REG_REG da tabela T1 ou CAV_REG da tabela C1
    /// </summary>
    [Column("REG")]
    public string? Reg
    {
        get; set;
    }

    /// <summary>
    /// Nome do Cavalo
    /// </summary>
    [Column("CAV_NOM")]
    public string? CavNom
    {
        get; set;
    }

    /// <summary>
    /// Indicador de Asterisco
    /// </summary>
    [Column("CAV_ASTERISCO")]
    public string? CavAsterisco
    {
        get; set;
    }
}
